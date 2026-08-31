using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 세이브 포인트 역할을 하는 초록색 발판. 트리거 콜라이더에 플레이어가 들어오면
    /// 그 순간부터 낙사해도 시작 지점이 아니라 이 발판 위로 리스폰되도록 갱신한다.
    ///
    /// Goal.cs와 달리 이 컴포넌트는 어떤 플레이어 인스턴스도 미리 연결해둘 필요가 없다.
    /// 발판은 플레이어보다 먼저 만들어지므로(GameBootstrap.BuildCourse가 CreatePlayer보다 먼저 실행됨)
    /// 생성 시점에는 아직 플레이어가 존재하지 않는데, 트리거에 "들어온 그 Collider"에서
    /// 직접 RespawnController를 찾아 쓰면 미리 연결(와이어링)하지 않고도 항상 정확히 동작한다.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("이 체크포인트에서 리스폰될 위치 (보통 발판 바로 위, 살짝 떠 있는 높이)")]
        public Vector3 respawnPosition;

        // OnTriggerEnter는 Unity가 자동으로 호출해주는 함수로, 이 오브젝트의 트리거 콜라이더 안으로
        // 다른 콜라이더가 들어오는 순간 한 번 호출된다 (Goal.cs, CrumblingPlatform.cs와 같은 패턴).
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // GetComponentInParent: 부딪힌 콜라이더 자신이나 그 부모 오브젝트들 중에서
            // RespawnController를 찾는다. 플레이어의 CharacterController(콜라이더)와
            // RespawnController는 같은 오브젝트에 있으므로 사실상 바로 찾아진다.
            RespawnController respawn = other.GetComponentInParent<RespawnController>();
            if (respawn == null) return;

            // 지금까지의 리스폰 지점을 이 체크포인트로 덮어쓴다. 이후 낙사하면 시작 지점이 아니라
            // 여기서 다시 시작한다. 이미 이 체크포인트를 지나간 뒤 다시 밟아도(같은 값으로) 문제없다.
            respawn.SetSpawnPoint(respawnPosition);
        }
    }
}
