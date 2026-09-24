using System.Collections.Generic;
using System.Linq;
using UABPetelnia.GGJ2025.Runtime;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors;
using UABPetelnia.GGJ2025.Runtime.Components.Utilities;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Products;
using UABPetelnia.GGJ2025.Runtime.Systems.Progress;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using UnityEditor;
using GamePlayerSettings = UABPetelnia.GGJ2025.Runtime.Settings.PlayerSettings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Тела отключенных инструментов (после return-гвардов) оставлены намеренно — глушим CS0162.
#pragma warning disable 0162

namespace UABPetelnia.GGJ2025.Editor
{
    /// <summary>
    /// Builds the scene and prefab wiring required by the new "warm bread" sales loop: a grabbable
    /// product prefab, shelf slots inside the kiosk, the player's grab interactor and the product
    /// system on the game manager.
    /// </summary>
    /// <remarks>
    /// Everything is created through the editor APIs instead of hand editing YAML, and every step
    /// is idempotent, so the tool can safely be re-run after the layout changes.
    /// </remarks>
    public static class KioskShopSetup
    {
        private const string ProductPrefabPath = "Assets/Prefabs/Actors/Actor_Product.prefab";
        private const string ProductMaterialPath = "Assets/Visuals/Objects/Materials/Product_Image.mat";
        private const string SourceMaterialPath = "Assets/Visuals/Bubbles/Materials/ChoiceBubble_Image.mat";

        private const string PlayerPrefabPath = "Assets/Prefabs/Actors/Actor_Player.prefab";
        private const string ShopperPrefabPath = "Assets/Prefabs/Actors/Actor_Shopper.prefab";
        private const string GameManagerPrefabPath = "Assets/Prefabs/GGJ2025GameManager.prefab";
        private const string GameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";
        private const string PlayerSettingsPath = "Assets/Settings/Game/Settings_Player.asset";
        private const string GameplaySettingsPath = "Assets/Settings/Game/Settings_Gameplay.asset";

        private const string InteractionLayerName = "Interactables";
        private const string HandName = "S_GrabHand";
        private const string ShelvesRootName = "Shop_Shelves";
        private const string RequestCardName = "RequestCard";

