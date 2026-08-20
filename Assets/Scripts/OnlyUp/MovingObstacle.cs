using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 장애물을 두 지점(pointA, pointB) 사이에서 왕복시킨다.
    /// 이 스크립트는 Obstacle이 붙은 로직 오브젝트(콜라이더가 있는 부모) 자체를 움직이므로,
    /// 콜라이더도 함께 이동해 CharacterController와 정확하게 충돌 판정된다.
    /// 나중에 "떨어지는 발판" 등 다른 움직임 패턴을 추가할 때도 같은 방식(부모 오브젝트를 직접
    /// 움직이는 방식)으로 확장하면 된다.
    /// </summary>
    public class MovingObstacle : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public float speed = 0.6f; // 왕복 속도 (클수록 빠르게 왕복)

        private float progress; // PingPong에 누적되는 시간 값

        private void Update()
        {
            progress += Time.deltaTime * speed;
            float t = Mathf.PingPong(progress, 1f);
            transform.position = Vector3.Lerp(pointA, pointB, t);
        }
    }
}
