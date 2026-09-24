using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Utilities
{
    /// <summary>
    /// Turns a flat picture (a quad) towards the player camera, so posters, products and cards stay
    /// readable from anywhere in the kiosk.
    /// </summary>
    /// <remarks>
    /// The picture is aimed with its <b>visible</b> side. Unity's built-in quad looks along -Z, so
    /// aiming the transform's forward at the camera presents the back face and the single sided URP
    /// materials cull the picture away — that is how the products, the delivery PC and the kiosk
    /// decor used to disappear in play mode while staying visible in the editor. Deriving the aim
    /// from the mesh normal works for any quad, no matter how the prefab was authored, which is why
    /// the old per-object flip flag is gone.
    /// </remarks>
    internal sealed class Billboard : MonoBehaviour
    {
        private Camera mainCamera;

        private Vector3 localNormal = Vector3.back;

        private void Awake()
        {
            mainCamera = Camera.main;
            CacheLocalNormal();
        }

        private void LateUpdate()
        {
            if (mainCamera == false)
            {
                mainCamera = Camera.main;

                if (mainCamera == false)
                {
                    return;
                }
            }

            var direction = mainCamera.transform.position - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            direction.Normalize();

            transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Inverse(Quaternion.LookRotation(localNormal));
        }

        /// <summary>
        /// Remembers which way the picture looks in local space, flattened so the billboard only
        /// spins around Y.
        /// </summary>
        private void CacheLocalNormal()
        {
            var filter = GetComponent<MeshFilter>();
            var mesh = filter ? filter.sharedMesh : null;

            if (mesh == null || mesh.normals.Length <= 0)
            {
                return;
            }

            localNormal = mesh.normals[0];
            localNormal.y = 0f;

            if (localNormal.sqrMagnitude < 0.0001f)
            {
                localNormal = Vector3.back;
            }

            localNormal.Normalize();
        }
    }
}
