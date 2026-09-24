using System.Collections.Generic;
using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Triggers
{
    internal sealed class HealthTrigger : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> hearts;

        private void OnEnable()
        {
            SystemsUtility.TryAddListener<PlayerHealthChanged>(OnPlayerHealthChanged);
        }

        private void OnDisable()
        {
            SystemsUtility.TryRemoveListener<PlayerHealthChanged>(OnPlayerHealthChanged);
        }

        private void OnPlayerHealthChanged(PlayerHealthChanged message)
        {
            // Список сердец может быть не назначен в сцене — без проверки падал NRE
            // при каждом изменении здоровья.
            if (hearts == null || hearts.Count == 0)
            {
                return;
            }

            if (message.Player == null
                || message.Player is UnityEngine.Object playerObject && playerObject == false)
            {
                return;
            }

            var health = Mathf.Max(0, message.Player.Health);

            for (var index = 0; index < hearts.Count; index++)
            {
                var heart = hearts[index];

                if (heart == false)
                {
                    continue;
                }

                heart.SetActive(index < health);
            }
        }
    }
}
