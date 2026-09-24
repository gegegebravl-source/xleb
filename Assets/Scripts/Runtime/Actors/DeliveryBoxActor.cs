using System.Collections.Generic;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using System;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// Картонная коробка с приехавшим товаром: курьер приносит её к прилавку, игрок забирает
    /// и через меню коробки раскладывает товар по слотам рук.
    /// </summary>
    /// <remarks>
    /// Коробка — обычный <c>GrabInteractable</c>, поэтому игрок берёт её той же кнопкой E,
    /// что и любой предмет в ларьке.
    /// </remarks>
    [DefaultExecutionOrder(-10)]
    internal sealed class DeliveryBoxActor : MonoBehaviour
    {
        private readonly List<ItemData> contents = new();

        private Rigidbody body;

        private GrabInteractable interactable;

        /// <summary>Сколько единиц товара осталось в коробке.</summary>
        public int Count => contents.Count;

        public bool IsEmpty => contents.Count <= 0;

        /// <summary>Товары в коробке в порядке добавления.</summary>
        public IReadOnlyList<ItemData> Contents => contents;

        /// <summary>Заполнить коробку позициями доставки.</summary>
        public void Fill(IReadOnlyList<ShopDeliveryLine> lines)
        {
            contents.Clear();

            if (lines == null)
            {
                return;
            }

            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                if (line == null || line.Product == null || line.Product.Item == false)
                {
                    continue;
                }

                var quantity = Math.Min(line.Quantity, ShopOrderRules.MaxLineQuantity);
                for (var unit = 0; unit < quantity; unit++)
                {
                    contents.Add(line.Product.Item);
                }
            }
        }

        /// <summary>Проверить, что кнопка интерфейса ещё указывает на живую позицию.</summary>
        public bool ContainsItem(ItemData item)
        {
            return item && contents.Contains(item);
        }

        /// <summary>Убрать одну единицу товара из коробки.</summary>
        public bool TryRemoveItem(ItemData item)
        {
            if (item == false)
            {
                return false;
            }

            return contents.Remove(item);
        }

        /// <summary>Rollback a failed hand-off without exposing the internal list.</summary>
        public void RestoreItem(ItemData item)
        {
            if (item)
            {
                contents.Add(item);
            }
        }

        /// <summary>
        /// Список позиций коробки без повторов: товар и сколько его осталось.
        /// </summary>
        public void CollectStacks(List<ItemData> items, List<int> counts)
        {
            if (items == null || counts == null)
            {
                return;
            }

            items.Clear();
            counts.Clear();

            for (var index = 0; index < contents.Count; index++)
            {
                var item = contents[index];
                var found = -1;

                for (var known = 0; known < items.Count; known++)
                {
                    if (items[known] == item)
                    {
                        found = known;
                        break;
                    }
                }

                if (found >= 0)
                {
                    counts[found] = counts[found] + 1;
                }
                else
                {
                    items.Add(item);
                    counts.Add(1);
                }
            }
        }

        /// <summary>Коробка пуста: объект убирается из мира.</summary>
        public void Discard()
        {
            Destroy(gameObject);
        }

        private void Awake()
        {
            // Порядок важен: GrabInteractable в Awake ищет Rigidbody через GetComponentInParent.
            body = GetComponent<Rigidbody>();
            if (body == false)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            // Пока коробку несут (курьер, рука игрока) — кинематик: ведём трансформом.
            // Физика включается только при отпускании, чтобы коробка падала на пол.
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            interactable = GetComponent<GrabInteractable>();

            if (interactable != false)
            {
                interactable.OnSelectEntered += OnGrabSelected;
                interactable.OnSelectExited += OnGrabDeselected;
            }
        }

        private void OnDestroy()
        {
            if (interactable != false)
            {
                interactable.OnSelectEntered -= OnGrabSelected;
                interactable.OnSelectExited -= OnGrabDeselected;
            }
        }

        private void OnGrabSelected(InteractableSelectEnteredArgs args)
        {
            body.isKinematic = true;
        }

        private void OnGrabDeselected(InteractableSelectExitedArgs args)
        {
            body.isKinematic = false;
            body.useGravity = true;
        }
    }
}
