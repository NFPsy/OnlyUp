using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// "GAME CLEAR" 패널을 보여주는 역할만 담당하는 UI 스크립트.
    /// Goal 스크립트가 목표 도달을 감지하면 이 스크립트의 Show()를 호출한다.
    /// </summary>
    public class GameClearUI : MonoBehaviour
    {
        [Tooltip("평소에는 비활성화되어 있다가, 클리어 시 활성화되는 패널")]
        public GameObject panel;

        public void Show()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
