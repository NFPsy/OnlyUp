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
            // Time.deltaTime은 "바로 직전 프레임 이후 몇 초가 지났는지"를 나타낸다. 이걸 곱해두면
            // 프레임이 빠른 컴퓨터든 느린 컴퓨터든 항상 같은 실제 속도로 progress가 증가한다.
            progress += Time.deltaTime * speed;
            // Mathf.PingPong(값, 1)은 값을 0→1로 올렸다가 다시 1→0으로 내리기를 반복해서 돌려준다
            // (숫자가 튕기며 왕복하는 느낌이라 "핑퐁"). 그래서 t는 계속 0과 1 사이를 왔다갔다한다.
            float t = Mathf.PingPong(progress, 1f);
            // Vector3.Lerp(A, B, t)는 t=0이면 A 위치, t=1이면 B 위치, 그 사이면 두 위치 사이의 한 점을 반환한다.
            // t가 위에서 0~1을 왕복하므로 결과적으로 장애물이 A와 B 사이를 계속 왕복하게 된다.
            transform.position = Vector3.Lerp(pointA, pointB, t);
        }
    }
}
