using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEditor;
using UnityEngine;

// Тела отключенных инструментов (после return-гвардов) оставлены намеренно — глушим CS0162.
#pragma warning disable 0162

namespace UABPetelnia.GGJ2025.Editor
{
    /// <summary>
    /// Wires the hand-drawn art pack in <c>Assets/Art/Kirill</c> into the game. The pack is plain
    /// PNG art, so this importer is the single place that decides how it maps onto the existing
    /// item, shopper and menu assets. It is idempotent: run it again after adding new art and only
    /// the matching pieces are refreshed.
    /// </summary>
    public static class UserArtPackImporter
    {
        private const string ArtRoot = "Assets/Art/Kirill";
        private const string MenuIconsRoot = ArtRoot + "/иконки главного меню/icons";
        private const string PeopleRoot = ArtRoot + "/люди";
        private const string BreadRoot = ArtRoot + "/хлеб и выпечка";
        private const string DrinkRoot = ArtRoot + "/напитки 2д спрайты";
        private const string CigaretteRoot = ArtRoot + "/сигареты";
        private const string RetroRoot = ArtRoot + "/предметы 2000 годов для вайба";
        private const string BoxRoot = ArtRoot + "/коробка картонная";

        private const string ItemsDataRoot = "Assets/Data/Items";
        private const string ShoppersDataRoot = "Assets/Data/Shoppers";
        private const string MenuIconsRoot_Target = "Assets/Visuals/UI/Icons";
        private const string GameplaySettingsPath = "Assets/Settings/Game/Settings_Gameplay.asset";
        private const string ShopperPrefabPath = "Assets/Prefabs/Actors/Actor_Shopper.prefab";

        private const string PurchasesRoot = "Assets/Data/Purchases";
        private const string WarmBreadPurchasesPath = PurchasesRoot + "/Data_Purchases_WarmBread.asset";

        /// <summary>
        /// Request templates. They are written so that a nominative keyword phrase reads correctly:
        /// "У вас есть свежий батон?" instead of needing case declension.
        /// </summary>
        private static readonly string[] RequestTemplates =
        {
            "Здравствуйте! У вас есть ${KEYWORD}?",
            "Добрый день! ${KEYWORD} найдётся?",
            "Привет! Есть ${KEYWORD}?",
            "Слышал, у вас тут ${KEYWORD}. Это правда?",
            "Мне сказали, тут всегда ${KEYWORD}.",
            "Здравствуйте! Подскажете, ${KEYWORD} у вас в наличии?",
        };

        /// <summary>
        /// Bread artwork has no descriptive file names, so the phrases are simply cycled by index.
        /// Rename the items afterwards and adjust these lines to match what each picture actually is.
        /// </summary>
        private static readonly string[] BreadPhrases =
        {
            "свежий батон",
            "тёплый хлеб",
            "булочка с маком",
            "румяный пирожок",
            "ватрушка",
            "плюшка с корицей",
            "ржаной хлеб",
            "багет",
            "круассан",
            "сдобная булочка",
        };

