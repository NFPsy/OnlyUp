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
        /// 기존 Visual의 로컬 위치/회전/스케일을 그대로 새 비주얼에 적용해 크기·방향을 맞춘다.
        /// </summary>
        public Transform SwapVisual(GameObject newVisualSource)
        {
            // 1. 기존 비주얼의 로컬 트랜스폼 값을 기억해둔다 (새 비주얼도 같은 위치/크기를 쓰도록)
            Vector3 localPos = Vector3.zero;
            Quaternion localRot = Quaternion.identity;
            Vector3 localScale = Vector3.one;
            if (visual != null)
            {
                localPos = visual.localPosition;
                localRot = visual.localRotation;
                localScale = visual.localScale;
                Destroy(visual.gameObject);
            }

            // 2. 새 비주얼을 이 오브젝트의 자식으로 생성한다
            GameObject go = Instantiate(newVisualSource, transform);
            go.name = "Visual";

            // 3. 비주얼 안에 딸려온 Collider/Rigidbody는 제거한다.
            //    충돌 판정은 항상 이 스크립트가 붙은 부모(로직) 오브젝트의 Collider가 담당한다.
            foreach (var col in go.GetComponentsInChildren<Collider>())
            {
                Destroy(col);
            }
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>())
            {
                Destroy(rb);
            }

            // 4. 기존 비주얼과 동일한 로컬 트랜스폼을 적용해 크기/방향을 맞춘다
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            visual = go.transform;
            return visual;
        }
    }
}
