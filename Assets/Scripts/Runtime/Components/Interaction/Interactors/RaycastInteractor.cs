using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors
{
    internal abstract class RaycastInteractor : Interactor
    {
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Transforms", Expanded = true)]
        [Sirenix.OdinInspector.Required]
#else
        [Header("General")]
#endif
        [SerializeField]
        private Transform raycastTransform;

        private RaycastHit raycastHit;

        /// <summary>
        /// Буфер для SphereCastAll без аллокаций: на полной полке под прицелом
        /// может быть с десяток коллайдеров товаров.
        /// </summary>
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[32];

        protected abstract IRaycastInteractorSettings Settings { get; }

        protected abstract bool IsValid(IInteractable interactable);

        private Transform InteractorTransform
        {
            get
            {
                if (raycastTransform)
                {
                    return raycastTransform;
                }

                return transform;
            }
        }

        /// <summary>
        /// Точка, из которой пускается луч (камера игрока). Нужна для проверок прицела
        /// вне самой иерархии интерактора, например для отдачи товара покупателю.
        /// </summary>
        public Transform RaycastOrigin => InteractorTransform;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Settings == null)
            {
                return;
            }

            Gizmos.color = Settings.RaycastColor;

            var interactorTransform = InteractorTransform;
            var interactorPosition = interactorTransform.position;
            var interactorForward = interactorTransform.forward;

            if (raycastHit.collider)
            {
                Gizmos.DrawLine(
                    interactorPosition,
                    raycastHit.point
                );

                Gizmos.DrawWireSphere(
                    raycastHit.point,
                    Settings.RaycastRadius
                );
            }
            else
            {
                Gizmos.DrawRay(
                    interactorPosition,
                    interactorForward * Settings.RaycastDistance
                );

                Gizmos.DrawWireSphere(
                    interactorPosition + interactorTransform.forward * Settings.RaycastDistance,
                    Settings.RaycastRadius
                );
            }
        }
#endif

        protected override void OnPhysicsUpdated()
        {
            base.OnPhysicsUpdated();
            UpdateHovering();
        }

        private void UpdateHovering()
        {
            var settings = Settings;
            if (settings == null)
            {
                raycastHit = default;
                UnHover();
                return;
            }

            if (IsSelecting)
            {
                // Already selected something - busy.
                return;
            }

            var hitCount = TryRaycastAll();

            IInteractable current = default;
            foreach (var hovered in HoveredInteractables)
            {
                current = hovered;
                break;
            }

            IInteractable best = default;
            var bestDistance = float.MaxValue;
            var keepCurrent = false;

            for (var index = 0; index < hitCount; index++)
            {
                var hit = HitBuffer[index];
                var hitCollider = hit.collider;
                if (hitCollider == false)
                {
                    continue;
                }

                var interactable = hitCollider.GetComponentInParent<IInteractable>();
                if (interactable is not { IsEnabled: true } || IsValid(interactable) == false)
                {
                    continue;
                }

                // Уже выбранный интерактор не ховерим: он в руке, а не под прицелом.
                if (IsSelected(interactable))
                {
                    continue;
                }

                // Липкий hover: пока текущий объект ещё под прицелом, не переключаемся
                // на соседний, даже если тот чуть ближе. На плотной полке соседние
                // коллайдеры почти пересекаются, и без этого выбор мигал бы через раз.
                if (current != null && ReferenceEquals(interactable, current))
                {
                    keepCurrent = true;
                    break;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    best = interactable;
                }
            }

            raycastHit = hitCount > 0 ? HitBuffer[0] : default;

            if (keepCurrent)
            {
                return;
            }

            // Only one interactable should be hovered at any given time.
            UnHover();

            if (best != null)
            {
                Hover(best);
            }
        }

        /// <summary>
        /// SphereCastAll в общий буфер. Возвращает число попаданий (не больше размера буфера).
        /// </summary>
        private int TryRaycastAll()
        {
            var settings = Settings;
            if (settings == null)
            {
                return 0;
            }

            var interactorTransform = InteractorTransform;
            var radius = IsFinite(settings.RaycastRadius) ? Mathf.Max(0f, settings.RaycastRadius) : 0f;
            var distance = IsFinite(settings.RaycastDistance) ? Mathf.Max(0f, settings.RaycastDistance) : 0f;

            // Направление обязано быть нормализованным: Unity 6 сыплет ассертами на
            // «forward * distance», а длину луча и так задаёт maxDistance.
            return Physics.SphereCastNonAlloc(
                interactorTransform.position,
                radius,
                interactorTransform.forward,
                HitBuffer,
                distance,
                settings.RaycastLayer,
                settings.QueryTriggerInteraction
            );
        }

        private static bool IsFinite(float value)
        {
            return float.IsNaN(value) == false && float.IsInfinity(value) == false;
        }
    }
}
