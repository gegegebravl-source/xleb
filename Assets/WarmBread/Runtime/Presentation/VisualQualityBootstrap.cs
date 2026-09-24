using UnityEngine;

namespace WarmBread
{
    public static class VisualQualityBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // Procedural quality injection is disabled: keep editor scene settings manual and editable.
        }
    }
}
