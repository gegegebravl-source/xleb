using System.Collections.Generic;
using System.Linq;
using CHARK.GameManagement;
using CHARK.ScriptableAudio;
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Shoppers;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    internal sealed class ShopperActor : MonoBehaviour, IShopperActor
    {
        [Header("General")]
        [SerializeField]
        private NavMeshAgent agent;

        [Header("Request")]
        [SerializeField]
        private GameObject requestCard;

        [SerializeField]
        private Renderer requestRenderer;

        [SerializeField]
        private string requestTexturePropertyId = "_BaseMap";

        [Header("Animations")]
        [SerializeField]
        private Animator buyAnimation;

        [SerializeField]
        private Animator punchAnimation;

        [SerializeField]
        private Animator walkAnimation;

        [Header("Text")]
        [SerializeField]
        private string keywordToken = "${KEYWORD}";

        [Header("Rendering")]
        [SerializeField]
        private Renderer bodyRenderer;

        [Header("Audio")]
        [SerializeField]
        private AudioEmitter speechAudioEmitter;

        [Header("Events")]
        [SerializeField]
        public UnityEvent onPunchStart;

        [SerializeField]
        public UnityEvent onPunchStop;

        [SerializeField]
        public UnityEvent OnBuyStart;

        [SerializeField]
        public UnityEvent OnBuyStop;

        [SerializeField]
        public UnityEvent OnMoveStart;

        [SerializeField]
        public UnityEvent OnMoveStop;

        [SerializeField]
        private string texturePropertyId = "_BaseMap";

        [Header("Speech bubble")]
        [Tooltip("Плашка с текстом реплики над головой покупателя.")]
        [SerializeField]
        private GameObject speechBubble;

        [SerializeField]
        private TMP_Text speechText;

        private IShopperSystem shopperSystem;
        private Camera mainCamera;

        public string Name => name;

        public ShopperData Data { get; private set; }

        public bool IsContainsPurchases => Data.PurchaseCollection.Purchases.Count > 0;

        public bool IsBuying
        {
            get
            {
                // Read every frame by the state machine, so a missing animator must not throw:
                // without this guard the shopper spammed a NullReferenceException per frame and the
                // shift could never finish.
                if (buyAnimation == false)
                {
                    return false;
                }

                var stateInfo = buyAnimation.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Animation_Shopper_Money") && stateInfo.normalizedTime < 1f)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsPunching
        {
            get
            {
                if (punchAnimation == false)
                {
                    return false;
                }

                var stateInfo = punchAnimation.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Animation_Shopper_Fist") && stateInfo.normalizedTime < 1f)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsMoving
        {
            get
            {
                if (agent == false)
                {
                    return false;
                }

                var dist = agent.remainingDistance;
                return float.IsPositiveInfinity(dist)
                    || agent.pathStatus != NavMeshPathStatus.PathComplete
                    || agent.remainingDistance != 0;
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying == false)
            {
                return;
            }

            if (IsMoving == false)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, agent.destination);
        }

        private void Awake()
        {
            shopperSystem = GameManager.GetSystem<IShopperSystem>();
            mainCamera = Camera.main;
        }

        /// <summary>
        /// Cached <see cref="Camera.main"/> accessor which survives scene changes.
        /// </summary>
        private bool TryGetCamera()
        {
            if (mainCamera != false)
            {
                return true;
            }

            mainCamera = Camera.main;

            return mainCamera != false;
        }

        private void OnEnable()
        {
            // shopperSystem может быть не получен, если Awake отработал раньше инициализации
            // GameManager (edge-случай редактора) — не роняем объект из-за этого.
            shopperSystem?.AddShopper(this);
        }

        private void OnDisable()
        {
            shopperSystem?.RemoveShopper(this);
        }

        private void Update()
        {
            Vector3 dir;
            if (IsMoving == false)
            {
                // The camera is cached on Awake, but it is destroyed and recreated on every scene
                // change. Without the refresh below a shopper threw a NullReferenceException for
                // every frame it stood still after the first scene transition.
                if (TryGetCamera() == false)
                {
                    return;
                }

                dir = mainCamera.transform.position - transform.position;
            }
            else
            {
                dir = agent.destination - transform.position;
            }

            dir.y = 0;

            if (dir == Vector3.zero)
            {
                return;
            }

            var rot = Quaternion.LookRotation(dir);
            transform.rotation = rot;
        }

        public void Initialize(ShopperData data)
        {
            // Data is a mutable clone, so can modify
            Data = data;

            if (bodyRenderer == false || data == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            block.SetTexture(texturePropertyId, data.Image);
            bodyRenderer.SetPropertyBlock(block);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void Move(Vector3 position)
        {
            if (agent == false)
            {
                return;
            }

            agent.SetDestination(position);
        }

        public PurchaseRequest PopPurchaseRequest()
        {
            var (purchase, keyword) = PopPurchaseLine(Data.PurchaseCollection.Purchases);

            // No lines at all left to say: the shopper just came to make a scene.
            if (keyword == null)
            {
                return new PurchaseRequest(
                    text: string.Empty,
                    wantedItems: null,
                    shopper: this
                );
            }

            var requestText = purchase.TemplateText.Replace(keywordToken, keyword.Text);

            // The shopper accepts any product named by the keyword, so a request that lists
            // several goods can be served with whichever of them the player picks up. A keyword
            // without items means the shopper only came to rant.
            return new PurchaseRequest(
                text: requestText,
                wantedItems: keyword.Items.Distinct().ToList(),
                shopper: this
            );
        }

        /// <summary>
        /// Pick one unused line and mark it as used. A whole shift can be longer than the line
        /// bank, so once everything is used up the lines start over instead of running dry.
        /// </summary>
        private static (PurchaseCollection.Purchase Purchase, Keyword Keyword) PopPurchaseLine(
            IReadOnlyCollection<PurchaseCollection.Purchase> purchases,
            bool isRetry = false)
        {
            if (purchases.Count <= 0)
            {
                return default;
            }

            var candidates = new List<(PurchaseCollection.Purchase Purchase, Keyword Keyword)>();

            foreach (var purchase in purchases)
            {
                foreach (var keyword in purchase.Keywords)
                {
                    if (keyword.IsUsed == false)
                    {
                        candidates.Add((purchase, keyword));
                    }
                }
            }

            if (candidates.Count <= 0)
            {
                if (isRetry)
                {
                    return default;
                }

                RefreshPurchaseLines(purchases);

                return PopPurchaseLine(purchases, isRetry: true);
            }

            var chosen = candidates.GetRandom();
            chosen.Keyword.IsUsed = true;

            return chosen;
        }

        public void PlayBuyAnimation()
        {
            OnBuyStart.Invoke();

            if (buyAnimation == false)
            {
                return;
            }

            buyAnimation.gameObject.SetActive(true);
            buyAnimation.Play("Animation_Shopper_Money");
        }

        public void StopBuyAnimation()
        {
            OnBuyStop.Invoke();

            if (buyAnimation == false)
            {
                return;
            }

            buyAnimation.gameObject.SetActive(false);
        }

        public void PlayPunchAnimation()
        {
            onPunchStart.Invoke();

            if (punchAnimation == false)
            {
                return;
            }

            punchAnimation.gameObject.SetActive(true);
            punchAnimation.Play("Animation_Shopper_Fist");
        }

        public void StopPunchAnimation()
        {
            onPunchStop.Invoke();

            if (punchAnimation == false)
            {
                return;
            }

            punchAnimation.gameObject.SetActive(false);
        }

        public void PlayWalkAnimation()
        {
            if (walkAnimation)
            {
                walkAnimation.enabled = true;
                walkAnimation.Play("Animation_Shooper_Walk_Jump", -1, 0f);
            }

            OnMoveStart.Invoke();
        }

        public void StopWalkAnimation()
        {
            if (walkAnimation)
            {
                walkAnimation.Play(walkAnimation.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);
                walkAnimation.Update(0f);
                walkAnimation.enabled = false;
            }

            OnMoveStop.Invoke();
        }

        public void PlaySpeech()
        {
            if (speechAudioEmitter == false)
            {
                return;
            }

            speechAudioEmitter.Play();
        }

        public void StopSpeech()
        {
            if (speechAudioEmitter == false)
            {
                return;
            }

            speechAudioEmitter.Stop();
        }

        /// <summary>
        /// Reset every used-up line so the shop keeps having something to ask for.
        /// </summary>
        private static void RefreshPurchaseLines(IReadOnlyCollection<PurchaseCollection.Purchase> purchases)
        {
            foreach (var purchase in purchases)
            {
                foreach (var keyword in purchase.Keywords)
                {
                    keyword.IsUsed = false;
                }
            }
        }

        public void ShowRequest(PurchaseRequest purchase)
        {
            if (requestCard == false)
            {
                return;
            }

            requestCard.SetActive(true);

            // Реплика дублируется над головой покупателя: игрок читает заказ, не глядя
            // в угол экрана.
            if (speechBubble != false)
            {
                speechBubble.SetActive(true);

                if (speechText != false && purchase != default)
                {
                    speechText.text = purchase.Text;
                }
            }

            if (requestRenderer == false || purchase == default)
            {
                return;
            }

            var item = purchase.PrimaryItem;
            if (item == false)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            block.SetTexture(requestTexturePropertyId, item.Image);
            requestRenderer.SetPropertyBlock(block);
        }

        public void HideRequest()
        {
            if (requestCard == false)
            {
                return;
            }

            requestCard.SetActive(false);

            if (speechBubble != false)
            {
                speechBubble.SetActive(false);
            }
        }
    }
}
