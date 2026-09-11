using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 밟으면 미끄러지는 얼음 발판. 이 발판 위에 서 있는 동안에는 플레이어의 수평 이동에 관성이 생겨서,
    /// 키를 놓아도 곧바로 멈추지 않고 잠시 더 미끄러져 나간다(평소에는 관성이 전혀 없어서 즉시 멈춘다).
    ///
    /// 무너지는 발판(<see cref="CrumblingPlatform"/>)과 같은 방식으로, 발판 윗면 바로 위 공간에 붙인
    /// 트리거 콜라이더로 "지금 이 발판을 밟고 있는지"를 감지한다. 밟힘 감지 트리거를 만들고 이 컴포넌트를
    /// 붙이는 일은 <see cref="CourseKit.MakePlatformIce"/>가 담당한다.
    ///
    /// <see cref="Checkpoint"/>와 마찬가지로 플레이어를 미리 연결해둘 필요가 없다 — 발판은 플레이어보다
    /// 먼저 만들어지므로(BuildCourse가 CreatePlayer보다 먼저 실행됨) 생성 시점엔 참조할 플레이어가 없고,
    /// 트리거에 실제로 들어온 Collider에서 <see cref="PlayerController"/>를 바로 찾아 쓰면 된다.
    /// </summary>
    public class IcePlatform : MonoBehaviour
    {
        [Tooltip("이 발판 위에서의 감속도(초당 줄어드는 속도). 작을수록 더 미끄럽다. " +
            "CourseKit.MakePlatformIce가 발판 크기에 맞춰 '너무 미끄럽지 않도록' 하한을 적용해 넣어준다")]
        public float slipDeceleration = 20f;

        // OnTriggerEnter/Exit은 Unity가 자동으로 호출해주는 함수로, 이 오브젝트의 트리거 콜라이더 안으로
        // 다른 콜라이더가 들어오는/나가는 순간 각각 한 번 호출된다 (Checkpoint.cs, CrumblingPlatform.cs와 같은 패턴).
        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = FindPlayer(other);
            if (player == null) return;
            player.EnterSlipperySurface(this, slipDeceleration);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController player = FindPlayer(other);
            if (player == null) return;
            // "지금 미끄러지게 만든 발판이 나인 경우에만" 해제된다(PlayerController가 확인).
            // 얼음 발판에서 바로 옆 얼음 발판으로 건너뛰면 Unity가 "새 발판 진입 -> 이전 발판 이탈" 순서로
            // 호출할 수 있는데, 그때 이전 발판의 이탈 처리가 새 발판의 미끄러짐까지 꺼버리면 안 되기 때문이다.
            player.ExitSlipperySurface(this);
        }

        private static PlayerController FindPlayer(Collider other)
        {
            if (!other.CompareTag("Player")) return null;
            return other.GetComponentInParent<PlayerController>();
        }
    }
}
