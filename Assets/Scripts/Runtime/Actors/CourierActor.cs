using System.Collections.Generic;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using UABPetelnia.GGJ2025.Runtime.Systems.Shoppers;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;
using UnityEngine.AI;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// Курьер с коробкой: приходит к прилавку, ждёт, пока игрок заберёт коробку, и уходит.
    /// </summary>
    internal sealed class CourierActor : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        private NavMeshAgent agent;

        [SerializeField]
        private Animator walkAnimation;

        [Header("Delivery")]
        [Tooltip("Коробка в руках курьера.")]
        [SerializeField]
        private DeliveryBoxActor box;

        [Min(0.05f)]
        [SerializeField]
        private float arriveDistance = 0.7f;

        private IShopperSystem shopperSystem;
        private Vector3 destination;
        private bool hasArrived;
        private bool isLeaving;

        public Vector3 Position => transform.position;

        /// <summary>Коробка, которую несёт курьер.</summary>
        public DeliveryBoxActor Box => box;

        private void Awake()
        {
            if (agent == false)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (box == false)
            {
                box = GetComponentInChildren<DeliveryBoxActor>(true);
            }

            SystemsUtility.TryGetSystem(out shopperSystem);
        }

        /// <summary>Наполнить коробку позициями заказа.</summary>
        public void Initialize(IReadOnlyList<ShopDeliveryLine> lines)
        {
            if (box != false)
            {
                box.Fill(lines);
            }

            SetWalking(true);
        }

        public void GoTo(Vector3 position)
        {
            destination = position;

            if (agent != false && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(position);
                return;
            }

            // A delivery must still be retrievable when a scene was authored without a baked
            // NavMesh. Teleport to the kiosk instead of leaving the box stranded at the spawn.
            transform.position = position;
            hasArrived = true;
            SetWalking(false);
        }

        private void Update()
        {
            if (isLeaving)
            {
                if (agent == false || agent.enabled == false || agent.isOnNavMesh == false)
                {
                    return;
                }

                if (agent.pathPending == false && agent.remainingDistance <= arriveDistance)
                {
                    Destroy(gameObject);
                }

                return;
            }

            if (hasArrived || agent == false || agent.enabled == false || agent.isOnNavMesh == false)
            {
                return;
            }

            if (agent.pathPending)
            {
                return;
            }

            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                // Do not strand a paid delivery forever on a partial/invalid path.
                transform.position = destination;
                hasArrived = true;
                SetWalking(false);
                return;
            }

            if (agent.remainingDistance <= arriveDistance)
            {
                hasArrived = true;
                SetWalking(false);
            }
        }

        /// <summary>Игрок забрал коробку: курьер уходит и пропадает.</summary>
        public void OnBoxTaken()
        {
            isLeaving = true;
            SetWalking(true);

            var exit = shopperSystem != null ? shopperSystem.RandomSpawnPoint : destination + Vector3.forward * 8f;

            if (agent != false && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(exit);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SetWalking(bool isWalking)
        {
            if (walkAnimation == false)
            {
                return;
            }

            if (isWalking)
            {
                walkAnimation.enabled = true;
                walkAnimation.Play("Animation_Shooper_Walk_Jump", -1, 0f);
                return;
            }

            walkAnimation.Play(walkAnimation.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);
            walkAnimation.Update(0f);
            walkAnimation.enabled = false;
        }
    }
}
