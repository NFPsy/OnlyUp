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
        // 점프 정점 높이. 발판이 수직으로 겹쳐 보이는(머리가 부딪히는) 문제는 jumpHeight를 낮춰도
        // 거의 개선되지 않는다는 것을 실측(캡슐 콜라이더로 실제 점프 궤적 시뮬레이션)으로 확인했다 —
        // 원인은 점프 속도가 아니라 발판끼리의 수평 겹침 자체였다(GameBootstrap의
        // MinHorizontalOffsetRatio로 해결). jumpHeight를 낮추면 오히려 점프 사거리만 줄어들어
        // 일부 넓은 점프(하늘 구간)가 아슬아슬하게 닿지 않게 되므로, 기존 값(5)을 그대로 유지한다.
        public float jumpHeight = 5f;
        public float gravity = -25f;
        public float rotateSpeed = 12f; // 이동 방향으로 몸을 돌리는 속도

        [Header("장애물 넉백 설정")]
        public float knockbackDrag = 18f; // 넉백 속도가 초당 이만큼씩 줄어들어 서서히 원래 조작으로 돌아온다

        [Header("사운드")]
        [Tooltip("점프할 때 재생할 효과음 (CourseKit.CreatePlayer가 할당)")]
        public AudioClip jumpClip;
        public AudioSource jumpAudioSource;

        [Header("참조")]
        [Tooltip("카메라 기준으로 이동 방향을 계산하기 위한 카메라 Transform (GameBootstrap이 할당)")]
        public Transform cameraTransform;

        private CharacterController controller;
        private Animator animator; // 실제 캐릭터 모델(Resources/PlayerModel)일 때만 존재, 더미 캡슐이면 null
        private Vector3 verticalVelocity; // y축 속도(중력/점프)만 별도로 관리
        private Vector3 knockbackVelocity; // 장애물에 부딪혔을 때의 수평 넉백 속도 (서서히 감쇠)

        // --- 얼음 발판(IcePlatform) 전용 상태 ---
        // 평소 이 게임의 수평 이동에는 관성이 전혀 없다(입력을 놓으면 그 프레임에 바로 멈춤).
        // 얼음 발판 위에 서 있는 동안에만 아래 값이 0보다 커지고, 그때는 목표 속도로 곧바로 바뀌는 대신
        // 초당 slipDeceleration만큼씩만 따라가서 "미끄러지는" 느낌이 난다.
        private Vector3 horizontalVelocity; // 지금 실제로 적용 중인 수평 속도 (얼음 위에서 서서히 변한다)
        private float slipDeceleration;     // 0이면 얼음이 아님 → 기존과 완전히 동일하게 즉시 반응
        private IcePlatform slipSource;     // 지금 미끄러짐을 걸어준 발판 (발판을 갈아탈 때 잘못 해제되는 것 방지)

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

                if (jumpAudioSource != null && jumpClip != null)
                {
                    jumpAudioSource.PlayOneShot(jumpClip);
                }

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

            // 6. 수평 속도 결정. 평소에는 입력에 즉시 반응하지만(관성 없음), 얼음 발판 위에 서 있는
            //    동안에는 목표 속도로 곧바로 바뀌지 않고 초당 slipDeceleration만큼만 따라가 미끄러진다.
            //    MoveTowards(현재값, 목표값, 이번 프레임에 움직일 수 있는 최대량)는 목표를 지나치지 않고
            //    딱 그만큼만 다가가므로, 가속(키를 눌렀을 때)과 감속(키를 놓았을 때) 모두 같은 비율로 적용된다.
            //    공중에서는 얼음이어도 기존과 똑같이 즉시 반응한다 — 점프하면 바로 평소 조작감으로 돌아와야
            //    "얼음 위에서만 미끄럽다"는 규칙이 분명해지기 때문이다.
            Vector3 targetHorizontal = moveDir * moveSpeed;
            if (slipDeceleration > 0f && isGrounded)
            {
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetHorizontal, slipDeceleration * Time.deltaTime);
            }
            else
            {
                horizontalVelocity = targetHorizontal;
            }

            // 7. 실제 이동 (수평 이동 + 넉백 속도 + 수직 속도)을 한 번에 Move로 적용
            Vector3 motion = (horizontalVelocity + knockbackVelocity + new Vector3(0f, verticalVelocity.y, 0f)) * Time.deltaTime;
            controller.Move(motion);

            // 넉백 속도는 시간이 지나면서 서서히 줄어들어 다시 일반 조작으로 돌아온다
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, knockbackDrag * Time.deltaTime);

            // 8. 이동 방향으로 캐릭터(비주얼 포함)를 부드럽게 회전
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }

            // 9. 실제 캐릭터 모델(Animator)이 붙어있을 때만 애니메이션 파라미터를 갱신한다
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
        /// 얼음 발판 위에서 떨어졌을 수도 있으므로 미끄러짐 상태와 관성 속도도 같이 초기화한다 —
        /// 리스폰은 순간이동이라 얼음 발판의 트리거 이탈(OnTriggerExit)이 확실히 불린다고 보장할 수 없고,
        /// 미끄러짐이 남아있으면 체크포인트에 되살아나자마자 이유 없이 미끄러지게 된다.
        /// </summary>
        public void ResetVerticalVelocity()
        {
            verticalVelocity = Vector3.zero;
            knockbackVelocity = Vector3.zero;
            horizontalVelocity = Vector3.zero;
            slipDeceleration = 0f;
            slipSource = null;
        }

        /// <summary>
        /// 얼음 발판(<see cref="IcePlatform"/>)을 밟았을 때 호출된다. 이 순간부터 발판에서 내려갈 때까지
        /// 수평 이동에 관성이 생겨 미끄러진다. deceleration이 작을수록 더 미끄럽다.
        /// </summary>
        public void EnterSlipperySurface(IcePlatform source, float deceleration)
        {
            slipSource = source;
            slipDeceleration = deceleration;
        }

        /// <summary>
        /// 얼음 발판에서 벗어났을 때 호출된다. 지금 미끄러짐을 걸어준 발판이 자기 자신일 때만 해제한다 —
        /// 얼음 발판에서 다른 얼음 발판으로 곧바로 옮겨가면 "새 발판 진입 → 이전 발판 이탈" 순서로 불릴 수
        /// 있는데, 그때 이전 발판이 새 발판의 미끄러짐까지 꺼버리면 안 되기 때문이다.
        /// </summary>
        public void ExitSlipperySurface(IcePlatform source)
        {
            if (slipSource != source) return;
            slipSource = null;
            slipDeceleration = 0f;
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
