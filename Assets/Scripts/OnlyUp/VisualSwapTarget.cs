using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 로직(Collider/Rigidbody/기능 스크립트)과 비주얼(외형 메시)을 분리하기 위한 컴포넌트.
    /// 이 컴포넌트가 붙은 오브젝트(부모)는 항상 충돌/기능을 담당하고,
    /// 실제 보이는 메시는 "Visual"이라는 자식 오브젝트 하나로만 존재한다.
    /// 나중에 VARCO 3D 등으로 만든 모델로 교체할 때는 SwapVisual()만 호출하면 되고,
    /// 부모의 Collider/스크립트는 전혀 건드릴 필요가 없다.
    /// </summary>
    public class VisualSwapTarget : MonoBehaviour
    {
        [Tooltip("현재 비주얼(외형) 오브젝트. 부모(this)에는 Collider/Rigidbody만 유지된다.")]
        public Transform visual;

        /// <summary>
        /// 기존 Visual 자식을 제거하고, 새 비주얼 프리팹/오브젝트로 교체한다.
        /// 기존 Visual의 로컬 위치/회전은 그대로 새 비주얼에 적용하고, 크기는 "실제 렌더링 크기(월드 바운즈)"가
        /// 기존과 같아지도록 배율을 계산해서 적용한다. 임포트된 모델마다 원본 데이터 단위가 제각각이라
        /// (예: 어떤 모델은 스케일 100을 곱해야 1유닛 크기가 되기도 함) 스케일 값을 그대로 복사하면 안 되고,
        /// 반드시 바운즈 기준으로 환산해야 한다.
        /// </summary>
        public Transform SwapVisual(GameObject newVisualSource)
        {
            // 1. 기존 비주얼의 로컬 위치/회전과, 목표로 삼을 실제 렌더링 크기(월드 바운즈)를 기억해둔다
            Vector3 localPos = Vector3.zero;
            Quaternion localRot = Quaternion.identity;
            Vector3 targetWorldSize = Vector3.one;
            bool hadOldVisual = visual != null;
            if (hadOldVisual)
            {
                localPos = visual.localPosition;
                localRot = visual.localRotation;
                Renderer oldRenderer = visual.GetComponentInChildren<Renderer>();
                targetWorldSize = oldRenderer != null ? oldRenderer.bounds.size : visual.lossyScale;
                DestroySafe(visual.gameObject);
            }

            // 2. 새 비주얼을 이 오브젝트의 자식으로 생성한다
            GameObject go = Instantiate(newVisualSource, transform);
            go.name = "Visual";

            // 3. 비주얼 안에 딸려온 Collider/Rigidbody는 제거한다.
            //    충돌 판정은 항상 이 스크립트가 붙은 부모(로직) 오브젝트의 Collider가 담당한다.
            foreach (var col in go.GetComponentsInChildren<Collider>())
            {
                DestroySafe(col);
            }
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>())
            {
                DestroySafe(rb);
            }

            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;

            // 4. 새 비주얼의 "지금 스케일 그대로일 때의 실제 렌더링 크기"를 측정해서,
            //    목표 크기(targetWorldSize)에 맞도록 배율을 곱한다 (스케일 값 자체를 복사하지 않음).
            if (hadOldVisual)
            {
                Renderer newRenderer = go.GetComponentInChildren<Renderer>();
                if (newRenderer != null)
                {
                    Vector3 nativeSize = newRenderer.bounds.size;
                    Vector3 currentScale = go.transform.localScale;
                    Vector3 factor = new Vector3(
                        nativeSize.x > 0.0001f ? targetWorldSize.x / nativeSize.x : 1f,
                        nativeSize.y > 0.0001f ? targetWorldSize.y / nativeSize.y : 1f,
                        nativeSize.z > 0.0001f ? targetWorldSize.z / nativeSize.z : 1f);
                    go.transform.localScale = Vector3.Scale(currentScale, factor);
                }
            }

            visual = go.transform;
            return visual;
        }

        /// <summary>
        /// Object.Destroy는 Play 모드에서만 동작하므로, 에디터에서 코스를 생성할 때(Play 중이 아닐 때)는
        /// DestroyImmediate를 써야 한다. 어느 쪽에서 호출되든 안전하게 제거한다.
        /// </summary>
        private static void DestroySafe(Object obj)
        {
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
    }
}
