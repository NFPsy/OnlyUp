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

        // 매 프레임 respawn.fallCount(RespawnController가 실제로 세고 있는 값)를 그대로 읽어와
        // 화면 텍스트만 갱신한다. 숫자를 세는 로직은 여기 없고, 표시만 이 스크립트가 담당한다.
        private void Update()
        {
            if (respawn == null || fallCountText == null) return;
            fallCountText.text = $"Falls: {respawn.fallCount}";
        }
    }
}
