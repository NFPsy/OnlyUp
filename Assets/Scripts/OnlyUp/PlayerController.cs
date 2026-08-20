using UnityEngine;
using UnityEngine.InputSystem;

namespace OnlyUp
{
    /// <summary>
    /// 플레이어 이동/점프를 담당하는 스크립트.
    /// 프로젝트의 Player Settings > Active Input Handling이 실제로는
    /// "Input System Package (New)"로 설정되어 있어(레거시 Input 클래스는 예외를 던짐),
    /// 이미 설치되어 있는 Input System 패키지의 Keyboard.current API로 직접 입력을 읽는다.
    /// (.inputactions 에셋 없이 저수준 API만 사용 — 새 패키지 추가 없음)
    /// CharacterController 기반으로 이동시켜 WebGL에서도 안정적으로 동작하게 한다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("이동 설정")]
        // 발판 간격이 더 넓어진 코스(GameBootstrap의 verticalStep=3.4, horizontalVariance=4.2)에
        // 맞춰, 대각선 최장 거리(약 5.9m)를 점프 궤적으로 여유 있게 커버할 수 있도록 소폭 상향
        public float moveSpeed = 7f;
        public float jumpHeight = 5f;
        public float gravity = -25f;
        public float rotateSpeed = 12f; // 이동 방향으로 몸을 돌리는 속도

        [Header("장애물 넉백 설정")]
        public float knockbackDrag = 18f; // 넉백 속도가 초당 이만큼씩 줄어들어 서서히 원래 조작으로 돌아온다

        [Header("참조")]
        [Tooltip("카메라 기준으로 이동 방향을 계산하기 위한 카메라 Transform (GameBootstrap이 할당)")]
        public Transform cameraTransform;

        private CharacterController controller;
        private Animator animator; // 실제 캐릭터 모델(Resources/PlayerModel)일 때만 존재, 더미 캡슐이면 null
        private Vector3 verticalVelocity; // y축 속도(중력/점프)만 별도로 관리
        private Vector3 knockbackVelocity; // 장애물에 부딪혔을 때의 수평 넉백 속도 (서서히 감쇠)

