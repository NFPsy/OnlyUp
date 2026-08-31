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
            StartCoroutine(CrumbleRoutine());
        }

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
                    visual.localPosition = visualBaseLocalPos + new Vector3(
                        Random.Range(-shakeAmount, shakeAmount),
                        0f,
                        Random.Range(-shakeAmount, shakeAmount));
                }
                yield return null;
            }
            if (visual != null) visual.localPosition = visualBaseLocalPos;

            // 2. 무너짐: 충돌/외형을 함께 꺼서 발판이 사라진 것처럼 보이게 한다
            SetPlatformEnabled(false);
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