        private const int ShelfColumns = 5;
        private const int ShelfRows = 3;
        private const float ShelfSpacing = 0.42f;
        private const float ShelfRowHeight = 0.42f;
        private const float ShelfRowDepth = 0.45f;
        private const float ShelfStartHeight = 0.95f;
        private const float ShelfStartOffset = 1.1f;

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/Shop/Build Warm Bread Kiosk Shop",
            priority = MenuItemConstants.BaseToolsItemPriority)]
        public static void BuildShop()
        {
            Debug.Log("[KioskShop] Процедурная сборка киоска отключена: собирайте сцену руками в редакторе.");
            return;

            var material = EnsureProductMaterial();
            var productPrefab = EnsureProductPrefab(material);

            EnsurePlayerGrabInteractor();
            EnsureShopperRequestCard();
            EnsureGameManagerProductSystem(productPrefab);

            BuildShelves();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[KioskShop] Киоск собран: товар можно брать с полок и отдавать покупателю.");
        }

        private static Material EnsureProductMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(ProductMaterialPath);
            if (existing)
            {
                return existing;
            }

            EnsureFolder("Assets/Visuals/Objects", "Materials");

            // Reuse the transparent untextured material that already paints a texture through a
            // material property block, which is exactly what the product quad needs.
            var source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
            var shader = source ? source.shader : Shader.Find("Universal Render Pipeline/Unlit");

            var material = new Material(shader)
            {
                name = "Product_Image",
            };

            AssetDatabase.CreateAsset(material, ProductMaterialPath);

            return material;
        }

        private static ProductActor EnsureProductPrefab(Material material)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ProductActor>(ProductPrefabPath);
            if (existing)
            {
                return existing;
            }

            var root = new GameObject("Actor_Product");
            root.layer = LayerMask.NameToLayer(InteractionLayerName);

            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, worldPositionStays: false);
            body.layer = root.layer;

            var meshFilter = body.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var meshRenderer = body.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var collider = body.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.3f, 0.3f, 0.05f);
            collider.isTrigger = false;

            // Artwork is a flat quad, so it has to turn towards the camera or the product reads as
            // a blank sheet whenever the shelf faces away from the player.
            body.AddComponent<Billboard>();

            root.AddComponent<GrabInteractable>();

            var product = root.AddComponent<ProductActor>();

            var serialized = new SerializedObject(product);
            serialized.FindProperty("imageRenderer").objectReferenceValue = meshRenderer;
            serialized.FindProperty("texturePropertyId").stringValue = "_BaseMap";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.transform.localScale = Vector3.one * 0.35f;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ProductPrefabPath);
            Object.DestroyImmediate(root);

            return prefab.GetComponent<ProductActor>();
        }

        private static void EnsurePlayerGrabInteractor()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (root == false)
            {
                Debug.LogError($"[KioskShop] Нет префаба игрока {PlayerPrefabPath}.");
                return;
            }

            try
            {
                var playerActor = root.GetComponent<DesktopPlayerActor>();
                var settings = AssetDatabase.LoadAssetAtPath<GamePlayerSettings>(PlayerSettingsPath);

                if (playerActor == false || settings == false)
                {
                    Debug.LogError("[KioskShop] У игрока нет DesktopPlayerActor или настроек.");
                    return;
                }

                var cameraTransform = FindChildByName(root.transform, "MainCamera") ?? root.transform;
                var hand = FindChildByName(root.transform, HandName);

                if (hand == false)
                {
                    var handObject = new GameObject(HandName);
                    hand = handObject.transform;
                    hand.SetParent(cameraTransform, worldPositionStays: false);
                    hand.localPosition = new Vector3(0f, -0.18f, 0.45f);
                    hand.localRotation = Quaternion.identity;
                }

                var interactor = FindComponentInChildren<GrabInteractor>(root.transform);
                if (interactor == false)
                {
                    interactor = hand.gameObject.AddComponent<GrabInteractor>();
                }

                var serialized = new SerializedObject(interactor);
                serialized.FindProperty("settings").objectReferenceValue = settings;
                serialized.FindProperty("raycastTransform").objectReferenceValue = cameraTransform;
                serialized.FindProperty("grabTransform").objectReferenceValue = hand;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var playerSerialized = new SerializedObject(playerActor);
                playerSerialized.FindProperty("grabInteractor").objectReferenceValue = interactor;
                playerSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureShopperRequestCard()
        {
            var root = PrefabUtility.LoadPrefabContents(ShopperPrefabPath);
            if (root == false)
            {
                Debug.LogError($"[KioskShop] Нет префаба покупателя {ShopperPrefabPath}.");
                return;
            }

            try
            {
                var shopper = root.GetComponent<ShopperActor>();
                if (shopper == false)
                {
                    return;
                }

                var material = AssetDatabase.LoadAssetAtPath<Material>(ProductMaterialPath);
                var card = root.transform.Find(RequestCardName);

                if (card == false)
                {
                    var cardObject = new GameObject(RequestCardName);
                    card = cardObject.transform;
                    card.SetParent(root.transform, worldPositionStays: false);
                    card.localPosition = new Vector3(0f, 2.2f, 0f);
                    card.localScale = Vector3.one * 0.6f;

                    var meshFilter = cardObject.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

                    var meshRenderer = cardObject.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = material;
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    // The card should always face the player, like the shopper body does.
                    cardObject.AddComponent<Billboard>();
                }

                var renderer = card.GetComponent<Renderer>();

                var serialized = new SerializedObject(shopper);
                serialized.FindProperty("requestCard").objectReferenceValue = card.gameObject;
                serialized.FindProperty("requestRenderer").objectReferenceValue = renderer;
                serialized.FindProperty("requestTexturePropertyId").stringValue = "_BaseMap";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                card.gameObject.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(root, ShopperPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Registers the shop and the shelf systems on the game manager. Both have to exist at the
        /// same time: the shelves read their stock from the shop, so a manager with only one of
        /// them would fail on start-up.
        /// </summary>
        private static void EnsureGameManagerProductSystem(ProductActor productPrefab)
        {
            var root = PrefabUtility.LoadPrefabContents(GameManagerPrefabPath);
            if (root == false)
            {
                Debug.LogError($"[KioskShop] Нет префаба менеджера {GameManagerPrefabPath}.");
                return;
            }

            try
            {
                var gameManager = root.GetComponent<GGJ2025GameManager>();
                if (gameManager == false)
                {
                    return;
                }

                var settings = AssetDatabase.LoadAssetAtPath<GameplaySettings>(GameplaySettingsPath);

                var shop = FindComponentInChildren<ShopSystem>(root.transform);
                if (shop == false)
                {
                    var shopObject = new GameObject("System_Shop");
                    shopObject.transform.SetParent(root.transform, worldPositionStays: false);
                    shop = shopObject.AddComponent<ShopSystem>();
                }

                var shopSerialized = new SerializedObject(shop);
                shopSerialized.FindProperty("gameplaySettings").objectReferenceValue = settings;
                shopSerialized.ApplyModifiedPropertiesWithoutUndo();

                var system = FindComponentInChildren<ProductSystem>(root.transform);
                if (system == false)
                {
                    var systemObject = new GameObject("System_Products");
                    systemObject.transform.SetParent(root.transform, worldPositionStays: false);
                    system = systemObject.AddComponent<ProductSystem>();
                }

                var serialized = new SerializedObject(system);
                serialized.FindProperty("productPrefab").objectReferenceValue = productPrefab;
                serialized.FindProperty("gameplaySettings").objectReferenceValue = settings;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var progress = FindComponentInChildren<ProgressSystem>(root.transform);
                if (progress == false)
                {
                    var progressObject = new GameObject("System_Progress");
                    progressObject.transform.SetParent(root.transform, worldPositionStays: false);
                    progress = progressObject.AddComponent<ProgressSystem>();
                }

                var managerSerialized = new SerializedObject(gameManager);
                managerSerialized.FindProperty("shopSystem").objectReferenceValue = shop;
                managerSerialized.FindProperty("productSystem").objectReferenceValue = system;
                managerSerialized.FindProperty("progressSystem").objectReferenceValue = progress;
                managerSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, GameManagerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Places the shelf slots along the kiosk's own axis, going away from the counter where
        /// shoppers stand so the products end up inside the stall. The markers draw yellow gizmos,
        /// so nudging them afterwards in the scene view is easy.
        /// </summary>
        private static void BuildShelves()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            var kiosk = FindInScene<Transform>("Building_Kiosk");
            var kioskPoint = Object.FindObjectsByType<KioskPointActor>(FindObjectsInactive.Include)
                .FirstOrDefault();

            if (kiosk == false || kioskPoint == false)
            {
                Debug.LogError("[KioskShop] В сцене нет Building_Kiosk или Actor_KioskPoint.");
                return;
            }

            var existing = scene.GetRootGameObjects()
                .FirstOrDefault(gameObject => gameObject.name == ShelvesRootName);

            if (existing)
            {
                Object.DestroyImmediate(existing);
            }

            var root = new GameObject(ShelvesRootName);

            var inward = kiosk.position - kioskPoint.transform.position;
            inward.y = 0f;
            inward = inward.sqrMagnitude > 0.001f ? inward.normalized : Vector3.forward;

            var right = Vector3.Cross(Vector3.up, inward);
            var firstSlot = kioskPoint.transform.position
                + inward * ShelfStartOffset
                + Vector3.up * ShelfStartHeight;
            var origin = firstSlot - right * (ShelfSpacing * (ShelfColumns - 1) * 0.5f);

            var created = 0;

            for (var row = 0; row < ShelfRows; row++)
            {
                for (var column = 0; column < ShelfColumns; column++)
                {
                    var position = origin
                        + right * (ShelfSpacing * column)
                        + inward * (ShelfRowDepth * row)
                        + Vector3.up * (ShelfRowHeight * row);

                    var slot = new GameObject($"ShelfPoint_{row}_{column}");
                    slot.transform.SetParent(root.transform);
                    slot.transform.position = position;
                    slot.transform.rotation = Quaternion.LookRotation(-inward, Vector3.up);
                    slot.AddComponent<ProductShelfPointActor>();

                    created++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[KioskShop] Полок создано: {created}.");
        }

        private static T FindInScene<T>(string name) where T : Component
        {
            return Object
                .FindObjectsByType<T>(FindObjectsInactive.Include)
                .FirstOrDefault(component => component.name == name);
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            return root
                .GetComponentsInChildren<Transform>(includeInactive: true)
                .FirstOrDefault(transform => transform.name == name);
        }

        private static T FindComponentInChildren<T>(Transform root) where T : Component
        {
            return root.GetComponentInChildren<T>(includeInactive: true);
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
