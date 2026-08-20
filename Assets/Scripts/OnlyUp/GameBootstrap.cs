using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 1스테이지: 좁은 외길 발판 코스. 씬 실행 시점에 플레이어/발판 코스/Goal/UI를 전부 코드로 생성한다.
    /// 씬에는 이 스크립트가 붙은 빈 오브젝트와 Camera, Light만 있으면 된다.
    /// 실제 생성 로직(플레이어/카메라/UI/발판/장애물)은 CourseKit에 공통으로 모아뒀고,
    /// 이 스크립트는 "코스를 어떻게 배치할지"만 담당한다.
    ///
    /// Goal에 도달하면 GAME CLEAR 대신 다음 스테이지 씬(nextStageSceneName)으로 넘어간다.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("코스 생성 설정")]
        public int platformCount = 20;
        public Vector3 platformSize = new Vector3(2.2f, 1f, 2.2f);
        public Vector3 startPlatformSize = new Vector3(6f, 1f, 6f);
        public Vector3 goalPlatformSize = new Vector3(5f, 1f, 5f);
        public float verticalStep = 3.4f;
        public float horizontalVariance = 4.2f;
        public float fallDistance = 18f;

        [Header("장애물 설정")]
        public int obstacleEveryNPlatforms = 3;
        public int movingObstacleEveryNth = 2;

        [Header("스테이지 전환")]
        [Tooltip("이 코스를 클리어하면 넘어갈 다음 씬 이름 (Build Settings에 등록되어 있어야 함)")]
        public string nextStageSceneName = "OnlyUpMountain";

        private Vector3 startPosition = Vector3.zero;
        private Transform courseParent;

        private void Awake()
        {
            // 씬에 "Course"가 이미 있으면(에디터에서 미리 생성/편집해둔 코스) 그대로 쓰고,
            // 없으면 지금 새로 만든다. 즉, 에디터에서 한 번 생성해두면 그 뒤로는 Play해도 다시 생성되지 않고
            // 직접 옮기거나 지운 발판이 그대로 유지된다.
            courseParent = CourseKit.FindOrCreateCourseParent();
            Vector3 goalPosition = startPosition;
            if (courseParent.childCount == 0)
            {
                goalPosition = BuildCourse();
            }
            else
            {
                Goal existingGoal = courseParent.GetComponentInChildren<Goal>();
                if (existingGoal != null)
                {
                    goalPosition = existingGoal.transform.position;
                }
            }

            // 플레이어/카메라/UI는 항상 Play할 때마다 새로 생성한다 (이 오브젝트들은 편집 대상이 아님)
            GameObject player = CourseKit.CreatePlayer(
                startPosition + new Vector3(0f, startPlatformSize.y * 0.5f + 1.05f, 0f),
                startPosition.y - fallDistance);

            CourseKit.SetupCameraFollow(player.transform);

            GameClearUI clearUI = CourseKit.CreateUI(player.transform, startPosition.y);

            CourseKit.ConfigureGoal(courseParent, goalPosition, goalPlatformSize, player.GetComponent<PlayerController>(), clearUI, nextStageSceneName);
        }

        /// <summary>
        /// 인스펙터의 컴포넌트 컨텍스트 메뉴(⋮)에서 실행: Play 없이 에디터에서 바로 코스를 생성해
        /// 씬에 실제 오브젝트로 배치한다. 이후 Hierarchy/Scene 뷰에서 발판을 직접 선택·이동·삭제할 수 있고,
        /// 씬을 저장하면 그 편집 내용이 그대로 유지된다 (Play할 때 다시 생성되지 않음).
        /// 기존에 생성된 Course가 있으면 지우고 새로 만든다.
        /// </summary>
        [ContextMenu("코스 생성/재생성 (에디터)")]
        public void GenerateCourseInEditor()
        {
            GameObject existing = GameObject.Find("Course");
            if (existing != null)
            {
                DestroyImmediate(existing);
            }

            courseParent = new GameObject("Course").transform;
            Vector3 goalPosition = BuildCourse();
            CourseKit.CreatePlatform(courseParent, "Goal", goalPosition, goalPlatformSize, isGoal: true);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        /// <summary>
        /// 시작 발판부터 Goal 직전까지의 발판 코스를 만들고, Goal이 놓일 위치를 반환한다.
        /// 발판 간 이동 거리를 매번 일정 범위로 제한해 점프로 항상 도달 가능하게 만든다.
        /// </summary>
        private Vector3 BuildCourse()
        {
            CourseKit.CreatePlatform(courseParent, "StartPlatform", startPosition, startPlatformSize, isGoal: false);

            Vector3 prev = startPosition;
            int obstacleIndex = 0;
            for (int i = 1; i <= platformCount; i++)
            {
                float dx = Mathf.Sin(i * 1.7f) * horizontalVariance;
                float dz = Mathf.Cos(i * 1.1f) * horizontalVariance;
                Vector3 pos = prev + new Vector3(dx, verticalStep, dz);

                bool isLast = i == platformCount;
                if (!isLast)
                {
                    CourseKit.CreatePlatform(courseParent, $"Platform_{i:00}", pos, platformSize, isGoal: false);

                    if (i % obstacleEveryNPlatforms == 0)
                    {
                        obstacleIndex++;
                        if (obstacleIndex % movingObstacleEveryNth == 0)
                        {
                            float travel = platformSize.x * 0.8f;
                            Vector3 basePos = pos + new Vector3(0f, platformSize.y * 0.5f + 0.5f, 0f);
                            CourseKit.CreateMovingObstacle(
                                courseParent,
                                basePos + new Vector3(-travel * 0.5f, 0f, 0f),
                                basePos + new Vector3(travel * 0.5f, 0f, 0f));
                        }
                        else
                        {
                            CourseKit.CreateStaticObstacle(courseParent, pos, platformSize);
                        }
                    }
                }

                prev = pos;
            }

            return prev; // 마지막 위치 = Goal 위치
        }
    }
}
