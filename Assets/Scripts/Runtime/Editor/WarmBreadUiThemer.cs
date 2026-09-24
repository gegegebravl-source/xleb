#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.UI.Controllers;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Тела отключенных инструментов (после return-гвардов) оставлены намеренно — глушим CS0162.
#pragma warning disable 0162

namespace UABPetelnia.GGJ2025.Runtime.EditorTools
{
    /// <summary>
    /// Repaints the UI prefabs that are not built from scratch by one of the other tools: the pause
    /// menu, the game over screen, the shopper chat bubble and the cash display.
    /// </summary>
    /// <remarks>
    /// The pass is name based and idempotent, so it can be re-run at any time. Window frames and
    /// headings are added once and then reused.
    /// </remarks>
    public static class WarmBreadUiThemer
    {
        private const string PauseMenuPath = "Assets/Prefabs/UI/View_PauseMenu.prefab";
        private const string GameOverPath = "Assets/Prefabs/UI/View_GameOverMenu.prefab";
        private const string ChatPath = "Assets/Prefabs/UI/View_Chat.prefab";
        private const string MoneyPath = "Assets/Prefabs/UI/View_Money.prefab";
        private const string ToastPath = "Assets/Prefabs/UI/View_AchievementToast.prefab";
        private const string PlayerPrefabPath = "Assets/Prefabs/Actors/Actor_Player.prefab";

        private const string WindowName = "Window";
        private const string TitleName = "Text_Title";
        private const string FooterName = "Text_Footer";

        private const string KioskTagline = "Ларёк у дома · тёплый хлеб";

        private const string ToastObjectName = "View_AchievementToast";
        private const string ToastControllerName = "UI_AchievementToast";
        private const string ToastCaption = "НОВОЕ ДОСТИЖЕНИЕ";

        private static readonly Vector2 PauseWindowSize = new(880f, 620f);
        private static readonly Vector2 GameOverWindowSize = new(880f, 560f);

