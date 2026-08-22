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

            bool cleared = clearUI != null && clearUI.panel != null && clearUI.panel.activeSelf;
            if (!cleared)
            {
                elapsed += Time.deltaTime;
            }

            int totalSeconds = Mathf.FloorToInt(elapsed);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }
}
