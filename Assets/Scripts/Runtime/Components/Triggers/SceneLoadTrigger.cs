using CHARK.GameManagement;
using CHARK.ScriptableScenes;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
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
            sceneSystem = GameManager.GetSystem<ISceneSystem>();
        }

        public void Trigger()
        {
            if (sceneCollection == false)
            {
                return;
            }

            sceneSystem.LoadScene(sceneCollection);
        }
    }
}
