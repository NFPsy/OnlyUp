using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 플레이어가 맵 아래로 떨어졌을 때 시작 위치로 되돌리는 스크립트.
    /// 발판이 늘어나면(체크포인트 등) spawnPosition을 갱신하는 방식으로 확장 가능하다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class RespawnController : MonoBehaviour
    {
        [Header("리스폰 설정")]
        public Vector3 spawnPosition;   // 현재 리스폰될 위치 (체크포인트 도입 시 여기만 갱신하면 됨)
        public float fallLimitY = -20f; // 이 높이보다 아래로 떨어지면 리스폰

        [Tooltip("지금까지 낙사해서 리스폰된 횟수 (FallCountUI가 화면에 표시)")]
        public int fallCount = 0;

        private CharacterController controller;
        private PlayerController playerController;

        // Awake()는 Unity가 오브젝트가 생성되자마자(Start()보다도 먼저) 한 번 호출해주는 함수다.
        // 다른 컴포넌트를 참조로 미리 받아두는(캐싱) 작업은 보통 여기서 한다.
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            // 매 프레임 현재 높이를 확인해서 낙사 여부를 판정한다
            if (transform.position.y < fallLimitY)
            {
                Respawn();
            }
        }

        /// <summary>
        /// 체크포인트 시스템을 나중에 추가할 때 이 메서드로 리스폰 지점을 갱신하면 된다.
        /// </summary>
        public void SetSpawnPoint(Vector3 newSpawnPosition)
        {
            spawnPosition = newSpawnPosition;
        }

        public void Respawn()
        {
            fallCount++;

            // CharacterController는 활성화된 상태에서 transform.position을 직접 바꾸면
            // 내부 충돌 처리와 충돌할 수 있으므로, 잠시 비활성화한 뒤 위치를 옮기고 다시 켠다.
            controller.enabled = false;
            transform.position = spawnPosition;
            controller.enabled = true;

            // 낙하 속도를 초기화해 리스폰 직후 바로 다시 떨어지는 것을 방지
            if (playerController != null)
            {
                playerController.ResetVerticalVelocity();
            }
        }
    }
}