        private static readonly Vector2 GameOverButtonsPosition = new(0f, -115f);
        private static readonly Vector2 GameOverButtonsSize = new(620f, 180f);

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/UI/Apply Warm Bread Theme",
            priority = MenuItemConstants.BaseToolsItemPriority)]
        public static void ApplyTheme()
        {
            Debug.Log("[WarmBreadTheme] Процедурное оформление UI отключено: оформляйте меню и окна вручную в редакторе.");
            return;

            ThemePauseMenu();
            ThemeGameOverMenu();
            ThemeChat();
            ThemeMoney();
            ThemeAchievementToast();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[WarmBreadTheme] Оформление применено: пауза, конец игры, чат, касса и всплывашка достижений."
            );
        }

        private static void ThemePauseMenu()
        {
            EditPrefab(PauseMenuPath, root =>
            {
                var content = FindDeep(root.transform, "Panel_Buttons")?.parent;
                var window = content ? EnsureWindow(content, PauseWindowSize, "Пауза") : null;

                if (window)
                {
                    var monitor = WarmBreadTheme.CreateImage(
                        window,
                        "Image_KirillMonitor",
                        WarmBreadTheme.LoadArtSprite("пк и заказ/пк для азказов и тд.png", warn: false)
                    );

                    monitor.color = new Color(1f, 1f, 1f, 0.9f);
                    monitor.preserveAspect = true;
                    WarmBreadTheme.Place(
                        monitor.rectTransform,
                        anchor: new Vector2(0.5f, 0.5f),
                        pivot: new Vector2(0.5f, 0.5f),
                        position: new Vector2(-200f, 0f),
                        size: new Vector2(300f, 240f)
                    );

                    MoveInto(window, FindDeep(root.transform, "Panel_Buttons"), new Vector2(430f, 300f), new Vector2(200f, -10f));
                    EnsureFooter(window);
                }
            });
        }

        private static void ThemeGameOverMenu()
        {
            EditPrefab(GameOverPath, root =>
            {
                var content = FindDeep(root.transform, "Panel_Buttons")?.parent;
                var window = content ? EnsureWindow(content, GameOverWindowSize, "Смена окончена") : null;

                if (window == null)
                {
                    return;
                }

                MoveInto(
                    window,
                    FindDeep(root.transform, "Panel_Buttons"),
                    GameOverButtonsSize,
                    GameOverButtonsPosition
                );

                EnsureFooter(window);
                EnsureShiftReport(root, window);
            });
        }

        /// <summary>
        /// The shift report on the game over screen: the big earned total plus the counters, both
        /// filled in at runtime from the progress system.
        /// </summary>
        private static void EnsureShiftReport(GameObject root, Transform window)
        {
            var earned = EnsureText(
                window,
                "Text_Earned",
                "Заработано: 0.00 руб.",
                40f,
                WarmBreadTheme.Jam,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -104f),
                new Vector2(760f, 50f)
            );

            var stats = EnsureText(
                window,
                "Text_Stats",
                string.Empty,
                22f,
                WarmBreadTheme.Crust,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -158f),
                new Vector2(760f, 112f)
            );

            var view = root.GetComponent<GameOverView>();
            if (view == false)
            {
                Debug.LogWarning("[WarmBreadTheme] На префабе конца смены нет GameOverView.");

                return;
            }

            var serialized = new SerializedObject(view);
            serialized.FindProperty("earnedText").objectReferenceValue = earned;
            serialized.FindProperty("statsText").objectReferenceValue = stats;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Build the achievement popup and hang its controller on the player, so the card shows up
        /// in the middle of a shift without touching the menu.
        /// </summary>
        private static void ThemeAchievementToast()
        {
            var viewPrefab = RebuildToastPrefab();

            if (viewPrefab == false)
            {
                Debug.LogError("[WarmBreadTheme] Не удалось собрать префаб всплывашки достижений.");

                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (root == false)
            {
                Debug.LogError($"[WarmBreadTheme] Нет префаба игрока {PlayerPrefabPath}.");

                return;
            }

            try
            {
                var controller = root
                    .GetComponentsInChildren<AchievementToastViewController>(includeInactive: true)
                    .FirstOrDefault();

                if (controller == false)
                {
                    var holder = new GameObject(ToastControllerName);
                    holder.transform.SetParent(root.transform, worldPositionStays: false);
                    controller = holder.AddComponent<AchievementToastViewController>();
                }

                var serialized = new SerializedObject(controller);
                serialized.FindProperty("viewPrefab").objectReferenceValue = viewPrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log("[WarmBreadTheme] Всплывашка достижений собрана и повешена на игрока.");
        }

        /// <summary>
        /// Build the popup itself: a parchment ribbon at the top of the screen with the butter
        /// seal, the achievement name and what it was earned for.
        /// </summary>
        private static AchievementToastView RebuildToastPrefab()
        {
            var root = WarmBreadTheme.CreateOverlayCanvas(null, ToastObjectName, sortOrder: 260);

            try
            {
                var group = root.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                var view = root.AddComponent<AchievementToastView>();

                var body = WarmBreadTheme.CreateWindow(root.transform, "Body", new Vector2(640f, 128f));
                WarmBreadTheme.Place(
                    body.rectTransform,
                    anchor: new Vector2(0.5f, 1f),
                    pivot: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -34f),
                    size: new Vector2(640f, 128f)
                );

                var ribbon = WarmBreadTheme.CreateImage(body.transform, "Ribbon", null);
                ribbon.color = WarmBreadTheme.Butter;
                WarmBreadTheme.Place(
                    ribbon.rectTransform,
                    anchor: new Vector2(0f, 0.5f),
                    pivot: new Vector2(0f, 0.5f),
                    position: new Vector2(12f, 0f),
                    size: new Vector2(16f, 100f)
                );

                var seal = WarmBreadTheme.CreateImage(
                    body.transform,
                    "Image_Seal",
                    WarmBreadTheme.LoadSprite("UI_Icon_Achievements", warn: false)
                );

                seal.color = WarmBreadTheme.Butter;
                WarmBreadTheme.Place(
                    seal.rectTransform,
                    anchor: new Vector2(0f, 0.5f),
                    pivot: new Vector2(0f, 0.5f),
                    position: new Vector2(42f, 0f),
                    size: new Vector2(80f, 80f)
                );

                var caption = EnsureText(
                    body.transform,
                    "Text_Caption",
                    ToastCaption,
                    20f,
                    WarmBreadTheme.Jam,
                    anchor: new Vector2(0f, 1f),
                    pivot: new Vector2(0f, 1f),
                    new Vector2(140f, -18f),
                    new Vector2(470f, 28f),
                    TextAlignmentOptions.Left
                );

                var title = EnsureText(
                    body.transform,
                    "Text_Title",
                    "Достижение",
                    36f,
                    WarmBreadTheme.Crust,
                    anchor: new Vector2(0f, 1f),
                    pivot: new Vector2(0f, 1f),
                    new Vector2(140f, -48f),
                    new Vector2(470f, 46f),
                    TextAlignmentOptions.Left
                );

                var description = EnsureText(
                    body.transform,
                    "Text_Description",
                    string.Empty,
                    20f,
                    WarmBreadTheme.Crust,
                    anchor: new Vector2(0f, 0f),
                    pivot: new Vector2(0f, 0f),
                    new Vector2(140f, 14f),
                    new Vector2(470f, 26f),
                    TextAlignmentOptions.Left
                );

                var serialized = new SerializedObject(view);
                serialized.FindProperty("body").objectReferenceValue = body.rectTransform;
                serialized.FindProperty("captionText").objectReferenceValue = caption;
                serialized.FindProperty("titleText").objectReferenceValue = title;
                serialized.FindProperty("descriptionText").objectReferenceValue = description;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, ToastPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<AchievementToastView>(ToastPath);
        }

        private static void ThemeChat()
        {
            EditPrefab(ChatPath, root =>
            {
                var text = FindDeep(root.transform, "Text_Chat");
                if (text && text.TryGetComponent(out TMP_Text label))
                {
                    WarmBreadTheme.StyleText(label, WarmBreadTheme.Crust);
                }
            });
        }

        /// <summary>
        /// The cash display on the counter: amber digits on the dark calculator screen.
        /// </summary>
        private static void ThemeMoney()
        {
            EditPrefab(
                MoneyPath,
                root =>
                {
                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(includeInactive: true))
                    {
                        WarmBreadTheme.StyleText(text, WarmBreadTheme.Butter);
                    }
                },
                themeTexts: false
            );
        }

        private static void EditPrefab(string path, System.Action<GameObject> edit, bool themeTexts = true)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == false)
            {
                Debug.LogWarning($"[WarmBreadTheme] Нет префаба {path}.");

                return;
            }

            try
            {
                edit(root);
                StyleTree(root.transform, isOnParchment: false, themeTexts: themeTexts);

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Walk the hierarchy and paint every graphic by the role its name describes.
        /// </summary>
        private static void StyleTree(Transform transform, bool isOnParchment, bool themeTexts)
        {
            foreach (var image in transform.GetComponentsInChildren<Image>(includeInactive: true))
            {
                var name = image.name;
                var insideWindow = image.transform.parent && image.transform.parent.name == WindowName;
                var onParchment = isOnParchment || insideWindow;

                if (name.Contains("Backdrop") || name.Contains("Background_Black") || name == "Background")
                {
                    // Old full-screen artwork from the game this project started as. Dropped on
                    // purpose: the screen is painted out of the palette instead.
                    image.sprite = default;
                    image.color = WarmBreadTheme.CrustDark;
                    continue;
                }

                if (name == WarmBreadTheme.ParchmentName)
                {
                    image.sprite = default;
                    image.color = WarmBreadTheme.Parchment;
                    continue;
                }

                if (name == WindowName)
                {
                    image.sprite = default;
                    image.color = WarmBreadTheme.Crust;
                    image.raycastTarget = false;
                    WarmBreadTheme.EnsureParchment(image.transform);
                    continue;
                }

                if (name.StartsWith("Image_") && name != "Image_Icon")
                {
                    image.color = WarmBreadTheme.WindowTint;
                    continue;
                }

                if (onParchment)
                {
                    image.color = Color.white;
                }
            }

            foreach (var button in transform.GetComponentsInChildren<Button>(includeInactive: true))
            {
                var isExit = button.name.Contains("Exit");

                WarmBreadTheme.StyleButton(button, isExit ? WarmBreadTheme.Jam : WarmBreadTheme.Butter);
            }

            foreach (var slider in transform.GetComponentsInChildren<Slider>(includeInactive: true))
            {
                WarmBreadTheme.StyleSlider(slider);
            }

            if (themeTexts == false)
            {
                return;
            }

            foreach (var text in transform.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                // Text sits either on a parchment window (crust) or straight on the dark backdrop
                // (cream). Buttons always carry dark labels, and the shopper's bubble is a light
                // picture, so its text stays dark as well.
                var onParchment = IsUnderWindow(text.transform)
                    || text.GetComponentInParent<Button>()
                    || text.name == "Text_Chat";
                var isTitle = text.name == TitleName;
                var isFooter = text.name == FooterName;

                var color = isTitle
                    ? WarmBreadTheme.Crust
                    : onParchment
                        ? WarmBreadTheme.Crust
                        : isFooter
                            ? WarmBreadTheme.CreamSoft
                            : WarmBreadTheme.Cream;

                WarmBreadTheme.StyleText(text, color);
            }
        }

        private static bool IsUnderWindow(Transform transform)
        {
            for (var current = transform; current; current = current.parent)
            {
                if (current.name == WindowName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find or create the parchment window a screen sits inside, together with its heading.
        /// </summary>
        private static Transform EnsureWindow(Transform content, Vector2 size, string title)
        {
            var existing = content.Find(WindowName);
            var window = existing ? existing : WarmBreadTheme.CreateWindow(content, WindowName, size).transform;

            if (window.TryGetComponent(out Image frame))
            {
                frame.sprite = default;
                frame.color = WarmBreadTheme.Crust;
                frame.raycastTarget = false;
            }

            WarmBreadTheme.EnsureParchment(window);

            if (window.Find(TitleName) == false)
            {
                var heading = WarmBreadTheme.CreateTitle(window, TitleName, title, 56f);
                WarmBreadTheme.Place(
                    heading.rectTransform,
                    anchor: new Vector2(0.5f, 1f),
                    pivot: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -28f),
                    size: new Vector2(size.x - 80f, 72f)
                );
            }

            return window;
        }

        private static void EnsureFooter(Transform window)
        {
            if (window.Find(FooterName))
            {
                return;
            }

            var footer = WarmBreadTheme.CreateText(
                window,
                FooterName,
                KioskTagline,
                22f,
                TextAlignmentOptions.Center
            );

            footer.color = WarmBreadTheme.CreamSoft;

            WarmBreadTheme.Place(
                footer.rectTransform,
                anchor: new Vector2(0.5f, 0f),
                pivot: new Vector2(0.5f, 0f),
                position: new Vector2(0f, 18f),
                size: new Vector2(520f, 32f)
            );
        }

        /// <summary>
        /// Re-parent the button block into the window and centre it, keeping its own layout group.
        /// </summary>
        private static void MoveInto(
            Transform window,
            Transform child,
            Vector2 size,
            Vector2 position = default
        )
        {
            if (child == false)
            {
                return;
            }

            if (child.parent != window)
            {
                child.SetParent(window, worldPositionStays: false);
            }

            if (child is not RectTransform rectTransform)
            {
                return;
            }

            WarmBreadTheme.Place(
                rectTransform,
                anchor: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                position: position == default ? new Vector2(0f, -20f) : position,
                size: size
            );
        }

        /// <summary>
        /// Find a caption by name, or create it on the warm bread palette and place it.
        /// </summary>
        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            Color color,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center
        )
        {
            var existing = parent.Find(name);
            var text = existing
                ? existing.GetComponent<TMP_Text>()
                : WarmBreadTheme.CreateText(parent, name, value, fontSize, alignment);

            if (text == false)
            {
                return default;
            }

            if (string.IsNullOrEmpty(text.text))
            {
                text.text = value;
            }

            WarmBreadTheme.StyleText(text, color, fontSize);
            text.alignment = alignment;

            WarmBreadTheme.Place(text.rectTransform, anchor, pivot, position, size);

            return text;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (transform.name == name)
                {
                    return transform;
                }
            }

            return null;
        }
    }
}
#endif
