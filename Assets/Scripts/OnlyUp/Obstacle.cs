using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 장애물 마커 컴포넌트. 이 컴포넌트가 붙은 오브젝트의 Collider에 플레이어가 부딪히면
    /// PlayerController(OnControllerColliderHit)가 이 값을 읽어 플레이어를 밀어낸다(넉백).
    /// 정지된 장애물이든 MovingObstacle로 움직이는 장애물이든 동일하게 동작하므로,
    /// 나중에 다른 종류의 장애물을 추가할 때도 이 컴포넌트 하나만 붙이면 된다.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        [Tooltip("플레이어가 수평으로 밀려나는 힘")]
        public float knockbackForce = 14f;

        [Tooltip("플레이어가 위로 튕겨 오르는 힘")]
        public float knockbackUpward = 6f;
    }
}
