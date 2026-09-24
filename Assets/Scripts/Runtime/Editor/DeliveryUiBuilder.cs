#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UABPetelnia.GGJ2025.Runtime;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.Components.Utilities;
using UABPetelnia.GGJ2025.Runtime.UI.Controllers;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Тела отключенных инструментов (после return-гвардов) оставлены намеренно — глушим CS0162.
#pragma warning disable 0162

namespace UABPetelnia.GGJ2025.Runtime.EditorTools
{
    /// <summary>
    /// Builds the "ordering PC" of the kiosk: the delivery panel prefab, the controller on the
    /// player and the physical PC that stands on the counter.
    /// </summary>
    /// <remarks>
    /// Every step is idempotent, so the tool can be re-run after the layout or the art pack
    /// changes. Artwork comes straight from the user's pack at
    /// <c>Assets/Art/Kirill/пк и заказ</c>.
    /// </remarks>
    public static class DeliveryUiBuilder
    {
        private const string ArtRoot = "Assets/Art/Kirill/пк и заказ";

        private const string PcTexturePath = ArtRoot + "/пк для азказов и тд.png";
        private const string BalanceIconPath = ArtRoot + "/баланс иконка.png";
        private const string DeliveryIconPath = ArtRoot + "/доставка иконка.png";
        private const string OrderIconPath = ArtRoot + "/заказать иконка листик с заказом.png";
        private const string StockIconPath = ArtRoot + "/иконка склада.png";
        private const string OrderListIconPath = ArtRoot + "/список заказов иконка.png";

        private const string ViewPrefabPath = "Assets/Prefabs/UI/View_Delivery.prefab";
        private const string PcMaterialPath = "Assets/Visuals/Objects/Materials/Pc_Image.mat";
        private const string SourceMaterialPath = "Assets/Visuals/Bubbles/Materials/ChoiceBubble_Image.mat";

        private const string PlayerPrefabPath = "Assets/Prefabs/Actors/Actor_Player.prefab";
        private const string GameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";
        private const string FontPath = "Assets/Visuals/Fonts/Font_Tiny5_Regular_SDF.asset";
        private const string SpriteFolder = "Assets/Visuals/UI/Sprites/";
        private const string IconFolder = "Assets/Visuals/UI/Icons/";

        private const string ControllerObjectName = "UI_Delivery";
        private const string PcObjectName = "Actor_DeliveryPc";



        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 720f;

        private const float WindowWidth = 1120f;
        private const float WindowHeight = 640f;
        private const float RowHeight = 92f;

        private static Color TextColor => WarmBreadTheme.Cream;

        private static Color MutedTextColor => WarmBreadTheme.CreamSoft;

