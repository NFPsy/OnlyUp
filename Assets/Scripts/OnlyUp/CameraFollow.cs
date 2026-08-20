using UnityEngine;
using UnityEngine.InputSystem;

namespace OnlyUp
{
    /// <summary>
    /// 3인칭 궤도(orbit) 카메라. 플레이어 위치를 중심점으로 삼아 항상 일정 거리에서 따라가되,
    /// 카메라 각도(좌우/상하)는 오직 마우스 움직임으로만 바뀐다.
    /// PlayerController가 이동 방향으로 캐릭터를 회전시키는 것과는 완전히 분리되어 있어서,
    /// WASD로 이동하거나 캐릭터가 회전해도 카메라 각도 자체는 전혀 바뀌지 않는다.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("참조")]
        public Transform target; // 플레이어

        [Header("궤도 카메라 설정 (마우스로만 회전)")]
        public float distance = 6.5f;     // 중심점으로부터 카메라가 떨어지는 거리
        public Vector3 pivotOffset = new Vector3(0f, 1.2f, 0f); // 플레이어 기준 카메라가 도는 중심점(머리 위쪽)
        public float mouseSensitivity = 0.15f; // 마우스 1픽셀당 회전 각도
        public float minPitch = -20f;   // 아래로 내려다볼 수 있는 최대 각도
        public float maxPitch = 80f;    // 위로 올려다볼 수 있는 최대 각도
        public float positionSmoothTime = 0.08f;

        private float yaw = 0f;
        private float pitch = 15f; // 씬에 배치했던 초기 카메라 각도(15도)와 맞춤
        private Vector3 velocity; // SmoothDamp 내부 상태

        private void Update()
        {
            // 마우스 왼쪽 버튼을 클릭하면 커서를 화면에 가두고 숨겨서(포인터 락) 마우스로만
            // 시점을 조작할 수 있게 한다. WebGL은 브라우저 보안 정책상 사용자 클릭이 있어야
            // 포인터 락이 걸리므로, 시작하자마자 자동으로 잠그지 않고 클릭을 기다린다.
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // 마우스 이동량만큼 좌우/상하 회전값을 갱신한다. WASD 이동은 절대 이 값을 건드리지 않는다.
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * mouseSensitivity;
                pitch -= delta.y * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // 1. 마우스로만 결정되는 회전값으로 카메라가 플레이어 주위를 도는 위치를 계산
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            Vector3 desiredPosition = pivot - orbitRotation * Vector3.forward * distance;

            // 2. 위치는 SmoothDamp로 살짝만 보간 (플레이어가 갑자기 움직여도 카메라가 순간이동하지 않게)
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, positionSmoothTime);

            // 3. 회전은 마우스 값 그대로 적용 (지연 없이 즉각 반응해야 조작감이 좋다)
            transform.rotation = orbitRotation;
        }
    }
}
