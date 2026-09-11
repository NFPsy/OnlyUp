#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OnlyUp
{
    /// <summary>
    /// 에디터에서 플레이 모드로 테스트할 때만 켜지는 개발용 이동 도구. 코스를 처음부터 다시 밟지
    /// 않고도 원하는 지점으로 바로 이동해서 특정 구간만 반복 테스트할 수 있게 해준다.
    /// 파일 전체가 UNITY_EDITOR로 감싸져 있어 실제 빌드(WebGL 등)에는 포함되지 않는다.
    ///
    /// 사용법: PageUp/PageDown 키로 발판을 하나씩 앞뒤로 이동, 숫자키 1~5로 각 구간(Zone)의
    /// 첫 발판으로 바로 이동한다. 이동한 자리로 리스폰 지점도 함께 옮겨서, 그 자리에서 떨어져도
    /// 계속 같은 구간에서 테스트할 수 있다.
    /// </summary>
    public class DebugCourseTeleport : MonoBehaviour
    {
        private CharacterController controller;
        private PlayerController playerController;
        private RespawnController respawnController;

        private Transform[] orderedPlatforms; // globalIndex 순서로 정렬된 발판들 (StartPlatform=0, Goal=마지막)
        private int[] zoneStartGlobalIndex;
        private int currentIndex;

        public void Setup(Transform courseParent, int[] zoneStartIndices)
        {
            zoneStartGlobalIndex = zoneStartIndices;
            controller = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();
            respawnController = GetComponent<RespawnController>();
            BuildOrderedPlatformList(courseParent);
        }

        private void BuildOrderedPlatformList(Transform courseParent)
        {
            var list = new List<Transform>();
            Transform goal = null;
            for (int i = 0; i < courseParent.childCount; i++)
            {
                Transform child = courseParent.GetChild(i);
                if (child.name == "Goal") { goal = child; continue; } // Goal은 정렬 후 맨 끝에 붙인다
                if (!IsPlatform(child.name)) continue; // 장애물(Obstacle_*) 등 발판이 아닌 자식은 제외
                list.Add(child);
            }
            list.Sort((a, b) => ExtractGlobalIndex(a.name).CompareTo(ExtractGlobalIndex(b.name)));
            if (goal != null) list.Add(goal);
            orderedPlatforms = list.ToArray();
        }

        private static bool IsPlatform(string objectName)
        {
            return objectName == "StartPlatform"
                || objectName.StartsWith("Platform_")
                || objectName.StartsWith("Checkpoint_")
                || objectName.StartsWith("IcePlatform_");
        }

        private static int ExtractGlobalIndex(string platformName)
        {
            if (platformName == "StartPlatform") return 0;
            Match match = Regex.Match(platformName, @"_(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || orderedPlatforms == null || orderedPlatforms.Length == 0) return;

            if (keyboard[Key.PageUp].wasPressedThisFrame) MoveByStep(1);
            else if (keyboard[Key.PageDown].wasPressedThisFrame) MoveByStep(-1);

            if (zoneStartGlobalIndex == null) return;
            for (int z = 0; z < zoneStartGlobalIndex.Length && z < 9; z++)
            {
                Key numberKey = (Key)((int)Key.Digit1 + z);
                if (keyboard[numberKey].wasPressedThisFrame)
                {
                    JumpToGlobalIndex(zoneStartGlobalIndex[z]);
                }
            }
        }

        private void MoveByStep(int direction)
        {
            currentIndex = Mathf.Clamp(currentIndex + direction, 0, orderedPlatforms.Length - 1);
            TeleportTo(orderedPlatforms[currentIndex]);
        }

        private void JumpToGlobalIndex(int globalIndex)
        {
            currentIndex = orderedPlatforms.Length - 1;
            for (int i = 0; i < orderedPlatforms.Length; i++)
            {
                if (ExtractGlobalIndex(orderedPlatforms[i].name) >= globalIndex)
                {
                    currentIndex = i;
                    break;
                }
            }
            TeleportTo(orderedPlatforms[currentIndex]);
        }

        private void TeleportTo(Transform platform)
        {
            Vector3 targetPos = platform.position + new Vector3(0f, 2.5f, 0f);

            bool wasEnabled = controller == null || controller.enabled;
            if (controller != null) controller.enabled = false;
            transform.position = targetPos;
            if (controller != null) controller.enabled = wasEnabled;

            if (playerController != null) playerController.ResetVerticalVelocity();

            // 테스트 목적이므로 "더 높을 때만 갱신"이라는 RespawnController의 일반 규칙을 거치지 않고
            // 리스폰 지점을 이동한 자리로 직접 지정한다 (그래야 떨어져도 같은 구간에서 계속 테스트된다).
            if (respawnController != null) respawnController.spawnPosition = targetPos;

            Debug.Log($"[DebugCourseTeleport] {platform.name}(으)로 이동");
        }
    }
}
#endif
