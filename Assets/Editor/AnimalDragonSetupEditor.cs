#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tools > Farm Game > Setup Animal Shop + Dragon Upgrades
//   Creates the animal products / feed / animal shop items, adds AnimalProducer
//   to the four animal prefabs, adds everything to the shop, and makes sure a
//   DragonStats object exists. Safe to run again: it never overwrites values
//   you have tuned.
//
// Tools > Farm Game > Validate Animals + Dragons
//   Re-tests the whole setup and reports anything that would cause errors
//   (missing prefab scripts, missing sell prices, missing Animator parameters,
//   items that are not in the shop, dragons that cannot see monsters ...).
public static class AnimalDragonSetupEditor
{
    private const string ItemSpriteFolder = "Assets/AnimalItems";
    private const string ItemDataFolder = "Assets/data/item";
    private const string AnimalPrefabFolder = "Assets/Prefabs/Animals";
    private const string TargetLayerName = "Targetable";

    private sealed class AnimalSpec
    {
        public string prefabName;     // Pig / Chicken / Cow / Sheep
        public string animalName;     // shown in the shop
        public int shopPrice;
        public int maxOwned;

        public string productAsset;   // asset file name, e.g. "Egg"
        public string productSprite;  // png file name
        public string productName;    // shown in inventory / shop
        public int productPrice;

        public int dropEveryDays;
        public float minSatisfaction;
    }

    private static readonly AnimalSpec[] Animals =
    {
        new AnimalSpec
        {
            prefabName = "Chicken", animalName = "Gà", shopPrice = 800, maxOwned = 6,
            productAsset = "Egg", productSprite = "egg.png", productName = "Trứng gà", productPrice = 110,
            dropEveryDays = 1, minSatisfaction = 45f
        },
        new AnimalSpec
        {
            prefabName = "Sheep", animalName = "Cừu", shopPrice = 2500, maxOwned = 5,
            productAsset = "Wool", productSprite = "wool.png", productName = "Len cừu", productPrice = 380,
            dropEveryDays = 2, minSatisfaction = 50f
        },
        new AnimalSpec
        {
            prefabName = "Pig", animalName = "Heo", shopPrice = 3000, maxOwned = 5,
            productAsset = "Truffle", productSprite = "truffle.png", productName = "Nấm truffle", productPrice = 450,
            dropEveryDays = 2, minSatisfaction = 55f
        },
        new AnimalSpec
        {
            prefabName = "Cow", animalName = "Bò", shopPrice = 4500, maxOwned = 4,
            productAsset = "Milk", productSprite = "milk.png", productName = "Sữa bò", productPrice = 520,
            dropEveryDays = 2, minSatisfaction = 60f
        }
    };

    private const string FeedAsset = "AnimalFeed";
    private const string FeedSprite = "animal_feed.png";
    private const string FeedName = "Thức ăn chăn nuôi";
    private const int FeedPrice = 60;
    private const int FeedSatisfaction = 30;

    // =========================================================================
    //  SETUP
    // =========================================================================

