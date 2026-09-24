using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UABPetelnia.GGJ2025.Editor
{
    public static class ProjectSceneRepairUtility
    {
        [MenuItem("Tools/Project/Repair scene and cursor warnings", priority = 600)]
        public static void RepairSceneWarnings()
        {
            FixCursorTextureImportSettings();
            EnsureMainCameraAudioListener();
            RepairPlayerCameraRig();
            RemoveMissingScriptComponents();
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneRepair] Cursor, camera and missing-script warnings were repaired.");
        }

        private static void FixCursorTextureImportSettings()
        {
            var basePath = Application.dataPath;
            var filePaths = Directory.GetFiles(basePath, "*Cursor*.png", SearchOption.AllDirectories);

            foreach (var fullPath in filePaths)
            {
                var relativePath = "Assets" + fullPath.Substring(basePath.Length);
                var importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var changed = false;

                if (importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    changed = true;
                }

                if (importer.isReadable != true)
                {
                    importer.isReadable = true;
                    changed = true;
                }

                if (importer.alphaIsTransparency != true)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                if (importer.mipmapEnabled != false)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void RepairPlayerCameraRig()
        {
            var sceneCount = SceneManager.sceneCount;
            for (var sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if (scene == null || scene.isLoaded == false)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root == null) continue;

                    var player = root.GetComponentInChildren<DesktopPlayerActor>(true);
                    if (player == null)
                    {
                        continue;
                    }

                    var cinemachineCamera = FindLocalCinemachineCamera(player.transform);
                    if (cinemachineCamera == null)
                    {
                        continue;
                    }

                    var serialised = new SerializedObject(player);
                    var property = serialised.FindProperty("cinemachineCamera");
                    if (property != null)
                    {
                        property.objectReferenceValue = cinemachineCamera;
                        serialised.ApplyModifiedPropertiesWithoutUndo();
                    }

                    if (cinemachineCamera != null && cinemachineCamera.transform.parent != player.transform)
                    {
                        cinemachineCamera.transform.SetParent(player.transform, worldPositionStays: false);
                    }
                }
            }
        }

        private static Component FindLocalCinemachineCamera(Transform root)
        {
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null)
                {
                    continue;
                }

                var type = component.GetType();
                if (type.FullName != null && type.FullName.Contains("CinemachineCamera"))
                {
                    return component;
                }
            }

            return null;
        }

        private static void EnsureMainCameraAudioListener()
        {
            var sceneCount = SceneManager.sceneCount;
            for (var sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if (scene == null || scene.isLoaded == false)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    EnsureAudioListenerRecursive(root);
                }
            }
        }

        private static void EnsureAudioListenerRecursive(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var camera = root.GetComponent<Camera>();
            if (camera != null && camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            var childCount = root.transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                EnsureAudioListenerRecursive(root.transform.GetChild(i).gameObject);
            }
        }

        private static void RemoveMissingScriptComponents()
        {
            var sceneCount = SceneManager.sceneCount;
            for (var sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if (scene == null || scene.isLoaded == false)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    RemoveMissingScriptComponentsRecursive(root);
                }
            }
        }

        private static void RemoveMissingScriptComponentsRecursive(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var components = root.GetComponents<Component>();
            for (var i = components.Length - 1; i >= 0; i--)
            {
                var component = components[i];
                if (component == null)
                {
                    try
                    {
                        var serializedObject = new SerializedObject(root);
                        var componentsProperty = serializedObject.FindProperty("m_Component");
                        if (componentsProperty == null)
                        {
                            continue;
                        }

                        for (var j = componentsProperty.arraySize - 1; j >= 0; j--)
                        {
                            var element = componentsProperty.GetArrayElementAtIndex(j);
                            if (element == null)
                            {
                                continue;
                            }

                            var objectReference = element.FindPropertyRelative("component");
                            if (objectReference == null || objectReference.objectReferenceValue == null)
                            {
                                componentsProperty.DeleteArrayElementAtIndex(j);
                            }
                        }

                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        EditorSceneManager.MarkSceneDirty(root.scene);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"[SceneRepair] Failed to remove missing component from {root.name}: {exception.Message}", root);
                    }
                }
            }

            for (var i = 0; i < root.transform.childCount; i++)
            {
                var child = root.transform.GetChild(i);
                if (child != null)
                {
                    RemoveMissingScriptComponentsRecursive(child.gameObject);
                }
            }
        }
    }
}
