using UnityEngine;
using UnityEngine.UI;

namespace OnlyUp
{
    /// <summary>
    /// 지금까지 낙사해서 리스폰된 횟수를 화면에 표시하는 UI.
    /// 실제 횟수는 RespawnController.fallCount가 갖고 있고, 이 스크립트는 매 프레임 그 값을 읽어서
    /// 텍스트만 갱신한다 (HeightUI와 동일한 패턴).
    /// </summary>
    public class FallCountUI : MonoBehaviour
    {
        public RespawnController respawn;
        public Text fallCountText;

        private void Update()
        {
            if (respawn == null || fallCountText == null) return;
            fallCountText.text = $"Falls: {respawn.fallCount}";
        }
    }
}
