using CHARK.GameManagement;
using CHARK.ScriptableScenes;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Triggers
{
    internal sealed class SceneLoadTrigger : MonoBehaviour
    {
        [SerializeField]
        private ScriptableSceneCollection sceneCollection;

        private ISceneSystem sceneSystem;

        private void Awake()
        {
            SystemsUtility.TryGetSystem(out sceneSystem);
        }

        public void Trigger()
        {
            if (sceneCollection == false)
            {
                return;
            }

            sceneSystem?.LoadScene(sceneCollection);
        }
    }
}
