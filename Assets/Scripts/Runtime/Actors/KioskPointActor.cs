using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Systems.Shoppers;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    internal sealed class KioskPointActor : MonoBehaviour, IDestinationActor
    {
        private IShopperSystem shopperSystem;

        public Vector3 Position => transform.position;

        private void OnDrawGizmos()
        {
            GizmoLocations.DrawLocationMarker(Position, name, lineHeight: 7f, color: Color.cyan);
        }

        private void Awake()
        {
            SystemsUtility.TryGetSystem(out shopperSystem);
        }

        private void OnEnable()
        {
            if (shopperSystem == null)
            {
                SystemsUtility.TryGetSystem(out shopperSystem);
            }

            shopperSystem?.AddDestination(this);
        }

        private void OnDisable()
        {
            shopperSystem?.RemoveDestination(this);
        }
    }
}
