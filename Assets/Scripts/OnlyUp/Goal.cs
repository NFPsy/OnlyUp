using UnityEngine;
using UnityEngine.SceneManagement;

namespace OnlyUp
{
    /// <summary>
    /// 골 지점에 붙는 스크립트. 트리거 콜라이더에 플레이어가 들어오면
    /// - nextSceneName이 비어있지 않으면: 다음 스테이지 씬으로 곧바로 전환한다 (예: OnlyUp -> OnlyUpMountain)
    /// - 비어있으면(최종 스테이지): 게임 클리어 UI를 띄우고 플레이어 조작을 막는다
    /// 나중에 체크포인트/장애물 등도 이런 트리거 기반 스크립트로 확장하면 된다.
    /// </summary>
    public class Goal : MonoBehaviour
    {
        [Tooltip("도달 시 조작을 멈출 플레이어 컨트롤러 (Bootstrap이 할당, 최종 스테이지에서만 사용)")]
        public PlayerController player;

        [Tooltip("도달 시 표시할 게임 클리어 UI (Bootstrap이 할당, 최종 스테이지에서만 사용)")]
        public GameClearUI clearUI;

        [Tooltip("비어있지 않으면 클리어 UI 대신 이 이름의 씬으로 즉시 전환한다 (Build Settings에 등록되어 있어야 함)")]
        public string nextSceneName;

        private bool cleared = false;

        private void OnTriggerEnter(Collider other)
        {
            if (cleared) return;

            // "Player" 태그를 가진 오브젝트(CharacterController)가 들어왔는지만 확인
            if (!other.CompareTag("Player")) return;

            cleared = true;

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                // 다음 스테이지로 곧바로 넘어간다 (예: 좁은 코스 -> 넓은 산 코스)
                SceneManager.LoadScene(nextSceneName);
                return;
            }

            // 조작 스크립트를 꺼서 더 이상 입력을 받지 않게 한다
            if (player != null)
            {
                player.enabled = false;
            }

            if (clearUI != null)
            {
                clearUI.Show();
            }
        }
    }
}
