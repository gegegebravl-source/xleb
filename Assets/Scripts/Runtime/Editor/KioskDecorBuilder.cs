#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Components.Utilities;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Тела отключенных инструментов (после return-гвардов) оставлены намеренно — глушим CS0162.
#pragma warning disable 0162

namespace UABPetelnia.GGJ2025.Runtime.EditorTools
{
    /// <summary>
    /// Dresses the gameplay scene with the user's art: the cash register on the counter, the retro
    /// trinkets, a working TV, a mirror, the tamagotchi toy and the street props around the shop
    /// front.
    /// </summary>
    /// <remarks>
    /// Every prop is a billboard picture built from the art pack, placed relative to the kiosk, so
    /// anything can be nudged in the scene view afterwards. The tool is idempotent: it rebuilds the
    /// <c>Decor_Kiosk</c> and <c>Decor_Street</c> roots from scratch.
    /// </remarks>
    public static class KioskDecorBuilder
    {
        private const string ArtRoot = "Assets/Art/Kirill";
        private const string GameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";
        private const string MaterialFolder = "Assets/Visuals/Objects/Materials";
        private const string CalculatorPrefabPath = "Assets/Prefabs/Actors/Actor_Calculator.prefab";
        private const string PlayerSettingsPath = "Assets/Settings/Game/Settings_Player.asset";

        private const string KioskRootName = "Decor_Kiosk";
        private const string StreetRootName = "Decor_Street";

        // Folder prefixes under Assets/Art/Kirill, resolved by prefix so the numbered street pack
        // folders survive re-export. A prefix starting with a digit is a subfolder of the street
        // pack, anything else is a folder of its own.
        private const string StreetRootPrefix = "ассеты";
        private const string RetroFolderPrefix = "предметы";
        private const string TvFolderPrefix = "телевизор";
        private const string MirrorFolderPrefix = "зеркало";
        private const string TamagotchiFolderPrefix = "анимации";

        private const string InteriorLayer = "Interactables";

        private static readonly DecorEntry[] KioskDecor =
        {
            // Skinny shelf of 2000s junk on the back wall of the stall.
            new("Poster", RetroFolderPrefix, "плакат рок группы", 1.35f, 0.72f, 1.42f, 0.85f),
            new("Tv", TvFolderPrefix, string.Empty, 0.45f, 0.78f, 1.85f, 0.62f, kind: DecorKind.Tv),
            new("Mirror", MirrorFolderPrefix, "зеркало", -0.55f, 0.74f, 1.35f, 0.75f, kind: DecorKind.Mirror),
            new("PhotoFrame", MirrorFolderPrefix, "фоторамка", -1.25f, 0.72f, 1.28f, 0.42f),
            new("Tamagotchi", TamagotchiFolderPrefix, string.Empty, -0.95f, 0.48f, 1.06f, 0.22f, kind: DecorKind.Tamagotchi),
            new("Walkman", RetroFolderPrefix, "плеер с наушниками", 1.05f, 0.42f, 1.06f, 0.26f),
            new("Cassettes", RetroFolderPrefix, "старые кассеты", 0.72f, 0.40f, 1.05f, 0.22f),
            new("Pager", RetroFolderPrefix, "пейджер", 0.38f, 0.42f, 1.05f, 0.18f),
            new("Dvd", RetroFolderPrefix, "двд диск", -0.05f, 0.40f, 1.05f, 0.20f),
            new("Floppy", RetroFolderPrefix, "диски старые дискетта", -0.38f, 0.40f, 1.05f, 0.20f),
        };

