using System;
using System.Collections;
using CHARK.SimpleUI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    #region Enum

    internal enum MainMenuPanel
    {
        Buttons = 0,
        SaveSlots = 1,
        Settings = 2,
        Journal = 3,
        Achievements = 4,
    }

    #endregion

    #region Palette

    internal static class WarmBreadUiColors
    {
        public static readonly Color CrustDark    = new(0.11f, 0.06f, 0.04f, 0.88f);
        public static readonly Color Crust        = new(0.29f, 0.16f, 0.09f, 1f);
        public static readonly Color CrustSoft    = new(0.52f, 0.33f, 0.21f, 1f);
        public static readonly Color Cream        = new(0.96f, 0.90f, 0.78f, 1f);
        public static readonly Color Butter       = new(0.99f, 0.80f, 0.36f, 1f);
        public static readonly Color ButterHover  = new(1f,    0.88f, 0.50f, 1f);
        public static readonly Color ButterBright = new(1f,    0.93f, 0.62f, 1f);
        public static readonly Color Jam          = new(0.79f, 0.25f, 0.20f, 1f);
        public static readonly Color JamHover     = new(0.88f, 0.32f, 0.26f, 1f);
        public static readonly Color JamDark      = new(0.55f, 0.17f, 0.11f, 1f);
        public static readonly Color Parchment    = new(0.94f, 0.88f, 0.76f, 1f);

        // Деревянные кнопки-доски (спрайт зелёного дерева) тонируем тёплым кремом,
        // чтобы фактура дерева читалась, а не превращалась в грязно-оливковую.
        public static readonly Color Plank            = new(0.97f, 0.93f, 0.82f, 1f);
        public static readonly Color PlankHover       = new(1.00f, 0.97f, 0.88f, 1f);
        public static readonly Color PlankSelected    = new(0.99f, 0.93f, 0.78f, 1f);
        public static readonly Color PlankPressed     = new(0.89f, 0.80f, 0.62f, 1f);
        public static readonly Color PlankJam         = new(0.96f, 0.87f, 0.82f, 1f);
        public static readonly Color PlankJamHover    = new(0.99f, 0.91f, 0.87f, 1f);
        public static readonly Color PlankJamPressed  = new(0.90f, 0.72f, 0.64f, 1f);

        // Panels are painted as dark crust cards at runtime, so row text has to be light.
        public static readonly Color JournalValue        = new(0.92f, 0.86f, 0.74f, 1f);
        public static readonly Color AchievementUnlocked = new(0.48f, 0.80f, 0.44f, 1f);
        public static readonly Color AchievementLocked   = new(0.62f, 0.55f, 0.47f, 0.85f);

        public static Color WithAlpha(Color c, float a) { c.a = Mathf.Clamp01(a); return c; }

        public static Color Lighten(Color c, float d) => new(
            Mathf.Clamp01(c.r + d),
            Mathf.Clamp01(c.g + d),
            Mathf.Clamp01(c.b + d),
            c.a);

        public static string ToHex(Color c) => $"#{ColorUtility.ToHtmlStringRGB(c)}";
    }

    #endregion

    [DisallowMultipleComponent]
    internal sealed class MainMenuView : View, ICancelHandler
    {
        #region Constants

        public const int SaveSlotCount       = 3;
        public const int JournalRowCount     = 8;
        public const int AchievementRowCount = 8;

        #endregion

        #region Serialized — Panels

        [Header("Panels")]
        [Tooltip("Логотип. Показывается только на корневой панели Buttons. " +
                 "Если хочешь убрать совсем — просто удали Panel_Title из префаба.")]
        [SerializeField] private GameObject titlePanel;

        [SerializeField] private GameObject buttonsPanel;
        [SerializeField] private GameObject saveSlotsPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject journalPanel;
        [SerializeField] private GameObject achievementsPanel;

        [Tooltip("Если поле Title Panel пустое — попробовать найти Panel_Title среди детей корня.")]
        [SerializeField] private bool autoFindTitlePanel = true;

        #endregion

        #region Serialized — Buttons

        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button journalButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button exitButton;

        #endregion

        #region Serialized — Save slots

        [Header("Save Slots")]
        [SerializeField] private Button[]   saveSlotButtons = new Button[SaveSlotCount];
        [SerializeField] private TMP_Text[] saveSlotLabels  = new TMP_Text[SaveSlotCount];

        [Range(0.1f, 1f)]
        [SerializeField] private float disabledSlotAlpha = 0.45f;

        #endregion

        #region Serialized — Back buttons

        [Header("Back Buttons")]
        [SerializeField] private Button saveSlotsBackButton;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button journalBackButton;
        [SerializeField] private Button achievementsBackButton;

        #endregion

        #region Serialized — Settings

        [Header("Settings Controls")]
        [SerializeField] private Slider lookSensitivitySlider;
        [SerializeField] private Slider masterVolumeSlider;

        #endregion

        #region Serialized — Rows

        [Header("Rows Content")]
        [SerializeField] private TMP_Text[] journalRows     = new TMP_Text[JournalRowCount];
        [SerializeField] private TMP_Text[] achievementRows = new TMP_Text[AchievementRowCount];

        #endregion

        #region Serialized — Behaviour

        [Header("Behaviour")]
        [SerializeField] private bool applyRuntimeTheme = true;
        [SerializeField] private bool returnToRootPanelOnCancel = true;
        [SerializeField] private bool animatePanels = true;
        [SerializeField] private bool animateTitle  = true;

        #endregion

        #region Events

        public event Action OnContinueClicked;
        public event Action OnNewGameClicked;
        public event Action OnSettingsClicked;
        public event Action OnJournalClicked;
        public event Action OnAchievementsClicked;
        public event Action OnExitClicked;
        public event Action OnBackClicked;
        public event Action<int> OnSaveSlotClicked;
        public event Action<float> OnLookSensitivityChanged;
        public event Action<float> OnMasterVolumeChanged;

        #endregion

        #region Cached state

        private UnityAction[] saveSlotActions;
        private GameObject[]  panels;
        private MainMenuPanel currentPanel = MainMenuPanel.Buttons;
        private bool          themeApplied;
        private bool          polishApplied;

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            ResolveTitlePanelIfNeeded();
            CachePanels();
            TryApplyTheme();
            TryApplyPolish();
            RegisterListeners();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnregisterListeners();
        }

        protected override void OnViewShowEntered()
        {
            base.OnViewShowEntered();
            ShowPanel(MainMenuPanel.Buttons);
        }

        #endregion

        #region Cancel handling

        void ICancelHandler.OnCancel(BaseEventData eventData)
        {
            if (currentPanel == MainMenuPanel.Buttons) return;

            if (returnToRootPanelOnCancel) ShowPanel(MainMenuPanel.Buttons);
            OnBackClicked?.Invoke();
        }

        #endregion

        #region Panel API

        public void ShowPanel(MainMenuPanel panel)
        {
            currentPanel = panel;
            var targetIndex = (int)panel;

            if (panels != null)
            {
                for (var i = 0; i < panels.Length; i++) SetActive(panels[i], i == targetIndex);
            }
            else
            {
                SetActive(buttonsPanel,      panel == MainMenuPanel.Buttons);
                SetActive(saveSlotsPanel,    panel == MainMenuPanel.SaveSlots);
                SetActive(settingsPanel,     panel == MainMenuPanel.Settings);
                SetActive(journalPanel,      panel == MainMenuPanel.Journal);
                SetActive(achievementsPanel, panel == MainMenuPanel.Achievements);
            }

            // Логотип только на корневой панели.
            SetActive(titlePanel, panel == MainMenuPanel.Buttons);

            // Тексты только что открытых панелей получили материал в Awake — дозаливаем обводки.
            TryApplyTheme();

            Select(ResolveDefaultSelection(panel));
        }

        private Selectable ResolveDefaultSelection(MainMenuPanel panel) => panel switch
        {
            MainMenuPanel.Buttons      => PickFirstInteractable(continueButton, newGameButton),
            MainMenuPanel.SaveSlots    => (Selectable)GetFirstInteractableSaveSlotButton() ?? saveSlotsBackButton,
            MainMenuPanel.Settings     => PickFirstInteractable(lookSensitivitySlider, masterVolumeSlider, settingsBackButton),
            MainMenuPanel.Journal      => journalBackButton,
            MainMenuPanel.Achievements => achievementsBackButton,
            _                          => continueButton,
        };

        #endregion

        #region Save slot API

        public void SetSaveSlotData(int slot, string label, bool isInteractable)
        {
            if (!IsValidSlot(slot)) return;

            if (TryGet(saveSlotButtons, slot, out var button) && button != null)
                button.interactable = isInteractable;

            if (TryGet(saveSlotLabels, slot, out var labelText) && labelText != null)
            {
                labelText.text  = label;
                labelText.alpha = isInteractable ? 1f : disabledSlotAlpha;
            }
        }

        #endregion

        #region Journal & Achievements API

        public void SetJournalRow(int index, string text)
        {
            if (!TryGet(journalRows, index, out var row) || row == null) return;
            row.text  = text;
            row.color = WarmBreadUiColors.JournalValue;
        }

        public void SetAchievementRow(int index, string title, string description, bool isUnlocked)
        {
            if (!TryGet(achievementRows, index, out var row) || row == null) return;

            var markHex  = isUnlocked ? WarmBreadUiColors.ToHex(WarmBreadUiColors.AchievementUnlocked) : "#8B7365";
            var titleHex = isUnlocked ? "#F4E5C6" : "#A69282";
            var mark     = isUnlocked ? "\u2713" : "\u00b7";
            var body     = $"<color={markHex}><b>[ {mark} ]</b></color> <color={titleHex}><b>{title}</b></color>";
            var hint     = isUnlocked ? string.Empty : $"\n<size=75%><color=#8B7365>{description}</color></size>";

            row.text  = body + hint;
            row.color = isUnlocked ? WarmBreadUiColors.AchievementUnlocked : WarmBreadUiColors.AchievementLocked;
        }

        #endregion

        #region Settings API

        public void SetLookSensitivityData(float min, float max, float value, bool isNotifyListeners = true)
        {
            if (lookSensitivitySlider == null) return;
            lookSensitivitySlider.minValue = min;
            lookSensitivitySlider.maxValue = max;

            var clamped = Mathf.Clamp(value, min, max);
            if (isNotifyListeners) lookSensitivitySlider.value = clamped;
            else                   lookSensitivitySlider.SetValueWithoutNotify(clamped);
        }

        public void SetMasterVolumeData(float min, float max, float value, bool isNotifyListeners = true)
        {
            if (masterVolumeSlider == null) return;
            masterVolumeSlider.minValue = min * 100f;
            masterVolumeSlider.maxValue = max * 100f;

            var percentage = Mathf.Clamp(value * 100f, masterVolumeSlider.minValue, masterVolumeSlider.maxValue);
            if (isNotifyListeners) masterVolumeSlider.value = percentage;
            else                   masterVolumeSlider.SetValueWithoutNotify(percentage);
        }

        #endregion

        #region Listener registration

        private void RegisterListeners()
        {
            AddListener(continueButton,     HandleContinueClicked);
            AddListener(newGameButton,      HandleNewGameClicked);
            AddListener(settingsButton,     HandleSettingsClicked);
            AddListener(journalButton,      HandleJournalClicked);
            AddListener(achievementsButton, HandleAchievementsClicked);
            AddListener(exitButton,         HandleExitClicked);

            AddListener(saveSlotsBackButton,    HandleBackClicked);
            AddListener(settingsBackButton,     HandleBackClicked);
            AddListener(journalBackButton,      HandleBackClicked);
            AddListener(achievementsBackButton, HandleBackClicked);

            AddSaveSlotListeners();

            if (lookSensitivitySlider != null) lookSensitivitySlider.onValueChanged.AddListener(HandleLookSensitivityChanged);
            if (masterVolumeSlider    != null) masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
        }

        private void UnregisterListeners()
        {
            RemoveListener(continueButton,     HandleContinueClicked);
            RemoveListener(newGameButton,      HandleNewGameClicked);
            RemoveListener(settingsButton,     HandleSettingsClicked);
            RemoveListener(journalButton,      HandleJournalClicked);
            RemoveListener(achievementsButton, HandleAchievementsClicked);
            RemoveListener(exitButton,         HandleExitClicked);

            RemoveListener(saveSlotsBackButton,    HandleBackClicked);
            RemoveListener(settingsBackButton,     HandleBackClicked);
            RemoveListener(journalBackButton,      HandleBackClicked);
            RemoveListener(achievementsBackButton, HandleBackClicked);

            RemoveSaveSlotListeners();

            if (lookSensitivitySlider != null) lookSensitivitySlider.onValueChanged.RemoveListener(HandleLookSensitivityChanged);
            if (masterVolumeSlider    != null) masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
        }

        #endregion

        #region Save slot listeners

        private void AddSaveSlotListeners()
        {
            if (saveSlotButtons == null) return;
            saveSlotActions ??= CreateSaveSlotListeners();

            var count = Mathf.Min(saveSlotButtons.Length, SaveSlotCount);
            for (var i = 0; i < count; i++)
                if (saveSlotButtons[i] != null) saveSlotButtons[i].onClick.AddListener(saveSlotActions[i]);
        }

        private void RemoveSaveSlotListeners()
        {
            if (saveSlotButtons == null || saveSlotActions == null) return;

            var count = Mathf.Min(saveSlotButtons.Length, SaveSlotCount);
            for (var i = 0; i < count; i++)
                if (saveSlotButtons[i] != null) saveSlotButtons[i].onClick.RemoveListener(saveSlotActions[i]);
        }

        private UnityAction[] CreateSaveSlotListeners()
        {
            var listeners = new UnityAction[SaveSlotCount];
            for (var slot = 0; slot < SaveSlotCount; slot++)
            {
                var captured = slot;
                listeners[slot] = () => HandleSaveSlotClicked(captured);
            }
            return listeners;
        }

        #endregion

        #region Helpers

        private void ResolveTitlePanelIfNeeded()
        {
            if (titlePanel != null) return;
            if (!autoFindTitlePanel) return;

            // Ищем Panel_Title среди прямых детей корня.
            var found = transform.Find("Panel_Title");
            if (found != null) titlePanel = found.gameObject;
        }

        private void CachePanels()
        {
            panels ??= new[]
            {
                buttonsPanel,
                saveSlotsPanel,
                settingsPanel,
                journalPanel,
                achievementsPanel,
            };
        }

        private void TryApplyTheme()
        {
            if (themeApplied || !applyRuntimeTheme) return;

            try
            {
                // false значит "часть текстов на выключенных панелях ещё не готова": не считаем
                // тему применённой и попробуем снова при следующем ShowPanel/OnEnable.
                themeApplied = WarmBreadRuntimeTheme.ApplyMainMenuTheme(this);
            }
            catch (Exception e)
            {
                themeApplied = false;
                Debug.LogWarning($"[MainMenuView] Theme application failed: {e}", this);
            }
        }

        private void TryApplyPolish()
        {
            if (polishApplied) return;
            polishApplied = true;

            if (animatePanels && panels != null)
                foreach (var panel in panels) EnsureFader(panel);

            if (animateTitle && titlePanel != null)
            {
                EnsureFader(titlePanel);
                if (!titlePanel.TryGetComponent<MenuTitleBreather>(out _))
                    titlePanel.AddComponent<MenuTitleBreather>();
            }
        }

        private static void EnsureFader(GameObject target)
        {
            if (target == null) return;
            if (!target.TryGetComponent<MenuPanelFader>(out _))
                target.AddComponent<MenuPanelFader>();
        }

        private Button GetFirstInteractableSaveSlotButton()
        {
            if (saveSlotButtons != null)
            {
                foreach (var button in saveSlotButtons)
                    if (button != null && button.gameObject.activeInHierarchy && button.interactable)
                        return button;
            }
            return saveSlotsBackButton;
        }

        private static Selectable PickFirstInteractable(params Selectable[] candidates)
        {
            foreach (var candidate in candidates)
                if (candidate != null && candidate.interactable && candidate.gameObject.activeInHierarchy)
                    return candidate;
            return null;
        }

        private static void AddListener(Button button, UnityAction listener)
        {
            if (button != null) button.onClick.AddListener(listener);
        }

        private static void RemoveListener(Button button, UnityAction listener)
        {
            if (button != null) button.onClick.RemoveListener(listener);
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive) target.SetActive(isActive);
        }

        private static void Select(Selectable selectable)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || selectable == null || !selectable.interactable) return;
            eventSystem.SetSelectedGameObject(selectable.gameObject);
        }

        private static bool IsValidSlot(int slot) => slot >= 0 && slot < SaveSlotCount;

        private static bool TryGet<T>(T[] array, int index, out T value)
        {
            if (array == null || index < 0 || index >= array.Length)
            {
                value = default;
                return false;
            }
            value = array[index];
            return true;
        }

        #endregion

        #region Event handlers

        private void HandleContinueClicked()     => OnContinueClicked?.Invoke();
        private void HandleNewGameClicked()      => OnNewGameClicked?.Invoke();
        private void HandleSettingsClicked()     => OnSettingsClicked?.Invoke();
        private void HandleJournalClicked()      => OnJournalClicked?.Invoke();
        private void HandleAchievementsClicked() => OnAchievementsClicked?.Invoke();
        private void HandleExitClicked()         => OnExitClicked?.Invoke();
        private void HandleBackClicked()         => OnBackClicked?.Invoke();
        private void HandleSaveSlotClicked(int slot) => OnSaveSlotClicked?.Invoke(slot);
        private void HandleLookSensitivityChanged(float value) => OnLookSensitivityChanged?.Invoke(value);
        private void HandleMasterVolumeChanged(float value)    => OnMasterVolumeChanged?.Invoke(value / 100f);

        #endregion
    }

    #region Warm Bread runtime theme

    internal static class WarmBreadRuntimeTheme
    {
        private static readonly Color MainMenuBackdrop  = new(0.11f, 0.06f, 0.04f, 0.88f);
        private static readonly Color MainMenuContrast  = new(0.18f, 0.10f, 0.06f, 0.95f);
        private static readonly Color PauseMenuBackdrop = new(0.09f, 0.05f, 0.03f, 0.92f);
        private static readonly Color PauseMenuContrast = new(0.16f, 0.09f, 0.05f, 0.97f);

        /// <returns>
        /// false, если часть текстов ещё не готова к оформлению: материал TMP создаётся в Awake,
        /// а у выключенных панелей он ещё не создан. Тему нужно переапплайнить позже.
        /// </returns>
        public static bool ApplyMainMenuTheme(Component root)
            => ApplyTheme(root, MainMenuBackdrop, MainMenuContrast, addButtonAnimator: true);

        public static bool ApplyPauseMenuTheme(Component root)
            => ApplyTheme(root, PauseMenuBackdrop, PauseMenuContrast, addButtonAnimator: true);

        private static bool ApplyTheme(Component root, Color backdrop, Color contrast, bool addButtonAnimator)
        {
            if (root == null || root.transform == null) return true;

            var t = root.transform;

            ApplyPanelTheme(t, backdrop, contrast);
            var buttonsThemed = ApplyButtonTheme(t.GetComponentsInChildren<Button>(true), addButtonAnimator);
            ApplySliderTheme(t.GetComponentsInChildren<Slider>(true));
            var textsThemed = ApplyTextTheme(t.GetComponentsInChildren<TMP_Text>(true));

            return buttonsThemed && textsThemed;
        }

        #region Panels

        private static void ApplyPanelTheme(Transform root, Color darkTint, Color contrastTint)
        {
            if (root == null) return;

            var images = root.GetComponentsInChildren<Image>(true);
            if (images == null) return;

            foreach (var image in images)
            {
                if (image == null || image.gameObject == null) continue;

                var name = image.name ?? string.Empty;

                if (name.Contains("Backdrop") || name.Contains("Background"))
                {
                    image.color         = darkTint;
                    image.raycastTarget = false;
                    EnsureShadow(image, new Vector2(4f, -4f), 0.40f);
                }
                else if (name.Contains("Panel") || name.Contains("Window") || name.Contains("Frame"))
                {
                    image.color = contrastTint;
                    EnsureShadow(image, new Vector2(6f, -6f), 0.35f);
                }
            }
        }

        #endregion

        #region Buttons

        private static bool ApplyButtonTheme(Button[] buttons, bool addAnimator)
        {
            if (buttons == null) return true;

            var allThemed = true;

            foreach (var button in buttons)
            {
                if (button == null || button.gameObject == null) continue;
                try { allThemed &= ApplySingleButtonTheme(button, addAnimator); }
                catch (Exception) { allThemed = false; /* одна битая кнопка не должна ронять весь проход */ }
            }

            return allThemed;
        }

        private static bool ApplySingleButtonTheme(Button button, bool addAnimator)
        {
            if (button == null || button.gameObject == null) return true;

            var isExit     = (button.name ?? string.Empty).Contains("Exit");
            var baseColor  = isExit ? WarmBreadUiColors.PlankJam         : WarmBreadUiColors.Plank;
            var hoverColor = isExit ? WarmBreadUiColors.PlankJamHover    : WarmBreadUiColors.PlankHover;

            var colors = button.colors;
            colors.normalColor      = baseColor;
            colors.highlightedColor = hoverColor;
            colors.selectedColor    = isExit ? WarmBreadUiColors.PlankJamHover : WarmBreadUiColors.PlankSelected;
            colors.pressedColor     = isExit ? WarmBreadUiColors.PlankJamPressed : WarmBreadUiColors.PlankPressed;
            colors.disabledColor    = WarmBreadUiColors.WithAlpha(baseColor, 0.45f);
            colors.colorMultiplier  = 1f;
            colors.fadeDuration     = 0.08f;

            button.colors     = colors;
            button.transition = Selectable.Transition.ColorTint;

            if (button.targetGraphic is Image image)
            {
                // ВАЖНО: Image.color держим белым — тон задаёт ТОЛЬКО ColorTint кнопки.
                // Если покрасить и то и другое, спрайт тонируется дважды и дерево
                // превращается в грязно-оливковое.
                image.color         = Color.white;
                image.raycastTarget = true;
                EnsureShadow(image, new Vector2(2.5f, -2.5f), 0.25f);
            }

            if (addAnimator && !button.TryGetComponent<MenuButtonAnimator>(out _))
                button.gameObject.AddComponent<MenuButtonAnimator>();

            return ApplyButtonLabelTheme(button, isExit);
        }

        private static bool ApplyButtonLabelTheme(Button button, bool isExit)
        {
            var allThemed = true;

            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null) continue;

                // ВАЖНО: НЕ трогаем enableAutoSizing / fontSizeMin / fontSizeMax.
                // Дизайнер выставил их в префабе — лезем только в цвета и обводку.
                text.color     = isExit ? WarmBreadUiColors.JamDark : WarmBreadUiColors.Crust;
                text.fontStyle = FontStyles.Bold;
                allThemed &= TryApplyOutline(
                    text,
                    0.05f,
                    new Color(0.15f, 0.08f, 0.04f, 0.35f)
                );
            }

            return allThemed;
        }

        #endregion

        #region Sliders

        private static void ApplySliderTheme(Slider[] sliders)
        {
            if (sliders == null) return;

            foreach (var slider in sliders)
            {
                if (slider == null || slider.transform == null) continue;

                if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fill))
                    fill.color = WarmBreadUiColors.Butter;

                if (slider.handleRect != null && slider.handleRect.TryGetComponent(out Image handle))
                {
                    handle.color = WarmBreadUiColors.Cream;
                    EnsureShadow(handle, new Vector2(2f, -2f), 0.30f);
                }

                var bg = slider.transform.Find("Background");
                if (bg != null && bg.TryGetComponent(out Image bgImage))
                    bgImage.color = WarmBreadUiColors.CrustDark;
            }
        }

        #endregion

        #region Text

        private static bool ApplyTextTheme(TMP_Text[] texts)
        {
            if (texts == null) return true;

            var allThemed = true;

            foreach (var text in texts)
            {
                if (text == null || text.gameObject == null) continue;
                if (text.GetComponentInParent<Button>() != null) continue; // кнопки отдельно

                var name = text.name ?? string.Empty;

                var themed = false;
                if (IsSubtitleLike(name))
                    themed = ApplySubtitleStyle(text);
                else if (IsTitleLike(name))
                    themed = ApplyTitleStyle(text);
                else
                {
                    ApplyBodyStyle(text);
                    themed = true;
                }

                allThemed &= themed;
            }

            return allThemed;
        }

        private static bool IsSubtitleLike(string n) =>
            n.Contains("Subtitle") || n.Contains("Tagline") ||
            n.Contains("Description") || n.Contains("Caption") ||
            n.Contains("Slogan") || n.Contains("Motto");

        private static bool IsTitleLike(string n) =>
            n.Contains("Title") || n.Contains("Header");

        private static bool ApplySubtitleStyle(TMP_Text text)
        {
            // Светлый Cream + тёмная обводка — читается и на светлом фоне, и на тёмном.
            text.color     = WarmBreadUiColors.Cream;
            text.fontStyle = FontStyles.Bold;
            return TryApplyOutline(text, 0.12f, new Color(0.12f, 0.07f, 0.05f, 0.75f));
        }

        private static bool ApplyTitleStyle(TMP_Text text)
        {
            text.color     = WarmBreadUiColors.ButterBright;
            text.fontStyle = FontStyles.Bold;
            return TryApplyOutline(text, 0.12f, new Color(0.12f, 0.07f, 0.05f, 0.80f));
        }

        /// <summary>
        /// TMP инициализирует материал и CanvasRenderer только в Awake: у текстов на выключенных
        /// панелях запись outlineWidth падает с NRE внутри TMP (m_canvasRenderer == null).
        /// Такой текст пропускаем — тема будет переапплайнена, когда панель первый раз откроют
        /// (ShowPanel → SetActive → Awake → повторный TryApplyTheme).
        /// </summary>
        private static bool TryApplyOutline(TMP_Text text, float width, Color color)
        {
            if (text == null || !text.gameObject.activeInHierarchy) return false;
            if (text.fontSharedMaterial == null) return false;

            text.outlineWidth = width;
            text.outlineColor = color;
            return true;
        }

        private static void ApplyBodyStyle(TMP_Text text)
        {
            text.color = WarmBreadUiColors.Parchment;
        }

        #endregion

        #region Shadow

        private static void EnsureShadow(Graphic graphic, Vector2 distance, float alpha)
        {
            if (graphic == null) return;

            if (!graphic.TryGetComponent<Shadow>(out var shadow))
                shadow = graphic.gameObject.AddComponent<Shadow>();

            shadow.effectDistance  = distance;
            shadow.effectColor     = new Color(0.04f, 0.02f, 0.01f, alpha);
            shadow.useGraphicAlpha = true;
        }

        #endregion
    }

    #endregion

    #region Menu button juice

    [DisallowMultipleComponent]
    internal sealed class MenuButtonAnimator
        : MonoBehaviour,
          IPointerEnterHandler, IPointerExitHandler,
          IPointerDownHandler,  IPointerUpHandler,
          ISelectHandler,       IDeselectHandler
    {
        [Header("Scales")]
        [SerializeField] private float hoverScale   = 1.045f;
        [SerializeField] private float pressedScale = 0.965f;

        [Header("Damping")]
        [SerializeField] private float damping = 18f;

        private Vector3 baseScale   = Vector3.one;
        private Vector3 targetScale = Vector3.one;
        private bool    isHovered;
        private bool    isPressed;
        private bool    isSelected;
        private bool    baseScaleCaptured;
        private Outline selectionFrame;

        private void Awake() => CaptureBaseScale();

        private void OnEnable()
        {
            CaptureBaseScale();
            transform.localScale = baseScale;
            targetScale          = baseScale;
            EnsureSelectionFrame();
            SyncSelectionFromEventSystem();
            RecomputeFrame();
        }

        private void OnDisable()
        {
            if (baseScaleCaptured) transform.localScale = baseScale;
            isHovered   = false;
            isPressed   = false;
            isSelected  = false;
            if (selectionFrame != null) selectionFrame.enabled = false;
        }

        private void Update()
        {
            var t = 1f - Mathf.Exp(-damping * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
        }

        private void CaptureBaseScale()
        {
            if (baseScaleCaptured) return;

            var s = transform.localScale;
            baseScale         = (s.sqrMagnitude > 0.0001f) ? s : Vector3.one;
            targetScale       = baseScale;
            baseScaleCaptured = true;
        }

        private void RecomputeTarget()
        {
            var highlighted = isHovered || isSelected;
            var multiplier  = isPressed ? pressedScale : highlighted ? hoverScale : 1f;
            targetScale = baseScale * multiplier;
        }

        /// <summary>
        /// Рамка выбранной кнопки: тёмный контур по спрайту доски. Виден при наведении мыши
        /// и при выборе с клавиатуры/геймпада — иначе навигация без мыши визуально невидима.
        /// </summary>
        private void EnsureSelectionFrame()
        {
            if (selectionFrame != null) return;

            if (!TryGetComponent(out selectionFrame))
                selectionFrame = gameObject.AddComponent<Outline>();

            selectionFrame.effectColor     = new Color(0.11f, 0.06f, 0.04f, 0.85f);
            selectionFrame.effectDistance  = new Vector2(3f, 3f);
            selectionFrame.useGraphicAlpha = false;
            selectionFrame.enabled         = false;
        }

        private void SyncSelectionFromEventSystem()
        {
            var eventSystem = EventSystem.current;
            isSelected = eventSystem != null && eventSystem.currentSelectedGameObject == gameObject;
        }

        private void RecomputeFrame()
        {
            if (selectionFrame != null)
                selectionFrame.enabled = isHovered || isSelected;
        }

        public void OnPointerEnter(PointerEventData _) { isHovered = true;  RecomputeTarget(); RecomputeFrame(); }
        public void OnPointerExit (PointerEventData _) { isHovered = false; RecomputeTarget(); RecomputeFrame(); }
        public void OnPointerDown (PointerEventData _) { isPressed = true;  RecomputeTarget(); }
        public void OnPointerUp   (PointerEventData _) { isPressed = false; RecomputeTarget(); }
        public void OnSelect      (BaseEventData   _) { isSelected = true; RecomputeTarget(); RecomputeFrame(); }
        public void OnDeselect    (BaseEventData   _) { isSelected = false; RecomputeTarget(); RecomputeFrame(); }
    }

    #endregion

    #region Menu panel fade+slide

    [DisallowMultipleComponent]
    internal sealed class MenuPanelFader : MonoBehaviour
    {
        [SerializeField] private float duration    = 0.20f;
        [SerializeField] private float slideOffset = 14f;
        [SerializeField] private bool  slideFromTop;

        private CanvasGroup   group;
        private RectTransform rect;
        private Vector2       basePosition;
        private bool          captured;

        private void Awake() => Capture();

        private void OnEnable()
        {
            Capture();

            if (group != null) group.alpha = 0f;
            if (rect != null)  rect.anchoredPosition = basePosition + SlideVector();

            StartCoroutine(PlayIn());
        }

        private void OnDisable()
        {
            if (group != null) group.alpha = 1f;
            if (rect != null)  rect.anchoredPosition = basePosition;
        }

        private Vector2 SlideVector()
            => slideFromTop ? new Vector2(0f, -slideOffset) : new Vector2(0f, slideOffset);

        private void Capture()
        {
            if (captured) return;

            if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
            rect = transform as RectTransform;
            if (rect != null) basePosition = rect.anchoredPosition;

            captured = true;
        }

        private IEnumerator PlayIn()
        {
            var t = 0f;
            var startOffset = basePosition + SlideVector();

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k     = Mathf.Clamp01(t / duration);
                var eased = 1f - Mathf.Pow(1f - k, 3f);

                if (group != null) group.alpha = eased;
                if (rect != null)  rect.anchoredPosition = Vector2.Lerp(startOffset, basePosition, eased);

                yield return null;
            }

            if (group != null) group.alpha = 1f;
            if (rect != null)  rect.anchoredPosition = basePosition;
        }
    }

    #endregion

    #region Menu title breather

    [DisallowMultipleComponent]
    internal sealed class MenuTitleBreather : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.015f;
        [SerializeField] private float period    = 3.5f;
        [SerializeField] private float phase     = 0f;

        private Vector3 baseScale;
        private bool    captured;
        private float   t;

        private void Awake()   => Capture();

        private void OnEnable()
        {
            Capture();
            t = phase;
        }

        private void Update()
        {
            if (!captured) return;

            t += Time.unscaledDeltaTime;
            var k = 1f + Mathf.Sin(t * Mathf.PI * 2f / period) * amplitude;
            transform.localScale = baseScale * k;
        }

        private void Capture()
        {
            if (captured) return;

            var s = transform.localScale;
            baseScale = (s.sqrMagnitude > 0.0001f) ? s : Vector3.one;
            captured  = true;
        }
    }

    #endregion
}