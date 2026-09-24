#if UNITY_EDITOR
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Components.Utilities;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.EditorTools
{
    /// <summary>
    /// Добавляет покупателям реплику над головой (мировую плашку с текстом заказа) и
    /// приводит плашку экранного чата к чистому виду без пиксельного градиента.
    /// </summary>
    public static class SpeechBubbleBuilder
    {
        private const string ShopperPrefabPath = "Assets/Prefabs/Actors/Actor_Shopper.prefab";
        private const string ChatViewPrefabPath = "Assets/Prefabs/UI/View_Chat.prefab";
        private const string BubbleName = "SpeechBubble";

        // Пергаментная плашка с тёмным текстом: читается и на тёмной улице, и на снегу,
        // и в палитре Warm Bread (см. WarmBreadUiColors/WarmBreadTheme).
        private static readonly Color BubbleColor = new(0.96f, 0.90f, 0.78f, 0.97f);
        private static readonly Color ChatPlateColor = new(0.10f, 0.055f, 0.035f, 0.94f);

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/Speech/Build Speech Bubbles",
            priority = MenuItemConstants.BaseToolsItemPriority)]
        public static void Build()
        {
            BuildShopperBubble();
            FixChatPlate();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SpeechBubble] Реплики над покупателями и плашка чата обновлены.");
        }

        private static void BuildShopperBubble()
        {
            var root = PrefabUtility.LoadPrefabContents(ShopperPrefabPath);

            try
            {
                var existing = root.transform.Find(BubbleName);
                GameObject bubbleGo;

                if (existing != null)
                {
                    bubbleGo = existing.gameObject;
                }
                else
                {
                    bubbleGo = new GameObject(BubbleName, typeof(RectTransform), typeof(Canvas));
                    bubbleGo.transform.SetParent(root.transform, worldPositionStays: false);
                }

                // Мировая плашка: canvas в мировых координатах, повёрнутый к камере билбордом.
                var canvas = bubbleGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 5;

                var rect = bubbleGo.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(420f, 170f);
                // Плашка целиком выше головы покупателя (но не улетает за верх кадра):
                // иначе текст перекрывается билбордом.
                rect.localPosition = new Vector3(0f, 2.45f, 0f);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one * 0.0045f;

                if (bubbleGo.GetComponent<Billboard>() == null)
                {
                    bubbleGo.AddComponent<Billboard>();
                }

                var plate = bubbleGo.transform.Find("Plate");
                GameObject plateGo;

                if (plate != null)
                {
                    plateGo = plate.gameObject;
                }
                else
                {
                    plateGo = new GameObject("Plate", typeof(RectTransform), typeof(Image));
                    plateGo.transform.SetParent(bubbleGo.transform, worldPositionStays: false);
                }

                var image = plateGo.GetComponent<Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Visuals/UI/Sprites/Generated/UI_Scrim_Rounded.png"
                );
                image.type = Image.Type.Sliced;
                image.color = BubbleColor;
                image.raycastTarget = false;

                var plateRect = plateGo.GetComponent<RectTransform>();
                plateRect.anchorMin = Vector2.zero;
                plateRect.anchorMax = Vector2.one;
                plateRect.offsetMin = Vector2.zero;
                plateRect.offsetMax = Vector2.zero;

                var textTransform = plateGo.transform.Find("Text_Speech");
                GameObject textGo;

                if (textTransform != null)
                {
                    textGo = textTransform.gameObject;
                }
                else
                {
                    textGo = new GameObject("Text_Speech", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textGo.transform.SetParent(plateGo.transform, worldPositionStays: false);
                }

                var text = textGo.GetComponent<TextMeshProUGUI>();
                text.text = "Здравствуйте!";
                text.fontSize = 42f;
                text.alignment = TextAlignmentOptions.Center;
                // Тёмный «корочный» текст на пергаменте — максимальный контраст.
                text.color = WarmBreadTheme.Crust;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.raycastTarget = false;

                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(WarmBreadTheme.FontPath);
                if (font != null)
                {
                    text.font = font;
                }

                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(18f, 14f);
                textRect.offsetMax = new Vector2(-18f, -14f);

                var shopper = root.GetComponent<ShopperActor>();
                var serialized = new SerializedObject(shopper);
                serialized.FindProperty("speechBubble").objectReferenceValue = bubbleGo;
                serialized.FindProperty("speechText").objectReferenceValue = text;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                bubbleGo.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(root, ShopperPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void FixChatPlate()
        {
            var root = PrefabUtility.LoadPrefabContents(ChatViewPrefabPath);

            try
            {
                var background = root.transform.Find("Panel/Background");
                if (background == null)
                {
                    Debug.LogWarning("[SpeechBubble] В View_Chat нет Panel/Background.");

                    return;
                }

                var image = background.GetComponent<Image>();
                if (image == null)
                {
                    return;
                }

                // Пиксельный градиент заменяется на чистую скруглённую плашку в палитре игры.
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Visuals/UI/Sprites/Generated/UI_Scrim_Rounded.png"
                );
                image.type = Image.Type.Sliced;
                image.color = ChatPlateColor;

                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.color = WarmBreadTheme.Cream;
                }

                PrefabUtility.SaveAsPrefabAsset(root, ChatViewPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