        /// <summary>
        /// Item name fragment to the phrase the shopper asks for. Matched with <c>Contains</c>, so
        /// <c>Item_Alus</c>, <c>Item_SodaBuratinas</c> and the generated transliterated slugs all
        /// resolve without keeping a brittle full-name table.
        /// </summary>
        private static readonly (string Fragment, string Phrase)[] ItemPhrases =
        {
            // Art pack: drinks.
            ("Kvasa", "бутылка кваса"),
            ("Kruzhka", "кружка чая"),
            ("Limonad", "лимонад «Буратино»"),
            ("Mors", "морс"),
            // Art pack: cigarettes.
            ("Zazhigalka", "зажигалка"),
            ("Prima", "пачка сигарет «Прима»"),
            ("Yava", "пачка сигарет «Ява»"),
            ("Sigareta", "сигарета"),
            // Art pack: 2000s nostalgia goods.
            ("Dvd", "DVD-диск"),
            ("Diskett", "старые дискеты"),
            ("Peydzher", "пейджер"),
            ("Plakat", "плакат с рок-группой"),
            ("Pleer", "плеер с наушниками"),
            ("Kasset", "старые кассеты"),
            ("Telefon", "старый телефон"),
            ("Tamagochi", "тамагочи"),
            ("Tetris", "тетрис"),
            ("Korobka", "картонная коробка"),
            // Original game assortment.
            ("Alus", "пиво"),
            ("Alita", "игристое"),
            ("Aspirin", "аспирин"),
            ("Balloon", "воздушный шарик"),
            ("BubbleTubeBlower", "трубочка для пузырей"),
            ("Colgate", "зубная паста"),
            ("CrazyDips", "сухарики"),
            ("Durex", "презервативы"),
            ("GumDbz", "жвачка с картинками"),
            ("GumHubbaBubba", "жвачка «Хубба-Бубба»"),
            ("GumLoveIs", "жвачка «Love is»"),
            ("Selita", "минеральная вода"),
            ("SoapUkinis", "мыло"),
            ("SodaBuratinas", "лимонад «Буратино»"),
            ("SodaVytautas", "газировка"),
            ("Zozole", "жвачка «Зозуле»"),
            ("ACC", "батарейки"),
        };

        /// <summary>
        /// Files that must never be pulled into the game, e.g. the author's own reference shots or
        /// the "this is only an example" mockup.
        /// </summary>
        private const string SkipMarker = "НЕ ВСТАВЛЯТЬ";

        /// <summary>
        /// Exact, one-to-one replacements for textures the game already uses. The target file is
        /// overwritten in place, so its <c>.meta</c> guid never changes and every prefab, material
        /// and data asset keeps pointing at the same texture with new pixels. Add pairs here when
        /// one of the author's drawings clearly replaces an existing game texture.
        /// </summary>
        private static readonly (string Source, string Target)[] DirectReplacements =
        {
            ("напитки 2д спрайты/лимонад буратино.png", "Assets/Visuals/Items/Textures/Item_SodaBuratinas.png"),
        };

