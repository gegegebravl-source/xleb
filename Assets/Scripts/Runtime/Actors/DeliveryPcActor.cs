using System.Collections.Generic;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// The ordering PC that sits on the kiosk counter. The player has to walk up to it before the
    /// delivery panel can be opened, which keeps the button from working from anywhere in the
    /// street.
    /// </summary>
    /// <remarks>
    /// Instances register themselves so the player prefab does not have to hold a reference to a
    /// scene object (prefabs cannot reference scene objects).
    /// </remarks>
    internal sealed class DeliveryPcActor : MonoBehaviour
    {
        private static readonly List<DeliveryPcActor> activeInstances = new();

        [Header("Interaction")]
        [Min(0.5f)]
        [SerializeField]
        private float interactDistance = 3f;

        private Renderer bodyRenderer;

        private void Awake()
        {
            bodyRenderer = GetComponentInChildren<Renderer>(true);
        }

        private void OnEnable()
        {
            if (bodyRenderer == false)
            {
                bodyRenderer = GetComponentInChildren<Renderer>(true);
            }

            activeInstances.Add(this);
        }

        private void OnDisable()
        {
            activeInstances.Remove(this);
        }

        /// <summary>
        /// Find the closest active PC that is within its own interaction distance of
        /// <paramref name="position"/>.
        /// </summary>
        public static bool TryFindNearest(Vector3 position, out DeliveryPcActor pc)
        {
            pc = default;

            var closestDistance = float.MaxValue;

            foreach (var instance in activeInstances)
            {
                if (instance == false)
                {
                    continue;
                }

                var distance = Vector3.Distance(position, instance.transform.position);
                if (distance > instance.interactDistance || distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                pc = instance;
            }

            return pc != default;
        }

        /// <summary>
        /// Find the closest active PC the camera is actually pointing at. Используется клавишей
        /// взаимодействия: панель заказов открывается только когда игрок смотрит на ПК, а не
        /// просто стоит рядом.
        /// </summary>
        public static bool TryFindAimed(Camera camera, out DeliveryPcActor pc)
        {
            pc = default;

            if (camera == false)
            {
                return false;
            }

            var origin = camera.transform.position;
            var direction = camera.transform.forward;
            var ray = new Ray(origin, direction);
            var closestDistance = float.MaxValue;

            foreach (var instance in activeInstances)
            {
                if (instance == false || instance.bodyRenderer == false)
                {
                    continue;
                }

                // Квад ПК тонкий, поэтому проверяем луч по границам рендера: попадание в них
                // означает, что игрок навёл камеру на экран заказов. Дистанция наведения шире
                // дистанции «стою рядом», иначе панель не открыть с другого конца прилавка.
                if (instance.bodyRenderer.bounds.IntersectRay(ray, out var distance) == false)
                {
                    continue;
                }

                if (distance > instance.interactDistance * 2f || distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                pc = instance;
            }

            return pc != default;
        }
    }
}
