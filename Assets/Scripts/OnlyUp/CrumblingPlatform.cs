using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 밟으면 잠깐 흔들리다가 무너져 사라지고, 얼마 뒤 다시 생기는 발판.
    /// "오래 서서 다음 점프를 고민할 수 없다"는 압박을 만들어 난이도를 올린다.
    ///
    /// 반드시 다시 생기게(respawnDelay) 만든 이유: 이 게임은 체크포인트가 없어서 떨어지면 항상
    /// 처음부터 다시 올라온다. 한 번 무너진 발판이 영영 사라지면 재시도할 때 코스가 끊겨서
    /// 클리어 자체가 불가능해지기 때문에, 일정 시간 뒤 원상복구되어야 한다.
    ///
    /// 발판 본체 콜라이더는 모델 모양에 맞춘 MeshCollider(자식 "CollisionMesh")인 경우가 많으므로,
    /// 자식까지 포함해 "트리거가 아닌" 콜라이더와 모든 Renderer를 함께 껐다 켠다.
    /// (이 스크립트가 밟힘을 감지하는 데 쓰는 트리거 콜라이더는 꺼지면 안 되므로 제외한다)
    /// </summary>
    public class CrumblingPlatform : MonoBehaviour
    {
        [Tooltip("밟은 뒤 무너지기까지의 시간(초). 이 동안 발판이 흔들려 경고를 준다")]
        public float crumbleDelay = 0.9f;

        [Tooltip("무너진 뒤 다시 생기기까지의 시간(초)")]
        public float respawnDelay = 3.5f;

        [Tooltip("무너지기 직전 흔들리는 폭")]
        public float shakeAmount = 0.06f;

        [Tooltip("흔들림 연출을 적용할 Visual (없으면 흔들림 없이 사라지기만 한다)")]
        public Transform visual;

        private Collider[] solidColliders;
        private Renderer[] renderers;
        private Vector3 visualBaseLocalPos;
        private bool busy;

        private void Awake()
        {
            // 밟힘 감지용 트리거는 계속 살아있어야 하므로 끄고 켤 대상에서 제외한다
            var solid = new List<Collider>();
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
            {
                if (!c.isTrigger) solid.Add(c);
            }
            solidColliders = solid.ToArray();
            renderers = GetComponentsInChildren<Renderer>(true);

            if (visual != null) visualBaseLocalPos = visual.localPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (busy) return;
            if (!other.CompareTag("Player")) return;
            // StartCoroutine은 "시간이 걸리는 작업을 여러 프레임에 걸쳐 순서대로 실행"하게 해주는
            // Unity 기능이다(코루틴). 아래 CrumbleRoutine()은 보통 함수와 달리 한 번에 끝나지 않고,
            // yield return을 만날 때마다 잠깐 멈췄다가 다음 프레임에 이어서 실행된다 — 그래서
            // "0.9초 동안 흔들다가 → 사라지고 → 3.5초 기다렸다가 → 다시 생김"처럼 시간이 걸리는
            // 연출을 자연스럽게 표현할 수 있다.
            StartCoroutine(CrumbleRoutine());
        }

        // 반환 타입이 IEnumerator인 함수는 코루틴으로 실행할 수 있다.
        // yield return을 만나면 그 지점에서 실행을 멈추고, 조건이 만족되면(다음 프레임이 되거나,
        // 지정한 시간이 지나면) 멈췄던 바로 다음 줄부터 다시 이어서 실행한다.
        private IEnumerator CrumbleRoutine()
        {
            busy = true;

            // 1. 무너지기 전 경고: 짧게 흔들린다
            float elapsed = 0f;
            while (elapsed < crumbleDelay)
            {
                elapsed += Time.deltaTime; // 일시정지 중(timeScale=0)에는 자연스럽게 멈춘다
                if (visual != null)
                {
                    // Random.Range(-a, a)는 -a~a 사이의 무작위 값을 하나 뽑아준다.
                    // 매 프레임 살짝 다른 위치로 옮기면 "덜덜 떨리는" 것처럼 보인다.
                    visual.localPosition = visualBaseLocalPos + new Vector3(
                        Random.Range(-shakeAmount, shakeAmount),
                        0f,
                        Random.Range(-shakeAmount, shakeAmount));
                }
                // yield return null은 "딱 한 프레임만 쉬었다가 다음 프레임에 여기부터 계속"이라는 뜻이다.
                // 이 덕분에 while 루프가 한 번에 다 도는 게 아니라, crumbleDelay초 동안 매 프레임
                // 흔들리는 모습을 실제로 화면에 보여줄 수 있다.
                yield return null;
            }
            if (visual != null) visual.localPosition = visualBaseLocalPos;

            // 2. 무너짐: 충돌/외형을 함께 꺼서 발판이 사라진 것처럼 보이게 한다
            SetPlatformEnabled(false);
            // WaitForSeconds(초)는 "그만큼 실제 시간이 지날 때까지 여기서 기다렸다가 이어서 실행"하라는
            // 뜻이다. respawnDelay초 동안 발판이 사라진 채로 유지된다.
            yield return new WaitForSeconds(respawnDelay);

            // 3. 복구: 다시 올라올 때를 위해 원상복구한다
            SetPlatformEnabled(true);
            busy = false;
        }

        private void SetPlatformEnabled(bool enabledState)
        {
            foreach (Collider c in solidColliders)
            {
                if (c != null) c.enabled = enabledState;
            }
            foreach (Renderer r in renderers)
            {
                if (r != null) r.enabled = enabledState;
            }
        }
    }
}
