using UnityEngine;
using UnityEngine.Events;

namespace UABPetelnia.GGJ2025.Runtime.Components.Triggers
{
    internal sealed class PlatformTrigger : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onWebGl;

        private void Start()
        {
#if UNITY_WEBGL
            onWebGl?.Invoke();
#endif
        }
    }
}
