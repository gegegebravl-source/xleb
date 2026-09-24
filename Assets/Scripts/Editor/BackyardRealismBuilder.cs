#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UABPetelnia.GGJ2025.Editor
{
    /// <summary>
    /// Replaces the flat street cut-outs behind the kiosk with a small, fully three-dimensional
    /// 2000s courtyard. It deliberately uses only assets already in the repository: the included
    /// PBR library, the realistic garage/concrete art pack and primitive geometry. No external
    /// unlicensed model is pulled into the project.
    /// </summary>
    internal static class BackyardRealismBuilder
    {
        private const string GameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";
        private const string RootName = "Environment_Backyard_Realism";
        private const string OldStreetRootName = "Decor_Street";
        private const string MaterialFolder = "Assets/Visuals/Materials/Backyard";
        private const string PbrRoot = "Assets/WarmBread/Resources/PBR";
        private const string StreetArtRoot = "Assets/Art/Kirill/ассеты и спрайты для улицы и атмосферы";

        private static readonly Color AsphaltTint = new(0.22f, 0.24f, 0.23f, 1f);
        private static readonly Color ConcreteTint = new(0.44f, 0.43f, 0.39f, 1f);
        private static readonly Color PlasterTint = new(0.32f, 0.30f, 0.28f, 1f);
        private static readonly Color RustTint = new(0.26f, 0.12f, 0.08f, 1f);
        private static readonly Color MetalTint = new(0.12f, 0.14f, 0.14f, 1f);
        private static readonly Color WoodTint = new(0.22f, 0.14f, 0.09f, 1f);
        private static readonly Color GlassTint = new(0.06f, 0.12f, 0.15f, 1f);
        private static readonly Color SnowTint = new(0.72f, 0.77f, 0.78f, 1f);

        private static readonly string AsphaltBase =
            PbrRoot + "/ConcretePavement/ConcretePavement_BaseColor.png";
        private static readonly string AsphaltNormal =
            PbrRoot + "/ConcretePavement/ConcretePavement_Height.png";
        private static readonly string PlasterBase =
            PbrRoot + "/WornPlaster/WornPlaster_BaseColor.png";
        private static readonly string PlasterNormal =
            PbrRoot + "/WornPlaster/WornPlaster_Height.png";
        private static readonly string RustBase =
            PbrRoot + "/RustedMetal/RustedMetal_BaseColor.png";
        private static readonly string RustNormal =
            PbrRoot + "/RustedMetal/RustedMetal_Height.png";
        private static readonly string GraffitiTexture =
            StreetArtRoot + "/08_ВЫВЕСКИ_ГРАФФИТИ_ОБЪЯВЛЕНИЯ_СТЕНЫ/graffiti_wall_v03.png";
        private static readonly string GarageTexture =
            StreetArtRoot + "/06_МАШИНЫ_ВЕЛОСИПЕДЫ_ГАРАЖИ/garage_metal_v01.png";
        private static readonly string OldWallTexture =
            StreetArtRoot + "/08_ВЫВЕСКИ_ГРАФФИТИ_ОБЪЯВЛЕНИЯ_СТЕНЫ/old_wall_v01.png";

        [MenuItem(
            "Tools/UAB Petelnia/Art/Build 3D 2000s Backyard",
            priority = 36)]
        public static void Build()
        {
            if (Application.isBatchMode == false &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                Debug.LogWarning("[BackyardRealism] Сборка отменена: текущая сцена не сохранена.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            var kioskPoint = GameObject.Find("Actor_KioskPoint");
            var kiosk = GameObject.Find("Building_Kiosk");

            var opening = kioskPoint != null
                ? kioskPoint.transform.position
                : new Vector3(0f, 0f, 3.45f);

            var outward = kioskPoint != null && kiosk != null
                ? kioskPoint.transform.position - kiosk.transform.position
                : Vector3.forward;

            outward.y = 0f;
            outward = outward.sqrMagnitude > 0.001f ? outward.normalized : Vector3.forward;

            var right = Vector3.Cross(Vector3.up, outward).normalized;
            var basis = Quaternion.LookRotation(outward, Vector3.up);

            RemoveRoot(OldStreetRootName);
            RemoveRoot(RootName);

            EnsureMaterialFolder();

            var root = new GameObject(RootName);
            root.isStatic = true;
            SceneManager.MoveGameObjectToScene(root, scene);

            var materials = CreateMaterials();

            BuildGround(root.transform, opening, right, outward, basis, materials);
            BuildCourtyardArchitecture(root.transform, opening, right, outward, basis, materials);
            BuildGarageAndWorkshop(root.transform, opening, right, outward, basis, materials);
            BuildCar(root.transform, opening, right, outward, basis, materials);
            BuildFenceAndUtilities(root.transform, opening, right, outward, basis, materials);
            BuildTreesAndSnow(root.transform, opening, right, outward, basis, materials);
            BuildRealistic2DDetails(root.transform, opening, right, outward, basis, materials);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[BackyardRealism] Готово: задний двор заменён на объёмную сцену с асфальтом, " +
                "гаражами, окнами, проводами, машиной, заборами, снегом, лужами и PBR-материалами."
            );
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            return new Dictionary<string, Material>
            {
                ["asphalt"] = EnsurePbrMaterial(
                    "Backyard_Asphalt",
                    AsphaltTint,
                    AsphaltBase,
                    AsphaltNormal,
                    metallic: 0f,
                    smoothness: 0.22f,
                    tiling: 8f
                ),
                ["concrete"] = EnsurePbrMaterial(
                    "Backyard_Concrete",
                    ConcreteTint,
                    PlasterBase,
                    PlasterNormal,
                    metallic: 0f,
                    smoothness: 0.18f,
                    tiling: 3f
                ),
                ["plaster"] = EnsurePbrMaterial(
                    "Backyard_WornPlaster",
                    PlasterTint,
                    PlasterBase,
                    PlasterNormal,
                    metallic: 0f,
                    smoothness: 0.2f,
                    tiling: 4f
                ),
                ["rust"] = EnsurePbrMaterial(
                    "Backyard_RustedMetal",
                    RustTint,
                    RustBase,
                    RustNormal,
                    metallic: 0.32f,
                    smoothness: 0.22f,
                    tiling: 2f
                ),
                ["metal"] = EnsureMaterial("Backyard_DarkMetal", MetalTint, null, 0.72f, 0.34f),
                ["wood"] = EnsureMaterial("Backyard_OldWood", WoodTint, null, 0.02f, 0.26f),
                ["glass"] = EnsureMaterial(
                    "Backyard_WindowGlass",
                    GlassTint,
                    null,
                    0.18f,
                    0.72f,
                    emission: new Color(0.12f, 0.18f, 0.2f, 1f)
                ),
                ["warmWindow"] = EnsureMaterial(
                    "Backyard_WarmWindow",
                    new Color(0.73f, 0.42f, 0.16f, 1f),
                    null,
                    0.05f,
                    0.36f,
                    emission: new Color(1f, 0.24f, 0.045f, 1f)
                ),
                ["water"] = EnsureMaterial(
                    "Backyard_Puddle",
                    new Color(0.06f, 0.11f, 0.13f, 1f),
                    null,
                    0.72f,
                    0.92f
                ),
                ["snow"] = EnsureMaterial("Backyard_DirtySnow", SnowTint, null, 0f, 0.12f),
                ["leaf"] = EnsureMaterial(
                    "Backyard_AutumnLeaves",
                    new Color(0.22f, 0.12f, 0.055f, 1f),
                    null,
                    0f,
                    0.28f
                ),
                ["wire"] = EnsureMaterial("Backyard_Wire", new Color(0.035f, 0.03f, 0.025f, 1f), null, 0.1f, 0.3f),
            };
        }

        private static void BuildGround(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials
        )
        {
            AddCube(
                root,
                "Ground_Asphalt_3D",
                LocalPoint(opening, right, outward, 0f, -0.16f, 15f),
                new Vector3(30f, 0.32f, 30f),
                basis,
                materials["asphalt"]
            );

            AddCube(
                root,
                "Sidewalk_Concrete",
                LocalPoint(opening, right, outward, 0f, 0.025f, 5.2f),
                new Vector3(13f, 0.12f, 2.4f),
                basis,
                materials["concrete"]
            );

            AddCube(
                root,
                "Curb_Left",
                LocalPoint(opening, right, outward, -7.1f, 0.22f, 8f),
                new Vector3(0.28f, 0.44f, 18f),
                basis,
                materials["concrete"]
            );

            AddCube(
                root,
                "Curb_Right",
                LocalPoint(opening, right, outward, 7.1f, 0.22f, 8f),
                new Vector3(0.28f, 0.44f, 18f),
                basis,
                materials["concrete"]
            );

            AddPuddle(root, opening, right, outward, basis, materials["water"], -3.5f, 7.2f, 2.2f, 0.72f);
            AddPuddle(root, opening, right, outward, basis, materials["water"], 2.7f, 11.8f, 1.45f, 0.5f);
            AddPuddle(root, opening, right, outward, basis, materials["water"], -1.2f, 18.4f, 2.8f, 0.38f);
        }

        private static void BuildCourtyardArchitecture(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials
        )
        {
            // A real far wall closes the view instead of leaving a flat black horizon.
            AddCube(
                root,
                "Courtyard_BackWall",
                LocalPoint(opening, right, outward, 0f, 4.6f, 25f),
                new Vector3(32f, 9.2f, 0.42f),
                basis,
                materials["plaster"]
            );

            AddCube(
                root,
                "Courtyard_BackWall_Foundation",
                LocalPoint(opening, right, outward, 0f, 0.28f, 24.65f),
                new Vector3(32f, 0.56f, 0.7f),
                basis,
                materials["concrete"]
            );

            BuildApartmentBlock(root, opening, right, outward, basis, materials, 8.2f, 20.3f, 9.4f, 10.5f);
            BuildWindowRow(root, opening, right, outward, basis, materials, 8.2f, 20.3f, 5.2f, 4);
            BuildWindowRow(root, opening, right, outward, basis, materials, 8.2f, 20.3f, 7.8f, 4);

            // Concrete utility boxes and a low service annex add scale close to the kiosk opening.
            AddCube(
                root,
                "UtilityRoom",
                LocalPoint(opening, right, outward, 4.2f, 1.45f, 10.2f),
                new Vector3(3.2f, 2.9f, 2.3f),
                basis,
                materials["concrete"]
            );
            AddCube(
                root,
                "UtilityRoom_Door",
                LocalPoint(opening, right, outward, 4.2f, 1.35f, 9.0f),
                new Vector3(1.15f, 2.15f, 0.08f),
                basis,
                materials["rust"]
            );
            AddCube(
                root,
                "UtilityRoom_Handle",
                LocalPoint(opening, right, outward, 4.55f, 1.35f, 8.9f),
                new Vector3(0.08f, 0.18f, 0.12f),
                basis,
                materials["metal"]
            );
        }

        private static void BuildGarageAndWorkshop(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials
        )
        {
            const float x = -7.4f;
            const float z = 14.2f;

            AddCube(
                root,
                "GarageBlock_3D",
                LocalPoint(opening, right, outward, x, 3.45f, z + 1.1f),
                new Vector3(8.6f, 6.9f, 3.9f),
                basis,
                materials["plaster"]
            );

            for (var index = 0; index < 3; index++)
            {
                var doorX = x - 2.7f + index * 2.7f;
                AddCube(
                    root,
                    $"GarageDoor_{index + 1}",
                    LocalPoint(opening, right, outward, doorX, 2.25f, z - 0.9f),
                    new Vector3(2.15f, 4.05f, 0.12f),
                    basis,
                    materials["rust"]
                );

                for (var stripe = 0; stripe < 5; stripe++)
                {
                    AddCube(
                        root,
                        $"GarageDoor_{index + 1}_Rib_{stripe + 1}",
                        LocalPoint(opening, right, outward, doorX, 0.65f + stripe * 0.8f, z - 0.99f),
                        new Vector3(1.98f, 0.045f, 0.05f),
                        basis,
                        materials["metal"]
                    );
                }
            }

            AddCube(
                root,
                "GarageRoof_Concrete",
                LocalPoint(opening, right, outward, x, 7.0f, z + 1.1f),
                new Vector3(9.2f, 0.34f, 4.5f),
                basis,
                materials["concrete"]
            );

            AddDumpster(root, opening, right, outward, basis, materials, -2.7f, 9.2f);
            AddDumpster(root, opening, right, outward, basis, materials, 3.4f, 13.2f);
        }

        private static void BuildCar(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials
        )
        {
            var car = new GameObject("Car_2000s_3D");
            car.transform.SetParent(root, true);
            car.transform.position = LocalPoint(opening, right, outward, -2.0f, 0f, 7.7f);
            car.transform.rotation = basis;
            car.isStatic = true;

            AddCube(car.transform, "Body", new Vector3(0f, 0.7f, 0f), new Vector3(2.65f, 0.66f, 4.8f), Quaternion.identity, materials["rust"], local: true);
            AddCube(car.transform, "Hood", new Vector3(0f, 1.08f, 1.25f), new Vector3(2.42f, 0.18f, 1.55f), Quaternion.identity, materials["rust"], local: true);
            AddCube(car.transform, "Cabin", new Vector3(0f, 1.45f, -0.25f), new Vector3(2.15f, 0.85f, 2.05f), Quaternion.identity, materials["metal"], local: true);
            AddCube(car.transform, "Windshield", new Vector3(0f, 1.48f, 0.76f), new Vector3(1.8f, 0.55f, 0.05f), Quaternion.identity, materials["glass"], local: true);
            AddCube(car.transform, "RearWindow", new Vector3(0f, 1.48f, -1.25f), new Vector3(1.8f, 0.55f, 0.05f), Quaternion.identity, materials["glass"], local: true);

            for (var side = -1; side <= 1; side += 2)
            {
                for (var axle = 0; axle < 2; axle++)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = side < 0 ? $"Wheel_Left_{axle + 1}" : $"Wheel_Right_{axle + 1}";
                    wheel.transform.SetParent(car.transform, false);
                    wheel.transform.localPosition = new Vector3(side * 1.34f, 0.46f, axle == 0 ? 1.42f : -1.42f);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    wheel.transform.localScale = new Vector3(0.23f, 0.23f, 0.23f);
                    wheel.GetComponent<MeshRenderer>().sharedMaterial = materials["metal"];
                    wheel.isStatic = true;
                }
            }
        }

        private static void BuildFenceAndUtilities(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials
        )
        {
            for (var index = 0; index < 7; index++)
            {
                var x = -5.5f + index * 1.8f;
                AddCylinder(
                    root,
                    $"FencePost_{index + 1}",
                    LocalPoint(opening, right, outward, x, 1.05f, 11.1f),
                    0.055f,
                    2.1f,
                    basis,
                    materials["metal"]
                );
            }

            AddCube(
                root,
                "FenceRail_Low",
                LocalPoint(opening, right, outward, 0f, 0.72f, 11.1f),
                new Vector3(12.3f, 0.07f, 0.07f),
                basis,
                materials["metal"]
            );
            AddCube(
                root,
                "FenceRail_High",
                LocalPoint(opening, right, outward, 0f, 1.8f, 11.1f),
                new Vector3(12.3f, 0.07f, 0.07f),
                basis,
                materials["metal"]
            );

            AddCylinder(
                root,
                "UtilityPole",
                LocalPoint(opening, right, outward, 6.2f, 3.7f, 15.2f),
                0.13f,
                7.4f,
                basis,
                materials["wood"]
            );

            AddWire(root, "PowerWire_Main", opening, right, outward, 5.0f, 15.2f, 8.2f, 24.4f, 6.9f, materials["wire"]);
            AddWire(root, "PowerWire_Sag", opening, right, outward, 6.2f, 15.2f, -5.5f, 20.2f, 6.25f, materials["wire"]);

            AddCube(
                root,
                "StreetLampHead",
                LocalPoint(opening, right, outward, 6.2f, 7.05f, 15.2f),
                new Vector3(0.7f, 0.18f, 0.38f),
                basis,
                materials["metal"]
            );

            var lamp = new GameObject("StreetLamp_WarmLight");
            lamp.transform.SetParent(root, true);
            lamp.transform.position = LocalPoint(opening, right, outward, 6.2f, 6.75f, 15.2f);
            var lampLight = lamp.AddComponent<Light>();
            lampLight.type = LightType.Point;
            lampLight.color = new Color(1f, 0.42f, 0.17f, 1f);
            lampLight.intensity = 34f;
            lampLight.range = 6f;
            lampLight.shadows = LightShadows.Soft;
        }

        private static void BuildTreesAndSnow(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials
        )
        {
            AddTree(root, opening, right, outward, basis, materials, -12f, 17.5f, 1.25f, 6.5f);
            AddTree(root, opening, right, outward, basis, materials, 12.4f, 25f, 1.05f, 7.2f);
            AddTree(root, opening, right, outward, basis, materials, -11.5f, 27f, 0.8f, 5.1f);

            AddSnowPile(root, opening, right, outward, basis, materials["snow"], -6f, 12.5f, 2.4f, 0.3f);
            AddSnowPile(root, opening, right, outward, basis, materials["snow"], 6f, 9.2f, 2f, 0.24f);
            AddSnowPile(root, opening, right, outward, basis, materials["snow"], 10.2f, 18.5f, 3.5f, 0.32f);
        }

        private static void BuildRealistic2DDetails(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials
        )
        {
            // Photographic 2D textures are used as surface detail on real 3D walls, never as the
            // whole background. This keeps the authored 2000s grime while removing the cardboard
            // cut-out look from the old yard.
            AddTexturedPanel(
                root,
                "Graffiti_On_Concrete",
                GraffitiTexture,
                LocalPoint(opening, right, outward, -0.5f, 3.1f, 24.74f),
                4.6f,
                2.2f,
                outward
            );
            AddTexturedPanel(
                root,
                "Garage_Door_Texture",
                GarageTexture,
                LocalPoint(opening, right, outward, -7.4f, 2.4f, 12.18f),
                6.1f,
                3.35f,
                outward
            );
            AddTexturedPanel(
                root,
                "Old_Wall_Patch",
                OldWallTexture,
                LocalPoint(opening, right, outward, 13.6f, 2.6f, 24.72f),
                4.7f,
                2.6f,
                outward
            );
        }

        private static void BuildApartmentBlock(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials,
            float x,
            float z,
            float width,
            float height
        )
        {
            AddCube(
                root,
                "ApartmentBlock_3D",
                LocalPoint(opening, right, outward, x, height * 0.5f, z),
                new Vector3(width, height, 3.6f),
                basis,
                materials["plaster"]
            );

            AddCube(
                root,
                "ApartmentBlock_Roof",
                LocalPoint(opening, right, outward, x, height + 0.16f, z),
                new Vector3(width + 0.35f, 0.32f, 3.95f),
                basis,
                materials["concrete"]
            );
        }

        private static void BuildWindowRow(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials,
            float x,
            float z,
            float y,
            int count
        )
        {
            for (var index = 0; index < count; index++)
            {
                var windowX = x - 3.15f + index * 2.1f;
                var material = index == 1 || index == 3 ? materials["warmWindow"] : materials["glass"];

                AddCube(
                    root,
                    $"ApartmentWindow_{y:0}_{index + 1}",
                    LocalPoint(opening, right, outward, windowX, y, z - 1.84f),
                    new Vector3(1.24f, 1.36f, 0.08f),
                    basis,
                    material
                );
                AddCube(
                    root,
                    $"ApartmentWindowFrame_Left_{y:0}_{index + 1}",
                    LocalPoint(opening, right, outward, windowX - 0.68f, y, z - 1.91f),
                    new Vector3(0.1f, 1.56f, 0.05f),
                    basis,
                    materials["wood"]
                );
                AddCube(
                    root,
                    $"ApartmentWindowFrame_Right_{y:0}_{index + 1}",
                    LocalPoint(opening, right, outward, windowX + 0.68f, y, z - 1.91f),
                    new Vector3(0.1f, 1.56f, 0.05f),
                    basis,
                    materials["wood"]
                );
                AddCube(
                    root,
                    $"ApartmentWindowFrame_Top_{y:0}_{index + 1}",
                    LocalPoint(opening, right, outward, windowX, y + 0.73f, z - 1.91f),
                    new Vector3(1.46f, 0.1f, 0.05f),
                    basis,
                    materials["wood"]
                );
                AddCube(
                    root,
                    $"ApartmentWindowFrame_Bottom_{y:0}_{index + 1}",
                    LocalPoint(opening, right, outward, windowX, y - 0.73f, z - 1.91f),
                    new Vector3(1.46f, 0.1f, 0.05f),
                    basis,
                    materials["wood"]
                );
            }
        }

        private static void AddTree(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials,
            float x,
            float z,
            float trunkRadius,
            float height
        )
        {
            AddCylinder(
                root,
                "Tree_Trunk",
                LocalPoint(opening, right, outward, x, height * 0.5f, z),
                trunkRadius,
                height,
                basis,
                materials["wood"]
            );

            var canopyRoot = new GameObject("Tree_Canopy");
            canopyRoot.transform.SetParent(root, true);
            canopyRoot.transform.position = LocalPoint(opening, right, outward, x, height + 0.8f, z);
            canopyRoot.isStatic = true;

            for (var index = 0; index < 5; index++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = "LeafCluster";
                leaf.transform.SetParent(canopyRoot.transform, false);
                leaf.transform.localPosition = new Vector3(
                    (index - 2) * 0.72f,
                    Mathf.Sin(index * 1.7f) * 0.45f,
                    Mathf.Cos(index * 1.2f) * 0.42f
                );
                var scale = 1.45f - Mathf.Abs(index - 2) * 0.12f;
                leaf.transform.localScale = new Vector3(scale, scale * 0.7f, scale);
                leaf.GetComponent<MeshRenderer>().sharedMaterial = materials["leaf"];
                leaf.isStatic = true;
            }
        }

        private static void AddDumpster(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            IReadOnlyDictionary<string, Material> materials,
            float x,
            float z
        )
        {
            AddCube(root, "Dumpster_Body", LocalPoint(opening, right, outward, x, 0.85f, z), new Vector3(1.8f, 1.7f, 1.2f), basis, materials["rust"]);
            AddCube(root, "Dumpster_Lid", LocalPoint(opening, right, outward, x, 1.75f, z - 0.08f), new Vector3(1.95f, 0.12f, 1.35f), basis, materials["metal"]);
            AddCube(root, "Dumpster_Handle", LocalPoint(opening, right, outward, x, 1.76f, z - 0.78f), new Vector3(0.52f, 0.08f, 0.08f), basis, materials["metal"]);
        }

        private static void AddPuddle(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            Material material,
            float x,
            float z,
            float size,
            float thickness
        )
        {
            AddCube(root, "Puddle_Reflective", LocalPoint(opening, right, outward, x, 0.012f, z), new Vector3(size, 0.025f, size * 0.52f), basis, material);
        }

        private static void AddSnowPile(
            Transform root,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            Quaternion basis,
            Material material,
            float x,
            float z,
            float size,
            float height
        )
        {
            var snow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            snow.name = "DirtySnowPile";
            snow.transform.SetParent(root, true);
            snow.transform.position = LocalPoint(opening, right, outward, x, height * 0.45f, z);
            snow.transform.rotation = basis;
            snow.transform.localScale = new Vector3(size, height, size * 0.48f);
            snow.GetComponent<MeshRenderer>().sharedMaterial = material;
            snow.isStatic = true;
        }

        private static void AddWire(
            Transform root,
            string name,
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            float x1,
            float z1,
            float x2,
            float z2,
            float y,
            Material material
        )
        {
            var wire = new GameObject(name);
            wire.transform.SetParent(root, true);
            wire.isStatic = true;

            var line = wire.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.positionCount = 3;
            line.startWidth = 0.025f;
            line.endWidth = 0.025f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = true;
            line.SetPosition(0, LocalPoint(opening, right, outward, x1, y, z1));
            line.SetPosition(1, LocalPoint(opening, right, outward, (x1 + x2) * 0.5f, y - 0.42f, (z1 + z2) * 0.5f));
            line.SetPosition(2, LocalPoint(opening, right, outward, x2, y, z2));
        }

        private static GameObject AddCube(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            Quaternion rotation,
            Material material,
            bool local = false
        )
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, worldPositionStays: !local);

            if (local)
            {
                cube.transform.localPosition = position;
                cube.transform.localRotation = rotation;
                cube.transform.localScale = size;
            }
            else
            {
                cube.transform.position = position;
                cube.transform.rotation = rotation;
                cube.transform.localScale = size;
            }

            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            cube.isStatic = true;
            return cube;
        }

        private static void AddCylinder(
            Transform parent,
            string name,
            Vector3 position,
            float radius,
            float height,
            Quaternion basis,
            Material material
        )
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, true);
            cylinder.transform.position = position;
            cylinder.transform.rotation = basis;
            cylinder.transform.localScale = new Vector3(radius, height * 0.5f, radius);
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
            cylinder.isStatic = true;
        }

        private static void AddTexturedPanel(
            Transform parent,
            string name,
            string texturePath,
            Vector3 position,
            float width,
            float height,
            Vector3 outward
        )
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == false)
            {
                Debug.LogWarning("[BackyardRealism] Не найден текстурный detail: " + texturePath);
                return;
            }

            var panel = new GameObject(name);
            panel.transform.SetParent(parent, true);
            panel.transform.position = position;
            panel.transform.rotation = Quaternion.LookRotation(-outward, Vector3.up);
            panel.transform.localScale = new Vector3(width, height, 1f);
            panel.isStatic = true;

            var filter = panel.AddComponent<MeshFilter>();
            filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var renderer = panel.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = EnsureCutoutMaterial(name, texture);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Vector3 LocalPoint(
            Vector3 opening,
            Vector3 right,
            Vector3 outward,
            float x,
            float y,
            float z
        )
        {
            return opening + right * x + Vector3.up * y + outward * z;
        }

        private static void RemoveRoot(string name)
        {
            var oldRoot = GameObject.Find(name);
            if (oldRoot != null)
            {
                Object.DestroyImmediate(oldRoot);
            }
        }

        private static void EnsureMaterialFolder()
        {
            if (AssetDatabase.IsValidFolder("Assets/Visuals/Materials") == false)
            {
                AssetDatabase.CreateFolder("Assets/Visuals", "Materials");
            }

            if (AssetDatabase.IsValidFolder(MaterialFolder) == false)
            {
                AssetDatabase.CreateFolder("Assets/Visuals/Materials", "Backyard");
            }
        }

        private static Material EnsureMaterial(
            string name,
            Color color,
            Texture2D texture,
            float metallic,
            float smoothness,
            Color? emission = null
        )
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == false)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            ApplyMaterialSettings(material, color, texture, metallic, smoothness, emission);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsurePbrMaterial(
            string name,
            Color tint,
            string basePath,
            string normalPath,
            float metallic,
            float smoothness,
            float tiling
        )
        {
            var baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath);
            var normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var material = EnsureMaterial(name, tint, baseTexture, metallic, smoothness);

            if (normalTexture != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normalTexture);
                material.EnableKeyword("_NORMALMAP");
                if (material.HasProperty("_BumpScale"))
                {
                    material.SetFloat("_BumpScale", 0.32f);
                }
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            }

            return material;
        }

        private static Material EnsureCutoutMaterial(string name, Texture2D texture)
        {
            var path = MaterialFolder + "/Backyard_Detail_" + Sanitize(name) + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == false)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent Cutout");
                material = new Material(shader) { name = "Backyard_Detail_" + Sanitize(name) };
                AssetDatabase.CreateAsset(material, path);
            }

            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }
            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 1f);
            }
            if (material.HasProperty("_Cutoff"))
            {
                material.SetFloat("_Cutoff", 0.22f);
            }
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            material.renderQueue = 2450;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyMaterialSettings(
            Material material,
            Color color,
            Texture2D texture,
            float metallic,
            float smoothness,
            Color? emission
        )
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;

            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
        }

        private static string Sanitize(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
            return string.IsNullOrWhiteSpace(clean) ? "Detail" : clean;
        }
    }
}
#endif
