using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Utilities
{
    internal sealed class ObjectFollowRotation : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [SerializeField]
        private bool isLockX;

        [SerializeField]
        private bool isLockY;

        [SerializeField]
        private bool isLockZ;

        private void LateUpdate()
        {
            if (target == false)
            {
                return;
            }

            var targetRotation = target.rotation.eulerAngles;
            var currentRotation = transform.rotation.eulerAngles;

            transform.rotation = Quaternion.Euler(
                isLockX ? currentRotation.x : targetRotation.x,
                isLockY ? currentRotation.y : targetRotation.y,
                isLockZ ? currentRotation.z : targetRotation.z
            );
        }
    }
}
