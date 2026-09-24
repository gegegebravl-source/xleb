using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables
{
    internal sealed class GrabInteractable : Interactable
    {
        private Rigidbody rigidBody;

        public override Vector3 Position
        {
            get
            {
                if (rigidBody)
                {
                    return rigidBody.position;
                }

                return transform.position;
            }
            set
            {
                if (rigidBody && rigidBody.isKinematic == false)
                {
                    rigidBody.MovePosition(value);
                }
                else
                {
                    // Кинематическое тело ведём трансформом: MovePosition двигает его только
                    // на физическом шаге, и товар в руке отстаёт от руки.
                    transform.position = value;
                }
            }
        }

        public override Quaternion Rotation
        {
            get
            {
                if (rigidBody)
                {
                    return rigidBody.rotation;
                }

                return transform.rotation;
            }
            set
            {
                if (rigidBody && rigidBody.isKinematic == false)
                {
                    rigidBody.MoveRotation(value);
                }
                else
                {
                    transform.rotation = value;
                }
            }
        }

        private void Awake()
        {
            rigidBody = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (IsSelected == false)
            {
                return;
            }

            if (rigidBody == false)
            {
                return;
            }

            if (rigidBody.isKinematic)
            {
                return;
            }

            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }
    }
}
