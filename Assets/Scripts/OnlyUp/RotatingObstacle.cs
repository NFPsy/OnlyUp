using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 장애물을 지정한 중심점(center) 주위로 원을 그리며 계속 돌린다.
    /// 왕복(MovingObstacle)과 달리 멈추는 순간이 없어서, 발판에 올라선 뒤에도 계속 피해야 한다.
    ///
    /// 반지름(radius)은 발판 반폭보다 작게 잡아서 원 안쪽(중심부)에는 항상 캐릭터가 설 수 있는
    /// 안전지대가 남도록 호출부(CourseKit/GameBootstrap)에서 계산해 넘긴다 — 그래야 "타이밍을 맞추면
    /// 반드시 통과 가능한" 장애물이 되고, 발판 전체를 쓸어버려서 착지 자체가 불가능해지지 않는다.
    ///
    /// MovingObstacle과 마찬가지로 Obstacle이 붙은 로직 오브젝트(콜라이더가 있는 부모) 자체를
    /// 움직이므로 콜라이더도 함께 이동해 CharacterController와 정확히 충돌 판정된다.
    /// </summary>
    public class RotatingObstacle : MonoBehaviour
    {
        [Tooltip("공전 중심 (보통 발판 중심의 살짝 위쪽)")]
        public Vector3 center;

        [Tooltip("공전 반지름. 발판 반폭보다 작아야 중심부에 안전지대가 남는다")]
        public float radius = 1f;

        [Tooltip("초당 회전 각도(도). 클수록 빨리 돌아 피하기 어렵다")]
        public float angularSpeed = 90f;

        [Tooltip("시작 각도(도). 장애물마다 다르게 주면 전부 같은 위상으로 돌지 않는다")]
        public float startAngleDeg;

        private float angle;

        private void Start()
        {
            angle = startAngleDeg;
            Apply();
        }

        private void Update()
        {
            // Time.deltaTime을 곱해 프레임 속도와 무관하게 항상 같은 실제 속도로 돌게 한다.
            // 일시정지(Time.timeScale = 0) 중에는 deltaTime이 0이라 자연스럽게 멈춘다.
            angle += angularSpeed * Time.deltaTime;
            Apply();
        }

        // 각도(angle)를 실제 좌표로 바꿔서 장애물을 원 위의 그 지점으로 옮긴다.
        // 수학에서 원 위의 점은 (중심 + cos(각도)*반지름, 중심 + sin(각도)*반지름)로 구할 수 있다
        // (시계 12시 방향을 각도 0으로 보고, 각도가 커질수록 시계 반대 방향으로 도는 원리).
        // Mathf.Deg2Rad는 "도(360도 기준)"를 삼각함수가 요구하는 "라디안" 단위로 바꿔주는 상수다.
        private void Apply()
        {
            float rad = angle * Mathf.Deg2Rad;
            transform.position = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
        }
    }
}
