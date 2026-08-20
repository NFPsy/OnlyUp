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
        /// Visual의 로컬 스케일 = 콜라이더 크기, Visual의 로컬 위치 = 콜라이더 중심이 되도록 맞춘다.
        /// (CourseKit이 처음 발판을 만들 때도 항상 이 관계가 성립하도록 생성한다)
        /// 혹시 자동 동기화가 안 됐다면 인스펙터 ⋮ 메뉴의 "지금 동기화"로 수동 실행할 수 있다.
        /// </summary>
        [ContextMenu("지금 동기화")]
        public void Sync()
        {
            if (targetCollider == null || visual == null) return;
            targetCollider.size = visual.localScale;
            targetCollider.center = visual.localPosition;
        }
    }
}
