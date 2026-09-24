#if UNITY_EDITOR
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.UI.Controllers;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.EditorTools
{
    /// <summary>
    /// Собирает экран настроек: звук, чувствительность, экран, сброс. Экран один на всю игру —
    /// он живёт на менеджере, поэтому доступен и из меню, и из паузы.
    /// </summary>
    public static class SettingsPanelBuilder
    {
        private const string ViewPrefabPath = "Assets/Prefabs/UI/View_Settings.prefab";
        private const string ManagerPrefabPath = "Assets/Prefabs/GGJ2025GameManager.prefab";
        private const string PausePrefabPath = "Assets/Prefabs/UI/View_PauseMenu.prefab";
        private const string GeneralSettingsPath = "Assets/Settings/Game/Settings_General.asset";

        private const float WindowWidth = 760f;
        private const float WindowHeight = 620f;
        private const float RowHeight = 56f;

        private static readonly Color PanelColor = new(0.10f, 0.055f, 0.035f, 0.96f);

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/UI/Build Settings Screen",
            priority = MenuItemConstants.BaseToolsItemPriority)]
        public static void Build()
        {
            var viewPrefab = BuildViewPrefab();
            WireManager(viewPrefab);
            AddPauseButton();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SettingsPanel] Экран настроек собран.");
        }

        private static GameObject BuildViewPrefab()
        {
            var root = WarmBreadTheme.CreateOverlayCanvas(null, "View_Settings", sortOrder: 30);
            var view = root.AddComponent<SettingsView>();

            var backdrop = WarmBreadTheme.CreateBackdrop(root.transform);
            if (backdrop != null)
            {
                backdrop.color = new Color(0f, 0f, 0f, 0.6f);
            }

            var window = WarmBreadTheme.CreateImage(root.transform, "Window", null);
            window.color = PanelColor;
            WarmBreadTheme.Place(
                window.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(WindowWidth, WindowHeight)
            );

            var title = WarmBreadTheme.CreateTitle(window.transform, "Text_Title", "Настройки", 40f);
            WarmBreadTheme.Place(
                title.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -20f),
                new Vector2(WindowWidth - 60f, 48f)
            );

            var y = -96f;

            var master = AddSliderRow(window.transform, "Звук: общая громкость", ref y, 0f, 100f, 100f);
            var music = AddSliderRow(window.transform, "Звук: музыка", ref y, 0f, 100f, 100f);
            var sfx = AddSliderRow(window.transform, "Звук: эффекты", ref y, 0f, 100f, 100f);

            y -= 14f;
            var sensitivity = AddSliderRow(
                window.transform,
                "Управление: чувствительность мыши",
                ref y,
                GeneralSettings.MinLookSensitivity,
                GeneralSettings.MaxLookSensitivity,
                5f
            );

            y -= 14f;
            var fullscreen = AddToggleRow(window.transform, "Экран: полный экран", ref y);
            var vsync = AddToggleRow(window.transform, "Экран: вертикальная синхронизация", ref y);

            var reset = CreateButton(window.transform, "Button_Reset", "Сбросить", 24f, new Vector2(180f, 54f));
            WarmBreadTheme.Place(
                reset.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-200f, 22f),
                new Vector2(180f, 54f)
            );

            var save = CreateButton(window.transform, "Button_Save", "Сохранить", 24f, new Vector2(180f, 54f));
            WarmBreadTheme.Place(
                save.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 22f),
                new Vector2(180f, 54f)
            );

            var close = CreateButton(window.transform, "Button_Close", "Закрыть", 24f, new Vector2(180f, 54f));
            WarmBreadTheme.Place(
                close.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(200f, 22f),
                new Vector2(180f, 54f)
            );

            var status = WarmBreadTheme.CreateText(window.transform, "Text_Status", "Изменения применяются сразу", 22f, TextAlignmentOptions.Center);
            status.color = WarmBreadTheme.CreamSoft;
            WarmBreadTheme.Place(
                status.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 86f),
                new Vector2(WindowWidth - 60f, 30f)
            );

            var serialized = new SerializedObject(view);
            serialized.FindProperty("masterSlider").objectReferenceValue = master.slider;
            serialized.FindProperty("masterValue").objectReferenceValue = master.value;
            serialized.FindProperty("musicSlider").objectReferenceValue = music.slider;
            serialized.FindProperty("musicValue").objectReferenceValue = music.value;
            serialized.FindProperty("sfxSlider").objectReferenceValue = sfx.slider;
            serialized.FindProperty("sfxValue").objectReferenceValue = sfx.value;
            serialized.FindProperty("sensitivitySlider").objectReferenceValue = sensitivity.slider;
            serialized.FindProperty("sensitivityValue").objectReferenceValue = sensitivity.value;
            serialized.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
            serialized.FindProperty("vsyncToggle").objectReferenceValue = vsync;
            serialized.FindProperty("resetButton").objectReferenceValue = reset;
            serialized.FindProperty("saveButton").objectReferenceValue = save;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ViewPrefabPath);
            Object.DestroyImmediate(root);

            return prefab;
        }

        private static (Slider slider, TMP_Text value) AddSliderRow(
            Transform parent,
            string label,
            ref float y,
            float min,
            float max,
            float current
        )
        {
            var caption = WarmBreadTheme.CreateText(parent, "Text_Label", label, 26f, TextAlignmentOptions.Left);
            caption.color = WarmBreadTheme.Cream;
            WarmBreadTheme.Place(
                caption.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(34f, y),
                new Vector2(420f, RowHeight)
            );

            var slider = CreateSlider(parent, "Slider", new Vector2(200f, 26f), min, max, current);
            WarmBreadTheme.Place(
                slider.GetComponent<RectTransform>(),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-120f, y - 14f),
                new Vector2(200f, 26f)
            );

            var value = WarmBreadTheme.CreateText(parent, "Text_Value", "—", 26f, TextAlignmentOptions.Right);
            value.color = WarmBreadTheme.Butter;
            WarmBreadTheme.Place(
                value.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-30f, y),
                new Vector2(84f, RowHeight)
            );

            y -= RowHeight + 8f;

            return (slider, value);
        }

        private static Toggle AddToggleRow(Transform parent, string label, ref float y)
        {
            var caption = WarmBreadTheme.CreateText(parent, "Text_Label", label, 26f, TextAlignmentOptions.Left);
            caption.color = WarmBreadTheme.Cream;
            WarmBreadTheme.Place(
                caption.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(34f, y),
                new Vector2(520f, RowHeight)
            );

            var go = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            go.layer = 5;
            go.transform.SetParent(parent, worldPositionStays: false);

            var boxGo = new GameObject("Box", typeof(RectTransform), typeof(Image));
            boxGo.layer = 5;
            boxGo.transform.SetParent(go.transform, worldPositionStays: false);

            var boxImage = boxGo.GetComponent<Image>();
            boxImage.color = new Color(0.18f, 0.10f, 0.06f, 0.95f);
            WarmBreadTheme.Place(
                boxImage.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(30f, 30f)
            );

            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.layer = 5;
            checkGo.transform.SetParent(boxGo.transform, worldPositionStays: false);

            var checkImage = checkGo.GetComponent<Image>();
            checkImage.color = WarmBreadTheme.Butter;
            WarmBreadTheme.Place(
                checkImage.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(18f, 18f)
            );

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;

            WarmBreadTheme.Place(
                go.GetComponent<RectTransform>(),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-40f, y - 12f),
                new Vector2(40f, 40f)
            );

            y -= RowHeight + 8f;

            return toggle;
        }

        private static Slider CreateSlider(
            Transform parent,
            string name,
            Vector2 size,
            float min,
            float max,
            float value
        )
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.layer = 5;
            go.transform.SetParent(parent, worldPositionStays: false);
            go.GetComponent<RectTransform>().sizeDelta = size;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.layer = 5;
            background.transform.SetParent(go.transform, worldPositionStays: false);
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.18f, 0.10f, 0.06f, 0.95f);
            var backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.layer = 5;
            fillArea.transform.SetParent(go.transform, worldPositionStays: false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.layer = 5;
            fill.transform.SetParent(fillArea.transform, worldPositionStays: false);
            var fillImage = fill.GetComponent<Image>();
            fillImage.color = WarmBreadTheme.Butter;
            var fillRect = fillImage.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.layer = 5;
            handleArea.transform.SetParent(go.transform, worldPositionStays: false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.layer = 5;
            handle.transform.SetParent(handleArea.transform, worldPositionStays: false);
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = WarmBreadTheme.Cream;
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 34f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.value = value;

            WarmBreadTheme.StyleSlider(slider);

            return slider;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            float fontSize,
            Vector2 size
        )
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.layer = 5;
            go.transform.SetParent(parent, worldPositionStays: false);

            var image = go.GetComponent<Image>();
            image.color = WarmBreadTheme.Butter;

            go.GetComponent<RectTransform>().sizeDelta = size;

            var text = WarmBreadTheme.CreateText(go.transform, "Text (TMP)", label, fontSize, TextAlignmentOptions.Center);
            text.color = WarmBreadTheme.Crust;
            WarmBreadTheme.Stretch(text.rectTransform);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            WarmBreadTheme.StyleButton(button, WarmBreadTheme.Butter);

            return button;
        }

        private static void WireManager(GameObject viewPrefab)
        {
            var root = PrefabUtility.LoadPrefabContents(ManagerPrefabPath);

            try
            {
                var existing = root.transform.Find("UI_Settings");
                GameObject controllerGo;

                if (existing != null)
                {
                    controllerGo = existing.gameObject;
                }
                else
                {
                    controllerGo = new GameObject("UI_Settings", typeof(RectTransform));
                    controllerGo.transform.SetParent(root.transform, worldPositionStays: false);
                }

                var controller = controllerGo.GetComponent<SettingsViewController>();
                if (controller == null)
                {
                    controller = controllerGo.AddComponent<SettingsViewController>();
                }

                var serialized = new SerializedObject(controller);
                serialized.FindProperty("viewPrefab").objectReferenceValue = viewPrefab.GetComponent<SettingsView>();
                serialized.FindProperty("generalSettings").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GeneralSettings>(GeneralSettingsPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, ManagerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddPauseButton()
        {
            var root = PrefabUtility.LoadPrefabContents(PausePrefabPath);

            try
            {
                var resume = root.transform.Find("Content/Window/Panel_Buttons/Button_Resume");
                if (resume == null)
                {
                    Debug.LogWarning("[SettingsPanel] В меню паузы не найдена кнопка «Продолжить».");

                    return;
                }

                if (root.transform.Find("Content/Window/Panel_Buttons/Button_Settings") != null)
                {
                    return;
                }

                var settings = CreateButton(
                    resume.parent,
                    "Button_Settings",
                    "Настройки",
                    32f,
                    new Vector2(320f, 58f)
                );

                // Кнопку вешаем на вью паузы: она сама расставит её по сетке карточки.
                var pauseView = root.GetComponent<PauseMenuView>();
                if (pauseView != null)
                {
                    var viewSerialized = new SerializedObject(pauseView);
                    viewSerialized.FindProperty("settingsButton").objectReferenceValue = settings;
                    viewSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                var resumeRect = resume.GetComponent<RectTransform>();
                WarmBreadTheme.Place(
                    settings.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(resumeRect.anchoredPosition.x, resumeRect.anchoredPosition.y - 70f),
                    new Vector2(320f, 58f)
                );

                PrefabUtility.SaveAsPrefabAsset(root, PausePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
