using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 플레이어의 현재 높이에 따라 배경(카메라 단색 배경 + Fog) 색을 서서히 바꾼다.
    /// Only Up!처럼 "올라갈수록 지상의 하늘색에서 점점 더 깊고 어두운(우주에 가까운) 색으로
    /// 바뀌어 간다"는 분위기를 낸다. 스카이박스/텍스처 없이 색상 보간만 사용하므로
    /// WebGL에서도 항상 동일하게 가볍게 동작한다.
    /// </summary>
    public class HeightSkyController : MonoBehaviour
    {
        [Tooltip("높이를 기준으로 삼을 플레이어 Transform (GameBootstrap이 할당)")]
        public Transform player;

        [Tooltip("이 높이(시작 지점)에서는 groundColor")]
        public float startHeight;

        [Tooltip("이 높이(Goal)에서는 skyColor")]
        public float endHeight = 100f;

        public Color groundColor = new Color(0.74f, 0.83f, 0.97f); // 지상의 파스텔 하늘색
        public Color skyColor = new Color(0.06f, 0.05f, 0.18f);    // 정상 근처의 짙은 밤/우주색

        private Camera cachedCamera;

        private void Update()
        {
            if (player == null) return;
            if (cachedCamera == null)
            {
                cachedCamera = GetComponent<Camera>();
                if (cachedCamera == null) return;
            }

            float t = endHeight > startHeight
                ? Mathf.InverseLerp(startHeight, endHeight, player.position.y)
                : 0f;
            Color current = Color.Lerp(groundColor, skyColor, t);

            cachedCamera.backgroundColor = current;
            RenderSettings.fogColor = current;
        }
    }
}
