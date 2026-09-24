using UnityEngine;
using UnityEngine.Rendering;

namespace UABPetelnia.GGJ2025.Runtime.Components.Utilities
{
    [RequireComponent(typeof(ReflectionProbe))]
    internal sealed class ProbeRefresher : MonoBehaviour
    {
        [Range(0f, 16f)]
        [SerializeField]
        private float refreshIntervalSeconds = 0.1f;

        private ReflectionProbe probe;
        private float timer;

        private void Awake()
        {
            probe = GetComponent<ReflectionProbe>();
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;

            timer = refreshIntervalSeconds;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                probe.RenderProbe();
                timer = refreshIntervalSeconds;
            }
        }
    }
}