        /// <summary>Warm shadow behind the lists, softer than the full backdrop.</summary>
        private static readonly Color FieldColor = new(0.16f, 0.09f, 0.06f, 0.45f);

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/Shop/Build Delivery PC Menu",
            priority = MenuItemConstants.BaseToolsItemPriority)]
        public static void Build()
        {
            Debug.Log("[DeliveryUi] Процедурная сборка ПК заказов отключена: настраивайте интерфейс вручную в редакторе.");
            return;

            if (Application.isBatchMode == false &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                Debug.LogWarning("[DeliveryUi] Сборка отменена, сцены не сохранены.");
                return;
            }

            var viewPrefab = RebuildViewPrefab();

            EnsurePlayerPanel(viewPrefab);
            EnsureCounterPc();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DeliveryUi] ПК заказов собран: подойди к прилавку и нажми Tab.");
        }

        private static DeliveryView RebuildViewPrefab()
        {
            var root = new GameObject(
                "View_Delivery",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup)
            );

            try
            {
                root.layer = 5;

                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                scaler.referencePixelsPerUnit = 100f;

                var canvasGroup = root.GetComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                var view = root.AddComponent<DeliveryView>();

                var backdrop = CreateImage("Backdrop", root.transform, null);
                backdrop.color = WarmBreadTheme.CrustDark;
                StretchTo(backdrop.rectTransform);

                var window = WarmBreadTheme.CreateWindow(
                    root.transform,
                    "Window",
                    new Vector2(WindowWidth, WindowHeight)
                );

                Place(
                    window.rectTransform,
                    anchor: new Vector2(0.5f, 0.5f),
                    pivot: new Vector2(0.5f, 0.5f),
                    position: Vector2.zero,
                    size: new Vector2(WindowWidth, WindowHeight)
                );

                var (balanceIcon, balanceText) = CreateHeader(window.transform);
                var (ordersIcon, ordersText) = CreateStats(window.transform);
                var (content, rowTemplate) = CreateCatalogue(window.transform);
                var (orderLines, orderLineTemplate, emptyOrdersText) = CreateDeliveries(window.transform);
                var (closeButton, orderEverythingButton, statusText, hintText) = CreateFooter(window.transform);

                ApplyViewReferences(
                    view: view,
                    canvasGroup: canvasGroup,
                    balanceIcon: balanceIcon,
                    ordersIcon: ordersIcon,
                    balanceText: balanceText,
                    ordersText: ordersText,
                    content: content,
                    rowTemplate: rowTemplate,
                    orderLinesContent: orderLines,
                    orderLineTemplate: orderLineTemplate,
                    emptyOrdersText: emptyOrdersText,
                    closeButton: closeButton,
                    orderEverythingButton: orderEverythingButton,
                    statusText: statusText,
                    hintText: hintText
                );

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, ViewPrefabPath);

                return prefab ? prefab.GetComponent<DeliveryView>() : default;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Header: the user's PC picture, the panel title.
        /// </summary>
        private static (RawImage BalanceIcon, TMP_Text BalanceText) CreateHeader(Transform window)
        {
            var header = CreateGroup("Header", window);
            StretchTo(header.GetComponent<RectTransform>(), 24f, WindowHeight - 20f - 96f, 24f, 20f);

            var pcTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PcTexturePath);
            var pcImage = CreateRawImage("Image_Pc", header.transform, pcTexture);
            pcImage.raycastTarget = false;
            Place(
                pcImage.rectTransform,
                anchor: new Vector2(0f, 0.5f),
                pivot: new Vector2(0f, 0.5f),
                position: Vector2.zero,
                size: new Vector2(86f, 86f)
            );

            if (pcTexture)
            {
                var fitter = pcImage.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = pcTexture.width / (float)pcTexture.height;
            }

            var title = CreateText(
                name: "Text_Title",
                parent: header.transform,
                value: "ПК — заказы и доставка",
                fontSize: 44f,
                alignment: TextAlignmentOptions.Left
            );

            title.raycastTarget = false;
            title.color = WarmBreadTheme.Butter;
            StretchTo(title.rectTransform, 104f, 0f, 8f, 0f);

            return (pcImage, title);
        }

        /// <summary>
        /// Two counters under the title: money in the cash box and deliveries still on the road.
        /// </summary>
        private static (RawImage OrdersIcon, TMP_Text OrdersText) CreateStats(Transform window)
        {
            var stats = CreateGroup("Stats", window);
            StretchTo(stats.GetComponent<RectTransform>(), 24f, WindowHeight - 114f - 76f, 24f, 114f);

            var balanceIcon = CreateRawImage(
                name: "Image_BalanceIcon",
                parent: stats.transform,
                texture: AssetDatabase.LoadAssetAtPath<Texture2D>(BalanceIconPath)
            );

            Place(
                balanceIcon.rectTransform,
                anchor: new Vector2(0f, 0.5f),
                pivot: new Vector2(0f, 0.5f),
                position: new Vector2(8f, 0f),
                size: new Vector2(64f, 64f)
            );

            var balanceText = CreateText(
                name: "Text_Balance",
                parent: stats.transform,
                value: "0.00 руб.",
                fontSize: 34f,
                alignment: TextAlignmentOptions.Left
            );

            StretchTo(balanceText.rectTransform, 84f, 0f, 500f, 0f);

            var ordersIcon = CreateRawImage(
                name: "Image_OrdersIcon",
                parent: stats.transform,
                texture: AssetDatabase.LoadAssetAtPath<Texture2D>(DeliveryIconPath)
            );

            Place(
                ordersIcon.rectTransform,
                anchor: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0f, 0.5f),
                position: new Vector2(8f, 0f),
                size: new Vector2(64f, 64f)
            );

            var ordersText = CreateText(
                name: "Text_Orders",
                parent: stats.transform,
                value: "Доставок в пути нет",
                fontSize: 28f,
                alignment: TextAlignmentOptions.Left
            );

            ordersText.color = MutedTextColor;
            StretchTo(ordersText.rectTransform, 584f, 0f, 8f, 0f);

            return (ordersIcon, ordersText);
        }

        /// <summary>
        /// Scrolling catalogue of everything the kiosk can order, under the warehouse icon.
        /// </summary>
        private static (RectTransform Content, DeliveryRowView RowTemplate) CreateCatalogue(Transform window)
        {
            var catalogue = CreateGroup("Catalogue", window);
            StretchTo(catalogue.GetComponent<RectTransform>(), 24f, 140f, 412f, 210f);

            CreateSectionHeader(
                parent: catalogue.transform,
                name: "Header_Stock",
                title: "Склад",
                texture: AssetDatabase.LoadAssetAtPath<Texture2D>(StockIconPath)
            );

            var list = CreateGroup("List", catalogue.transform);
            StretchTo(list.GetComponent<RectTransform>(), 0f, 0f, 0f, 52f);

            var viewport = CreateImage("Viewport", list.transform, null);
            viewport.color = FieldColor;
            StretchTo(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateGroup("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rowTemplate = CreateRowTemplate(contentRect);
            rowTemplate.gameObject.SetActive(false);

            var scroll = list.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            return (contentRect, rowTemplate);
        }

        /// <summary>
        /// Right hand column: everything that was paid for and is still on the road.
        /// </summary>
        private static (RectTransform Content, TMP_Text Template, TMP_Text Empty) CreateDeliveries(
            Transform window
        )
        {
            var deliveries = CreateGroup("Deliveries", window);
            StretchTo(deliveries.GetComponent<RectTransform>(), 748f, 140f, 24f, 210f);

            CreateSectionHeader(
                parent: deliveries.transform,
                name: "Header_Orders",
                title: "В пути",
                texture: AssetDatabase.LoadAssetAtPath<Texture2D>(OrderListIconPath)
            );

            var list = CreateImage("List", deliveries.transform, null);
            list.color = FieldColor;
            StretchTo(list.rectTransform, 0f, 0f, 0f, 52f);
            list.gameObject.AddComponent<RectMask2D>();

            var content = CreateGroup("Content", list.transform);
            StretchTo(content.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var template = CreateText(
                name: "Text_OrderLineTemplate",
                parent: content.transform,
                value: "Товар ×1 — через 25 с",
                fontSize: 22f,
                alignment: TextAlignmentOptions.Left
            );

            template.color = TextColor;
            template.raycastTarget = false;
            AddLayoutElement(template.gameObject, 26f);
            template.gameObject.SetActive(false);

            var empty = CreateText(
                name: "Text_EmptyOrders",
                parent: content.transform,
                value: "Доставок в пути нет",
                fontSize: 22f,
                alignment: TextAlignmentOptions.Left
            );

            empty.color = MutedTextColor;
            empty.raycastTarget = false;
            AddLayoutElement(empty.gameObject, 26f);

            return (content.GetComponent<RectTransform>(), template, empty);
        }

        /// <summary>
        /// Section title with one of the user's icons and a divider line under it.
        /// </summary>
        private static void CreateSectionHeader(Transform parent, string name, string title, Texture2D texture)
        {
            var header = CreateGroup(name, parent);
            StretchTo(header.GetComponent<RectTransform>(), 0f, 238f, 0f, 0f);

            var icon = CreateRawImage("Image_Icon", header.transform, texture);
            icon.raycastTarget = false;
            Place(
                icon.rectTransform,
                anchor: new Vector2(0f, 0.5f),
                pivot: new Vector2(0f, 0.5f),
                position: Vector2.zero,
                size: new Vector2(40f, 40f)
            );

            var label = CreateText(
                name: "Text_Title",
                parent: header.transform,
                value: title,
                fontSize: 30f,
                alignment: TextAlignmentOptions.Left
            );

            label.raycastTarget = false;
            StretchTo(label.rectTransform, 52f, 0f, 0f, 0f);
        }

        private static DeliveryRowView CreateRowTemplate(RectTransform content)
        {
            var row = new GameObject("RowTemplate", typeof(RectTransform));
            row.transform.SetParent(content, worldPositionStays: false);

            var element = row.AddComponent<LayoutElement>();
            element.minHeight = RowHeight;
            element.preferredHeight = RowHeight;

            var rowView = row.AddComponent<DeliveryRowView>();

            var icon = CreateRawImage("Image_Icon", row.transform, null);
            icon.raycastTarget = false;
            Place(
                icon.rectTransform,
                anchor: new Vector2(0f, 0.5f),
                pivot: new Vector2(0f, 0.5f),
                position: new Vector2(12f, 0f),
                size: new Vector2(68f, 68f)
            );

            var title = CreateText(
                name: "Text_Title",
                parent: row.transform,
                value: "Товар",
                fontSize: 30f,
                alignment: TextAlignmentOptions.Left
            );

            title.color = TextColor;
            title.raycastTarget = false;
            StretchTo(title.rectTransform, 96f, RowHeight * 0.5f, 320f, 6f);

            var details = CreateText(
                name: "Text_Details",
                parent: row.transform,
                value: "остаток 0 · закуп 0.00 руб.",
                fontSize: 22f,
                alignment: TextAlignmentOptions.Left
            );

            details.color = MutedTextColor;
            details.raycastTarget = false;
            StretchTo(details.rectTransform, 96f, 8f, 320f, RowHeight * 0.5f);

            var orderButton = CreateButton(
                name: "Button_Order",
                parent: row.transform,
                label: "+1",
                iconTexture: AssetDatabase.LoadAssetAtPath<Texture2D>(OrderIconPath)
            );

            Place(
                orderButton.GetComponent<RectTransform>(),
                anchor: new Vector2(1f, 0.5f),
                pivot: new Vector2(1f, 0.5f),
                position: new Vector2(-12f, 0f),
                size: new Vector2(288f, 68f)
            );

            var label = orderButton.GetComponentInChildren<TMP_Text>(includeInactive: true);

            var serialized = new SerializedObject(rowView);
            SetReference(serialized, "icon", icon);
            SetReference(serialized, "title", title);
            SetReference(serialized, "details", details);
            SetReference(serialized, "orderButton", orderButton);
            SetReference(serialized, "orderButtonLabel", label);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return rowView;
        }

        private static (Button Close, Button OrderEverything, TMP_Text Status, TMP_Text Hint) CreateFooter(
            Transform window
        )
        {
            var footer = CreateGroup("Footer", window);
            StretchTo(footer.GetComponent<RectTransform>(), 24f, 16f, 24f, WindowHeight - 16f - 116f);

            var orderEverything = CreateButton(
                name: "Button_OrderEverything",
                parent: footer.transform,
                label: "Заказать всё, чего нет",
                iconTexture: AssetDatabase.LoadAssetAtPath<Texture2D>(OrderIconPath)
            );

            Place(
                orderEverything.GetComponent<RectTransform>(),
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f),
                position: Vector2.zero,
                size: new Vector2(460f, 84f)
            );

            var close = CreateButton(
                name: "Button_Close",
                parent: footer.transform,
                label: "Закрыть"
            );

            Place(
                close.GetComponent<RectTransform>(),
                anchor: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                position: Vector2.zero,
                size: new Vector2(240f, 84f)
            );

            var status = CreateText(
                name: "Text_Status",
                parent: footer.transform,
                value: string.Empty,
                fontSize: 26f,
                alignment: TextAlignmentOptions.Center
            );

            status.color = MutedTextColor;
            status.raycastTarget = false;
            Place(
                status.rectTransform,
                anchor: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -6f),
                size: new Vector2(380f, 48f)
            );

            var hint = CreateText(
                name: "Text_Hint",
                parent: footer.transform,
                value: "Tab — закрыть · кнопки справа — заказать товар",
                fontSize: 22f,
                alignment: TextAlignmentOptions.Center
            );

            hint.color = MutedTextColor;
            hint.raycastTarget = false;
            Place(
                hint.rectTransform,
                anchor: new Vector2(0.5f, 0f),
                pivot: new Vector2(0.5f, 0f),
                position: new Vector2(0f, 2f),
                size: new Vector2(760f, 28f)
            );

            return (close, orderEverything, status, hint);
        }

        private static void ApplyViewReferences(
            DeliveryView view,
            CanvasGroup canvasGroup,
            RawImage balanceIcon,
            RawImage ordersIcon,
            TMP_Text balanceText,
            TMP_Text ordersText,
            RectTransform content,
            DeliveryRowView rowTemplate,
            RectTransform orderLinesContent,
            TMP_Text orderLineTemplate,
            TMP_Text emptyOrdersText,
            Button closeButton,
            Button orderEverythingButton,
            TMP_Text statusText,
            TMP_Text hintText
        )
        {
            var serialized = new SerializedObject(view);

            SetReference(serialized, "canvasGroup", canvasGroup);
            SetReference(serialized, "balanceIcon", balanceIcon);
            SetReference(serialized, "ordersIcon", ordersIcon);
            SetReference(serialized, "balanceText", balanceText);
            SetReference(serialized, "ordersText", ordersText);
            SetReference(serialized, "content", content);
            SetReference(serialized, "rowTemplate", rowTemplate);
            SetReference(serialized, "orderLinesContent", orderLinesContent);
            SetReference(serialized, "orderLineTemplate", orderLineTemplate);
            SetReference(serialized, "emptyOrdersText", emptyOrdersText);
            SetReference(serialized, "closeButton", closeButton);
            SetReference(serialized, "orderEverythingButton", orderEverythingButton);
            SetReference(serialized, "statusText", statusText);
            SetReference(serialized, "hintText", hintText);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Put the controller on the player prefab, next to the chat one, and point it at the panel.
        /// </summary>
        private static void EnsurePlayerPanel(DeliveryView viewPrefab)
        {
            if (viewPrefab == false)
            {
                Debug.LogError("[DeliveryUi] Не удалось собрать View_Delivery, панель игрока не настроена.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (root == false)
            {
                Debug.LogError($"[DeliveryUi] Нет префаба игрока {PlayerPrefabPath}.");
                return;
            }

            try
            {
                var player = root.GetComponent<DesktopPlayerActor>();
                var existing = root
                    .GetComponentsInChildren<DeliveryViewController>(includeInactive: true)
                    .FirstOrDefault();

                DeliveryViewController controller = existing;

                if (controller == false)
                {
                    var holder = new GameObject(ControllerObjectName);
                    holder.transform.SetParent(root.transform, worldPositionStays: false);
                    controller = holder.AddComponent<DeliveryViewController>();
                }

                var serialized = new SerializedObject(controller);
                serialized.FindProperty("viewPrefab").objectReferenceValue = viewPrefab;

                // Same as the chat view: hidden on awake, shown on demand.
                serialized.FindProperty("awakeBehaviour").intValue = 4;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                if (player)
                {
                    var playerSerialized = new SerializedObject(player);
                    playerSerialized.FindProperty("deliveryViewController").objectReferenceValue = controller;
                    playerSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Place the physical PC on the kiosk counter, next to the shelves, facing the shoppers.
        /// </summary>
        private static void EnsureCounterPc()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            var kiosk = FindInScene<Transform>("Building_Kiosk");
            var kioskPoint = Object.FindObjectsByType<KioskPointActor>(FindObjectsInactive.Include)
                .FirstOrDefault();

            if (kiosk == false || kioskPoint == false)
            {
                Debug.LogError("[DeliveryUi] В сцене нет Building_Kiosk или Actor_KioskPoint.");
                return;
            }

            var existing = FindInScene<DeliveryPcActor>(PcObjectName);
            if (existing)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var material = EnsurePcMaterial();

            var inward = kiosk.position - kioskPoint.transform.position;
            inward.y = 0f;
            inward = inward.sqrMagnitude > 0.001f ? inward.normalized : Vector3.forward;

            var right = Vector3.Cross(Vector3.up, inward);

            var root = new GameObject(PcObjectName);
            root.layer = LayerMask.NameToLayer("Interactables");

            root.transform.position = kioskPoint.transform.position
                + inward * 0.55f
                + right * 0.8f
                + Vector3.up * 1.02f;
            root.transform.rotation = Quaternion.LookRotation(-inward, Vector3.up);

            var body = new GameObject("Body");
            body.layer = root.layer;
            body.transform.SetParent(root.transform, worldPositionStays: false);

            var meshFilter = body.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var meshRenderer = body.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // The PC is a flat picture, so it has to keep facing the player.
            body.AddComponent<Billboard>();

            root.transform.localScale = Vector3.one * 0.55f;

            var pcActor = root.AddComponent<DeliveryPcActor>();

            var serialized = new SerializedObject(pcActor);
            serialized.FindProperty("interactDistance").floatValue = 3.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[DeliveryUi] ПК заказов поставлен на прилавок в {GameplayScenePath}.");
        }

        private static Material EnsurePcMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PcTexturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(PcMaterialPath);

            if (material == false)
            {
                EnsureFolder("Assets/Visuals/Objects", "Materials");

                var source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
                var shader = source ? source.shader : Shader.Find("Universal Render Pipeline/Unlit");

                material = new Material(shader)
                {
                    name = "Pc_Image",
                };

                AssetDatabase.CreateAsset(material, PcMaterialPath);
            }

            if (texture)
            {
                material.mainTexture = texture;
                material.SetTexture("_BaseMap", texture);
            }
            else
            {
                // Staying silent here is how a misspelt file name lived in this file unnoticed:
                // the order screen just stayed grey and nothing said why.
                Debug.LogWarning(
                    $"[DeliveryUi] Нет картинки ПК по пути {PcTexturePath} — экран ПК останется пустым."
                );
            }

            EditorUtility.SetDirty(material);

            return material;
        }

        private static T FindInScene<T>(string name) where T : Component
        {
            return Object
                .FindObjectsByType<T>(FindObjectsInactive.Include)
                .FirstOrDefault(component => component.name == name);
        }

        private static GameObject CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name, typeof(RectTransform));
            group.layer = 5;
            group.transform.SetParent(parent, worldPositionStays: false);

            return group;
        }

        private static Image CreateImage(string name, Transform parent, string spriteName)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(parent, worldPositionStays: false);

            var image = root.AddComponent<Image>();
            if (string.IsNullOrEmpty(spriteName) == false)
            {
                image.sprite = LoadSprite(spriteName);
            }

            return image;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Texture2D texture)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(parent, worldPositionStays: false);

            var image = root.AddComponent<RawImage>();
            image.texture = texture;

            return image;
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
            root.layer = 5;
            root.transform.SetParent(parent, worldPositionStays: false);

            var text = root.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = TextColor;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font)
            {
                text.font = font;
            }

            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Texture2D iconTexture = null
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

            WarmBreadTheme.StyleButton(button);

            var leftInset = 16f;

            if (iconTexture)
            {
                var icon = CreateRawImage("Image_Icon", root.transform, iconTexture);
                icon.raycastTarget = false;
                Place(
                    icon.rectTransform,
                    anchor: new Vector2(0f, 0.5f),
                    pivot: new Vector2(0f, 0.5f),
                    position: new Vector2(8f, 0f),
                    size: new Vector2(64f, 64f)
                );

                leftInset = 84f;
            }

            var text = CreateText("Text_Label", root.transform, label, 28f, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            StretchTo(text.rectTransform, leftInset, 6f, 12f, 6f);

            return button;
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

        private static void SetReference(SerializedObject serialized, string name, Object value)
        {
            var property = serialized.FindProperty(name);
            if (property == null)
            {
                Debug.LogWarning($"[DeliveryUi] Не найдено поле {name}.");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static void Place(
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

        private static void StretchTo(RectTransform rectTransform, float left = 0f, float bottom = 0f,
            float right = 0f, float top = 0f)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static Sprite LoadSprite(string name)
        {
            foreach (var folder in new[] { IconFolder, SpriteFolder })
            {
                var path = folder + name + ".png";

                if (AssetDatabase.LoadAssetAtPath<Sprite>(path) is { } sprite)
                {
                    return sprite;
                }
            }

            Debug.LogWarning($"[DeliveryUi] Не найден спрайт {name}.");

            return default;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";

            if (AssetDatabase.IsValidFolder(path) == false)
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
