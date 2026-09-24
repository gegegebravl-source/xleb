using System;
using System.Collections.Generic;
using System.IO;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Editor
{
    internal sealed class PurchaseCollectionImporterWindow : EditorWindow
    {
        private PurchaseCollection purchaseCollection;
        private string jsonPath;

        [MenuItem(MenuItemConstants.BaseWindowItemName + "/Purchase Collection Importer")]
        private static void ShowWindow()
        {
            var window = GetWindow<PurchaseCollectionImporterWindow>();
            window.titleContent = new GUIContent("Purchase Collection Loader");
            window.Show();
        }

        private void OnGUI()
        {
            purchaseCollection = (PurchaseCollection)EditorGUILayout.ObjectField(
                label: "Purchase Collection",
                obj: purchaseCollection,
                objType: typeof(PurchaseCollection),
                allowSceneObjects: false
            );

            if (GUILayout.Button("Select JSON File"))
            {
                jsonPath = EditorUtility.OpenFilePanel("Select JSON File", "", "json");
            }

            EditorGUILayout.LabelField("Selected File:", jsonPath);

            if (purchaseCollection == false || string.IsNullOrEmpty(jsonPath))
            {
                return;
            }

            if (GUILayout.Button("Load JSON Into ScriptableObject"))
            {
                LoadJsonIntoScriptableObject();
            }
        }

        private void LoadJsonIntoScriptableObject()
        {
            if (File.Exists(jsonPath) == false)
            {
                return;
            }

            var jsonData = File.ReadAllText(jsonPath);
            var purchaseWrapper = JsonUtility.FromJson<PurchaseListWrapper>("{\"purchases\":" + jsonData + "}");
            var purchaseDatas = purchaseWrapper.purchases;

            var purchases = purchaseCollection.Purchases;
            purchases.Clear();

            foreach (var purchaseData in purchaseDatas)
            {
                var keywordDatas = purchaseData.keywords;
                var purchase = new PurchaseCollection.Purchase
                {
                    TemplateText = purchaseData.text,
                };

                var keywords = purchase.Keywords;
                foreach (var keywordData in keywordDatas)
                {
                    var keyword = new Keyword();
                    var items = keyword.Items;

                    keyword.Text = keywordData.text;

                    var itemNames = keywordData.items;
                    foreach (var itemName in itemNames)
                    {
                        var item = GetItemData(itemName);
                        if (item == false)
                        {
                            Debug.LogWarning($"Item {itemName} is not found");
                            continue;
                        }

                        items.Add(item);
                    }

                    keywords.Add(keyword);
                }

                purchases.Add(purchase);
            }

            EditorUtility.SetDirty(purchaseCollection);
            AssetDatabase.SaveAssets();
        }

        private static ItemData GetItemData(string itemName)
        {
            return AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Items/Data_" + itemName + ".asset");
        }

        [Serializable]
        private sealed class PurchaseListWrapper
        {
            public List<PurchaseData> purchases;
        }

        [Serializable]
        private sealed class PurchaseData
        {
            public string text;
            public List<KeywordData> keywords;
        }

        [Serializable]
        private sealed class KeywordData
        {
            public string text;
            public List<string> items;
        }
    }
}
