using System.Collections.Generic;
using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Components.Input;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Cursors;
using UABPetelnia.GGJ2025.Runtime.Systems.Gameplay;
using UABPetelnia.GGJ2025.Runtime.Systems.Input;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UABPetelnia.GGJ2025.Runtime.Systems.Pausing;
using UABPetelnia.GGJ2025.Runtime.Systems.Products;
using UABPetelnia.GGJ2025.Runtime.Systems.Saves;
using UABPetelnia.GGJ2025.Runtime.Systems.Shoppers;
using UABPetelnia.GGJ2025.Runtime.UI.Controllers;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    internal sealed class DesktopPlayerActor : MonoBehaviour, IPlayerActor
    {
        [Header("General")]
        [SerializeField]
        private PlayerSettings settings;

        [SerializeField]
        private GrabInteractor grabInteractor;

        [Header("Cameras")]
        [SerializeField]
        private CinemachineCamera cinemachineCamera;

        [SerializeField]
        private CinemachineImpulseSource cinemachineImpulse;

        [Header("Rendering")]
        [SerializeField]
        private Renderer itemRenderer;

        [SerializeField]
        private string itemTexturePropertyId = "_BaseMap";

        [SerializeField]
        private Renderer bodyRenderer;

        [SerializeField]
        private string bodyTexturePropertyId = "_BaseMap";

        [Header("UI")]
        [SerializeField]
        private Animator giveAnimation;

        [FormerlySerializedAs("gameplayViewController")]
        [SerializeField]
        private ChatViewController chatViewController;

        [SerializeField]
        private DeliveryViewController deliveryViewController;

        [SerializeField]
        private HandsViewController handsViewController;

        /// <summary>Таблички денег и времени: прячутся за экраном ПК и вне геймплея.</summary>
        private StatusHudViewController statusHud;

        /// <summary>Меню плеера с наушниками: открывается по E под прицелом.</summary>
        private MusicPlayerViewController musicPlayer;

        [SerializeField]
        private DeliveryBoxViewController deliveryBoxViewController;

        [Header("Input")]
        [SerializeField]
        private ButtonInputActionListener zoomInputListener;

        [SerializeField]
        private ButtonInputActionListener selectListener;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onHealthChanged;

        [SerializeField]
        private UnityEvent onCentsChanged;

        [SerializeField]
        private UnityEvent onItemsDisappear;

        private Rigidbody rb;
        private CharacterController characterController;
        private bool hasLoggedMoveInputWarning;
        private bool hasLoggedMoveInput;

        private IShopperSystem shopperSystem;
        private IProductSystem productSystem;
        private IPauseSystem pauseSystem;
        private IGameplaySystem gameplaySystem;
        private IPlayerSystem playerSystem;
        private ICursorSystem cursorSystem;
        private ISaveSystem saveSystem;
        private IInputSystem inputSystem;

        /// <summary>
        /// Слот рук: товар и его физический объект. Пока слот не выбран клавишей 1/2,
        /// объект выключен и ждёт своей очереди в слоте.
        /// </summary>
        private sealed class CarrySlot
        {
            public ItemData Item;

            public ProductActor Product;
        }

        private readonly List<CarrySlot> carrySlots = new();

        private readonly List<ItemData> hudItems = new();

        /// <summary>
        /// Активный слот рук (0/1): выделяется рамкой, его товар лежит в руке.
        /// Переключается клавишами 1/2.
        /// </summary>
        private int activeSlot;

        public const int MaxCarrySlots = 2;

        /// <summary>Колесо мыши двигает взятый предмет к себе и от себя.</summary>
        private const float MinCarryDistance = -0.35f;

        private const float MaxCarryDistance = 0.6f;

        /// <summary>Радиус выкладки товара из руки на полку.</summary>
        private const float ShelfPlaceRadius = 0.7f;

        private const float CameraEyeHeight = 1.75f;
        private const float CrouchCameraEyeHeight = 1.2f;
        private const float MinimumCameraHeight = 1.2f;
        private const float GroundStickSpeed = 2f;

        /// <summary>Множитель скорости бега на Shift.</summary>
        private const float SprintMultiplier = 1.7f;

        /// <summary>Множитель скорости на Ctrl.</summary>
        private const float CrouchSpeedMultiplier = 0.5f;

        private float initialFov;
        private float currentFov;
        private float targetFov;

        private int currentHealth;
        private int currentCents;

        public IReadOnlyList<ItemData> CarriedItems
        {
            get
            {
                var items = new List<ItemData>(carrySlots.Count);

                foreach (var slot in carrySlots)
                {
                    items.Add(slot.Item);
                }

                return items;
            }
        }

        public bool HasCarriedItem => carrySlots.Count > 0;

        public bool HasFreeCarrySlot => carrySlots.Count < MaxCarrySlots;

        public int ActiveSlot => activeSlot;

        public bool TryGetCarriedItem(int slotIndex, out ItemData item)
        {
            if (slotIndex < 0 || slotIndex >= carrySlots.Count)
            {
                item = default;
                return false;
            }

            item = carrySlots[slotIndex].Item;
            return item != false;
        }

        public int Health
        {
            get => currentHealth;
            set
            {
                currentHealth = value;
                currentHealth = Mathf.Max(currentHealth, 0);

                OnCurrentHealthChanged();
            }
        }

        public int Cents
        {
            get => currentCents;
            set
            {
                currentCents = Mathf.Max(0, value);
                GameManager.Publish(new PlayerCentsChanged(this));
                onCentsChanged?.Invoke();
            }
        }

        public bool IsCentsGoalReached => settings != false && settings.GoalCents <= currentCents;

        private void Awake()
        {
            if (settings != false && settings.MaxHealth > 0)
            {
                currentHealth = settings.MaxHealth;
            }
            else
            {
                currentHealth = 3;
                Debug.LogWarning("[Player] PlayerSettings is not assigned; using fallback health value.", this);
            }

            shopperSystem = GameManager.GetSystem<IShopperSystem>();
            gameplaySystem = GameManager.GetSystem<IGameplaySystem>();
            playerSystem = GameManager.GetSystem<IPlayerSystem>();
            cursorSystem = GameManager.GetSystem<ICursorSystem>();
            saveSystem = GameManager.GetSystem<ISaveSystem>();
            inputSystem = GameManager.GetSystem<IInputSystem>();

            // The giving hand lives on an object that is active in the scene, so it has to be
            // hidden before the first frame is rendered and not just on Start().
            StopGiveAnimation();
        }

        private void ApplyPendingSave()
        {
            if (saveSystem == null || saveSystem.TryConsumePendingLoad(out var data) == false)
            {
                return;
            }

            Cents = data.Cents;
            Health = data.Health;
        }

        private void Start()
        {
            ResolvePlayerCamera();

            if (cinemachineCamera == false)
            {
                Debug.LogError(
                    "[Player] CinemachineCamera не назначена: зум и тряска камеры работать не будут.",
                    this
                );
            }
            else
            {
                initialFov = cinemachineCamera.Lens.FieldOfView;
                targetFov = cinemachineCamera.Lens.FieldOfView;
                currentFov = cinemachineCamera.Lens.FieldOfView;
            }

            rb = GetComponent<Rigidbody>();
            characterController = GetComponent<CharacterController>();
            EnsurePlayerCameraPlacement();

            // The giving hand is only supposed to be visible while an item is handed over,
            // so make sure it starts hidden.
            StopGiveAnimation();

            inputSystem?.EnablePlayerInput();

            ApplyPendingSave();
            RefreshHandsHud();

            gameplaySystem?.StartGameplay();
            cursorSystem?.LockCursor();
        }

        private void OnEnable()
        {
            playerSystem?.AddPlayer(this);

            if (zoomInputListener)
            {
                zoomInputListener.OnPerformed += OnZoomPerformed;
                zoomInputListener.OnCanceled += OnZoomCanceled;
            }

            if (selectListener)
            {
                selectListener.OnPerformed += OnSelectPerformed;
            }

            if (chatViewController)
            {
                chatViewController.OnSpeechEntered += OnSpeechEntered;
                chatViewController.OnSpeechExited += OnSpeechExited;
            }
        }

        private void OnDisable()
        {
            playerSystem?.RemovePlayer(this);

            if (zoomInputListener)
            {
                zoomInputListener.OnPerformed -= OnZoomPerformed;
                zoomInputListener.OnCanceled -= OnZoomCanceled;
            }

            if (selectListener)
            {
                selectListener.OnPerformed -= OnSelectPerformed;
            }

            if (chatViewController)
            {
                chatViewController.OnSpeechEntered -= OnSpeechEntered;
                chatViewController.OnSpeechExited -= OnSpeechExited;
            }
        }

        private void OnDestroy()
        {
            cursorSystem?.UnLockCursor();
        }

        private void Update()
        {
            EnsurePlayerCameraPlacement();
            UpdateCameraZoom();
            UpdateMovement();
            UpdateGiveAnimation();
            UpdateSlotInput();
            UpdateBoxInput();
            UpdateCarryDistance();
            UpdateHandsVisibility();
        }

        /// <summary>
        /// Колесо мыши: приблизить или отдалить предмет в руке, чтобы удобнее было
        /// целиться в полку при раскладке товара.
        /// </summary>
        private void UpdateCarryDistance()
        {
            if (grabInteractor == false)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            grabInteractor.CarryDistance = Mathf.Clamp(
                grabInteractor.CarryDistance + scroll * 0.0015f,
                MinCarryDistance,
                MaxCarryDistance
            );
        }

        /// <summary>
        /// Слоты рук и таблички денег/времени не должны висеть поверх меню паузы,
        /// экрана ПК и главного меню.
        /// </summary>
        private void UpdateHandsVisibility()
        {
            var isMenuOpen = IsDeliveryPanelOpen;

            if (isMenuOpen == false)
            {
                if (pauseSystem == null)
                {
                    SystemsUtility.TryGetSystem(out pauseSystem);
                }

                isMenuOpen = pauseSystem != null && pauseSystem.IsPaused;
            }

            if (handsViewController != false)
            {
                handsViewController.SetVisible(isMenuOpen == false);
            }

            if (statusHud == false)
            {
                statusHud = GetComponentInChildren<StatusHudViewController>(true);
            }

            statusHud?.SetSuppressed(isMenuOpen);
        }

        /// <summary>
        /// Переключение активного слота рук клавишами 1/2. Выбранный слот подсвечивается
        /// рамкой в HUD, а его товар сразу появляется в руке.
        /// </summary>
        private void UpdateSlotInput()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SetActiveSlot(0);
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SetActiveSlot(1);
            }
        }

        /// <summary>
        /// Клавиша X: положить коробку доставки из рук на пол. Работает и при открытом
        /// меню коробки — меню закрывается вместе с ней.
        /// </summary>
        private void UpdateBoxInput()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || keyboard.xKey.wasPressedThisFrame == false)
            {
                return;
            }

            PutDownHeldBox();
        }

        /// <summary>
        /// Опустить держимую коробку на пол. Коробку нельзя «поставить»: интерактор
        /// отпускает её, дальше Rigidbody кладёт на пол.
        /// </summary>
        private void PutDownHeldBox()
        {
            if (pauseSystem == null)
            {
                SystemsUtility.TryGetSystem(out pauseSystem);
            }

            if (pauseSystem != null && pauseSystem.IsPaused)
            {
                return;
            }

            // За экраном ПК закупок класть коробку нечего: X там не работает.
            if (deliveryViewController != false && deliveryViewController.IsOpen)
            {
                return;
            }

            if (grabInteractor == false || grabInteractor.HeldInteractable == false)
            {
                return;
            }

            var heldBox = grabInteractor.HeldInteractable.GetComponentInParent<DeliveryBoxActor>();
            if (heldBox == false)
            {
                return;
            }

            // Меню коробки закрываем вместе с коробкой.
            if (deliveryBoxViewController != false && deliveryBoxViewController.IsOpen)
            {
                deliveryBoxViewController.Close();
            }

            grabInteractor.Deselect();
        }

        private void SetActiveSlot(int slot)
        {
            var clamped = Mathf.Clamp(slot, 0, MaxCarrySlots - 1);

            // Повторное нажатие по активному слоту убирает товар из руки в инвентарь.
            if (clamped == activeSlot && IsSlotInHand(clamped))
            {
                StowSlotProduct(carrySlots[clamped].Product);
                RefreshHandsHud();

                return;
            }

            activeSlot = clamped;

            SyncHandWithActiveSlot();
            RefreshHandsHud();
        }

        /// <returns><c>true</c>, если товар этого слота сейчас в руке.</returns>
        private bool IsSlotInHand(int index)
        {
            if (index < 0 || index >= carrySlots.Count)
            {
                return false;
            }

            var product = carrySlots[index].Product;

            if (product == false || grabInteractor == false || product.gameObject.activeSelf == false)
            {
                return false;
            }

            var interactable = product.GetComponentInChildren<GrabInteractable>(true);

            return interactable != false && grabInteractor.HeldInteractable == interactable;
        }

        /// <summary>
        /// Приводит руку в соответствие с активным слотом: товар выбранного слота достаётся
        /// в руку, остальные товары остаются спрятанными в своих слотах.
        /// </summary>
        private void SyncHandWithActiveSlot()
        {
            if (grabInteractor == false)
            {
                return;
            }

            // Сначала прячем все неактивные слоты: рука освобождается ДО того,
            // как активный товар пробует в неё вернуться.
            for (var index = 0; index < carrySlots.Count; index++)
            {
                if (index == activeSlot)
                {
                    continue;
                }

                var stowedProduct = carrySlots[index].Product;
                if (stowedProduct != false)
                {
                    StowSlotProduct(stowedProduct);
                }
            }

            if (activeSlot < 0 || activeSlot >= carrySlots.Count)
            {
                return;
            }

            var activeProduct = carrySlots[activeSlot].Product;
            if (activeProduct != false)
            {
                TakeSlotProductIntoHand(activeProduct);
            }
        }

        /// <summary>Достаёт товар из слота в руку: включает объект и отдаёт его интерактору.</summary>
        private void TakeSlotProductIntoHand(ProductActor product)
        {
            var interactable = product.GetComponentInChildren<GrabInteractable>(true);
            if (interactable == false)
            {
                return;
            }

            product.gameObject.SetActive(true);

            if (grabInteractor.TryForceSelect(interactable) == false)
            {
                // Рука занята другим предметом: товар остаётся ждать в слоте.
                product.gameObject.SetActive(false);
            }
        }

        /// <summary>Прячет товар назад в слот: интерактор отпускает объект, объект выключается.</summary>
        private void StowSlotProduct(ProductActor product)
        {
            var interactable = product.GetComponentInChildren<GrabInteractable>(true);
            if (interactable != false)
            {
                grabInteractor.Deselect(interactable);
            }

            product.gameObject.SetActive(false);
        }

        private void ResolvePlayerCamera()
        {
            if (cinemachineCamera == false)
            {
                cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
            }

            if (cinemachineCamera == false)
            {
                var fallback = transform.Find("CinemachineCamera");
                if (fallback != null)
                {
                    cinemachineCamera = fallback.GetComponent<CinemachineCamera>();
                }
            }

            if (cinemachineCamera != false && cinemachineCamera.transform.parent != transform)
            {
                cinemachineCamera.transform.SetParent(transform, worldPositionStays: false);
            }
        }

        private void EnsurePlayerCameraPlacement()
        {
            ResolvePlayerCamera();

            if (cinemachineCamera == false)
            {
                return;
            }

            if (cinemachineCamera.transform.parent != transform)
            {
                cinemachineCamera.transform.SetParent(transform, worldPositionStays: false);
            }

            var localPosition = cinemachineCamera.transform.localPosition;
            localPosition.x = 0f;
            localPosition.y = Mathf.Max(localPosition.y, MinimumCameraHeight);
            localPosition.y = Mathf.Max(localPosition.y, GetTargetEyeHeight());
            localPosition.z = Mathf.Abs(localPosition.z) < 0.05f ? 0.2f : localPosition.z;
            var localRotation = cinemachineCamera.transform.localRotation.eulerAngles;
            localRotation.x = Mathf.Clamp(localRotation.x, -89f, 89f);

            // Пишем в транформ только при реальном изменении: эта проверка вызывается каждый
            // кадр, а каждая запись помечает транформ камеры грязным и дёргает Cinemachine.
            if (localPosition != cinemachineCamera.transform.localPosition)
            {
                cinemachineCamera.transform.localPosition = localPosition;
            }

            var targetRotation = Quaternion.Euler(localRotation);
            if (targetRotation != cinemachineCamera.transform.localRotation)
            {
                cinemachineCamera.transform.localRotation = targetRotation;
            }
        }

        /// <summary>
        /// Hides the giving hand once its single-shot animation has finished, so it can never
        /// stay stretched towards the shoppers.
        /// </summary>
        private void UpdateGiveAnimation()
        {
            if (giveAnimation == false || giveAnimation.gameObject.activeSelf == false)
            {
                return;
            }

            if (giveAnimation.runtimeAnimatorController == false)
            {
                return;
            }

            var state = giveAnimation.GetCurrentAnimatorStateInfo(0);
            if (state.loop || state.normalizedTime < 1f)
            {
                return;
            }

            StopGiveAnimation();
        }

        private Vector2 ReadMoveInput()
        {
            var moveInput = inputSystem != null ? inputSystem.MoveInput : Vector2.zero;
            if (moveInput.sqrMagnitude > 0.01f)
            {
                if (hasLoggedMoveInput == false)
                {
                    hasLoggedMoveInput = true;

                    Debug.Log($"Move action input detected: {moveInput}", this);
                }

                return moveInput;
            }

            var fallback = ReadFallbackMoveInput();
            if (fallback.sqrMagnitude <= 0.01f)
            {
                return moveInput;
            }

            if (hasLoggedMoveInputWarning == false)
            {
                hasLoggedMoveInputWarning = true;

                Debug.LogWarning(
                    "The Move input action did not report any input, falling back to raw device "
                    + "reads for player movement.",
                    this
                );
            }

            return fallback;
        }

        /// <summary>
        /// Raw device read used when the "Move" action is unavailable (for example when the
        /// action map did not get enabled, or when entering play mode with domain reload
        /// disabled). Without it the player would silently lose the ability to walk.
        /// </summary>
        private static Vector2 ReadFallbackMoveInput()
        {
            var horizontal = 0f;
            var vertical = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    horizontal += 1f;
                }

                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    horizontal -= 1f;
                }

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    vertical += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    vertical -= 1f;
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                var stick = gamepad.leftStick.ReadValue();
                horizontal += stick.x;
                vertical += stick.y;
            }

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private void UpdateMovement()
        {
            if (IsDeliveryPanelOpen)
            {
                // The ordering panel is a mouse-driven screen, walking around while it is up would
                // drag the camera away from it.
                return;
            }

            var moveInput = ReadMoveInput();
            if (moveInput.magnitude < 0.1f || cinemachineCamera == false || settings == false)
            {
                // Standing still still has to keep the player glued to the kiosk floor.
                MovePlayer(GetGroundStickDelta());
                return;
            }

            var forward = cinemachineCamera.transform.forward;
            var right = cinemachineCamera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward = forward.normalized;
            right = right.normalized;

            if (forward == Vector3.zero)
            {
                // Camera looks straight down, fall back to the player's own orientation.
                forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            }

            if (right == Vector3.zero)
            {
                right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            }

            var moveDirection = forward * moveInput.y + right * moveInput.x;
            var speed = settings.MoveSpeed * GetSpeedMultiplier();
            MovePlayer((moveDirection * speed * Time.deltaTime) + GetGroundStickDelta());

            if (moveDirection == Vector3.zero || IsCameraAttachedToPlayer)
            {
                // Camera is parented to the player, so turning the player would also turn the
                // camera. Since movement is camera-relative that would make the player spiral,
                // hence the player is only translated.
                return;
            }

            // Exponential smoothing, so turning feels the same at 60 and at 144 fps.
            var turnSpeed = 1f - Mathf.Exp(-10f * Time.deltaTime);
            var targetRotation = Quaternion.LookRotation(moveDirection);
            SetRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed));
        }

        /// <summary>
        /// Скорость ходьбы с учётом Shift (бег) и Ctrl (присед).
        /// </summary>
        private float GetSpeedMultiplier()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return 1f;
            }

            var isCrouching = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            if (isCrouching)
            {
                return CrouchSpeedMultiplier;
            }

            var isSprinting = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

            return isSprinting ? SprintMultiplier : 1f;
        }

        /// <summary>Высота глаз: на Ctrl игрок приседает.</summary>
        private float GetTargetEyeHeight()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return CameraEyeHeight;
            }

            var isCrouching = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;

            return isCrouching ? CrouchCameraEyeHeight : CameraEyeHeight;
        }

        /// <summary>
        /// A small downward push that keeps the capsule on the kiosk floor instead of drifting over
        /// the counter or the shelves.
        /// </summary>
        private static Vector3 GetGroundStickDelta()
        {
            return Vector3.down * (GroundStickSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Moves the player through the <see cref="CharacterController"/> when one is present, so
        /// the kiosk walls, counter and shelves really stop them. Falls back to the older
        /// translate-only movement when no controller is set up.
        /// </summary>
        private void MovePlayer(Vector3 delta)
        {
            if (characterController != false && characterController.enabled)
            {
                characterController.Move(delta);
                return;
            }

            SetPosition(Position + delta);
        }

        private bool IsCameraAttachedToPlayer =>
            cinemachineCamera != false && cinemachineCamera.transform.IsChildOf(transform);

        private Vector3 Position => rb != null ? rb.position : transform.position;

        private void SetPosition(Vector3 position)
        {
            if (rb != null)
            {
                rb.MovePosition(position);
                return;
            }

            transform.position = position;
        }

        private void SetRotation(Quaternion rotation)
        {
            if (rb != null)
            {
                rb.rotation = rotation;
                return;
            }

            transform.rotation = rotation;
        }

        private void UpdateCameraZoom()
        {
            if (cinemachineCamera == false || settings == false)
            {
                return;
            }

            var zoomSpeed = 1f - Mathf.Exp(-settings.ZoomInSpeed * Time.deltaTime);
            currentFov = Mathf.Lerp(currentFov, targetFov, zoomSpeed);

            cinemachineCamera.Lens.FieldOfView = currentFov;
        }


        private void OnZoomPerformed(bool value)
        {
            StartZoomingIn();
        }

        /// <summary>
        /// Одна кнопка взаимодействия ведёт весь цикл торговли: отдать товар из руки покупателю
        /// под прицелом, иначе вернуть его на полку, взять товар с полки в слот или подобрать
        /// прочий предмет вроде тамагочи.
        /// </summary>
        private void OnSelectPerformed(bool value)
        {
            if (IsDeliveryPanelOpen || grabInteractor == false)
            {
                return;
            }

            // Товар в руке: покупателю под прицелом, иначе — назад на полку.
            if (TryHandOverHeldProduct())
            {
                return;
            }

            var heldProduct = GetHeldProduct();
            if (heldProduct)
            {
                // Покупатель под прицелом, но этот товар ему не нужен: оставляем его в руке.
                if (IsShopperUnderCrosshair())
                {
                    return;
                }

                // Сначала пробуем поставить товар на свободную полку рядом (выкладка),
                // и только потом просто отпускаем.
                if (TryPlaceHeldProductOnShelf(heldProduct))
                {
                    return;
                }

                DropHeldProduct(heldProduct);
                return;
            }

            if (grabInteractor.HeldInteractable)
            {
                // Коробка с доставкой: по E открывается её меню со списком товара.
                var heldBox = grabInteractor.HeldInteractable.GetComponentInParent<DeliveryBoxActor>();
                if (heldBox != false)
                {
                    if (deliveryBoxViewController != false)
                    {
                        deliveryBoxViewController.Toggle(heldBox);
                    }

                    return;
                }

                // Something that is not goods, like the tamagotchi toy: put it back down and let
                // the object itself walk home.
                grabInteractor.Deselect();
                return;
            }

            // Рука пуста: плеер открывает меню музыки, товар с полки уходит в слот,
            // всё остальное берётся в руку как раньше.
            if (TryToggleMusicPlayerMenu())
            {
                return;
            }

            if (TryTakeProductIntoSlot())
            {
                return;
            }

            if (grabInteractor.IsHovering)
            {
                PlayTakeAnimation();
                grabInteractor.Select();

                // Коробку курьера берём в руки и сразу показываем её содержимое.
                var box = grabInteractor.HeldInteractable != null
                    ? grabInteractor.HeldInteractable.GetComponentInParent<DeliveryBoxActor>()
                    : default;

                if (box != false && deliveryBoxViewController != false)
                {
                    deliveryBoxViewController.Open(box);
                }
            }
        }

        /// <summary>
        /// Плеер с наушниками — не товар: по E на нём открывается меню выбора музыки.
        /// </summary>
        private bool TryToggleMusicPlayerMenu()
        {
            var hovered = grabInteractor != false ? grabInteractor.HoveredProduct : default;

            if (hovered == false || hovered.GetComponent<MusicPlayerActor>() == false)
            {
                return false;
            }

            if (musicPlayer == false)
            {
                musicPlayer = GetComponentInChildren<MusicPlayerViewController>(true);
            }

            musicPlayer?.Toggle();

            return true;
        }

        /// <summary>Короткий взмах рукой, когда игрок берёт товар с полки.</summary>
        private void PlayTakeAnimation()
        {
            if (giveAnimation == false || grabInteractor == false)
            {
                return;
            }

            var product = grabInteractor.HoveredProduct;
            PlayGiveAnimation(product ? product.Item : default);
        }

        /// <summary>
        /// <c>true</c> while the counter PC panel is open.
        /// </summary>
        private bool IsDeliveryPanelOpen =>
            (deliveryViewController != false && deliveryViewController.IsOpen)
            || (deliveryBoxViewController != false && deliveryBoxViewController.IsOpen);

        private ProductActor GetHeldProduct()
        {
            if (grabInteractor == false)
            {
                return default;
            }

            var heldInteractable = grabInteractor.HeldInteractable;
            if (heldInteractable == false)
            {
                return default;
            }

            return heldInteractable.GetComponentInParent<ProductActor>();
        }

        /// <summary>
        /// Берёт товар под прицелом с полки и сразу прячет его в свободный слот рук.
        /// В руку товар попадёт только после выбора слота клавишей 1/2.
        /// </summary>
        /// <returns><c>true</c>, когда под прицелом был товар (даже если слоты заняты).</returns>
        private bool TryTakeProductIntoSlot()
        {
            var product = grabInteractor.HoveredProduct;
            if (product == false || product.Item == false)
            {
                return false;
            }

            // Этот товар уже лежит в слоте: второй раз его не берём.
            if (IndexOfCarriedItem(product.Item) >= 0)
            {
                return true;
            }

            if (carrySlots.Count >= MaxCarrySlots)
            {
                return true;
            }

            PlayTakeAnimation();

            // Товар уходит с полки прямо в руку: в инвентарь он попадёт только по клавише слота.
            product.PlaceOn(default);

            carrySlots.Add(new CarrySlot { Item = product.Item, Product = product });
            activeSlot = carrySlots.Count - 1;

            // Активным становится новый слот: прежний предмет из руки прячется. Прямой
            // TakeSlotProductIntoHand оставлял бы прежний товар выбранным: интерактор
            // держал оба, а переключение слотов прятало «занятые» предметы в никуда.
            SyncHandWithActiveSlot();
            RefreshHandsHud();
            return true;
        }

        /// <summary>
        /// Взять товар из коробки в свободный слот рук: физический товар создаётся сразу,
        /// но ждёт в слоте и появится в руке только по клавише 1/2.
        /// </summary>
        public bool TryCarryItem(ItemData item)
        {
            if (item == false || carrySlots.Count >= MaxCarrySlots)
            {
                return false;
            }

            if (IndexOfCarriedItem(item) >= 0)
            {
                return false;
            }

            var system = ResolveProductSystem();
            var product = system != null ? system.SpawnLooseProduct(item, transform.position) : default;

            if (product == false)
            {
                return false;
            }

            product.gameObject.SetActive(false);

            carrySlots.Add(new CarrySlot { Item = item, Product = product });

            // Товар из коробки сразу показываем в руке: «взял — и несёт». Без этого
            // предмет молча выключался в слоте, рука выглядела пустой и выставлять
            // товар на полки было невозможно, пока игрок не догадается нажать 1/2.
            activeSlot = carrySlots.Count - 1;
            SyncHandWithActiveSlot();
            RefreshHandsHud();

            return true;
        }

        private IProductSystem ResolveProductSystem()
        {
            if (productSystem != null)
            {
                return productSystem;
            }

            SystemsUtility.TryGetSystem(out productSystem);

            return productSystem;
        }

        /// <summary>
        /// Поставить товар из руки на свободную полку рядом: так игрок выкладывает товар
        /// из коробки доставки. Радиус чуть больше магнита, но требование к прицелу строже.
        /// </summary>
        /// <returns><c>true</c>, если товар встал на полку.</returns>
        private bool TryPlaceHeldProductOnShelf(ProductActor product)
        {
            var system = ResolveProductSystem();

            if (system == null || product == false)
            {
                return false;
            }

            var aim = GetAimDirection();

            if (system.TryFindFreeShelfPoint(product.transform.position, aim, ShelfPlaceRadius, out var shelfPoint) == false)
            {
                return false;
            }

            // Ставим только туда, куда игрок действительно смотрит.
            var toSlot = shelfPoint.Position - product.transform.position;

            if (toSlot.sqrMagnitude > 0.001f && Vector3.Dot(toSlot.normalized, aim) < 0.75f)
            {
                return false;
            }

            var slotIndex = IndexOfCarriedItem(product.Item);
            if (slotIndex >= 0)
            {
                carrySlots.RemoveAt(slotIndex);
            }

            var interactable = product.GetComponentInChildren<GrabInteractable>(true);
            if (interactable != false)
            {
                grabInteractor.Deselect(interactable);
            }

            product.PlaceOn(shelfPoint);

            activeSlot = Mathf.Clamp(activeSlot, 0, Mathf.Max(0, carrySlots.Count - 1));
            RefreshHandsHud();

            return true;
        }

        /// <summary>
        /// Отпустить товар из руки: рядом со свободной полкой он сам встанет на место
        /// (магнит), иначе просто упадёт и останется лежать в мире.
        /// </summary>
        private void DropHeldProduct(ProductActor product)
        {
            var slotIndex = IndexOfCarriedItem(product.Item);
            if (slotIndex >= 0)
            {
                carrySlots.RemoveAt(slotIndex);
            }

            var interactable = product.GetComponentInChildren<GrabInteractable>(true);
            if (interactable != false)
            {
                grabInteractor.Deselect(interactable);
            }

            product.Drop(GetAimDirection());

            activeSlot = Mathf.Clamp(activeSlot, 0, Mathf.Max(0, carrySlots.Count - 1));
            RefreshHandsHud();
        }

        /// <summary>Направление взгляда игрока: по нему товар выбирает полку при отпускании.</summary>
        private Vector3 GetAimDirection()
        {
            var origin = grabInteractor != false ? grabInteractor.RaycastOrigin : null;

            return origin != false ? origin.forward : transform.forward;
        }

        private int IndexOfCarriedItem(ItemData item)
        {
            for (var index = 0; index < carrySlots.Count; index++)
            {
                if (carrySlots[index].Item == item)
                {
                    return index;
                }
            }

            return -1;
        }

        private void RefreshHandsHud()
        {
            if (handsViewController == false)
            {
                return;
            }

            hudItems.Clear();

            foreach (var slot in carrySlots)
            {
                hudItems.Add(slot.Item);
            }

            handsViewController.SetItems(hudItems);
            handsViewController.SetSelectedSlot(activeSlot);
        }

        private bool TryHandOverProduct(ItemData item)
        {
            if (item == false)
            {
                return false;
            }

            if (shopperSystem == null || shopperSystem.IsAwaitingItem == false)
            {
                return false;
            }

            var slotIndex = IndexOfCarriedItem(item);
            if (slotIndex < 0)
            {
                return false;
            }

            var product = carrySlots[slotIndex].Product;
            carrySlots.RemoveAt(slotIndex);

            // Активный слот не должен указывать в пустоту после отдачи товара.
            activeSlot = Mathf.Clamp(activeSlot, 0, Mathf.Max(0, carrySlots.Count - 1));
            RefreshHandsHud();

            if (product != false)
            {
                var interactable = product.GetComponentInChildren<GrabInteractable>(true);
                if (interactable != false)
                {
                    grabInteractor.Deselect(interactable);
                }
            }

            PlayGiveAnimation(item);
            GameManager.Publish(new ItemHandedOverMessage(item));

            if (product != false)
            {
                product.Consume();
            }

            return true;
        }

        /// <returns>
        /// <c>true</c> when the product in the hand was handed over to the shopper at the counter.
        /// Отдача идёт только из руки: сначала товар достаётся из слота клавишей 1/2.
        /// </returns>
        private bool TryHandOverHeldProduct()
        {
            if (IsShopperUnderCrosshair() == false)
            {
                // Никакой отдачи «в никуда»: товар уходит только покупателю под прицелом.
                return false;
            }

            var product = GetHeldProduct();
            if (product == false || product.Item == false)
            {
                return false;
            }

            return TryHandOverProduct(product.Item);
        }

        /// <summary>
        /// Покупатель под перекрестьем? Рейкаст из камеры по слою Default (там стоят
        /// коллайдеры покупателей): если ближе всего стена/полка — отдачи не будет.
        /// </summary>
        private bool IsShopperUnderCrosshair()
        {
            if (grabInteractor == false)
            {
                return false;
            }

            var origin = grabInteractor.RaycastOrigin;
            if (origin == false)
            {
                return false;
            }

            var isHit = Physics.SphereCast(
                origin.position,
                0.25f,
                origin.forward,
                out var hit,
                5f,
                1 << 0
            );

            return isHit && hit.collider.GetComponentInParent<ShopperActor>() != false;
        }

        private void OnZoomCanceled(bool value)
        {
            StopZoomingIn();
        }

        private void OnSpeechEntered()
        {
            if (shopperSystem != null && shopperSystem.TryGetShopper(out var shopper))
            {
                shopper.PlaySpeech();
            }
        }

        private void OnSpeechExited()
        {
            if (shopperSystem != null && shopperSystem.TryGetShopper(out var shopper))
            {
                shopper.StopSpeech();
            }
        }

        private void OnCurrentHealthChanged()
        {
            var texture = settings != false ? settings.GetHealthTexture(Health) : default;

            if (texture && bodyRenderer != false)
            {
                var block = new MaterialPropertyBlock();
                block.SetTexture(bodyTexturePropertyId, texture);
                bodyRenderer.SetPropertyBlock(block);
            }

            if (cinemachineImpulse != false && settings != false)
            {
                cinemachineImpulse.GenerateImpulse(settings.CameraShakeForce);
            }

            Debug.Log($"Health: {Health}", this);

            GameManager.Publish(new PlayerHealthChanged(this));

            onHealthChanged?.Invoke();
        }

        private void StartZoomingIn()
        {
            if (settings == false)
            {
                return;
            }

            targetFov = settings.ZoomInFov;
        }

        private void StopZoomingIn()
        {
            targetFov = initialFov;
        }

        public void ShowPurchase(PurchaseRequest purchase)
        {
            if (chatViewController != false)
            {
                chatViewController.ShowPurchase(purchase);
            }
        }

        public void PlayGiveAnimation(ItemData item)
        {
            if (giveAnimation == false)
            {
                return;
            }

            giveAnimation.gameObject.SetActive(true);
            giveAnimation.Play("Animation_Player_Give");

            if (itemRenderer == false)
            {
                return;
            }

            if (item)
            {
                var block = new MaterialPropertyBlock();
                block.SetTexture(itemTexturePropertyId, item.Image);
                itemRenderer.SetPropertyBlock(block);
            }
        }

        public void StopGiveAnimation()
        {
            if (giveAnimation == false)
            {
                return;
            }

            if (giveAnimation.runtimeAnimatorController != false)
            {
                // Reset the state so the next hand-over plays from the start again.
                giveAnimation.Rebind();
            }

            giveAnimation.gameObject.SetActive(false);
        }

        public void HidePurchase()
        {
            if (chatViewController != false)
            {
                chatViewController.Hide();
            }

            onItemsDisappear?.Invoke();
        }
    }
}
