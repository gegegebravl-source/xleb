using CHARK.GameManagement;
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    /// <summary>
    /// Подсказка действия на самом предмете: появляется над тем, на что игрок навёл взгляд,
    /// и текст зависит от того, что это и что у игрока в руках.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class HoverHintController : MonoBehaviour
    {
        private const string TakeHint = "E — взять";
        private const string PutHint = "E — положить";
        private const string GiveHint = "E — отдать покупателю";
        private const string BoxHint = "E — открыть коробку";

        [Tooltip("На сколько поднять плашку над предметом, в метрах.")]
        [Min(0f)]
        [SerializeField]
        private float heightOffset = 0.18f;

        [Tooltip("Размер плашки в метрах.")]
        [SerializeField]
        private Vector2 worldSize = new(0.34f, 0.12f);

        private Canvas canvas;
        private RectTransform plate;
        private TMP_Text label;
        private Transform target;

        private void Awake()
        {
            Build();
        }

        private void OnEnable()
        {
            SystemsUtility.TryAddListener<InteractorHoveredEnteredMessage>(OnHoverEntered);
            SystemsUtility.TryAddListener<InteractorHoveredExitedMessage>(OnHoverExited);

            SetVisible(false);
        }

        private void OnDisable()
        {
            SystemsUtility.TryRemoveListener<InteractorHoveredEnteredMessage>(OnHoverEntered);
            SystemsUtility.TryRemoveListener<InteractorHoveredExitedMessage>(OnHoverExited);

            target = default;
        }

        private void LateUpdate()
        {
            if (canvas == null || target == false)
            {
                SetVisible(false);

                return;
            }

            // Пока предмет в руке — подсказка не нужна: она бы висела в руке.
            var interactable = target.GetComponentInChildren<GrabInteractable>(true);

            if (interactable != false && interactable.IsSelected)
            {
                SetVisible(false);

                return;
            }

            // Держим плашку над предметом и разворачиваем к камере.
            canvas.transform.position = target.position + Vector3.up * heightOffset;

            var camera = Camera.main;

            if (camera != false)
            {
                canvas.transform.rotation = camera.transform.rotation;
            }
        }

        private void OnHoverEntered(InteractorHoveredEnteredMessage message)
        {
            if (message.Interactable is not Component component || component == false)
            {
                return;
            }

            // Взятый в руку предмет не подсказываем: подсказка нужна только на наведении.
            if (message.Interactable.IsSelected)
            {
                return;
            }

            target = component.transform;
            ApplyHint(ResolveHint(component));
            SetVisible(true);
        }

        /// <summary>
        /// Текст подсказки зависит от того, что под взглядом и есть ли товар в руках.
        /// </summary>
        private string ResolveHint(Component hovered)
        {
            if (hovered.GetComponentInParent<DeliveryBoxActor>() != false)
            {
                return BoxHint;
            }

            if (hovered.GetComponentInParent<ProductActor>() != false)
            {
                return HasItemInHand() ? PutHint : TakeHint;
            }

            if (hovered.GetComponentInParent<ShopperActor>() != false)
            {
                return GiveHint;
            }

            return TakeHint;
        }

        private bool HasItemInHand()
        {
            if (SystemsUtility.TryGetSystem<IPlayerSystem>(out var players) == false)
            {
                return false;
            }

            if (players.TryGetPlayer(out var player) == false)
            {
                return false;
            }

            return player.HasCarriedItem;
        }

        /// <summary>Показать текст и растянуть плашку под его длину.</summary>
        private void ApplyHint(string text)
        {
            if (label == null)
            {
                return;
            }

            label.text = text;

            if (canvas == null)
            {
                return;
            }

            // Ширина плашки считается по длине строки, чтобы текст не вылезал.
            var width = Mathf.Clamp(text.Length * 19f + 46f, 170f, 460f);
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 80f);
        }

        private void OnHoverExited(InteractorHoveredExitedMessage message)
        {
            target = default;
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            if (canvas != null && canvas.gameObject.activeSelf != isVisible)
            {
                canvas.gameObject.SetActive(isVisible);
            }
        }

        /// <summary>Собирает мир-канвас с плашкой и текстом подсказки.</summary>
        private void Build()
        {
            var canvasGo = new GameObject("HoverHint", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(transform, worldPositionStays: false);

            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 12;

            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 80f);
            rect.localScale = Vector3.one * (worldSize.x / 220f);

            var plateGo = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            plateGo.transform.SetParent(canvasGo.transform, worldPositionStays: false);

            var plateImage = plateGo.GetComponent<Image>();
            plateImage.sprite = Resources.Load<Sprite>("UI_Scrim_Rounded");

            if (plateImage.sprite == null)
            {
                plateImage.sprite = FindScrimSprite();
            }

            plateImage.type = Image.Type.Sliced;
            plateImage.color = new Color(0.10f, 0.055f, 0.035f, 0.92f);
            plateImage.raycastTarget = false;

            plate = plateImage.rectTransform;
            plate.anchorMin = Vector2.zero;
            plate.anchorMax = Vector2.one;
            plate.offsetMin = Vector2.zero;
            plate.offsetMax = Vector2.zero;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(plateGo.transform, worldPositionStays: false);

            label = textGo.GetComponent<TextMeshProUGUI>();
            label.text = TakeHint;
            label.fontSize = 40f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.96f, 0.90f, 0.78f, 1f);
            label.raycastTarget = false;

            var font = TMP_Settings.defaultFontAsset;

            if (font != null)
            {
                label.font = font;
            }

            var textRect = label.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);

            SetVisible(false);
        }

        /// <summary>Запасной путь к спрайту плашки, если Resources его не отдал.</summary>
        private static Sprite FindScrimSprite()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Visuals/UI/Sprites/Generated/UI_Scrim_Rounded.png"
            );
#else
            return default;
#endif
        }
    }
}
