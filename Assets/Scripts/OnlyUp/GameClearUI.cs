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

        // SetActive(true/false)는 Unity의 기본 함수로, 오브젝트를 화면에 보이게/안 보이게(그리고
        // 동작하게/멈추게) 켜고 끈다. 여기서는 평소 숨겨둔 패널을 클리어 시점에 보여주는 데 쓴다.
        public void Show()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        // Show()와 반대로, 패널을 다시 숨긴다. 지금은 아무도 호출하지 않지만
        // (한 번 클리어하면 게임이 끝이라 다시 숨길 일이 없음) 나중에 "다시하기" 기능을
        // 넣을 때를 대비해 Show()와 짝을 맞춰 만들어 둔 함수다.
        public void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
