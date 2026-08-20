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

        private void Update()
        {
            if (player == null || heightText == null) return;

            float height = player.position.y - startHeight;
            heightText.text = $"Height: {height:F1} m";
        }
    }
}
