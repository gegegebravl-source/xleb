#if UNITY_EDITOR
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Components.Utilities;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.Systems.Delivery;
using UABPetelnia.GGJ2025.Runtime.UI.Controllers;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.EditorTools
{
    /// <summary>
    /// Собирает доставку целиком: меню коробки, курьера с коробкой в руках и связи
    /// на игроке и на менеджере игры.
    /// </summary>
    public static class DeliveryFeatureBuilder
    {
        private const string BoxViewPrefabPath = "Assets/Prefabs/UI/View_DeliveryBox.prefab";
        private const string CourierPrefabPath = "Assets/Prefabs/Actors/Actor_Courier.prefab";
        private const string ShopperPrefabPath = "Assets/Prefabs/Actors/Actor_Shopper_Man.prefab";
        private const string PlayerPrefabPath = "Assets/Prefabs/Actors/Actor_Player.prefab";
        private const string ManagerPrefabPath = "Assets/Prefabs/GGJ2025GameManager.prefab";
        private const string ProductPrefabPath = "Assets/Prefabs/Actors/Actor_Product.prefab";
        private const string BoxTexturePath = "Assets/Art/Kirill/коробка картонная/korobka.png";
        private const string BoxMaterialPath = "Assets/Visuals/Objects/Materials/Kiosk_Box_Cardboard.mat";

        private const float WindowWidth = 560f;
        private const float WindowHeight = 520f;
        private const float RowHeight = 68f;

        private static readonly Color PanelColor = new(0.11f, 0.06f, 0.04f, 0.92f);
        private static readonly Color RowColor = new(0.18f, 0.10f, 0.06f, 0.9f);

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/Shop/Build Delivery Box And Courier",
            priority = MenuItemConstants.BaseToolsItemPriority)]
        public static void Build()
        {
            var boxMaterial = CreateBoxMaterial();
            var boxView = BuildBoxViewPrefab();
            var courier = BuildCourierPrefab(boxMaterial);

            WirePlayer(boxView);
            WireManager(courier);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DeliveryFeature] Меню коробки и курьер собраны.");
        }

        private static Material CreateBoxMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(BoxMaterialPath);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, BoxMaterialPath);
            }

            material.shader = shader;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BoxTexturePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.SetFloat("_Smoothness", 0.12f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = 2450;
            EditorUtility.SetDirty(material);

            return material;
        }

        private static GameObject BuildCourierPrefab(Material boxMaterial)
        {
            var root = PrefabUtility.LoadPrefabContents(ShopperPrefabPath);

            try
            {
                // Курьер — это человек, но без логики покупателя.
                var shopper = root.GetComponent<ShopperActor>();
                if (shopper != null)
                {
                    Object.DestroyImmediate(shopper, allowDestroyingAssets: false);
                }

                var courier = root.GetComponent<CourierActor>();
                if (courier == null)
                {
                    courier = root.AddComponent<CourierActor>();
                }

                // Коробка в руках.
                var boxTransform = root.transform.Find("Box");
                GameObject boxGo;

                if (boxTransform != null)
                {
                    boxGo = boxTransform.gameObject;
                }
                else
                {
                    boxGo = new GameObject("Box", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
                    boxGo.transform.SetParent(root.transform, worldPositionStays: false);
                    boxGo.layer = 6;
                }

                boxGo.transform.localPosition = new Vector3(0f, 0.95f, 0.42f);
                boxGo.transform.localRotation = Quaternion.identity;
                boxGo.transform.localScale = Vector3.one * 0.55f;

                var filter = boxGo.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh == null)
                {
                    filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                }

                var renderer = boxGo.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = boxMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }

                var collider = boxGo.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    collider.size = new Vector3(0.5f, 0.5f, 0.05f);
                }

                if (boxGo.GetComponent<Billboard>() == null)
                {
                    boxGo.AddComponent<Billboard>();
                }

                if (boxGo.GetComponent<GrabInteractable>() == null)
                {
                    boxGo.AddComponent<GrabInteractable>();
                }

                if (boxGo.GetComponent<DeliveryBoxActor>() == null)
                {
                    boxGo.AddComponent<DeliveryBoxActor>();
                }

                var courierSerialized = new SerializedObject(courier);
                courierSerialized.FindProperty("box").objectReferenceValue = boxGo.GetComponent<DeliveryBoxActor>();
                courierSerialized.FindProperty("walkAnimation").objectReferenceValue = root.GetComponent<Animator>();
                courierSerialized.ApplyModifiedPropertiesWithoutUndo();

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, CourierPrefabPath);

                return prefab;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject BuildBoxViewPrefab()
        {
            var root = WarmBreadTheme.CreateOverlayCanvas(null, "View_DeliveryBox", sortOrder: 16);
            var view = root.AddComponent<DeliveryBoxView>();

            var backdrop = WarmBreadTheme.CreateBackdrop(root.transform);
            if (backdrop != null)
            {
                backdrop.color = new Color(0f, 0f, 0f, 0.55f);
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

            var title = WarmBreadTheme.CreateTitle(window.transform, "Text_Title", "Коробка с доставкой", 38f);
            WarmBreadTheme.Place(
                title.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -18f),
                new Vector2(WindowWidth - 60f, 46f)
            );

            var rowsRoot = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rowsRoot.layer = 5;
            rowsRoot.transform.SetParent(window.transform, worldPositionStays: false);

            var rowsRect = rowsRoot.GetComponent<RectTransform>();
            WarmBreadTheme.Place(
                rowsRect,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -76f),
                new Vector2(WindowWidth - 48f, WindowHeight - 200f)
            );

            var layout = rowsRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var rowTemplate = BuildRowTemplate(window.transform);
            rowTemplate.gameObject.SetActive(false);

            var empty = WarmBreadTheme.CreateText(window.transform, "Text_Empty", "Коробка пуста", 28f, TextAlignmentOptions.Center);
            empty.color = WarmBreadTheme.CreamSoft;
            WarmBreadTheme.Place(
                empty.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(WindowWidth - 60f, 40f)
            );

            var hint = WarmBreadTheme.CreateText(
                window.transform,
                "Text_Hint",
                "Взять — товар уйдёт в свободный слот рук",
                22f,
                TextAlignmentOptions.Center
            );
            hint.color = WarmBreadTheme.CreamSoft;
            WarmBreadTheme.Place(
                hint.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 84f),
                new Vector2(WindowWidth - 48f, 34f)
            );

            var closeButton = CreateButton(
                window.transform,
                "Button_Close",
                "Закрыть",
                26f,
                new Vector2(200f, 52f),
                WarmBreadTheme.Butter
            );
            WarmBreadTheme.Place(
                closeButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 20f),
                new Vector2(200f, 52f)
            );

            var serialized = new SerializedObject(view);
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("hintText").objectReferenceValue = hint;
            serialized.FindProperty("rowsContent").objectReferenceValue = rowsRect;
            serialized.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
            serialized.FindProperty("emptyText").objectReferenceValue = empty;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BoxViewPrefabPath);
            Object.DestroyImmediate(root);

            return prefab;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            float fontSize,
            Vector2 size,
            Color color
        )
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.layer = 5;
            go.transform.SetParent(parent, worldPositionStays: false);

            var image = go.GetComponent<Image>();
            image.color = color;

            go.GetComponent<RectTransform>().sizeDelta = size;

            var text = WarmBreadTheme.CreateText(go.transform, "Text (TMP)", label, fontSize, TextAlignmentOptions.Center);
            text.color = WarmBreadTheme.Crust;
            WarmBreadTheme.Stretch(text.rectTransform);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            WarmBreadTheme.StyleButton(button, color);

            return button;
        }

        private static DeliveryBoxRowView BuildRowTemplate(Transform parent)
        {
            var rowGo = new GameObject("Row", typeof(RectTransform), typeof(Image));
            rowGo.layer = 5;
            rowGo.transform.SetParent(parent, worldPositionStays: false);

            var image = rowGo.GetComponent<Image>();
            image.color = RowColor;
            image.raycastTarget = false;

            var rect = rowGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(WindowWidth - 48f, RowHeight);

            var layoutElement = rowGo.AddComponent<LayoutElement>();
            layoutElement.minHeight = RowHeight;
            layoutElement.preferredHeight = RowHeight;

            var row = rowGo.AddComponent<DeliveryBoxRowView>();

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.layer = 5;
            iconGo.transform.SetParent(rowGo.transform, worldPositionStays: false);

            var icon = iconGo.AddComponent<RawImage>();
            icon.raycastTarget = false;
            WarmBreadTheme.Place(
                icon.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(12f, 0f),
                new Vector2(54f, 54f)
            );

            var title = WarmBreadTheme.CreateText(rowGo.transform, "Text_Name", "Товар", 26f, TextAlignmentOptions.Left);
            WarmBreadTheme.Place(
                title.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(78f, 0f),
                new Vector2(220f, 34f)
            );

            var count = WarmBreadTheme.CreateText(rowGo.transform, "Text_Count", "×1", 26f, TextAlignmentOptions.Left);
            count.color = WarmBreadTheme.Butter;
            WarmBreadTheme.Place(
                count.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(306f, 0f),
                new Vector2(70f, 34f)
            );

            var takeButton = CreateButton(
                rowGo.transform,
                "Button_Take",
                "Взять",
                24f,
                new Vector2(120f, 46f),
                WarmBreadTheme.Butter
            );
            WarmBreadTheme.Place(
                takeButton.GetComponent<RectTransform>(),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-12f, 0f),
                new Vector2(120f, 46f)
            );

            var serialized = new SerializedObject(row);
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("count").objectReferenceValue = count;
            serialized.FindProperty("takeButton").objectReferenceValue = takeButton;
            serialized.FindProperty("takeLabel").objectReferenceValue = takeButton.GetComponentInChildren<TMP_Text>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }

        private static void WirePlayer(GameObject boxView)
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            try
            {
                var existing = root.transform.Find("UI_DeliveryBox");
                GameObject controllerGo;

                if (existing != null)
                {
                    controllerGo = existing.gameObject;
                }
                else
                {
                    controllerGo = new GameObject("UI_DeliveryBox", typeof(RectTransform));
                    controllerGo.transform.SetParent(root.transform, worldPositionStays: false);
                }

                var controller = controllerGo.GetComponent<DeliveryBoxViewController>();
                if (controller == null)
                {
                    controller = controllerGo.AddComponent<DeliveryBoxViewController>();
                }

                var serialized = new SerializedObject(controller);
                serialized.FindProperty("viewPrefab").objectReferenceValue = boxView.GetComponent<DeliveryBoxView>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var player = root.GetComponent<DesktopPlayerActor>();
                var playerSerialized = new SerializedObject(player);
                playerSerialized.FindProperty("deliveryBoxViewController").objectReferenceValue = controller;
                playerSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireManager(GameObject courierPrefab)
        {
            var root = PrefabUtility.LoadPrefabContents(ManagerPrefabPath);

            try
            {
                var existing = root.GetComponentInChildren<CourierSystem>(true);
                GameObject systemGo;

                if (existing != null)
                {
                    systemGo = existing.gameObject;
                }
                else
                {
                    systemGo = new GameObject("System_Courier");
                    systemGo.transform.SetParent(root.transform, worldPositionStays: false);
                }

                var system = systemGo.GetComponent<CourierSystem>();
                if (system == null)
                {
                    system = systemGo.AddComponent<CourierSystem>();
                }

                var serialized = new SerializedObject(system);
                serialized.FindProperty("courierPrefab").objectReferenceValue = courierPrefab.GetComponent<CourierActor>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var manager = root.GetComponent<UABPetelnia.GGJ2025.Runtime.GGJ2025GameManager>();
                var managerSerialized = new SerializedObject(manager);
                managerSerialized.FindProperty("courierSystem").objectReferenceValue = system;
                managerSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, ManagerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