        private static readonly Dictionary<char, string> Transliteration = new()
        {
            { 'а', "a" }, { 'б', "b" }, { 'в', "v" }, { 'г', "g" }, { 'д', "d" }, { 'е', "e" },
            { 'ё', "e" }, { 'ж', "zh" }, { 'з', "z" }, { 'и', "i" }, { 'й', "y" }, { 'к', "k" },
            { 'л', "l" }, { 'м', "m" }, { 'н', "n" }, { 'о', "o" }, { 'п', "p" }, { 'р', "r" },
            { 'с', "s" }, { 'т', "t" }, { 'у', "u" }, { 'ф', "f" }, { 'х', "h" }, { 'ц', "ts" },
            { 'ч', "ch" }, { 'ш', "sh" }, { 'щ', "sch" }, { 'ъ', "" }, { 'ы', "y" }, { 'ь', "" },
            { 'э', "e" }, { 'ю', "yu" }, { 'я', "ya" },
        };

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/Art/Import Kirill Art Pack",
            priority = MenuItemConstants.BaseToolsItemPriority)]
        public static void ImportArtPack()
        {
            Debug.Log("[UserArtPack] Процедурный импорт арт-пака отключён: всё раскладывайте руками в редакторе.");
            return;

            ApplyImportSettings();

            var replacements = ApplyDirectReplacements();
            var icons = ReplaceMenuIcons();

            // Items come first: the shopper lines are generated from the finished assortment.
            var items = CreateItemData();
            var purchases = CreateWarmBreadPurchases(items);
            var shoppers = CreateShopperData(purchases);

            ApplyToGameplaySettings(shoppers, items.Select(item => item.Data).ToList());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[UserArtPack] Готово. Заменённых текстур: {replacements}. Иконки меню: {icons}. " +
                $"Персонажи: {shoppers.Count}. Товаров всего: {items.Count}. " +
                $"Диалогов покупок: {purchases.Purchases.Count}. Ассортимент обновлён.",
                AssetDatabase.LoadAssetAtPath<GameplaySettings>(GameplaySettingsPath)
            );
        }

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/Art/Settings/Validate Kirill Art Pack",
            priority = MenuItemConstants.BaseToolsItemPriority + 1)]
        private static void ValidateArtPack()
        {
            var problems = new List<string>();

            if (Directory.Exists(ArtRoot) == false)
            {
                problems.Add($"Нет папки {ArtRoot}. Перенеси свой арт внутрь Assets.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameplaySettings>(GameplaySettingsPath) == false)
            {
                problems.Add($"Нет настроек {GameplaySettingsPath}.");
            }

            if (AssetDatabase.LoadAssetAtPath<ShopperActor>(ShopperPrefabPath) == false)
            {
                problems.Add($"Нет префаба покупателя {ShopperPrefabPath}.");
            }

            problems.AddRange(
                Directory.GetFiles(ArtRoot, "*.png", SearchOption.AllDirectories)
                    .Where(path => path.Contains(SkipMarker))
                    .Select(path => $"Пропущен (помечен как не вставлять): {path}")
            );

            if (problems.Count == 0)
            {
                Debug.Log("[UserArtPack] Проверка пройдена, арт-пак на месте.");
                return;
            }

            foreach (var problem in problems)
            {
                Debug.LogWarning($"[UserArtPack] {problem}");
            }
        }

        /// <summary>
        /// Menu icons need a Sprite importer, the rest of the pack is used as a plain texture
        /// painted into material property blocks.
        /// </summary>
        private static void ApplyImportSettings()
        {
            var paths = Directory.GetFiles(ArtRoot, "*.png", SearchOption.AllDirectories);

            foreach (var path in paths)
            {
                var assetPath = path.Replace('\\', '/');
                if (ShouldSkip(assetPath))
                {
                    continue;
                }

                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                {
                    continue;
                }

                var isMenuIcon = assetPath.StartsWith(MenuIconsRoot);

                importer.textureType = isMenuIcon
                    ? TextureImporterType.Sprite
                    : TextureImporterType.Default;

                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = isMenuIcon ? 512 : 2048;

                importer.SaveAndReimport();
            }
        }

        private static int ApplyDirectReplacements()
        {
            var replaced = 0;

            foreach (var (source, target) in DirectReplacements)
            {
                var sourcePath = $"{ArtRoot}/{source}";

                if (File.Exists(sourcePath) == false)
                {
                    Debug.LogWarning($"[UserArtPack] Нет исходника {sourcePath}.");
                    continue;
                }

                if (File.Exists(target) == false)
                {
                    Debug.LogWarning($"[UserArtPack] Нет игровой текстуры {target}.");
                    continue;
                }

                File.Copy(sourcePath, target, overwrite: true);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);

                replaced++;
            }

            return replaced;
        }

        /// <remarks>
        /// The existing icon files are overwritten in place so their <c>.meta</c> guid never
        /// changes: the main menu prefab keeps pointing at the same assets and just gets new pixels.
        /// </remarks>
        private static int ReplaceMenuIcons()
        {
            var mapping = new (string Source, string Target)[]
            {
                ("01_new_game.png", "UI_Icon_NewGame.png"),
                ("02_continue.png", "UI_Icon_Continue.png"),
                ("03_settings.png", "UI_Icon_Settings.png"),
                ("04_journal.png", "UI_Icon_Journal.png"),
                ("05_achievements.png", "UI_Icon_Achievements.png"),
                ("06_exit.png", "UI_Icon_Exit.png"),
            };

            var replaced = 0;

            foreach (var (source, target) in mapping)
            {
                var sourcePath = $"{MenuIconsRoot}/{source}";
                var targetPath = $"{MenuIconsRoot_Target}/{target}";

                if (File.Exists(sourcePath) == false)
                {
                    Debug.LogWarning($"[UserArtPack] Нет иконки {sourcePath}.");
                    continue;
                }

                File.Copy(sourcePath, targetPath, overwrite: true);
                AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

                if (AssetImporter.GetAtPath(targetPath) is TextureImporter importer)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = 512;
                    importer.SaveAndReimport();
                }

                replaced++;
            }

            return replaced;
        }

        /// <summary>
        /// One <see cref="ShopperData"/> per character pack folder, reusing the existing purchase
        /// collections so the dialogue keeps working with the new cast.
        /// </summary>
        private static List<ShopperData> CreateShopperData(PurchaseCollection purchases)
        {
            var created = new List<ShopperData>();
            var prefab = AssetDatabase.LoadAssetAtPath<ShopperActor>(ShopperPrefabPath);

            if (prefab == false || Directory.Exists(PeopleRoot) == false)
            {
                Debug.LogWarning("[UserArtPack] Нет префаба покупателя или папки с персонажами.");
                return created;
            }

            var characterFolders = Directory.GetDirectories(PeopleRoot).OrderBy(path => path);

            foreach (var folder in characterFolders)
            {
                var slug = Path.GetFileName(folder);
                var texture = LoadCharacterTexture(folder);

                if (texture == false)
                {
                    Debug.LogWarning($"[UserArtPack] У персонажа {slug} нет подходящего PNG.");
                    continue;
                }

                var assetPath = $"{ShoppersDataRoot}/Data_Shopper_Kirill_{slug}.asset";
                var data = AssetDatabase.LoadAssetAtPath<ShopperData>(assetPath);

                if (data == false)
                {
                    data = ScriptableObject.CreateInstance<ShopperData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                var serialized = new SerializedObject(data);
                serialized.FindProperty("shopperPrefab").objectReferenceValue = prefab;
                serialized.FindProperty("image").objectReferenceValue = texture;
                serialized.FindProperty("purchases").objectReferenceValue = purchases;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                created.Add(data);
            }

            return created;
        }

        private static Texture2D LoadCharacterTexture(string folder)
        {
            // sprites_1024 is the transparent canvas variant, which is what the billboard needs.
            var candidates = new[]
            {
                Path.Combine(folder, "sprites_1024", "view_01.png"),
                Path.Combine(folder, "sprites_1024", "view_02.png"),
                Path.Combine(folder, "native", "view_01.png"),
            };

            foreach (var candidate in candidates)
            {
                var assetPath = candidate.Replace('\\', '/');
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                if (texture)
                {
                    return texture;
                }
            }

            return default;
        }

        /// <summary>
        /// Turns every PNG in the goods folders into a sellable <see cref="ItemData"/>, so the
        /// shelves can offer the whole assortment.
        /// </summary>
        private static List<CreatedItem> CreateItemData()
        {
            var created = new List<CreatedItem>();

            created.AddRange(CreateItems(BreadRoot, prefix: "Hleb", price: 300));
            created.AddRange(CreateItems(DrinkRoot, prefix: "Napitok", price: 500));
            created.AddRange(CreateItems(CigaretteRoot, prefix: "Sigarety", price: 900));
            created.AddRange(CreateItems(RetroRoot, prefix: "Retro", price: 1200));
            created.AddRange(CreateItems(BoxRoot, prefix: "Korobka", price: 200));

            return created;
        }

        private static IEnumerable<CreatedItem> CreateItems(string folder, string prefix, int price)
        {
            if (Directory.Exists(folder) == false)
            {
                yield break;
            }

            var files = Directory
                .GetFiles(folder, "*.png", SearchOption.AllDirectories)
                .Where(path => path.Contains(SkipMarker) == false)
                .OrderBy(path => path)
                .ToList();

            for (var index = 0; index < files.Count; index++)
            {
                var assetPath = files[index].Replace('\\', '/');
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                if (texture == false)
                {
                    Debug.LogWarning($"[UserArtPack] Не удалось загрузить текстуру {assetPath}.");
                    continue;
                }

                var slug = Slugify(Path.GetFileNameWithoutExtension(assetPath), prefix, index);
                var dataPath = $"{ItemsDataRoot}/Data_Item_{slug}.asset";

                var data = AssetDatabase.LoadAssetAtPath<ItemData>(dataPath);

                if (data == false)
                {
                    data = ScriptableObject.CreateInstance<ItemData>();
                    AssetDatabase.CreateAsset(data, dataPath);
                }

                var serialized = new SerializedObject(data);
                serialized.FindProperty("price").intValue = GetPrice(assetPath, price);
                serialized.FindProperty("image").objectReferenceValue = texture;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                yield return new CreatedItem(data, slug, Path.GetFileNameWithoutExtension(assetPath));
            }
        }

        /// <summary>
        /// Builds the shopper dialogue for the finished assortment. Every item in the shop - both the
        /// hand drawn goods and the original game stock - gets its own line, so anything sitting on a
        /// shelf can actually be asked for.
        /// </summary>
        private static PurchaseCollection CreateWarmBreadPurchases(IReadOnlyCollection<CreatedItem> created)
        {
            var shopItems = new List<CreatedItem>(created);

            foreach (var path in Directory.GetFiles(ItemsDataRoot, "Data_Item_*.asset"))
            {
                var assetPath = path.Replace('\\', '/');
                var data = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

                if (data == false || shopItems.Any(item => item.Data == data))
                {
                    continue;
                }

                var fileName = Path.GetFileNameWithoutExtension(assetPath);
                var slug = fileName.StartsWith("Data_Item_") ? fileName["Data_Item_".Length..] : fileName;

                shopItems.Add(new CreatedItem(data, slug, fileName));
            }

            var collection = AssetDatabase.LoadAssetAtPath<PurchaseCollection>(WarmBreadPurchasesPath);

            if (collection == false)
            {
                collection = ScriptableObject.CreateInstance<PurchaseCollection>();
                AssetDatabase.CreateAsset(collection, WarmBreadPurchasesPath);
            }

            var purchases = collection.Purchases;
            purchases.Clear();

            foreach (var template in RequestTemplates)
            {
                var purchase = new PurchaseCollection.Purchase
                {
                    TemplateText = template,
                };

                purchases.Add(purchase);
            }

            for (var index = 0; index < shopItems.Count; index++)
            {
                var item = shopItems[index];
                var keyword = new Keyword
                {
                    Text = DescribeItem(item),
                };

                keyword.Items.Add(item.Data);

                // Spread the lines over every template so the shop does not sound repetitive.
                purchases[index % purchases.Count].Keywords.Add(keyword);
            }

            EditorUtility.SetDirty(collection);

            return collection;
        }

        /// <summary>
        /// The nominative phrase the shopper uses for the given item.
        /// </summary>
        private static string DescribeItem(CreatedItem item)
        {
            if (item.Slug.StartsWith("Hleb", System.StringComparison.OrdinalIgnoreCase))
            {
                var digits = new string(item.Slug.Where(char.IsDigit).ToArray());

                if (int.TryParse(digits, out var breadIndex) && breadIndex > 0)
                {
                    return BreadPhrases[(breadIndex - 1) % BreadPhrases.Length];
                }

                return BreadPhrases[0];
            }

            foreach (var (fragment, phrase) in ItemPhrases)
            {
                if (item.Slug.Contains(fragment, System.StringComparison.OrdinalIgnoreCase))
                {
                    return phrase;
                }
            }

            // Nothing matched: fall back to the file name, which is still better than no line at all.
            return item.FileName.Replace('_', ' ').ToLowerInvariant();
        }

        /// <summary>
        /// An item that was just created or discovered, together with the naming information the
        /// dialogue generator needs.
        /// </summary>
        private sealed class CreatedItem
        {
            public CreatedItem(ItemData data, string slug, string fileName)
            {
                Data = data;
                Slug = slug;
                FileName = fileName;
            }

            public ItemData Data { get; }

            public string Slug { get; }

            public string FileName { get; }
        }

        /// <remarks>
        /// Generated art (AI exports, scanned sheets) has unusable file names, so those fall back
        /// to a stable <c>Prefix_NN</c> name. Descriptive names are transliterated instead, which
        /// keeps the data readable: "бутылка кваса" becomes <c>ButylkaKvasa</c>.
        /// </remarks>
        private static string Slugify(string fileName, string prefix, int index)
        {
            var builder = new StringBuilder();

            foreach (var character in fileName.ToLowerInvariant())
            {
                if (Transliteration.TryGetValue(character, out var replacement))
                {
                    builder.Append(replacement);
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
                else
                {
                    builder.Append(' ');
                }
            }

            var words = builder
                .ToString()
                .Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1))
                .ToList();

            var slug = string.Concat(words);

            var isUsable = slug.Length is > 0 and <= 28
                && slug.StartsWith("Chatgpt", System.StringComparison.OrdinalIgnoreCase) == false
                && slug.StartsWith("Image", System.StringComparison.OrdinalIgnoreCase) == false;

            return isUsable
                ? slug
                : $"{prefix}_{index + 1:D2}";
        }

        private static int GetPrice(string assetPath, int fallbackPrice)
        {
            var name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();

            if (name.Contains("hleb"))
            {
                return 250;
            }

            if (name.Contains("cigaret") || name.Contains("прима") || name.Contains("ява"))
            {
                return 900;
            }

            return fallbackPrice;
        }

        /// <summary>
        /// The art pack replaces the whole shop: the new cast of shoppers and the full assortment
        /// (existing items are kept so old dialogue still resolves its goods).
        /// </summary>
        private static void ApplyToGameplaySettings(
            IReadOnlyCollection<ShopperData> shoppers,
            IReadOnlyCollection<ItemData> items)
        {
            var settings = AssetDatabase.LoadAssetAtPath<GameplaySettings>(GameplaySettingsPath);

            if (settings == false)
            {
                Debug.LogError($"[UserArtPack] Не найдены настройки {GameplaySettingsPath}.");
                return;
            }

            var serialized = new SerializedObject(settings);

            if (shoppers.Count > 0)
            {
                var availableShoppers = serialized.FindProperty("availableShoppers");
                availableShoppers.arraySize = shoppers.Count;

                var shopperIndex = 0;
                foreach (var shopper in shoppers)
                {
                    availableShoppers
                        .GetArrayElementAtIndex(shopperIndex)
                        .objectReferenceValue = shopper;

                    shopperIndex++;
                }
            }

            if (items.Count > 0)
            {
                var availableItems = serialized.FindProperty("availableItems");
                var existing = new List<Object>();

                for (var index = 0; index < availableItems.arraySize; index++)
                {
                    var value = availableItems.GetArrayElementAtIndex(index).objectReferenceValue;
                    if (value)
                    {
                        existing.Add(value);
                    }
                }

                foreach (var item in items)
                {
                    if (existing.Contains(item) == false)
                    {
                        existing.Add(item);
                    }
                }

                availableItems.arraySize = existing.Count;

                for (var index = 0; index < existing.Count; index++)
                {
                    availableItems.GetArrayElementAtIndex(index).objectReferenceValue = existing[index];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(settings);
        }

        private static bool ShouldSkip(string assetPath)
        {
            if (assetPath.Contains(SkipMarker))
            {
                return true;
            }

            if (assetPath.Contains("/native/")
                || assetPath.Contains("/90_ИСХОДНЫЕ_СПРАЙТ_ЛИСТЫ/")
                || assetPath.Contains("/99_ПРЕВЬЮ/")
                || assetPath.Contains("/00_КАТАЛОГ/"))
            {
                return true;
            }

            var fileName = Path.GetFileName(assetPath);

            return fileName is "source_sheet.png" or "reference_original.png" or "sheet_3x2.png" or "preview.png";
        }
    }
}