    [MenuItem("Tools/Farm Game/Setup Animal Shop + Dragon Upgrades")]
    public static void RunSetup()
    {
        AssetDatabase.Refresh();
        ConfigureSprites();

        ItemData feed = CreateFeedItem();
        List<ItemData> shopAnimals = new List<ItemData>();
        List<ItemData> products = new List<ItemData>();
        List<string> notes = new List<string>();

        foreach (AnimalSpec spec in Animals)
        {
            ItemData product = CreateProductItem(spec);
            products.Add(product);

            ItemData animalItem = SetupAnimal(spec, product, feed, notes);
            if (animalItem != null)
            {
                shopAnimals.Add(animalItem);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        List<ItemData> animalCatalogEntries = new List<ItemData>(shopAnimals) { feed };
        List<ItemData> trainerItems = new List<ItemData>(products) { feed };

        string sceneReport = WireScene(animalCatalogEntries, products, trainerItems);

        string message =
            $"Animals set up: {shopAnimals.Count}/4\n"
            + $"Products: {products.Count}   Feed: 1\n\n"
            + sceneReport
            + (notes.Count > 0 ? "\n\nNotes:\n- " + string.Join("\n- ", notes) : string.Empty)
            + "\n\nSave the scene (Ctrl+S) to keep the scene changes.";

        Debug.Log("[Setup] " + message.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Animal shop + dragon upgrades", message, "OK");
    }

    private static void ConfigureSprites()
    {
        foreach (AnimalSpec spec in Animals)
        {
            ConfigureSprite($"{ItemSpriteFolder}/{spec.productSprite}");
        }

        ConfigureSprite($"{ItemSpriteFolder}/{FeedSprite}");
    }

    private static void ConfigureSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[Setup] Không tìm thấy ảnh: {path} (đã giải nén zip vào Assets chưa?)");
            return;
        }

        bool changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (!Mathf.Approximately(importer.spritePixelsPerUnit, 32f))
        {
            importer.spritePixelsPerUnit = 32f;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        return asset;
    }

    private static ItemData CreateFeedItem()
    {
        ItemData feed = LoadOrCreate<ItemData>($"{ItemDataFolder}/{FeedAsset}Data.asset");
        feed.itemName = FeedName;
        feed.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{ItemSpriteFolder}/{FeedSprite}");
        feed.itemType = ItemType.AnimalFeed;
        feed.maxStack = 99;
        feed.purchasePrice = FeedPrice;
        feed.sellPrice = 0;
        feed.feedSatisfaction = FeedSatisfaction;
        EditorUtility.SetDirty(feed);
        return feed;
    }

    private static ItemData CreateProductItem(AnimalSpec spec)
    {
        ItemData product = LoadOrCreate<ItemData>($"{ItemDataFolder}/{spec.productAsset}Data.asset");
        product.itemName = spec.productName;
        product.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{ItemSpriteFolder}/{spec.productSprite}");
        product.itemType = ItemType.Produce;   // sells through the existing Sell tab
        product.maxStack = 99;
        product.purchasePrice = 0;
        product.sellPrice = spec.productPrice; // each animal item has its own price
        product.sellPriceMode = SellPriceMode.Fixed;
        EditorUtility.SetDirty(product);
        return product;
    }

    // Adds AnimalProducer to the prefab (if missing) and creates its shop item.
    private static ItemData SetupAnimal(
        AnimalSpec spec,
        ItemData product,
        ItemData feed,
        List<string> notes
    )
    {
        string prefabPath = $"{AnimalPrefabFolder}/{spec.prefabName}.prefab";
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefabAsset == null)
        {
            notes.Add($"Prefab not found: {prefabPath} - {spec.animalName} skipped.");
            return null;
        }

        Sprite iconSprite = null;
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            if (root.GetComponent<RandomWanderAI>() == null)
            {
                notes.Add($"{spec.prefabName} has no RandomWanderAI (it will stand still).");
            }

            AnimalProducer producer = root.GetComponent<AnimalProducer>();
            bool isNew = producer == null;
            if (isNew)
            {
                producer = root.AddComponent<AnimalProducer>();
            }

            SerializedObject so = new SerializedObject(producer);

            if (isNew)
            {
                SetString(so, "displayName", spec.animalName);
                SetInt(so, "dropAmount", 1);
                SetInt(so, "dropEveryDays", spec.dropEveryDays);
                SetFloat(so, "minSatisfactionToDrop", spec.minSatisfaction);
            }

            // References are always filled in if they are empty.
            SetObjectIfEmpty(so, "dropItem", product);
            SetObjectIfEmpty(so, "feedItem", feed);
            so.ApplyModifiedPropertiesWithoutUndo();

            SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                iconSprite = renderer.sprite;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        ItemData animal = LoadOrCreate<ItemData>($"{ItemDataFolder}/{spec.prefabName}AnimalData.asset");
        animal.itemName = spec.animalName;
        animal.itemType = ItemType.Animal;
        animal.maxStack = 1;
        animal.purchasePrice = spec.shopPrice;
        animal.sellPrice = 0;
        animal.animalPrefab = prefabAsset;
        animal.maxOwned = spec.maxOwned;

        if (animal.icon == null)
        {
            animal.icon = iconSprite; // the animal's own sprite is the shop icon
        }

        EditorUtility.SetDirty(animal);
        return animal;
    }

    private static string WireScene(
        List<ItemData> animalCatalogEntries,
        List<ItemData> products,
        List<ItemData> trainerItems
    )
    {
        List<string> report = new List<string>();

        CoinManager coinManager = Object.FindFirstObjectByType<CoinManager>();
        if (coinManager != null)
        {
            int animals = AppendUnique(coinManager, "animalCatalog", animalCatalogEntries);
            int produce = AppendUnique(coinManager, "produceCatalog", products);
            report.Add(
                animals >= 0
                    ? $"Shop: +{animals} animal/feed rows, +{produce} sellable products."
                    : "Shop: replace CoinManager.cs first (no animalCatalog field yet)."
            );
        }
        else
        {
            report.Add("Shop: CoinManager not found in the open scene - open demo1 and run this again.");
        }

        DemoTrainerMenu trainer = Object.FindFirstObjectByType<DemoTrainerMenu>();
        if (trainer != null)
        {
            int added = AppendUnique(trainer, "animalItems", trainerItems);
            report.Add(
                added >= 0
                    ? "Trainer (F1): feed + animal products added."
                    : "Trainer (F1): replace DemoTrainerMenu.cs to get the animal buttons."
            );
        }

        if (Object.FindFirstObjectByType<DragonStats>() == null)
        {
            GameObject holder = new GameObject("DragonStats");
            Undo.RegisterCreatedObjectUndo(holder, "Create DragonStats");
            holder.AddComponent<DragonStats>();
            report.Add("DragonStats added to the scene (12 upgrades, editable in the Inspector).");
        }
        else
        {
            report.Add("DragonStats already in the scene.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return string.Join("\n", report);
    }

    // =========================================================================
    //  VALIDATE ("re-test so animals do not get errors")
    // =========================================================================

    [MenuItem("Tools/Farm Game/Validate Animals + Dragons")]
    public static void RunValidation()
    {
        List<string> problems = new List<string>();
        List<string> infos = new List<string>();
        int checks = 0;

        CoinManager coinManager = Object.FindFirstObjectByType<CoinManager>();
        SerializedProperty animalCatalog = null;
        SerializedProperty produceCatalog = null;

        if (coinManager == null)
        {
            problems.Add("CoinManager is not in the open scene (open demo1) - shop checks skipped.");
        }
        else
        {
            SerializedObject so = new SerializedObject(coinManager);
            animalCatalog = so.FindProperty("animalCatalog");
            produceCatalog = so.FindProperty("produceCatalog");
        }

        // ---- feed -----------------------------------------------------------
        ItemData feed = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemDataFolder}/{FeedAsset}Data.asset");
        checks++;
        if (feed == null)
        {
            problems.Add("Animal feed item is missing - run the setup.");
        }
        else
        {
            Require(feed.itemType == ItemType.AnimalFeed, "Feed: Item Type must be AnimalFeed.", problems);
            Require(feed.purchasePrice > 0, "Feed: needs a Purchase Price > 0 to appear in the shop.", problems);
            Require(feed.feedSatisfaction > 0, "Feed: Feed Satisfaction must be > 0.", problems);
            Require(feed.icon != null, "Feed: has no icon.", problems);
            RequireInCatalog(animalCatalog, feed, "Feed", "animalCatalog", problems);
        }

        // ---- each animal ----------------------------------------------------
        foreach (AnimalSpec spec in Animals)
        {
            checks++;
            string prefabPath = $"{AnimalPrefabFolder}/{spec.prefabName}.prefab";
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabAsset == null)
            {
                problems.Add($"{spec.prefabName}: prefab not found at {prefabPath}.");
                continue;
            }

            ValidateAnimalPrefab(spec, prefabPath, problems);

            ItemData animalItem = AssetDatabase.LoadAssetAtPath<ItemData>(
                $"{ItemDataFolder}/{spec.prefabName}AnimalData.asset"
            );

            if (animalItem == null)
            {
                problems.Add($"{spec.prefabName}: shop item missing - run the setup.");
            }
            else
            {
                Require(animalItem.itemType == ItemType.Animal, $"{spec.prefabName}: shop item type must be Animal.", problems);
                Require(animalItem.purchasePrice > 0, $"{spec.prefabName}: shop item needs a Purchase Price > 0.", problems);
                Require(animalItem.animalPrefab == prefabAsset, $"{spec.prefabName}: shop item points at the wrong prefab.", problems);
                Require(animalItem.maxOwned >= 1, $"{spec.prefabName}: Max Owned must be at least 1.", problems);
                Require(animalItem.icon != null, $"{spec.prefabName}: shop item has no icon.", problems);
                RequireInCatalog(animalCatalog, animalItem, spec.prefabName, "animalCatalog", problems);
            }
        }

        // ---- products: price + sellable ---------------------------------------
        foreach (AnimalSpec spec in Animals)
        {
            checks++;
            ItemData product = AssetDatabase.LoadAssetAtPath<ItemData>(
                $"{ItemDataFolder}/{spec.productAsset}Data.asset"
            );

            if (product == null)
            {
                problems.Add($"{spec.productName}: product item missing - run the setup.");
                continue;
            }

            Require(product.itemType == ItemType.Produce, $"{product.itemName}: Item Type must be Produce to be sellable.", problems);
            Require(product.sellPrice > 0, $"{product.itemName}: has no sell price.", problems);
            Require(product.icon != null, $"{product.itemName}: has no icon.", problems);
            RequireInCatalog(produceCatalog, product, product.itemName, "produceCatalog", problems);
        }

        // ---- dragons ----------------------------------------------------------
        checks++;
        ValidateDragons(problems, infos);

        // ---- report -------------------------------------------------------------
        foreach (string info in infos)
        {
            Debug.Log("[Validate] " + info);
        }

        foreach (string problem in problems)
        {
            Debug.LogWarning("[Validate] " + problem);
        }

        string summary = problems.Count == 0
            ? $"All {checks} check groups passed. No problems found."
            : $"{problems.Count} problem(s) found (see the Console for all of them):\n\n- "
              + string.Join("\n- ", problems.GetRange(0, Mathf.Min(10, problems.Count)))
              + (problems.Count > 10 ? "\n- ..." : string.Empty);

        if (infos.Count > 0)
        {
            summary += "\n\nInfo:\n- " + string.Join("\n- ", infos);
        }

        EditorUtility.DisplayDialog("Validate animals + dragons", summary, "OK");
    }

    private static void ValidateAnimalPrefab(AnimalSpec spec, string prefabPath, List<string> problems)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            RandomWanderAI wander = root.GetComponent<RandomWanderAI>();
            if (wander == null)
            {
                problems.Add($"{spec.prefabName}: missing RandomWanderAI.");
            }
            else
            {
                ValidateAnimatorParameters(spec.prefabName, wander, problems);
            }

            AnimalProducer producer = root.GetComponent<AnimalProducer>();
            if (producer == null)
            {
                problems.Add($"{spec.prefabName}: missing AnimalProducer - it will never drop items. Run the setup.");
                return;
            }

            SerializedObject so = new SerializedObject(producer);

            Require(so.FindProperty("dropItem").objectReferenceValue != null,
                $"{spec.prefabName}: AnimalProducer has no Drop Item.", problems);
            Require(so.FindProperty("feedItem").objectReferenceValue != null,
                $"{spec.prefabName}: AnimalProducer has no Feed Item (it can only be petted).", problems);
            Require(so.FindProperty("dropEveryDays").intValue >= 1,
                $"{spec.prefabName}: Drop Every Days must be at least 1.", problems);

            float min = so.FindProperty("minSatisfactionToDrop").floatValue;
            if (min > 90f)
            {
                problems.Add($"{spec.prefabName}: Min Satisfaction {min:0} is very hard to keep up (max is 100).");
            }

            float decay = so.FindProperty("dailyDecay").floatValue;
            float start = so.FindProperty("startingSatisfaction").floatValue;
            if (start - decay < 0f)
            {
                problems.Add($"{spec.prefabName}: Daily Decay ({decay:0}) is larger than Starting Satisfaction ({start:0}).");
            }

            Require(root.GetComponentInChildren<SpriteRenderer>() != null,
                $"{spec.prefabName}: has no SpriteRenderer (the satisfaction bar cannot be placed).", problems);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // RandomWanderAI sets Animator parameters by name. A parameter that does
    // not exist in the controller logs a warning on every single step.
    private static void ValidateAnimatorParameters(string label, RandomWanderAI wander, List<string> problems)
    {
        SerializedObject so = new SerializedObject(wander);
        Animator animator = so.FindProperty("animator")?.objectReferenceValue as Animator;
        if (animator == null)
        {
            return; // no animator assigned = it simply slides, nothing to check
        }

        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            problems.Add($"{label}: its Animator has no Animator Controller assigned.");
            return;
        }

        foreach (string field in new[] { "isWalkingParam", "moveXParam", "moveYParam" })
        {
            string parameterName = so.FindProperty(field)?.stringValue;
            if (string.IsNullOrEmpty(parameterName))
            {
                continue;
            }

            bool found = false;
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == parameterName)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                problems.Add($"{label}: Animator Controller '{controller.name}' has no parameter '{parameterName}' (warning spam each step).");
            }
        }
    }

    private static void ValidateDragons(List<string> problems, List<string> infos)
    {
        // Default upgrade table: 4 elements x 3 stats, each exactly once.
        GameObject temporary = new GameObject("DragonStats_ValidateTemp");
        try
        {
            DragonStats stats = Object.FindFirstObjectByType<DragonStats>();
            if (stats == null)
            {
                stats = temporary.AddComponent<DragonStats>();
                infos.Add("No DragonStats in the scene yet (the shop creates one on demand; run the setup to add it permanently).");
            }

            HashSet<string> seen = new HashSet<string>();
            foreach (DragonUpgradeTrack track in stats.Tracks)
            {
                if (track == null)
                {
                    continue;
                }

                if (!seen.Add(track.element + "/" + track.stat))
                {
                    problems.Add($"DragonStats: duplicate upgrade {track.element}/{track.stat}.");
                }

                Require(track.maxLevel >= 1, $"DragonStats: {track.element}/{track.stat} Max Level must be >= 1.", problems);
                Require(track.baseCost >= 1, $"DragonStats: {track.element}/{track.stat} needs a Base Cost.", problems);
            }

            Require(seen.Count == 12, $"DragonStats: expected 12 upgrades (4 dragons x 3 stats), found {seen.Count}.", problems);
        }
        finally
        {
            Object.DestroyImmediate(temporary);
        }

        // Dragons in the open scene.
        ElementalDragon[] dragons = Object.FindObjectsByType<ElementalDragon>(FindObjectsSortMode.None);
        int targetableLayer = LayerMask.NameToLayer(TargetLayerName);

        if (dragons.Length == 0)
        {
            infos.Add("No dragons in the open scene (upgrades still work once you place them).");
        }

        HashSet<DragonElement> present = new HashSet<DragonElement>();

        foreach (ElementalDragon dragon in dragons)
        {
            present.Add(dragon.Element);

            SerializedProperty bits = new SerializedObject(dragon).FindProperty("targetLayer.m_Bits");
            if (bits != null && targetableLayer >= 0 && (bits.intValue & (1 << targetableLayer)) == 0)
            {
                problems.Add($"{dragon.name}: Target Layer does not include '{TargetLayerName}' - it cannot see monsters.");
            }
        }

        foreach (DragonElement element in System.Enum.GetValues(typeof(DragonElement)))
        {
            if (dragons.Length > 0 && !present.Contains(element))
            {
                infos.Add($"{DragonStats.GetElementName(element)}: none placed in the scene.");
            }
        }
    }

    // =========================================================================
    //  helpers
    // =========================================================================

    private static void Require(bool condition, string message, List<string> problems)
    {
        if (!condition)
        {
            problems.Add(message);
        }
    }

    private static void RequireInCatalog(
        SerializedProperty catalog,
        Object item,
        string label,
        string catalogName,
        List<string> problems
    )
    {
        if (catalog == null)
        {
            return; // CoinManager missing or older version: already reported
        }

        for (int i = 0; i < catalog.arraySize; i++)
        {
            if (catalog.GetArrayElementAtIndex(i).objectReferenceValue == item)
            {
                return;
            }
        }

        problems.Add($"{label}: not in CoinManager.{catalogName}, so it will not show up in the shop.");
    }

    private static void SetString(SerializedObject so, string name, string value)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetInt(SerializedObject so, string name, int value)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetFloat(SerializedObject so, string name, float value)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetObjectIfEmpty(SerializedObject so, string name, Object value)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property != null && property.objectReferenceValue == null)
        {
            property.objectReferenceValue = value;
        }
    }

    // Returns how many were added, or -1 if the target has no such array.
    private static int AppendUnique(Object target, string arrayName, IEnumerable<Object> items)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty array = so.FindProperty(arrayName);

        if (array == null || !array.isArray)
        {
            Debug.LogWarning($"[Setup] {target.name} không có trường '{arrayName}'.");
            return -1;
        }

        int added = 0;

        foreach (Object item in items)
        {
            if (item == null || ContainsReference(array, item))
            {
                continue;
            }

            array.InsertArrayElementAtIndex(array.arraySize);
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = item;
            added++;
        }

        so.ApplyModifiedProperties();
        return added;
    }

    private static bool ContainsReference(SerializedProperty array, Object item)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            if (array.GetArrayElementAtIndex(i).objectReferenceValue == item)
            {
                return true;
            }
        }

        return false;
    }
}
#endif
