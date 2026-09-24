#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Components.Triggers;
using UABPetelnia.GGJ2025.Runtime.UI.Controllers;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UABPetelnia.GGJ2025.Runtime.EditorTools
{
    /// <summary>
    /// Editor utility which rebuilds the main menu view prefab and makes sure every gameplay scene
    /// has an <see cref="EventSystem"/> (required for UI input).
    /// </summary>
    public static class MainMenuPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/UI/View_MainMenu.prefab";
        private const string PlayerPrefabPath = "Assets/Prefabs/Actors/Actor_Player.prefab";
        private const string MenuScenePath = "Assets/Scenes/Scene_Menu.unity";
        private const string GameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";
        private const string GiveAnimationName = "Animation_Give";
        private const string SpriteFolder = "Assets/Visuals/UI/Sprites/";
        private const string IconFolder = "Assets/Visuals/UI/Icons/";
        private const string FontPath = "Assets/Visuals/Fonts/Font_Tiny5_Regular_SDF.asset";

        private const string ControlsAnimatorPath =
            "Assets/Visuals/UI/Animations/AnimationController_Controls_Wobble.controller";

        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 720f;

        // Header.
        private const float TitleFontSize = 80f;

        // Buttons of the main column.
        private const float MenuButtonHeight = 68f;
        private const float ButtonSpacing = 10f;

        /// <summary>
        /// Height of the button column. Every child of the column is measured from its
        /// <see cref="LayoutElement"/>, so this has to be at least
        /// <c>MenuButtonCount * MenuButtonHeight + (MenuButtonCount - 1) * ButtonSpacing</c>.
        /// </summary>
        private const int MenuButtonCount = 6;

        private static readonly float ButtonsPanelHeight =
            MenuButtonCount * MenuButtonHeight + (MenuButtonCount - 1) * ButtonSpacing;

        // Panels: one window size for every screen, so none of them looks like a leftover.
        private const float PanelSpacing = 10f;
        private const float PanelInset = 48f;
        private const float PanelTitleHeight = 60f;
        private const float PanelRowHeight = 42f;
        private const float PanelBackHeight = 76f;
        private const float SlotButtonHeight = 100f;
        private const float ControlsHeight = 170f;
        private const float SliderEntryHeight = 100f;

        private static readonly Vector2 TitleSize = new(920f, 166f);
        private static readonly Vector2 TitlePanelSize = new(960f, 176f);
        private static readonly Vector2 ButtonsPanelSize = new(460f, ButtonsPanelHeight);
        private static readonly Vector2 ButtonsPanelPosition = new(0f, -79f);
        private static readonly Vector2 PanelWindowSize = new(920f, 680f);
        private static readonly Vector2 CenterAnchor = new(0.5f, 0.5f);

        private static Color TextColor => WarmBreadTheme.Cream;

        private static readonly string[] ScenesWithUi =
        {
            MenuScenePath,
            GameplayScenePath,
            "Assets/Scenes/Scene_GameOver.unity",
        };

        [MenuItem("Tools/Warm Bread/Rebuild Main Menu")]
        public static void Rebuild()
        {
            if (Application.isBatchMode == false &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                // This tool re-opens scenes, so unsaved work has to be handled first.
                Debug.LogWarning("Rebuild cancelled, scenes were not saved.");
                return;
            }

            EnsureIconImport();
            BuildMainMenuPrefab();
            FixMenuSceneViewReference();
            RemoveRedundantEventSystems();
            FixGiveAnimationOverride();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Main menu rebuilt.");
        }

        /// <summary>
        /// Batchmode entry point, so the whole thing can be run without opening the editor.
        /// </summary>
        public static void RebuildBatch()
        {
            Rebuild();
        }

        private static void BuildMainMenuPrefab()
        {
            var root = LoadPrefabRoot();
            var isLoadedContents = root != null;

            if (root == null)
            {
                Debug.LogWarning($"Could not load {PrefabPath}, creating a new prefab.");
                root = new GameObject("View_MainMenu");
            }

            try
            {
                ClearChildren(root);

                var canvas = GetOrAdd<Canvas>(root);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = GetOrAdd<CanvasScaler>(root);
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                scaler.referencePixelsPerUnit = 100f;

                GetOrAdd<GraphicRaycaster>(root);

                var canvasGroup = GetOrAdd<CanvasGroup>(root);
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                var view = GetOrAdd<MainMenuView>(root);
                NormalizeRoot(root);

                // NOTE: no full screen background image is created on purpose - the main menu
                // scene renders a video background into the camera, an opaque UI image would
                // hide it.
                // The title is written out, not a picture: the artwork this project started with
                // had a title of its own, and leaving it up made the menu look like the old game.
                CreateTitle(root.transform);

                var buttonsPanel = CreateButtonsPanel(root.transform, out var continueButton,
                    out var newGameButton, out var settingsButton, out var journalButton,
                    out var achievementsButton, out var exitButton);

                var saveSlotsPanel = CreateSaveSlotsPanel(root.transform, out var saveSlotButtons,
                    out var saveSlotLabels, out var saveSlotsBackButton);

                var settingsPanel = CreateSettingsPanel(root.transform, out var settingsBackButton,
                    out var lookSensitivitySlider, out var masterVolumeSlider);

                var journalPanel = CreateTextPanel(
                    parent: root.transform,
                    name: "Panel_Journal",
                    title: "Журнал смены",
                    rowCount: MainMenuView.JournalRowCount,
                    rows: out var journalRows,
                    backButton: out var journalBackButton
                );

                var achievementsPanel = CreateTextPanel(
                    parent: root.transform,
                    name: "Panel_Achievements",
                    title: "Достижения",
                    rowCount: MainMenuView.AchievementRowCount,
                    rows: out var achievementRows,
                    backButton: out var achievementsBackButton
                );

                buttonsPanel.SetActive(true);
                saveSlotsPanel.SetActive(false);
                settingsPanel.SetActive(false);
                journalPanel.SetActive(false);
                achievementsPanel.SetActive(false);

                WireWebGlExitButton(root, exitButton);

                ApplyViewReferences(
                    view,
                    canvasGroup,
                    buttonsPanel,
                    saveSlotsPanel,
                    settingsPanel,
                    continueButton,
                    newGameButton,
                    settingsButton,
                    exitButton,
                    saveSlotButtons,
                    saveSlotLabels,
                    saveSlotsBackButton,
                    settingsBackButton,
                    lookSensitivitySlider,
                    masterVolumeSlider,
                    journalPanel,
                    achievementsPanel,
                    journalButton,
                    achievementsButton,
                    journalBackButton,
                    achievementsBackButton,
                    journalRows,
                    achievementRows
                );

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                if (isLoadedContents)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        /// <summary>
        /// The shop sign at the top of the screen. The sign is written out and not a picture: the art
        /// the project started with had a title of its own, and leaving it in made the menu look like
        /// the old game. The panel around it is kept, because the view animates the sign as a whole.
        /// </summary>
        private static void CreateTitle(Transform parent)
        {
            const string title = "ТЁПЛЫЙ ХЛЕБ";
            const string subtitle = "ЛАРЁК У ДОМА";

            var panel = new GameObject("Panel_Title", typeof(RectTransform));
            panel.transform.SetParent(parent, worldPositionStays: false);

            // An image without a sprite: the panel only groups the sign so the view can animate it,
            // it draws nothing on its own.
            var panelImage = panel.AddComponent<Image>();
            panelImage.sprite = default;
            panelImage.raycastTarget = false;

            WarmBreadTheme.Place(
                panel.GetComponent<RectTransform>(),
                anchor: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -16f),
                size: TitlePanelSize
            );

            // The second line is written into the same label through rich text, so both lines stay
            // centered on each other at any width.
            var titleLabel = WarmBreadTheme.CreateText(
                panel.transform,
                "Text_Title",
                $"{title}\n<size=34%>{subtitle}</size>",
                TitleFontSize,
                TextAlignmentOptions.Center
            );

            titleLabel.color = WarmBreadTheme.Butter;
            titleLabel.raycastTarget = false;
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.enableAutoSizing = false;
            titleLabel.characterSpacing = 4f;

            WarmBreadTheme.Place(
                titleLabel.rectTransform,
                anchor: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -2f),
                size: TitleSize
            );
        }

        private static GameObject CreateButtonsPanel(
            Transform parent,
            out Button continueButton,
            out Button newGameButton,
            out Button settingsButton,
            out Button journalButton,
            out Button achievementsButton,
            out Button exitButton
        )
        {
            var panel = CreateGroup("Panel_Buttons", parent, ButtonsPanelSize, spacing: ButtonSpacing);
            Anchor(panel.GetComponent<RectTransform>(), CenterAnchor, CenterAnchor,
                ButtonsPanelPosition, ButtonsPanelSize);

            continueButton = CreateButton(
                "Button_Continue",
                panel.transform,
                "Продолжить игру",
                iconSpriteName: "UI_Icon_Continue"
            );

            newGameButton = CreateButton(
                "Button_NewGame",
                panel.transform,
                "Начать новую игру",
                iconSpriteName: "UI_Icon_NewGame"
            );

            settingsButton = CreateButton(
                "Button_Settings",
                panel.transform,
                "Настройки",
                iconSpriteName: "UI_Icon_Settings"
            );

            journalButton = CreateButton(
                "Button_Journal",
                panel.transform,
                "Журнал",
                iconSpriteName: "UI_Icon_Journal"
            );

            achievementsButton = CreateButton(
                "Button_Achievements",
                panel.transform,
                "Достижения",
                iconSpriteName: "UI_Icon_Achievements"
            );

            exitButton = CreateButton(
                "Button_Exit",
                panel.transform,
                "Выход",
                iconSpriteName: "UI_Icon_Exit",
                isDanger: true
            );

            return panel;
        }

        /// <summary>
        /// A parchment window holding a list of text rows and a back button, used by the shift
        /// journal and the achievements screen.
        /// </summary>
        private static GameObject CreateTextPanel(
            Transform parent,
            string name,
            string title,
            int rowCount,
            out TMP_Text[] rows,
            out Button backButton
        )
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, worldPositionStays: false);
            Stretch(panel.GetComponent<RectTransform>());

            var backdrop = CreateImage("Backdrop", panel.transform, null);
            backdrop.color = WarmBreadTheme.CrustDark;
            Stretch(backdrop.rectTransform);

            var framed = CreateFramedWindow(panel.transform, "Window", PanelWindowSize);
            var content = CreateGroup("Content", framed.transform, Vector2.zero, PanelSpacing);
            Stretch(content.GetComponent<RectTransform>(), PanelInset);

            AddLabel(content.transform, "Label_Title", title, 40f, PanelTitleHeight);

            rows = new TMP_Text[rowCount];

            for (var index = 0; index < rowCount; index++)
            {
                var row = CreateText(
                    $"Row_{index + 1}",
                    content.transform,
                    string.Empty,
                    26f,
                    TextAlignmentOptions.Left
                );

                row.color = WarmBreadTheme.Crust;
                row.raycastTarget = false;

                AddLayoutElement(row.gameObject, PanelRowHeight);

                rows[index] = row;
            }

            backButton = CreateButton("Button_Back", content.transform, "Назад", PanelBackHeight);

            return panel;
        }

        private static GameObject CreateSaveSlotsPanel(
            Transform parent,
            out Button[] saveSlotButtons,
            out TMP_Text[] saveSlotLabels,
            out Button backButton
        )
        {
            var slotCount = MainMenuView.SaveSlotCount;

            saveSlotButtons = new Button[slotCount];
            saveSlotLabels = new TMP_Text[slotCount];

            var panel = new GameObject("Panel_SaveSlots", typeof(RectTransform));
            panel.transform.SetParent(parent, worldPositionStays: false);
            Stretch(panel.GetComponent<RectTransform>());

            var backdrop = CreateImage("Backdrop", panel.transform, null);
            backdrop.color = WarmBreadTheme.CrustDark;
            Stretch(backdrop.rectTransform);

            var framed = CreateFramedWindow(panel.transform, "Window", PanelWindowSize);
            var content = CreateGroup("Content", framed.transform, Vector2.zero, PanelSpacing);
            Stretch(content.GetComponent<RectTransform>(), PanelInset);

            AddLabel(content.transform, "Label_Title", "Выберите сохранение", 40f, PanelTitleHeight);

            for (var slot = 0; slot < slotCount; slot++)
            {
                saveSlotButtons[slot] = CreateButton(
                    $"Button_SaveSlot_{slot + 1}",
                    content.transform,
                    $"Сохранение {slot + 1}",
                    SlotButtonHeight
                );

                saveSlotLabels[slot] = saveSlotButtons[slot].GetComponentInChildren<TMP_Text>(true);
            }

            backButton = CreateButton("Button_Back", content.transform, "Назад", PanelBackHeight);

            return panel;
        }

        private static GameObject CreateSettingsPanel(
            Transform parent,
            out Button backButton,
            out Slider lookSensitivitySlider,
            out Slider masterVolumeSlider
        )
        {
            var panel = new GameObject("Panel_Settings", typeof(RectTransform));
            panel.transform.SetParent(parent, worldPositionStays: false);
            Stretch(panel.GetComponent<RectTransform>());

            var backdrop = CreateImage("Backdrop", panel.transform, null);
            backdrop.color = WarmBreadTheme.CrustDark;
            Stretch(backdrop.rectTransform);

            var framed = CreateFramedWindow(panel.transform, "Window", PanelWindowSize);
            var content = CreateGroup("Content", framed.transform, Vector2.zero, PanelSpacing);
            Stretch(content.GetComponent<RectTransform>(), PanelInset);

            AddLabel(content.transform, "Label_Title", "Настройки", 40f, PanelTitleHeight);

            lookSensitivitySlider = AddSliderEntry(content.transform, "Entry_Look_Sensitivity",
                "Чувствительность обзора");

            masterVolumeSlider = AddSliderEntry(content.transform, "Entry_Volume_Master", "Громкость");

            var controls = CreateImage("Image_Controls", content.transform, "UI_Controls");
            controls.preserveAspect = true;
            controls.raycastTarget = false;
            AddLayoutElement(controls.gameObject, ControlsHeight);

            var animator = controls.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                ControlsAnimatorPath
            );
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            backButton = CreateButton("Button_Back", content.transform, "Назад", PanelBackHeight);

            return panel;
        }

        private static Slider AddSliderEntry(Transform parent, string name, string label)
        {
            var entry = CreateGroup(name, parent, new Vector2(640f, SliderEntryHeight));
            AddLayoutElement(entry, SliderEntryHeight);

            var layout = entry.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandHeight = false;

            // Same reason as CreateGroup: the label and the slider set their height through a
            // LayoutElement, so the group has to apply it.
            layout.childControlHeight = true;

            AddLabel(entry.transform, "Label", label, 28f, 42f);

            var slider = CreateSlider("Slider", entry.transform);
            AddLayoutElement(slider.gameObject, 40f);

            return slider;
        }

        private static Slider CreateSlider(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, worldPositionStays: false);

            var background = CreateImage("Background", root.transform, null);
            background.color = WarmBreadTheme.CrustDark;
            Stretch(background.rectTransform);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, worldPositionStays: false);
            Stretch(fillArea.GetComponent<RectTransform>(), 8f);

            var fill = CreateImage("Fill", fillArea.transform, null);
            fill.color = WarmBreadTheme.Butter;
            Stretch(fill.rectTransform);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, worldPositionStays: false);
            Stretch(handleArea.GetComponent<RectTransform>(), 8f);

            var handle = CreateImage("Handle", handleArea.transform, null);
            handle.color = WarmBreadTheme.Cream;
            var handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(24f, 0f);

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            return slider;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            float height = MenuButtonHeight,
            string iconSpriteName = null,
            bool isDanger = false
        )
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(parent, worldPositionStays: false);

            var image = root.AddComponent<Image>();
            image.sprite = default;
            image.type = Image.Type.Simple;
            image.color = Color.white;

            var button = root.AddComponent<Button>();
            button.targetGraphic = image;

            WarmBreadTheme.StyleButton(button, isDanger ? WarmBreadTheme.Jam : WarmBreadTheme.Butter);

            AddLayoutElement(root, height);

            var iconSize = height * 0.62f;
            var leftInset = 24f;

            if (string.IsNullOrEmpty(iconSpriteName) == false)
            {
                var icon = CreateImage("Image_Icon", root.transform, iconSpriteName);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                Anchor(
                    icon.rectTransform,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(20f + iconSize * 0.5f, 0f),
                    new Vector2(iconSize, iconSize)
                );

                leftInset = 32f + iconSize;
            }

            var fontSize = Mathf.Clamp(height * 0.5f, 18f, 34f);
            var text = CreateText("Text (TMP)", root.transform, label, fontSize, TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(leftInset, 12f);
            text.rectTransform.offsetMax = new Vector2(-24f, -12f);
            text.raycastTarget = false;
            text.color = WarmBreadTheme.Crust;

            return button;
        }

        private static GameObject CreateFramedWindow(Transform parent, string name, Vector2 size)
        {
            var window = WarmBreadTheme.CreateWindow(parent, name, size);

            Anchor(window.rectTransform, CenterAnchor, CenterAnchor, Vector2.zero, size);

            return window.gameObject;
        }

        private static void CreateKirillMonitorArt(Transform parent)
        {
            var frame = WarmBreadTheme.CreateWindow(
                parent,
                "Window_KirillMonitor",
                new Vector2(540f, 280f)
            );

            frame.color = WarmBreadTheme.Crust;
            WarmBreadTheme.Place(
                frame.rectTransform,
                anchor: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                position: new Vector2(300f, -125f),
                size: new Vector2(540f, 280f)
            );

            var parchment = WarmBreadTheme.EnsureParchment(frame.transform);
            parchment.color = WarmBreadTheme.ParchmentShade;

            var art = WarmBreadTheme.CreateImage(
                frame.transform,
                "Image_KirillMonitorArt",
                WarmBreadTheme.LoadArtSprite("пк и заказ/пк для азказов и тд.png", warn: false)
            );

            art.color = WarmBreadTheme.WindowTint;
            art.preserveAspect = true;
            WarmBreadTheme.Place(
                art.rectTransform,
                anchor: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                position: Vector2.zero,
                size: new Vector2(460f, 220f)
            );
        }

        private static GameObject CreateGroup(
            string name,
            Transform parent,
            Vector2 size,
            float spacing = 14f
        )
        {
            var group = new GameObject(name, typeof(RectTransform));
            group.transform.SetParent(parent, worldPositionStays: false);

            var rectTransform = group.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = size;

            var layout = group.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;

            // The height of every child has to be controlled by the group, otherwise the
            // LayoutElement heights written by AddLayoutElement are ignored and each row falls
            // back to the default 100 px RectTransform, spilling out of its window.
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return group;
        }

        /// <summary>
        /// A caption that lives inside a parchment window, so it is always written in crust brown.
        /// </summary>
        private static TMP_Text AddLabel(Transform parent, string name, string value, float fontSize, float height)
        {
            var text = CreateText(name, parent, value, fontSize, TextAlignmentOptions.Center);
            text.color = WarmBreadTheme.Crust;
            AddLayoutElement(text.gameObject, height);

            return text;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            float fontSize,
            TextAlignmentOptions alignment
        )
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, worldPositionStays: false);

            var text = root.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = TextColor;

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font)
            {
                text.font = font;
            }

            return text;
        }

        private static Image CreateImage(string name, Transform parent, string spriteName)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, worldPositionStays: false);

            var image = root.AddComponent<Image>();
            if (string.IsNullOrEmpty(spriteName) == false)
            {
                image.sprite = LoadSprite(spriteName);
            }

            return image;
        }

        /// <summary>
        /// On WebGL the application cannot be quit, so the exit button is hidden by the platform
        /// trigger (same as it was set up in the editor before).
        /// </summary>
        private static void WireWebGlExitButton(GameObject root, Button exitButton)
        {
            var trigger = root.GetComponent<PlatformTrigger>();
            if (trigger == false || exitButton == false)
            {
                return;
            }

            var serialized = new SerializedObject(trigger);
            var calls = serialized.FindProperty("onWebGl.m_PersistentCalls.m_Calls");
            if (calls == null)
            {
                return;
            }

            calls.arraySize = 1;

            var call = calls.GetArrayElementAtIndex(0);
            call.FindPropertyRelative("m_Target").objectReferenceValue = exitButton.gameObject;
            call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue =
                "UnityEngine.GameObject, UnityEngine";
            call.FindPropertyRelative("m_MethodName").stringValue = "SetActive";
            call.FindPropertyRelative("m_Mode").intValue = 6;
            call.FindPropertyRelative("m_Arguments.m_ObjectArgument")
                .objectReferenceValue = null;
            call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue =
                "UnityEngine.Object, UnityEngine";
            call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = false;
            call.FindPropertyRelative("m_CallState").intValue = 2;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyViewReferences(
            MainMenuView view,
            CanvasGroup canvasGroup,
            GameObject buttonsPanel,
            GameObject saveSlotsPanel,
            GameObject settingsPanel,
            Button continueButton,
            Button newGameButton,
            Button settingsButton,
            Button exitButton,
            Button[] saveSlotButtons,
            TMP_Text[] saveSlotLabels,
            Button saveSlotsBackButton,
            Button settingsBackButton,
            Slider lookSensitivitySlider,
            Slider masterVolumeSlider,
            GameObject journalPanel,
            GameObject achievementsPanel,
            Button journalButton,
            Button achievementsButton,
            Button journalBackButton,
            Button achievementsBackButton,
            TMP_Text[] journalRows,
            TMP_Text[] achievementRows
        )
        {
            var serialized = new SerializedObject(view);

            SetReference(serialized, "canvasGroup", canvasGroup);
            SetReference(serialized, "buttonsPanel", buttonsPanel);
            SetReference(serialized, "saveSlotsPanel", saveSlotsPanel);
            SetReference(serialized, "settingsPanel", settingsPanel);
            SetReference(serialized, "journalPanel", journalPanel);
            SetReference(serialized, "achievementsPanel", achievementsPanel);
            SetReference(serialized, "journalButton", journalButton);
            SetReference(serialized, "achievementsButton", achievementsButton);
            SetReference(serialized, "journalBackButton", journalBackButton);
            SetReference(serialized, "achievementsBackButton", achievementsBackButton);
            SetReferenceArray(serialized, "journalRows", journalRows);
            SetReferenceArray(serialized, "achievementRows", achievementRows);

            SetReference(serialized, "continueButton", continueButton);
            SetReference(serialized, "newGameButton", newGameButton);
            SetReference(serialized, "settingsButton", settingsButton);
            SetReference(serialized, "exitButton", exitButton);

            SetReference(serialized, "saveSlotsBackButton", saveSlotsBackButton);
            SetReference(serialized, "settingsBackButton", settingsBackButton);

            SetReference(serialized, "lookSensitivitySlider", lookSensitivitySlider);
            SetReference(serialized, "masterVolumeSlider", masterVolumeSlider);

            SetReferenceArray(serialized, "saveSlotButtons", saveSlotButtons);
            SetReferenceArray(serialized, "saveSlotLabels", saveSlotLabels);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(SerializedObject serialized, string name, Object value)
        {
            var property = serialized.FindProperty(name);
            if (property == null)
            {
                Debug.LogWarning($"Could not find serialized property {name}.");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static void SetReferenceArray<T>(SerializedObject serialized, string name, T[] values)
            where T : Object
        {
            var property = serialized.FindProperty(name);
            if (property == null)
            {
                Debug.LogWarning($"Could not find serialized property {name}.");
                return;
            }

            property.arraySize = values.Length;

            for (var index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void FixMenuSceneViewReference()
        {
            var prefabView = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)?.GetComponent<MainMenuView>();
            if (prefabView == false)
            {
                Debug.LogWarning($"Could not load {PrefabPath} view, scene reference is left as is.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponentInChildren<MainMenuViewController>(true);
                if (controller == false)
                {
                    continue;
                }

                var serialized = new SerializedObject(controller);
                var property = serialized.FindProperty("viewPrefab");
                if (property == null)
                {
                    continue;
                }

                property.objectReferenceValue = prefabView;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(controller);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        // The GameManager prefab already spawns a DontDestroyOnLoad EventSystem, so any
        // EventSystem living inside a scene only produces "multiple EventSystems" warnings.
        private static void RemoveRedundantEventSystems()
        {
            foreach (var scenePath in ScenesWithUi)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                var redundant = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include)
                    .Where(system => system.gameObject.scene == scene)
                    .ToArray();

                if (redundant.Length == 0)
                {
                    continue;
                }

                foreach (var system in redundant)
                {
                    Object.DestroyImmediate(system.gameObject);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, scenePath);

                Debug.Log($"Removed {redundant.Length} redundant EventSystem(s) from {scenePath}.");
            }
        }

        // The gameplay scene stored an "always active" override for the giving animation,
        // which kept the player's arm stretched out at all times. Driving it from code only.
        private static void FixGiveAnimationOverride()
        {
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefabRoot == false)
            {
                Debug.LogWarning($"Could not load {PlayerPrefabPath}, giving animation override is left as is.");
                return;
            }

            var giveAnimation = FindInChildren(prefabRoot.transform, GiveAnimationName);
            if (giveAnimation == null)
            {
                Debug.LogWarning($"Could not find {GiveAnimationName} in {PlayerPrefabPath}.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            var changed = false;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var instance in root.GetComponentsInChildren<Transform>(true))
                {
                    var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(instance.gameObject);
                    if (instanceRoot == null || instanceRoot != instance.gameObject)
                    {
                        continue;
                    }

                    var modifications = PrefabUtility.GetPropertyModifications(instanceRoot);
                    if (modifications == null)
                    {
                        continue;
                    }

                    var kept = modifications
                        .Where(modification => modification.target != giveAnimation.gameObject)
                        .ToArray();

                    if (kept.Length == modifications.Length)
                    {
                        continue;
                    }

                    PrefabUtility.SetPropertyModifications(instanceRoot, kept);
                    EditorUtility.SetDirty(instanceRoot);
                    changed = true;

                    Debug.Log($"Cleared the {GiveAnimationName} override on {instanceRoot.name} in {GameplayScenePath}.");
                }
            }

            if (changed == false)
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameplayScenePath);
        }

        private static Transform FindInChildren(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var found = FindInChildren(parent.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject LoadPrefabRoot()
        {
            try
            {
                return PrefabUtility.LoadPrefabContents(PrefabPath);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Failed to load {PrefabPath}: {exception.Message}");
                return null;
            }
        }

        private static void ClearChildren(GameObject root)
        {
            var transform = root.transform;

            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(transform.GetChild(index).gameObject);
            }
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>();
            if (component == null)
            {
                component = root.AddComponent<T>();
            }

            return component;
        }

        private static void AddLayoutElement(GameObject target, float preferredHeight)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.AddComponent<LayoutElement>();
            }

            element.minHeight = preferredHeight;
            element.preferredHeight = preferredHeight;
        }

        /// <summary>
        /// The view root is a canvas of its own, so it has to be a full screen, unscaled rect. Hand
        /// edits in the prefab stage used to leave it at a zero scale, which hides the whole menu.
        /// </summary>
        private static void NormalizeRoot(GameObject root)
        {
            if (root == null) return;

            root.transform.localScale = Vector3.one;

            if (root.transform is RectTransform rectTransform)
            {
                Stretch(rectTransform);
            }
        }

        private static void Stretch(RectTransform rectTransform, float padding = 0f)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(-padding * 2f, -padding * 2f);
        }

        private static void Anchor(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta
        )
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(anchorMin.x, anchorMin.y);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        /// <summary>
        /// The menu icons live outside of the project's sprite folder, so make sure they are
        /// imported as sprites before the prefab tries to reference them.
        /// </summary>
        private static void EnsureIconImport()
        {
            if (AssetDatabase.IsValidFolder(IconFolder) == false)
            {
                Debug.LogWarning($"Icon folder {IconFolder} does not exist, menu icons are skipped.");
                return;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }

                var isDirty = false;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    isDirty = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    isDirty = true;
                }

                if (importer.alphaIsTransparency == false)
                {
                    importer.alphaIsTransparency = true;
                    isDirty = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    isDirty = true;
                }

                if (importer.maxTextureSize > 2048)
                {
                    importer.maxTextureSize = 2048;
                    isDirty = true;
                }

                if (isDirty)
                {
                    importer.SaveAndReimport();
                    Debug.Log($"Imported {path} as a UI sprite.");
                }
            }
        }

        private static Sprite LoadSprite(string name)
        {
            var iconPath = IconFolder + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(iconPath) is { } iconSprite)
            {
                return iconSprite;
            }

            var path = SpriteFolder + name + ".png";

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == false)
            {
                Debug.LogWarning($"Could not load sprite at {path}.");
            }

            return sprite;
        }
    }
}
#endif
