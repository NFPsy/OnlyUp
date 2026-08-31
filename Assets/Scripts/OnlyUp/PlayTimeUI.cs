using UnityEngine;
using UnityEngine.UI;

namespace OnlyUp
{
    /// <summary>
    /// 플레이 시작부터 흐른 시간을 화면에 표시하는 UI (HeightUI와 동일한 패턴).
    /// Time.deltaTime을 누적하므로 ESC 일시정지(Time.timeScale=0) 중에는 자동으로 멈춘다.
    /// 낙사 후 리스폰돼도 초기화하지 않고 총 플레이 시간을 계속 누적하며,
    /// GAME CLEAR 패널이 뜨면 그 시점 기록으로 멈춘다.
    /// </summary>
    public class PlayTimeUI : MonoBehaviour
    {
        public Text timeText;
        public GameClearUI clearUI;

        private float elapsed;

        private void Update()
        {
            if (timeText == null) return;

            // GameClearUI의 패널(panel)이 켜져 있으면 이미 클리어한 상태라는 뜻이다.
            // activeSelf는 "이 오브젝트 자체가 켜져 있는지"를 알려주는 Unity 기본 속성이다.
            bool cleared = clearUI != null && clearUI.panel != null && clearUI.panel.activeSelf;
            if (!cleared)
            {
                // Time.deltaTime은 "직전 프레임 이후 몇 초가 지났는지"이므로, 매 프레임 계속
                // 더해나가면 elapsed에 실제로 흐른 시간(초)이 누적된다. 클리어한 뒤에는 더하지
                // 않으므로 그 시점 기록에서 시간이 멈춘 것처럼 보인다.
                elapsed += Time.deltaTime;
            }

            // elapsed는 소수점이 있는 초 단위(예: 75.34초)라서, 정수 초로 자르고(FloorToInt)
            // 분(/60, 나눗셈의 몫)과 초(%60, 나눗셈의 나머지)로 나눠 "분:초" 형태로 만든다.
            int totalSeconds = Mathf.FloorToInt(elapsed);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            // {minutes:00}은 숫자가 한 자리여도 앞에 0을 채워 두 자리로 보여준다 (예: 5 -> "05").
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }
}
