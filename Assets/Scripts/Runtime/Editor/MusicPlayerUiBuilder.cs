#if UNITY_EDITOR
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.UI.Controllers;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.EditorTools
{
    /// <summary>
    /// Собирает меню плеера: окно с прокручиваемым списком песен, строкой «сейчас играет»
    /// и кнопкой закрытия. Контроллер кладётся на префаб игрока, как остальной HUD.
    /// </summary>
    /// <remarks>
    /// Инструмент идемпотентный: префаб вью пересобирается целиком, а на игроке остаётся
    /// контроллер <see cref="MusicPlayerViewController"/> со ссылкой на префаб вью.
    /// </remarks>
    public static class MusicPlayerUiBuilder
    {
        private const string ViewPrefabPath = "Assets/Prefabs/UI/View_MusicPlayer.prefab";
        private const string PlayerPrefabPath = "Assets/Prefabs/Actors/Actor_Player.prefab";
        private const string ControllerObjectName = "UI_MusicPlayer";

        private const float WindowWidth = 620f;
        private const float WindowHeight = 560f;
        private const float RowHeight = 56f;

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/UI/Build Music Player Menu",
            priority = MenuItemConstants.BaseToolsItemPriority)]
        public static void Build()
        {
            var viewPrefab = RebuildViewPrefab();
            EnsurePlayerController(viewPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MusicPlayer] Меню плеера собрано.");
        }

        private static GameObject RebuildViewPrefab()
        {
            var root = WarmBreadTheme.CreateOverlayCanvas(null, "View_MusicPlayer", sortOrder: 15);
            root.transform.localScale = Vector3.one;

            var view = root.AddComponent<MusicPlayerView>();
            var scrim = WarmBreadTheme.LoadSprite("Generated/UI_Scrim_Rounded", warn: false);

            // Затемнение фона, чтобы список читался поверх сцены.
            var backdrop = WarmBreadTheme.CreateImage(root.transform, "Backdrop", null);
            backdrop.color = WarmBreadTheme.CrustDark;
            backdrop.raycastTarget = true;
            WarmBreadTheme.Stretch(backdrop.rectTransform);

            // Окно: тёмная рамка + пергамент внутри.
            var window = WarmBreadTheme.CreateWindow(root.transform, "Window", new Vector2(WindowWidth, WindowHeight));
            window.sprite = scrim;
            window.color = new Color(0.29f, 0.16f, 0.09f, 1f); // Crust
            window.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.layer = 5;
            content.transform.SetParent(window.transform, worldPositionStays: false);
            var contentRect = content.GetComponent<RectTransform>();
            WarmBreadTheme.Stretch(contentRect, 24f);

            // Заголовок.
            var title = WarmBreadTheme.CreateText(content.transform, "Text_Title", "Плеер", 36f, TextAlignmentOptions.Center);
            title.color = WarmBreadTheme.Crust;
            title.fontStyle = FontStyles.Bold;
            WarmBreadTheme.Place(title.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -6f), new Vector2(WindowWidth - 48f, 48f));

            // Прокручиваемый список песен.
            var scrollRect = CreateSongList(content.transform, scrim);
            WarmBreadTheme.Place(scrollRect.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -62f), new Vector2(WindowWidth - 48f, 320f));

            // Шаблон строки: выключенная кнопка, из неё клонируются строки списка.
            var rowTemplate = CreateRowTemplate(scrollRect.content, scrim);
            rowTemplate.gameObject.SetActive(false);

            // Строка «сейчас играет».
            var nowPlaying = WarmBreadTheme.CreateText(content.transform, "Text_NowPlaying", "Ничего не играет", 24f, TextAlignmentOptions.Center);
            nowPlaying.color = WarmBreadUiColors.CrustSoft;
            WarmBreadTheme.Place(nowPlaying.rectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 92f), new Vector2(WindowWidth - 48f, 30f));

            // Кнопка закрытия.
            var closeButton = CreateThemedButton(content.transform, "Button_Close", "Закрыть", 60f);
            WarmBreadTheme.Place(closeButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(WindowWidth - 48f, 60f));

            // Ссылки вью.
            var serialized = new SerializedObject(view);
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("nowPlayingText").objectReferenceValue = nowPlaying;
            serialized.FindProperty("rowsContent").objectReferenceValue = scrollRect.content;
            serialized.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ViewPrefabPath);
            Object.DestroyImmediate(root);

            return prefab;
        }

        private static ScrollRect CreateSongList(Transform parent, Sprite scrim)
        {
            var rootGo = new GameObject("SongList", typeof(RectTransform), typeof(ScrollRect));
            rootGo.layer = 5;
            rootGo.transform.SetParent(parent, worldPositionStays: false);
            var rootRect = rootGo.GetComponent<RectTransform>();

            // Подложка списка: чуть темнее пергамента, мягкие углы.
            var listBack = rootGo.AddComponent<Image>();
            listBack.sprite = scrim;
            listBack.color = WarmBreadTheme.ParchmentShade;
            listBack.raycastTarget = false;
            WarmBreadTheme.Stretch(rootRect, 6f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.layer = 5;
            viewportGo.transform.SetParent(rootGo.transform, worldPositionStays: false);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.sprite = scrim;
            viewportImage.color = new Color(1f, 1f, 1f, 0.35f);
            var viewportMask = viewportGo.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            WarmBreadTheme.Stretch(viewportGo.GetComponent<RectTransform>(), 10f);

            var contentGo = new GameObject("Rows", typeof(RectTransform));
            contentGo.layer = 5;
            contentGo.transform.SetParent(viewportGo.transform, worldPositionStays: false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 8, 0, 0);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = rootGo.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect(viewportGo);
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.horizontalScrollbar = null;
            scrollRect.verticalScrollbar = null;

            return scrollRect;
        }

        private static RectTransform viewportRect(GameObject viewportGo)
        {
            var rect = viewportGo.GetComponent<RectTransform>();
            WarmBreadTheme.Stretch(rect, 10f);
            return rect;
        }

        private static MusicPlayerRowView CreateRowTemplate(RectTransform content, Sprite scrim)
        {
            var rowGo = new GameObject("RowTemplate", typeof(RectTransform));
            rowGo.layer = 5;
            rowGo.transform.SetParent(content, worldPositionStays: false);

            var image = rowGo.AddComponent<Image>();
            image.sprite = scrim;
            image.color = Color.white;
            image.type = Image.Type.Simple;

            var button = rowGo.AddComponent<Button>();
            button.targetGraphic = image;
            WarmBreadTheme.StyleButton(button, WarmBreadUiColors.Plank);

            var element = rowGo.AddComponent<LayoutElement>();
            element.minHeight = RowHeight;
            element.preferredHeight = RowHeight;

            var label = WarmBreadTheme.CreateText(rowGo.transform, "Text (TMP)", "Песня", 26f, TextAlignmentOptions.Left);
            label.color = WarmBreadTheme.Crust;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24f, 8f);
            labelRect.offsetMax = new Vector2(-24f, -8f);
            label.raycastTarget = false;

            var rowView = rowGo.AddComponent<MusicPlayerRowView>();
            var serialized = new SerializedObject(rowView);
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return rowView;
        }

        private static Button CreateThemedButton(Transform parent, string name, string label, float height)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(parent, worldPositionStays: false);

            var image = root.AddComponent<Image>();
            image.sprite = WarmBreadTheme.LoadSprite("UI_ButtonEmpty", warn: false);
            image.type = Image.Type.Simple;
            image.color = Color.white;

            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            WarmBreadTheme.StyleButton(button, WarmBreadTheme.Butter);

            var element = root.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;

            var text = WarmBreadTheme.CreateText(root.transform, "Text (TMP)", label, 28f, TextAlignmentOptions.Center);
            text.color = WarmBreadTheme.Crust;
            text.fontStyle = FontStyles.Bold;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 8f);
            textRect.offsetMax = new Vector2(-24f, -8f);
            text.raycastTarget = false;

            return button;
        }

        private static void EnsurePlayerController(GameObject viewPrefab)
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            try
            {
                var existing = root.transform.Find(ControllerObjectName);
                GameObject controllerGo;

                if (existing != null)
                {
                    controllerGo = existing.gameObject;
                }
                else
                {
                    controllerGo = new GameObject(ControllerObjectName, typeof(RectTransform));
                    controllerGo.transform.SetParent(root.transform, worldPositionStays: false);
                }

                var controller = controllerGo.GetComponent<MusicPlayerViewController>();
                if (controller == null)
                {
                    controller = controllerGo.AddComponent<MusicPlayerViewController>();
                }

                var serialized = new SerializedObject(controller);
                serialized.FindProperty("viewPrefab").objectReferenceValue =
                    viewPrefab.GetComponent<MusicPlayerView>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // Камера не должна крутиться, пока открыто меню плеера.
                var cameraLook = root.GetComponentInChildren<PlayerCameraLook>(true);
                if (cameraLook != false)
                {
                    var lookSerialized = new SerializedObject(cameraLook);
                    lookSerialized.FindProperty("musicPlayerViewController").objectReferenceValue = controller;
                    lookSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
