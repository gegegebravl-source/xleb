using UnityEngine;
using UnityEngine.EventSystems;

namespace WarmBread
{
    public sealed class WarmBreadButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 baseScale;
        private Vector3 targetScale;

        private void Awake()
        {
            baseScale = transform.localScale;
            targetScale = baseScale;
        }

        private void OnEnable()
        {
            baseScale = transform.localScale;
            targetScale = baseScale;
        }

        private void Update()
        {
            var blend = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, blend);
        }

        public void OnPointerEnter(PointerEventData eventData) => targetScale = baseScale * 1.035f;
        public void OnPointerExit(PointerEventData eventData) => targetScale = baseScale;
        public void OnPointerDown(PointerEventData eventData) => targetScale = baseScale * 0.97f;
        public void OnPointerUp(PointerEventData eventData) => targetScale = baseScale * 1.035f;
    }
}
