#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.EditorTools
{
    /// <summary>
    /// One place for the look of the game: warm bread kiosk. Crust browns, parchment creams and
    /// butter highlights, with the retro pixel font the menus already use.
    /// </summary>
    /// <remarks>
    /// Every UI builder and the theming pass read their colours from here, so the whole game can be
    /// re-skinned by editing this file and re-running the tools.
    /// </remarks>
    internal static class WarmBreadTheme
    {
        // Sprites live in these two folders, icons first.
        public const string IconFolder = "Assets/Visuals/UI/Icons/";
        public const string SpriteFolder = "Assets/Visuals/UI/Sprites/";
        public const string FontPath = "Assets/Visuals/Fonts/Font_Tiny5_Regular_SDF.asset";

        /// <summary>
        /// Name of the inner parchment plate every window gets. The frame is the window itself,
        /// this child is what content sits on.
        /// </summary>
        public const string ParchmentName = "Parchment";

        /// <summary>How thick the crust border around a parchment window is.</summary>
        public const float FrameThickness = 8f;

        /// <summary>Dark crust: backdrops behind windows.</summary>
        public static readonly Color CrustDark = new(0.11f, 0.06f, 0.04f, 0.82f);

        /// <summary>Crust: text and outlines on parchment.</summary>
        public static readonly Color Crust = new(0.29f, 0.16f, 0.09f, 1f);

        /// <summary>Parchment: text on the dark crust.</summary>
        public static readonly Color Cream = new(0.96f, 0.90f, 0.78f, 1f);

        /// <summary>Second level parchment: captions and hints.</summary>
        public static readonly Color CreamSoft = new(0.85f, 0.77f, 0.62f, 1f);

        /// <summary>Butter: buttons, sliders, highlights.</summary>
        public static readonly Color Butter = new(0.99f, 0.80f, 0.36f, 1f);

        /// <summary>Jam: the exit button and anything destructive.</summary>
        public static readonly Color Jam = new(0.79f, 0.29f, 0.20f, 1f);

        /// <summary>Leaf: the money display inside the kiosk.</summary>
        public static readonly Color Leaf = new(0.62f, 0.72f, 0.31f, 1f);

        /// <summary>Warm tint multiplied over window artwork so it never looks cold.</summary>
        public static readonly Color WindowTint = new(1f, 0.97f, 0.92f, 1f);

        /// <summary>How the window artwork is tinted behind dark text.</summary>
        public static readonly Color ParchmentTint = new(1f, 1f, 1f, 1f);

        /// <summary>Plate colour of a window, what the text and buttons sit on.</summary>
        public static readonly Color Parchment = new(0.96f, 0.90f, 0.78f, 1f);

        /// <summary>Darker parchment used for inset fields inside a window.</summary>
        public static readonly Color ParchmentShade = new(0.90f, 0.82f, 0.66f, 1f);

        /// <summary>Soft highlight drawn across the top of a button, gives it a pressed-tin feel.</summary>
        public static readonly Color Shine = new(1f, 1f, 1f, 0.22f);

        public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        /// <summary>
        /// Look a sprite up in the icon folder first and the UI sprite folder second. Pass
        /// <paramref name="warn"/> as false for optional artwork that may not be imported yet.
        /// </summary>
        public static Sprite LoadSprite(string name, bool warn = true)
        {
            var iconPath = IconFolder + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(iconPath) is { } icon)
            {
                return icon;
            }

            var path = SpriteFolder + name + ".png";

            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) is { } sprite)
            {
                return sprite;
            }

            if (warn)
            {
                Debug.LogWarning($"[WarmBreadTheme] Не найден спрайт {name}.");
            }

            return default;
        }

        /// <summary>
        /// Load a sprite from the imported Kirill art pack, which is stored under Assets/Art/Kirill
        /// with descriptive folder names.
        /// </summary>
        public static Sprite LoadArtSprite(string relativePath, bool warn = true)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return default;
            }

            var fullPath = relativePath.StartsWith("Assets/")
                ? relativePath
                : $"Assets/Art/Kirill/{relativePath}";

            if (AssetDatabase.LoadAssetAtPath<Sprite>(fullPath) is { } artSprite)
            {
                return artSprite;
            }

            if (warn)
            {
                Debug.LogWarning($"[WarmBreadTheme] Не найден арт-спрайт {relativePath}.");
            }

            return default;
        }

        /// <summary>
        /// Paint a button: butter blank, crust label, warm hover and a jam-red pressed state so
        /// every click feels like a till key.
        /// </summary>
        public static void StyleButton(Button button, Color? normal = null)
        {
            if (button == false)
            {
                return;
            }

            var baseColor = normal ?? Butter;

            if (button.targetGraphic is Image image)
            {
                // Flat fill on purpose: the look comes from the palette, not from the artwork of
                // whatever game this project started as.
                image.sprite = default;
                image.type = Image.Type.Simple;
                image.color = Color.white;
            }

            EnsureShine(button.transform);

            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Lighten(baseColor, 0.14f);
            colors.pressedColor = Jam;
            colors.selectedColor = Lighten(baseColor, 0.08f);
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;

            foreach (var text in button.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                StyleText(text, Crust);
            }
        }

        /// <summary>
        /// Paint a caption. The font is the project's retro pixel font, which is what gives the
        /// menus their little-window-shop feel.
        /// </summary>
        public static void StyleText(TMP_Text text, Color color, float? fontSize = null)
        {
            if (text == false)
            {
                return;
            }

            text.color = color;

            if (fontSize.HasValue)
            {
                text.fontSize = fontSize.Value;
            }

            var font = Font;
            if (font)
            {
                text.font = font;
            }
        }

        /// <summary>
        /// Paint a slider as a butter gauge on dark crust.
        /// </summary>
        public static void StyleSlider(Slider slider)
        {
            if (slider == false)
            {
                return;
            }

            if (slider.fillRect && slider.fillRect.TryGetComponent(out Image fill))
            {
                fill.color = Butter;
            }

            if (slider.handleRect && slider.handleRect.TryGetComponent(out Image handle))
            {
                handle.color = Cream;
            }

            if (slider.targetGraphic is Image target)
            {
                target.color = Cream;
            }

            foreach (var image in slider.GetComponentsInChildren<Image>(includeInactive: true))
            {
                if (image.name == "Background")
                {
                    image.color = CrustDark;
                    image.sprite = default;
                }
            }
        }

        /// <summary>
        /// Create a parchment window: a dark crust frame with a lighter plate inside, so a screen
        /// reads as one piece without depending on any leftover artwork.
        /// </summary>
        public static Image CreateWindow(
            Transform parent,
            string name,
            Vector2 size,
            Sprite sprite = null
        )
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(parent, worldPositionStays: false);

            var image = root.AddComponent<Image>();
            image.sprite = default;
            image.color = Crust;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = size;

            EnsureParchment(root.transform);

            return image;
        }

        /// <summary>
        /// Make sure the parchment plate exists inside a window and covers it apart from the frame.
        /// </summary>
        public static Image EnsureParchment(Transform window)
        {
            var existing = window.Find(ParchmentName);

            if (existing && existing.TryGetComponent(out Image found))
            {
                found.color = Parchment;

                return found;
            }

            var plate = new GameObject(ParchmentName, typeof(RectTransform));
            plate.layer = 5;
            plate.transform.SetParent(window, worldPositionStays: false);

            var image = plate.AddComponent<Image>();
            image.sprite = default;
            image.color = Parchment;
            image.raycastTarget = false;

            Stretch(image.rectTransform, FrameThickness);
            plate.transform.SetAsFirstSibling();

            return image;
        }

        /// <summary>
        /// Add the thin highlight across the top of a button once, so painted buttons look pressed
        /// out of a sheet rather than flat.
        /// </summary>
        public static void EnsureShine(Transform button)
        {
            const string shineName = "Shine";

            if (button.Find(shineName))
            {
                return;
            }

            var shine = new GameObject(shineName, typeof(RectTransform));
            shine.layer = 5;
            shine.transform.SetParent(button, worldPositionStays: false);

            var image = shine.AddComponent<Image>();
            image.sprite = default;
            image.color = Shine;
            image.raycastTarget = false;

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -4f);
            rectTransform.sizeDelta = new Vector2(-8f, 6f);

            shine.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// Create a dark backdrop that dims whatever is behind the window.
        /// </summary>
        public static Image CreateBackdrop(Transform parent, string name = "Backdrop")
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(parent, worldPositionStays: false);

            var image = root.AddComponent<Image>();
            image.color = CrustDark;
            image.sprite = default;

            Stretch(image.rectTransform);

            return image;
        }

        /// <summary>
        /// Create a plain overlay canvas: its own scaler and raycaster, sized like the rest of the
        /// interface, so a screen or a popup can live on its own without touching the scene canvas.
        /// </summary>
        public static GameObject CreateOverlayCanvas(Transform parent, string name, int sortOrder)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup)
            );

            root.layer = 5;
            root.transform.SetParent(parent, worldPositionStays: false);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            return root;
        }

        /// <summary>
        /// Create a plain image with given <paramref name="sprite"/> (or a flat rectangle when there
        /// is none), so a builder can lay out a screen without repeating the boilerplate.
        /// </summary>
        public static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(parent, worldPositionStays: false);

            var image = root.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;

            return image;
        }

        /// <summary>
        /// Create a themed heading, used for screen titles.
        /// </summary>
        public static TMP_Text CreateTitle(Transform parent, string name, string value, float fontSize)
        {
            var text = CreateText(parent, name, value, fontSize, TextAlignmentOptions.Center);
            text.color = Butter;

            return text;
        }

        public static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            TextAlignmentOptions alignment
        )
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(parent, worldPositionStays: false);

            var text = root.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Cream;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var font = Font;
            if (font)
            {
                text.font = font;
            }

            return text;
        }

        public static void Stretch(RectTransform rectTransform, float padding = 0f)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(-padding * 2f, -padding * 2f);
        }

        public static void Place(
            RectTransform rectTransform,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size
        )
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        private static Color Lighten(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a
            );
        }
    }
}
#endif
