#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Components.Utilities;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UABPetelnia.GGJ2025.Editor
{
    /// <summary>
    /// Включает анимации предметов ларька: пейджер грузит сообщение, тетрис играет сам с собой,
    /// телевизор щёлкает каналы, тамагочи живёт своей жизнью.
    /// </summary>
    /// <remarks>
    /// Кадры анимаций лежат в одной картинке подряд (пейджер — 4 кадра, тетрис — 4), поэтому
    /// проигрывает их <see cref="SpriteSheetAnimator"/>: он сдвигает <c>_BaseMap_ST</c> и не
    /// требует отдельной текстуры на каждый кадр.
    /// </remarks>
    public static class KioskDecorAnimationWirer
    {
        private const string GameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";
        private const string AnimationsFolder = "Assets/Art/Kirill/анимации для тамагочи плеера и тд полноценные";
        private const string TvFolder = "Assets/Art/Kirill/телевизор";

        [MenuItem(MenuItemConstants.BaseToolsItemName + "/Декор: включить анимации предметов", priority = 102)]
        public static void WireDecorAnimations()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[DecorAnim] Сначала выйди из Play Mode.");
                return;
            }

            var scene = EnsureGameplayScene();
            if (scene.IsValid() == false)
            {
                return;
            }

            var report = new List<string>();

            // Пейджер на прилавке грузит сообщение.
            WireDecor(
                "Decor_Kiosk/Decor_Pager/Body",
                $"{AnimationsFolder}/загрузка пейджера.png",
                frames: 4,
                framesPerSecond: 5f,
                report
            );

            // Тетрис на витрине играет сам с собой.
            WireProduct(
                "тетрис",
                $"{AnimationsFolder}/тетрис анимация.png",
                frames: 4,
                framesPerSecond: 4f,
                report
            );

            // Телевизор: каналы переключаются, как и раньше, но проверяем, что они подключены.
            WireTelevision(report);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[DecorAnim] " + string.Join(" | ", report));
        }

        private static void WireDecor(
            string hierarchyPath,
            string texturePath,
            int frames,
            float framesPerSecond,
            List<string> report
        )
        {
            var target = FindByPath(hierarchyPath);

            if (target == false)
            {
                report.Add($"{hierarchyPath} не найден");
                return;
            }

            var renderer = target.GetComponent<Renderer>();
            if (renderer == false)
            {
                report.Add($"{hierarchyPath}: нет Renderer");
                return;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture)
            {
                var material = renderer.sharedMaterial;

                if (material)
                {
                    material.SetTexture("_BaseMap", texture);
                    EditorUtility.SetDirty(material);
                }
            }

            var animator = target.GetComponent<SpriteSheetAnimator>();

            if (animator == false)
            {
                animator = target.AddComponent<SpriteSheetAnimator>();
            }

            animator.Initialize(renderer, texture, frames, framesPerSecond);

            report.Add($"{target.name}: {frames} кадра, {framesPerSecond} к/с");
        }

        private static void WireProduct(
            string itemId,
            string texturePath,
            int frames,
            float framesPerSecond,
            List<string> report
        )
        {
            var productName = $"Actor_Product_{itemId}";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var found = 0;

            foreach (var product in Object.FindObjectsByType<ProductActor>(FindObjectsInactive.Include))
            {
                if (product.name != productName)
                {
                    continue;
                }

                var renderer = product.GetComponentInChildren<Renderer>();

                if (renderer == false)
                {
                    continue;
                }

                if (texture)
                {
                    var material = renderer.sharedMaterial;

                    if (material)
                    {
                        material.SetTexture("_BaseMap", texture);
                        EditorUtility.SetDirty(material);
                    }
                }

                var animator = renderer.gameObject.GetComponent<SpriteSheetAnimator>();

                if (animator == false)
                {
                    animator = renderer.gameObject.AddComponent<SpriteSheetAnimator>();
                }

                animator.Initialize(renderer, texture, frames, framesPerSecond);
                found++;
            }

            report.Add($"{productName}: {found} шт по {frames} кадра");
        }

        private static void WireTelevision(List<string> report)
        {
            var target = FindByPath("Decor_Kiosk/Decor_Tv/Body");

            if (target == false)
            {
                report.Add("телевизор не найден");
                return;
            }

            var television = target.GetComponent<TvActor>();
            if (television == false)
            {
                report.Add("на телевизоре нет TvActor");
                return;
            }

            if (television.Channels.Count > 0)
            {
                report.Add($"телевизор: каналов {television.Channels.Count}");
                return;
            }

            var channels = new List<Texture2D>();
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TvFolder });

            foreach (var guid in guids)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));

                if (texture)
                {
                    channels.Add(texture);
                }
            }

            var renderer = target.GetComponent<Renderer>();

            if (renderer && channels.Count > 0)
            {
                television.Initialize(renderer, channels);
                EditorUtility.SetDirty(television);
            }

            report.Add($"телевизор: подключено каналов {channels.Count}");
        }

        private static GameObject FindByPath(string hierarchyPath)
        {
            var segments = hierarchyPath.Split('/');
            var current = GameObject.Find(segments[0]);

            for (var index = 1; index < segments.Length && current; index++)
            {
                var next = current.transform.Find(segments[index]);
                current = next ? next.gameObject : null;
            }

            return current;
        }

        private static Scene EnsureGameplayScene()
        {
            var active = SceneManager.GetActiveScene();

            if (active.path == GameplayScenePath)
            {
                return active;
            }

            if (active.isDirty)
            {
                Debug.LogError($"[DecorAnim] Сцена {Path.GetFileName(active.path)} не сохранена — сначала сохрани её.");
                return default;
            }

            return EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        }
    }
}
#endif
