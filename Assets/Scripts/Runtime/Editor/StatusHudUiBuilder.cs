#if UNITY_EDITOR
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.UI.Controllers;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.EditorTools
{
    /// <summary>
    /// Собирает HUD справа вверху: часы смены, номер дня, полоска смены и баланс в рублях.
    /// </summary>
    /// <remarks>
    /// Инструмент идемпотентный: префаб пересобирается целиком, а на игроке остаётся
    /// контроллер <see cref="StatusHudViewController"/> со ссылкой на префаб вью.
    /// </remarks>
    public static class StatusHudUiBuilder
    {
        private const string ViewPrefabPath = "Assets/Prefabs/UI/View_StatusHud.prefab";
        private const string CrosshairPrefabPath = "Assets/Prefabs/UI/View_Crosshair.prefab";
        private const string CrosshairSpritePath = "Assets/Visuals/UI/Icons/UI_Crosshair_Dot.png";
        private const string PlayerPrefabPath = "Assets/Prefabs/Actors/Actor_Player.prefab";
        private const string BalanceIconPath = "Assets/Visuals/UI/Icons/UI_Icon_Coin.png";
        private const string ControllerObjectName = "UI_StatusHud";

        // Панели по краям экрана: деньги слева, часы справа.
        private const float MoneyPanelWidth = 320f;
        private const float MoneyPanelHeight = 96f;
        private const float ClockPanelWidth = 220f;
        private const float ClockPanelHeight = 96f;
        private const float PanelMargin = 22f;

        private const float ClockFontSize = 42f;
        private const float DayFontSize = 22f;
        private const float MoneyFontSize = 40f;

        private static readonly Color PanelColor = new(0.10f, 0.055f, 0.035f, 0.94f);
        private static readonly Color BarBackColor = new(0.29f, 0.16f, 0.09f, 0.9f);

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/HUD/Build Status HUD",
            priority = MenuItemConstants.BaseToolsItemPriority)]
        public static void Build()
        {
            var viewPrefab = RebuildViewPrefab();
            var crosshairPrefab = RebuildCrosshairPrefab();

            EnsurePlayerController(viewPrefab, crosshairPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[StatusHud] HUD времени и баланса собран.");
        }

        private static GameObject RebuildViewPrefab()
        {
            var root = WarmBreadTheme.CreateOverlayCanvas(null, "View_StatusHud", sortOrder: 10);
            root.transform.localScale = Vector3.one;

            var view = root.AddComponent<StatusHudView>();
            var scrim = WarmBreadTheme.LoadSprite("Generated/UI_Scrim_Rounded", warn: false);

            // === Баланс: слева сверху ===
            var moneyPanel = WarmBreadTheme.CreateImage(root.transform, "Panel_Money", scrim);
            moneyPanel.color = PanelColor;

            WarmBreadTheme.Place(
                moneyPanel.rectTransform,
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f),
                position: new Vector2(PanelMargin, -PanelMargin),
                size: new Vector2(MoneyPanelWidth, MoneyPanelHeight)
            );

            var iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BalanceIconPath);
            var iconGo = new GameObject("Icon_Balance", typeof(RectTransform));
            iconGo.layer = 5;
            iconGo.transform.SetParent(moneyPanel.transform, worldPositionStays: false);

            var icon = iconGo.AddComponent<RawImage>();
            icon.texture = iconTexture;
            icon.raycastTarget = false;

            WarmBreadTheme.Place(
                icon.rectTransform,
                anchor: new Vector2(0f, 0.5f),
                pivot: new Vector2(0f, 0.5f),
                position: new Vector2(18f, 0f),
                size: new Vector2(58f, 58f)
            );

            var money = WarmBreadTheme.CreateText(
                moneyPanel.transform,
                "Text_Money",
                "0 руб.",
                MoneyFontSize,
                TextAlignmentOptions.Left
            );

            money.color = WarmBreadTheme.Butter;
            money.fontStyle = FontStyles.Bold;
            money.textWrappingMode = TextWrappingModes.NoWrap;

            WarmBreadTheme.Place(
                money.rectTransform,
                anchor: new Vector2(0f, 0.5f),
                pivot: new Vector2(0f, 0.5f),
                position: new Vector2(86f, 0f),
                size: new Vector2(MoneyPanelWidth - 100f, 54f)
            );

            // === Время и день: справа сверху ===
            var clockPanel = WarmBreadTheme.CreateImage(root.transform, "Panel_Clock", scrim);
            clockPanel.color = PanelColor;

            WarmBreadTheme.Place(
                clockPanel.rectTransform,
                anchor: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                position: new Vector2(-PanelMargin, -PanelMargin),
                size: new Vector2(ClockPanelWidth, ClockPanelHeight)
            );

            var clock = WarmBreadTheme.CreateText(
                clockPanel.transform,
                "Text_Clock",
                "09:00",
                ClockFontSize,
                TextAlignmentOptions.Right
            );

            clock.color = WarmBreadTheme.Cream;

            WarmBreadTheme.Place(
                clock.rectTransform,
                anchor: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                position: new Vector2(-16f, -12f),
                size: new Vector2(ClockPanelWidth - 32f, 50f)
            );

            var day = WarmBreadTheme.CreateText(
                clockPanel.transform,
                "Text_Day",
                "День 1",
                DayFontSize,
                TextAlignmentOptions.Right
            );

            day.color = WarmBreadTheme.CreamSoft;

            WarmBreadTheme.Place(
                day.rectTransform,
                anchor: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                position: new Vector2(-16f, -60f),
                size: new Vector2(ClockPanelWidth - 32f, 26f)
            );

            var barBack = WarmBreadTheme.CreateImage(clockPanel.transform, "ShiftBar", null);
            barBack.color = BarBackColor;

            WarmBreadTheme.Place(
                barBack.rectTransform,
                anchor: new Vector2(0.5f, 0f),
                pivot: new Vector2(0.5f, 0f),
                position: new Vector2(0f, 10f),
                size: new Vector2(ClockPanelWidth - 32f, 6f)
            );

            var barFill = WarmBreadTheme.CreateImage(barBack.transform, "Fill", null);
            barFill.color = WarmBreadTheme.Butter;
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFill.fillAmount = 0f;

            WarmBreadTheme.Stretch(barFill.rectTransform);

            // Ссылки вью.
            var serialized = new SerializedObject(view);
            serialized.FindProperty("clockText").objectReferenceValue = clock;
            serialized.FindProperty("dayText").objectReferenceValue = day;
            serialized.FindProperty("moneyText").objectReferenceValue = money;
            serialized.FindProperty("shiftFill").objectReferenceValue = barFill;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ViewPrefabPath);
            Object.DestroyImmediate(root);

            return prefab;
        }

        /// <summary>Прицел по центру экрана: точка в палитре игры, без «мобильных» градиентов.</summary>
        private static GameObject RebuildCrosshairPrefab()
        {
            var root = WarmBreadTheme.CreateOverlayCanvas(null, "View_Crosshair", sortOrder: 20);
            root.transform.localScale = Vector3.one;

            var view = root.AddComponent<CrosshairView>();

            var dotGo = new GameObject("Dot", typeof(RectTransform));
            dotGo.layer = 5;
            dotGo.transform.SetParent(root.transform, worldPositionStays: false);

            var dot = dotGo.AddComponent<Image>();
            dot.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrosshairSpritePath);
            dot.raycastTarget = false;
            dot.color = new Color(1f, 1f, 1f, 0.5f);

            WarmBreadTheme.Place(
                dot.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(9f, 9f)
            );

            var serialized = new SerializedObject(view);
            serialized.FindProperty("dot").objectReferenceValue = dot;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CrosshairPrefabPath);
            Object.DestroyImmediate(root);

            return prefab;
        }

        private static void EnsurePlayerController(GameObject viewPrefab, GameObject crosshairPrefab)
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

                var controller = controllerGo.GetComponent<StatusHudViewController>();
                if (controller == null)
                {
                    controller = controllerGo.AddComponent<StatusHudViewController>();
                }

                var serialized = new SerializedObject(controller);
                serialized.FindProperty("viewPrefab").objectReferenceValue =
                    viewPrefab.GetComponent<StatusHudView>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // Прицел живёт на игроке так же, как остальные HUD-элементы.
                var crosshairTransform = root.transform.Find("UI_Crosshair");
                GameObject crosshairGo;

                if (crosshairTransform != null)
                {
                    crosshairGo = crosshairTransform.gameObject;
                }
                else
                {
                    crosshairGo = new GameObject("UI_Crosshair", typeof(RectTransform));
                    crosshairGo.transform.SetParent(root.transform, worldPositionStays: false);
                }

                var crosshairController = crosshairGo.GetComponent<CrosshairViewController>();
                if (crosshairController == null)
                {
                    crosshairController = crosshairGo.AddComponent<CrosshairViewController>();
                }

                var crosshairSerialized = new SerializedObject(crosshairController);
                crosshairSerialized.FindProperty("viewPrefab").objectReferenceValue =
                    crosshairPrefab.GetComponent<CrosshairView>();
                crosshairSerialized.ApplyModifiedPropertiesWithoutUndo();

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