        // --- 애니메이션 전용 상태 (Animator가 없으면 전혀 쓰이지 않는다) ---
        private bool wasStationary = true;  // 직전 프레임에 거의 멈춰있었는지 (제자리 방향전환 감지용)
        private int jumpVariantCounter;     // 이동 중 점프 애니메이션 3종을 돌아가며 쓰기 위한 카운터

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            // Visual이 더미 캡슐이면 Animator가 없다 - null이면 애니메이션 갱신을 그냥 건너뛴다
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            // 게임 클리어 등으로 컨트롤이 비활성화되면 Update 자체가 호출되지 않으므로
            // 별도의 "조작 잠금" 플래그 없이 enabled = false 만으로 입력을 막을 수 있다.
            HandleMovement();
        }

        private void HandleMovement()
        {
            // 1. 새 Input System의 Keyboard.current로 WASD 입력 읽기
            float h = 0f;
            float v = 0f;
            bool jumpPressed = false;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed) h -= 1f;
                if (keyboard.dKey.isPressed) h += 1f;
                if (keyboard.sKey.isPressed) v -= 1f;
                if (keyboard.wKey.isPressed) v += 1f;
                jumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            }

            // 2. 카메라가 바라보는 방향을 기준으로 이동 방향 계산 (카메라의 y축 회전만 사용)
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 camRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * v + camRight * h;
            if (moveDir.sqrMagnitude > 1f)
            {
                moveDir.Normalize();
            }
            float normalizedSpeed = moveDir.magnitude; // 0~1, 애니메이션 판단에도 재사용

            // 3. 접지 상태 처리: 땅에 붙어있을 때는 약한 하강 속도를 유지해 경사/턱에서 안 뜨게 한다
            bool isGrounded = controller.isGrounded;
            if (isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }

            // 3-1. 제자리 방향전환 감지: 거의 멈춰있다가 갑자기 확 다른 방향으로 움직이려 하면
            // (좌/우90도 방향전환 애니메이션 트리거용). 회전이 실제로 적용되기 전(7번) 값으로 각도를 잰다.
            bool isStationaryNow = normalizedSpeed < 0.1f;
            if (animator != null && isGrounded && wasStationary && !isStationaryNow)
            {
                float turnAngle = Vector3.SignedAngle(transform.forward, moveDir, Vector3.up);
                if (Mathf.Abs(turnAngle) > 100f)
                {
                    animator.SetTrigger(turnAngle > 0f ? "TurnRight" : "TurnLeft");
                }
            }
            wasStationary = isStationaryNow;

            // 4. Space 입력으로 점프 (땅에 있을 때만)
            if (isGrounded && jumpPressed)
            {
                // v = sqrt(2 * h * g) 공식으로 원하는 점프 높이에 맞는 초기 속도 계산
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (animator != null)
                {
                    // 거의 멈춰있다 뛴 건지, 이동 중에 뛴 건지에 따라 다른 점프 애니메이션을 고른다
                    animator.SetBool("StationaryJump", isStationaryNow);
                    if (!isStationaryNow)
                    {
                        // 이동 중 점프 애니메이션 3종(달리며 점프/오른발/왼발)을 순서대로 돌려써서 단조롭지 않게 한다
                        animator.SetInteger("JumpVariant", jumpVariantCounter);
                        jumpVariantCounter = (jumpVariantCounter + 1) % 3;
                    }
                    // 실제로 지금 이 순간 점프했다는 신호 (Grounded 값은 기본값도 false라 신뢰할 수 없어 별도 트리거 사용)
                    animator.SetTrigger("Jump");
                }
            }

            // 5. 중력 적용
            verticalVelocity.y += gravity * Time.deltaTime;

            // 6. 실제 이동 (수평 이동 + 넉백 속도 + 수직 속도)을 한 번에 Move로 적용
            Vector3 motion = (moveDir * moveSpeed + knockbackVelocity + new Vector3(0f, verticalVelocity.y, 0f)) * Time.deltaTime;
            controller.Move(motion);

            // 넉백 속도는 시간이 지나면서 서서히 줄어들어 다시 일반 조작으로 돌아온다
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, knockbackDrag * Time.deltaTime);

            // 7. 이동 방향으로 캐릭터(비주얼 포함)를 부드럽게 회전
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }

            // 8. 실제 캐릭터 모델(Animator)이 붙어있을 때만 애니메이션 파라미터를 갱신한다
            if (animator != null)
            {
                animator.SetFloat("Speed", normalizedSpeed);
                animator.SetFloat("MoveX", h); // 좌우 입력 -> 걷기 방향전환 블렌드
                animator.SetFloat("MoveZ", v); // 앞뒤 입력 -> 달리기/뒤로 달리기 블렌드
                animator.SetBool("Grounded", isGrounded);
            }
        }

        /// <summary>
        /// 리스폰 시 RespawnController가 호출: 낙하 속도를 0으로 초기화해 리스폰 직후 순간 낙하를 방지한다.
        /// </summary>
        public void ResetVerticalVelocity()
        {
            verticalVelocity = Vector3.zero;
            knockbackVelocity = Vector3.zero;
        }

        /// <summary>
        /// CharacterController가 다른 Collider에 부딪힐 때 Unity가 자동으로 호출한다.
        /// 부딪힌 대상에 Obstacle 컴포넌트가 있으면(정지된 장애물이든 MovingObstacle로 움직이는
        /// 장애물이든 동일) 부딪힌 면의 반대 방향(hit.normal)으로 플레이어를 튕겨낸다.
        /// </summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Obstacle obstacle = hit.collider.GetComponentInParent<Obstacle>();
            if (obstacle == null) return;

            Vector3 pushDir = hit.normal;
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.0001f)
            {
                // 장애물이 정확히 위/아래에서 부딪힌 경우(법선의 수평 성분이 거의 없는 경우)를 대비한 대체 방향
                pushDir = -transform.forward;
            }
            pushDir.Normalize();

            knockbackVelocity = pushDir * obstacle.knockbackForce;
            // 기존에 위로 오르는 중이 아니었다면(또는 더 낮은 상승 속도였다면) 넉백의 상승력으로 덮어써서 팅겨오르게 한다
            verticalVelocity.y = Mathf.Max(verticalVelocity.y, obstacle.knockbackUpward);

            if (animator != null)
            {
                animator.SetTrigger("Hit"); // 장애물에 맞아 뒤로 넘어지는 반응 애니메이션
            }
        }
    }
}
