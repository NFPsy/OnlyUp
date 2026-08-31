using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OnlyUp
{
    /// <summary>
    /// P로 여닫는 일시정지 메뉴. "감도 설정"/"소리"/"게임 끝내기" 버튼이 있는 메인 바에서
    /// 감도 설정을 누르면 마우스 감도 슬라이더가, 소리를 누르면 배경음악/점프 사운드 켜기·끄기
    /// 서브 메뉴가 나타난다. P를 누를 때마다 열림/닫힘이 토글되고, 열려있는 동안은
    /// Time.timeScale=0으로 멈춘다.
    /// (원래 Esc였는데, WebGL 브라우저에서 Esc가 포인터 락 해제 등 브라우저 자체 동작과 겹쳐
    /// 게임 쪽 입력으로 안정적으로 전달되지 않는 경우가 있어 P로 바꿨다.)
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public GameObject pausePanel;
        public GameObject soundPanel;
        public GameObject sensitivityPanel;
        public AudioSource bgmSource;
        public PlayerController playerController;
        public CameraFollow cameraFollow;
        public Text bgmToggleText;
        public Text jumpToggleText;
        public Text sensitivityValueText;

        /// <summary>
        /// 다른 스크립트(CameraFollow)가 "지금 일시정지 메뉴가 열려있는지"를 확인할 수 있게 하는 값.
        /// 열려있는 동안 카메라 궤도 조작이 마우스 클릭/이동을 그대로 가져가면, 메뉴 버튼을 눌러도
        /// 클릭이 커서 잠금 쪽으로 먼저 소비되어(재잠금+숨김) 버튼에 반응이 없는 것처럼 보인다.
        /// </summary>
        public static bool IsPaused { get; private set; }

        private bool isPaused;
        private bool bgmEnabled = true;
        private bool jumpSfxEnabled = true;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
            {
                SetPaused(!isPaused);
            }
        }

        private void SetPaused(bool paused)
        {
            isPaused = paused;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (pausePanel != null) pausePanel.SetActive(paused);
            if (!paused)
            {
                // 다음에 열 때는 항상 메인 메뉴부터 보이게, 서브 메뉴는 전부 닫아둔 채로 초기화한다.
                if (soundPanel != null) soundPanel.SetActive(false);
                if (sensitivityPanel != null) sensitivityPanel.SetActive(false);
            }

            // 일시정지 중에는 항상 커서를 보이게 해서 메뉴 버튼을 클릭할 수 있게 한다.
            // 재개 후 다시 시점을 조작하려면 CameraFollow의 기존 "클릭해서 잠금" 흐름을 그대로 따른다.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ToggleSoundPanel()
        {
            if (soundPanel == null) return;
            bool willOpen = !soundPanel.activeSelf;
            soundPanel.SetActive(willOpen);
            // 서브 메뉴 두 개가 동시에 겹쳐 보이지 않도록, 하나를 열면 다른 하나는 닫는다.
            if (willOpen && sensitivityPanel != null) sensitivityPanel.SetActive(false);
        }

        public void ToggleSensitivityPanel()
        {
            if (sensitivityPanel == null) return;
            bool willOpen = !sensitivityPanel.activeSelf;
            sensitivityPanel.SetActive(willOpen);
            if (willOpen && soundPanel != null) soundPanel.SetActive(false);
        }

        // Slider의 onValueChanged가 값이 바뀔 때마다(드래그 도중 매 프레임) 이 함수를 호출해준다.
        public void OnSensitivityChanged(float value)
        {
            if (cameraFollow != null) cameraFollow.mouseSensitivity = value;
            if (sensitivityValueText != null) sensitivityValueText.text = "마우스 감도: " + value.ToString("F2");
        }

        public void ToggleBgm()
        {
            bgmEnabled = !bgmEnabled;
            if (bgmSource != null) bgmSource.mute = !bgmEnabled;
            if (bgmToggleText != null) bgmToggleText.text = "배경음악: " + (bgmEnabled ? "켜짐" : "꺼짐");
        }

        public void ToggleJumpSfx()
        {
            jumpSfxEnabled = !jumpSfxEnabled;
            if (playerController != null && playerController.jumpAudioSource != null)
            {
                playerController.jumpAudioSource.mute = !jumpSfxEnabled;
            }
            if (jumpToggleText != null) jumpToggleText.text = "점프 사운드: " + (jumpSfxEnabled ? "켜짐" : "꺼짐");
        }

        public void QuitGame()
        {
            // Time.timeScale이 0인 채로 에디터 Play 모드를 멈추면 다음 실행에 영향을 줄 수 있으니 되돌려둔다.
            Time.timeScale = 1f;
            // #if / #else / #endif는 "코드를 실행하기 전에" 둘 중 한쪽만 골라 포함시키는 전처리기 지시문이다.
            // UNITY_EDITOR는 유니티 에디터 안에서 Play 버튼으로 실행 중일 때만 자동으로 정의되는 값이라,
            // 에디터에서는 위쪽(Play 모드 종료)이, 실제로 빌드된 게임(itch.io 웹 버전 등)에서는 아래쪽
            // (Application.Quit, 게임 자체를 종료)이 실행된다. Application.Quit()은 에디터에서는
            // 아무 효과가 없어서(그냥 무시됨), 이렇게 나눠야 에디터에서 테스트할 때도 "종료" 버튼이 동작한다.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
