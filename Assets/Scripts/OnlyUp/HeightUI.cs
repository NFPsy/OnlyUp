using UnityEngine;
using UnityEngine.UI;

namespace OnlyUp
{
    /// <summary>
    /// 현재 플레이어 높이를 화면에 표시하는 UI.
    /// 프로젝트에 TextMeshPro Essentials가 임포트되어 있지 않으므로
    /// 기본 UGUI의 Text 컴포넌트를 사용한다 (TMP 임포트 시 Text -> TMP_Text로 교체 가능).
    /// </summary>
    public class HeightUI : MonoBehaviour
    {
        public Transform player;
        public Text heightText;

        [Tooltip("높이 계산 기준(시작 지점) y좌표")]
        public float startHeight;

        // Update()는 매 프레임 자동으로 호출되므로, 여기서 매번 텍스트를 다시 계산해 넣으면
        // 화면의 숫자가 플레이어가 움직이는 대로 실시간으로 갱신되는 것처럼 보인다.
        private void Update()
        {
            if (player == null || heightText == null) return;

            // 현재 플레이어의 y좌표(높이)에서 시작 지점의 높이를 빼면 "시작점 대비 얼마나 올라왔는지"가 나온다.
            float height = player.position.y - startHeight;
            // $"..."는 문자열 보간(interpolation) 문법으로, {height:F1}은 height 값을 소수점 첫째 자리까지만 표시한다.
            heightText.text = $"Height: {height:F1} m";
        }
    }
}
