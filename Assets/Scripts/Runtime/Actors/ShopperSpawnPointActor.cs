using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Systems.Shoppers;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    internal sealed class ShopperSpawnPointActor : MonoBehaviour, IDestinationActor
    {
        private IShopperSystem shopperSystem;

        public Vector3 Position => transform.position;

        private void OnDrawGizmos()
        {
            GizmoLocations.DrawLocationMarker(position: Position, label: name, lineHeight: 5f, color: Color.green);
        }

        private void Awake()
        {
            SystemsUtility.TryGetSystem(out shopperSystem);
        }

        private void OnEnable()
        {
            // The systems may not have existed yet when this actor woke up, so try once more
            // before giving up: without this the spawn point silently never registers.
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
