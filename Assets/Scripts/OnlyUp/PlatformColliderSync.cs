using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 부모(로직)의 BoxCollider 크기/중심을 Visual 자식의 실제 로컬 스케일/위치에 맞춰 에디터에서
    /// 자동으로 동기화한다.
    ///
    /// 이 프로젝트는 "충돌은 부모 Collider, 외형은 Visual 자식" 구조라서, 씬 뷰에서 발판을 클릭해
    /// 늘리거나 옮기면 실제로는 눈에 보이는 Visual(자식)이 선택되어 바뀌는 경우가 많다.
    /// 그러면 비주얼만 커지고/움직이고 콜라이더는 그대로 남아서, 늘어나거나 옮겨진 부분에
    /// 충돌이 없어 플레이어가 그대로 떨어지는 문제가 생긴다. 이 스크립트가 그 둘을 항상 맞춰준다.
    ///
    /// [ExecuteAlways]라서 Play 중이 아닌 에디터 편집 상태에서도 동작하며, Play 중에는(성능을 위해)
    /// 동작하지 않는다 — 어차피 게임 중에는 발판 크기가 바뀔 일이 없기 때문이다.
    /// </summary>
    [ExecuteAlways]
    public class PlatformColliderSync : MonoBehaviour
    {
        public BoxCollider targetCollider;
        public Transform visual;

        [Tooltip("true면 콜라이더 두께를 maxThickness로 제한하고 항상 Visual의 맨 윗면에 붙인다. " +
            "발판처럼 위쪽에 다음 발판까지의 점프 공간이 필요한 경우에만 켠다 (장애물 등 일반 충돌체는 끔).")]
        public bool capThickness = false;
        public float maxThickness = 0.3f;

        private void Update()
        {
            if (Application.isPlaying) return; // 게임 중에는 발판 크기가 안 바뀌므로 에디터 편집 때만 동작
            Sync();
        }

        // 인스펙터에서 숫자를 직접 입력했을 때(드래그가 아니라)도 확실히 반영되도록 한 번 더 동기화
        private void OnValidate()
        {
            Sync();
        }

        /// <summary>
        /// Visual의 "실제 렌더링된 월드 바운즈"를 기준으로 콜라이더 크기/중심을 맞춘다.
        /// Visual의 localScale 값 자체는 신뢰하지 않는다 — 프리미티브 큐브는 원본 크기가 1이라
        /// localScale이 곧 실제 크기와 같지만, 임포트된 3D 모델은 원본 메시 단위가 제각각이라
        /// (예: 어떤 모델은 스케일을 100배 곱해야 1유닛 크기가 됨) localScale만 보면 완전히 틀어진다.
        /// 그래서 항상 Renderer.bounds(실제 렌더 크기)를 부모 로컬 공간으로 환산해서 사용한다.
        /// 혹시 자동 동기화가 안 됐다면 인스펙터 ⋮ 메뉴의 "지금 동기화"로 수동 실행할 수 있다.
        /// </summary>
        [ContextMenu("지금 동기화")]
        public void Sync()
        {
            if (targetCollider == null || visual == null) return;

            Renderer renderer = visual.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                // 렌더러가 없는 특수한 경우를 대비한 대체 동작
                targetCollider.size = visual.localScale;
                targetCollider.center = visual.localPosition;
                return;
            }

            Vector3 worldSize = renderer.bounds.size;
            Vector3 worldCenter = renderer.bounds.center;
            Vector3 parentLossyScale = transform.lossyScale;

            float sizeY = worldSize.y;
            float centerY = worldCenter.y;
            if (capThickness && sizeY > maxThickness)
            {
                // 윗면(착지면) 위치는 그대로 두고, 아랫면만 끌어올려서 두께만 얇게 만든다.
                // 모델에 달린 장식(작은 토퍼 등)이 바운즈 높이를 부풀려도, 다음 발판까지의
                // 점프 공간(clearance)은 항상 확보되어 캐릭터 머리가 위쪽 발판에 끼는 문제를 막는다.
                float worldTop = worldCenter.y + worldSize.y * 0.5f;
                sizeY = maxThickness;
                centerY = worldTop - maxThickness * 0.5f;
            }

            targetCollider.size = new Vector3(
                parentLossyScale.x != 0f ? worldSize.x / parentLossyScale.x : worldSize.x,
                parentLossyScale.y != 0f ? sizeY / parentLossyScale.y : sizeY,
                parentLossyScale.z != 0f ? worldSize.z / parentLossyScale.z : worldSize.z);
            targetCollider.center = transform.InverseTransformPoint(new Vector3(worldCenter.x, centerY, worldCenter.z));
        }
    }
}