        private static readonly DecorEntry[] StreetDecor =
        {
            new("OverheadWires", "03_", "overhead_wires", 0f, -1.2f, 3.2f, 1.6f),
            new("StreetLamp", "02_", "street_lamp", 3.4f, -2.4f, 0f, 3.2f),
            new("Bench", "02_", "bench_old", 4.9f, -1.3f, 0f, 1.0f),
            new("Bin", "02_", "dumpster", -3.1f, -1.7f, 0f, 1.0f),
            new("Mailbox", "03_", "mailbox_blue", -4.4f, -2.7f, 0f, 1.2f),
            new("Bush", "05_", "bush", 6.3f, -3.1f, 0f, 1.1f),
            new("AutumnTree", "05_", "autumn_tree", -6.8f, -3.4f, 0f, 3.6f),
            new("Bicycle", "06_", "bicycle_old", 5.8f, -0.7f, 0f, 1.1f),
            new("StreetCat", "07_", "street_cat", 1.3f, -3.5f, 0f, 0.5f),
            new("Pigeons", "07_", "pigeons", -1.7f, -4.3f, 0f, 0.5f),
            new("Car2000s", "06_", "car_2000s", -9.2f, -5.2f, 0f, 1.5f),
            new("Puddle", "01_", "puddle_reflection", 2.4f, -4.8f, 0.01f, 2.2f, kind: DecorKind.FloorDecal),
        };

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/Shop/Build Kiosk Decor",
            priority = MenuItemConstants.BaseToolsItemPriority)]
        public static void BuildDecor()
        {
            if (Application.isBatchMode == false &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                Debug.LogWarning("[KioskDecor] Сборка отменена, сцены не сохранены.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            var kiosk = FindInScene<Transform>("Building_Kiosk");
            var kioskPoint = Object.FindObjectsByType<KioskPointActor>(FindObjectsInactive.Include)
                .FirstOrDefault();

            if (kiosk == false || kioskPoint == false)
            {
                Debug.LogError("[KioskDecor] В сцене нет Building_Kiosk или Actor_KioskPoint.");
                return;
            }

            var inward = kiosk.position - kioskPoint.transform.position;
            inward.y = 0f;
            inward = inward.sqrMagnitude > 0.001f ? inward.normalized : Vector3.forward;

            var right = Vector3.Cross(Vector3.up, inward);
            var origin = kioskPoint.transform.position;

            RemoveExistingRoot(KioskRootName);
            RemoveExistingRoot(StreetRootName);

            var kioskRoot = new GameObject(KioskRootName).transform;
            var streetRoot = new GameObject(StreetRootName).transform;

            var placed = 0;
            placed += PlaceAll(KioskDecor, kioskRoot, origin, right, inward);
            placed += PlaceAll(StreetDecor, streetRoot, origin, right, inward);
            placed += PlaceCalculator(kioskRoot, origin, right, inward);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                $"[KioskDecor] В сцене расставлено объектов: {placed}. "
                + "Корни Decor_Kiosk и Decor_Street можно двигать по объектам."
            );
        }

        private static int PlaceAll(
            IEnumerable<DecorEntry> entries,
            Transform parent,
            Vector3 origin,
            Vector3 right,
            Vector3 inward
        )
        {
            var placed = 0;

            foreach (var entry in entries)
            {
                if (Place(entry, parent, origin, right, inward))
                {
                    placed++;
                }
            }

            return placed;
        }

        private static bool Place(
            DecorEntry entry,
            Transform parent,
            Vector3 origin,
            Vector3 right,
            Vector3 inward
        )
        {
            var texture = FindTexture(entry);
            if (texture == false)
            {
                Debug.LogWarning($"[KioskDecor] Не найден спрайт для {entry.Name}.");
                return false;
            }

            var root = new GameObject($"Decor_{entry.Name}");
            root.layer = LayerMask.NameToLayer(InteriorLayer);

            root.transform.SetParent(parent, worldPositionStays: true);
            root.transform.position = origin
                + right * entry.X
                + inward * entry.Z
                + Vector3.up * entry.Y;
            root.transform.rotation = entry.Kind == DecorKind.FloorDecal
                ? Quaternion.Euler(90f, 0f, 0f)
                : Quaternion.LookRotation(-inward, Vector3.up);

            var body = CreateQuad(root.transform, texture, entry.Height, entry.Kind != DecorKind.FloorDecal);

            switch (entry.Kind)
            {
                case DecorKind.Tv:
                {
                    WireTv(body);
                    break;
                }

                case DecorKind.Mirror:
                {
                    WireMirror(root.transform, body, entry);
                    break;
                }

                case DecorKind.Tamagotchi:
                {
                    WireTamagotchi(root, body);
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// One flat picture, kept at the art's own proportions.
        /// </summary>
        private static GameObject CreateQuad(Transform parent, Texture2D texture, float height, bool billboard)
        {
            var body = new GameObject("Body");
            body.layer = parent.gameObject.layer;
            body.transform.SetParent(parent, worldPositionStays: false);

            var meshFilter = body.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var meshRenderer = body.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = EnsureMaterial(texture.name, texture);
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var aspect = texture.width / (float)Mathf.Max(1, texture.height);

            body.transform.localScale = new Vector3(height * aspect, height, 1f);

            if (billboard)
            {
                body.AddComponent<Billboard>();
            }

            return body;
        }

        /// <summary>
        /// The TV that zaps through the channels from the art pack.
        /// </summary>
        private static void WireTv(GameObject body)
        {
            var folder = FindFolder(ArtRoot, TvFolderPrefix);
            var channels = folder == null
                ? new List<Texture2D>()
                : FindAllTextures(folder);

            if (channels.Count == 0)
            {
                Debug.LogWarning("[KioskDecor] У телевизора нет ни одного канала.");

                return;
            }

            var tv = body.AddComponent<TvActor>();
            tv.Initialize(body.GetComponent<Renderer>(), channels);

            Debug.Log($"[KioskDecor] Телевизор: каналов {channels.Count}.");
        }

        /// <summary>
        /// The mirror figure that follows the shopkeeper.
        /// </summary>
        private static void WireMirror(Transform root, GameObject body, DecorEntry entry)
        {
            var folder = FindFolder(ArtRoot, MirrorFolderPrefix);
            var textures = folder == null ? new List<Texture2D>() : FindAllTextures(folder);

            // The pack holds the mirror, the photo frame and the little reflection figure; the figure
            // is the last one alphabetically, which is what sits inside the frame.
            var fallback = textures.Count >= 3 ? textures[^1] : default;
            var settings = AssetDatabase.LoadAssetAtPath<Settings.PlayerSettings>(PlayerSettingsPath);

            var reflection = CreateQuad(root, fallback ? fallback : textures.LastOrDefault(), entry.Height * 0.62f, true);
            reflection.name = "Reflection";
            reflection.transform.localPosition = new Vector3(0f, 0f, -0.01f);

            var mirror = body.AddComponent<MirrorActor>();
            mirror.Initialize(
                reflection.GetComponent<Renderer>(),
                reflection.transform,
                settings,
                fallback
            );

            Debug.Log("[KioskDecor] Зеркало подключено: отражение следует за игроком.");
        }

        /// <summary>
        /// The tamagotchi toy: grabbable, animates while it is in the hand.
        /// </summary>
        private static void WireTamagotchi(GameObject root, GameObject body)
        {
            var collider = body.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1f, 0.35f);

            var interactable = root.AddComponent<GrabInteractable>();

            var folder = FindFolder(ArtRoot, TamagotchiFolderPrefix);
            var frames = folder == null ? new List<Texture2D>() : FindAllTextures(folder);

            var tamagotchi = root.AddComponent<TamagotchiActor>();
            tamagotchi.Initialize(interactable, body.GetComponent<Renderer>(), frames);

            Debug.Log($"[KioskDecor] Тамагочи: кадров {frames.Count}.");
        }

        /// <summary>
        /// The cash register screen that shows the money the shopkeeper has earned.
        /// </summary>
        private static int PlaceCalculator(Transform parent, Vector3 origin, Vector3 right, Vector3 inward)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CalculatorPrefabPath);
            if (prefab == false)
            {
                Debug.LogWarning($"[KioskDecor] Нет префаба {CalculatorPrefabPath}.");
                return 0;
            }

            var existing = Object
                .FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .FirstOrDefault(transform => transform.name == "Actor_Calculator");

            if (existing)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, worldPositionStays: true);
            instance.transform.position = origin + inward * 0.62f + right * -1.35f + Vector3.up * 1.05f;
            instance.transform.rotation = Quaternion.LookRotation(-inward, Vector3.up);

            return 1;
        }

        private static Material EnsureMaterial(string name, Texture2D texture)
        {
            if (AssetDatabase.IsValidFolder(MaterialFolder) == false)
            {
                AssetDatabase.CreateFolder("Assets/Visuals/Objects", "Materials");
            }

            var safeName = name.Replace(' ', '_');
            var path = $"{MaterialFolder}/Decor_{safeName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == false)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = $"Decor_{safeName}",
                };

                AssetDatabase.CreateAsset(material, path);
            }

            material.mainTexture = texture;
            material.SetTexture("_BaseMap", texture);

            EditorUtility.SetDirty(material);

            return material;
        }

        private static Texture2D FindTexture(DecorEntry entry)
        {
            var folder = ResolveFolder(entry.Folder);
            if (folder == null)
            {
                return default;
            }

            if (string.IsNullOrEmpty(entry.Texture))
            {
                return FindAllTextures(folder).FirstOrDefault();
            }

            return FindTextureInFolder(folder, entry.Texture);
        }

        private static string ResolveFolder(string prefix)
        {
            if (prefix.StartsWith("0") == false)
            {
                return FindFolder(ArtRoot, prefix);
            }

            // Street props live in numbered folders with long names ("03_ПРОВОДА_..."), so the
            // short "03_" prefix has to be resolved to the real folder, not used as a path.
            var streetRoot = FindFolder(ArtRoot, StreetRootPrefix);

            return streetRoot == null ? null : FindFolder(streetRoot, prefix);
        }

        private static List<Texture2D> FindAllTextures(string folder)
        {
            var textures = new List<Texture2D>();

            if (AssetDatabase.IsValidFolder(folder) == false)
            {
                return textures;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

                // Sprite sheets and previews are not props.
                if (name.Contains("sheet") || name.Contains("preview") || name.Contains("исходник"))
                {
                    continue;
                }

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture)
                {
                    textures.Add(texture);
                }
            }

            textures.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

            return textures;
        }

        private static Texture2D FindTextureInFolder(string folder, string texturePrefix)
        {
            Texture2D firstMatch = default;

            foreach (var texture in FindAllTextures(folder))
            {
                if (texture.name.ToLowerInvariant().StartsWith(texturePrefix.ToLowerInvariant()) == false)
                {
                    continue;
                }

                // Always the first version of a set, so repeated runs look identical.
                if (texture.name.Contains("_v01"))
                {
                    return texture;
                }

                if (firstMatch == false)
                {
                    firstMatch = texture;
                }
            }

            return firstMatch;
        }

        private static string FindFolder(string parent, string prefix)
        {
            return AssetDatabase
                .GetSubFolders(parent)
                .FirstOrDefault(folder => Path.GetFileName(folder).StartsWith(prefix));
        }

        private static void RemoveExistingRoot(string name)
        {
            var existing = Object
                .FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .FirstOrDefault(transform => transform.name == name);

            if (existing)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static T FindInScene<T>(string name) where T : Component
        {
            return Object
                .FindObjectsByType<T>(FindObjectsInactive.Include)
                .FirstOrDefault(component => component.name == name);
        }

        private enum DecorKind
        {
            /// <summary>Flat picture that keeps facing the player.</summary>
            Billboard = 0,

            /// <summary>Lies on the ground, like a puddle.</summary>
            FloorDecal = 1,

            /// <summary>Flat picture with a working TV screen.</summary>
            Tv = 2,

            /// <summary>Flat picture with a reflection that follows the player.</summary>
            Mirror = 3,

            /// <summary>Cute toy the player can pick up.</summary>
            Tamagotchi = 4,
        }

        /// <summary>
        /// One picture in the scene: where it goes in kiosk space, how tall it is and what it does.
        /// </summary>
        private readonly struct DecorEntry
        {
            public DecorEntry(
                string name,
                string folder,
                string texture,
                float x,
                float z,
                float y,
                float height,
                DecorKind kind = DecorKind.Billboard
            )
            {
                Name = name;
                Folder = folder;
                Texture = texture;
                X = x;
                Z = z;
                Y = y;
                Height = height;
                Kind = kind;
            }

            public string Name { get; }

            public string Folder { get; }

            public string Texture { get; }

            public float X { get; }

            public float Z { get; }

            public float Y { get; }

            public float Height { get; }

            public DecorKind Kind { get; }
        }
    }
}
#endif
