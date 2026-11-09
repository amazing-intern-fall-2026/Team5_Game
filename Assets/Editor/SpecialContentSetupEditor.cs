#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-click setup:  Tools > Farm Game > Setup Special Crops + Monsters
//
// It is safe to run more than once - it reuses anything that already exists and
// never overwrites values you have tuned.  It:
//   1. makes sure the "Targetable" layer exists (dragons use it to find targets)
//   2. imports the placeholder sprites correctly
//   3. creates CropData + seed + produce assets for the 4 special crops
//   4. adds the seeds/produce to CoinManager's catalogs  => they appear in the shop
//   5. creates the 4 monster prefabs
//   6. adds a MonsterSpawner (unlock days 1 / 3 / 5 / 7) to the open scene
//   7. adds seeds + monsters to the F1 trainer menu if it is in the scene
public static class SpecialContentSetupEditor
{
    private const string TargetLayerName = "Targetable";

    private const string CropSpriteFolder = "Assets/crop/special";
    private const string SeedSpriteFolder = "Assets/hatgiongphanbon/special";
    private const string MonsterSpriteFolder = "Assets/Monsters";
    private const string CropDataFolder = "Assets/data/crop";
    private const string ItemDataFolder = "Assets/data/item";
    private const string MonsterPrefabFolder = "Assets/Prefabs/Monsters";

    private const string TemplateCropPath = "Assets/data/crop/TomatoCropData.asset";
    private const string CommonSproutPath = "Assets/crop/crop-stage-1-common-sprout.png";

    private sealed class CropSpec
    {
        public string id;            // matches the sprite file names
        public string assetName;     // used in asset file names
        public string cropName;
        public string produceName;
        public string seedName;
        public CropType type;
        public int seedPrice;
        public int sellPrice;
        public int harvestAmount;
        public float stageMinutes;
        public float maxHealth;
        public SpecialCropType special;
        public bool requiresMature;
        public float power;
        public float radius;
        public float interval;
    }

    private static readonly CropSpec[] CropSpecs =
    {
        new CropSpec
        {
            id = "thornvine", assetName = "Thornvine",
            cropName = "Dây gai tím", produceName = "Quả gai tím",
            seedName = "Hạt giống dây gai tím", type = CropType.Thornvine,
            seedPrice = 450, sellPrice = 320, harvestAmount = 3, stageMinutes = 6f,
            maxHealth = 45f, special = SpecialCropType.Thorns, requiresMature = true,
            power = 8f, radius = 0f, interval = 1.5f
        },
        new CropSpec
        {
            id = "lavender", assetName = "Lavender",
            cropName = "Oải hương", produceName = "Hoa oải hương",
            seedName = "Hạt giống oải hương", type = CropType.Lavender,
            seedPrice = 500, sellPrice = 350, harvestAmount = 3, stageMinutes = 6f,
            maxHealth = 30f, special = SpecialCropType.RepelAura, requiresMature = true,
            power = 0.5f, radius = 2.5f, interval = 1f
        },
        new CropSpec
        {
            id = "aloe", assetName = "HealingAloe",
            cropName = "Nha đam", produceName = "Lá nha đam",
            seedName = "Hạt giống nha đam", type = CropType.HealingAloe,
            seedPrice = 550, sellPrice = 380, harvestAmount = 3, stageMinutes = 7f,
            maxHealth = 35f, special = SpecialCropType.HealAura, requiresMature = true,
            power = 6f, radius = 2.5f, interval = 2f
        },
        new CropSpec
        {
            id = "pumpkin", assetName = "DecoyPumpkin",
            cropName = "Bí ngô mồi", produceName = "Bí ngô",
            seedName = "Hạt giống bí ngô mồi", type = CropType.DecoyPumpkin,
            seedPrice = 400, sellPrice = 300, harvestAmount = 2, stageMinutes = 5f,
            maxHealth = 120f, special = SpecialCropType.Decoy, requiresMature = false,
            power = 0f, radius = 0f, interval = 1.5f
        }
    };

    private sealed class MonsterSpec
    {
        public string prefabName;
        public string spriteFile;
        public System.Type scriptType;
        public float health, speed, damage, interval, reach;
        public int unlockDay, baseCount, maxCount;
        public float extraPerDay;
    }

    private static readonly MonsterSpec[] MonsterSpecs =
    {
        new MonsterSpec
        {
            prefabName = "Locust", spriteFile = "locust.png", scriptType = typeof(LocustMonster),
            health = 15f, speed = 2.6f, damage = 2f, interval = 0.8f, reach = 0.5f,
            unlockDay = 1, baseCount = 3, extraPerDay = 0.5f, maxCount = 14
        },
        new MonsterSpec
        {
            prefabName = "BlightSlime", spriteFile = "blight_slime.png", scriptType = typeof(BlightSlimeMonster),
            health = 45f, speed = 1f, damage = 3f, interval = 1.5f, reach = 0.6f,
            unlockDay = 3, baseCount = 1, extraPerDay = 0.5f, maxCount = 6
        },
        new MonsterSpec
        {
            prefabName = "Boar", spriteFile = "boar.png", scriptType = typeof(BoarMonster),
            health = 110f, speed = 1.6f, damage = 12f, interval = 2f, reach = 0.7f,
            unlockDay = 5, baseCount = 1, extraPerDay = 0.34f, maxCount = 4
        },
        new MonsterSpec
        {
            prefabName = "NightWraith", spriteFile = "night_wraith.png", scriptType = typeof(NightWraithMonster),
            health = 60f, speed = 2f, damage = 6f, interval = 1.6f, reach = 0.6f,
            unlockDay = 7, baseCount = 1, extraPerDay = 0.34f, maxCount = 4
        }
    };

    [MenuItem("Tools/Farm Game/Setup Special Crops + Monsters")]
    public static void RunSetup()
    {
        AssetDatabase.Refresh();

        int layerIndex = EnsureLayer(TargetLayerName);

        EnsureFolder(CropDataFolder);
        EnsureFolder(ItemDataFolder);
        EnsureFolder(MonsterPrefabFolder);

        ConfigureAllSprites();

        List<ItemData> seedItems = new List<ItemData>();
        List<ItemData> produceItems = new List<ItemData>();
        int cropsMade = CreateCrops(seedItems, produceItems);

        List<MonsterBase> monsterPrefabs = CreateMonsterPrefabs(layerIndex);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string sceneReport = WireScene(seedItems, produceItems, monsterPrefabs);

        EditorUtility.DisplayDialog(
            "Special crops + monsters",
            $"Crops set up: {cropsMade}/4\n"
            + $"Monster prefabs: {monsterPrefabs.Count}/4\n\n"
            + sceneReport
            + "\n\nSave the scene (Ctrl+S) to keep the scene changes.",
            "OK"
        );
    }

    // ------------------------------------------------------------------ layer

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
        {
            return existing;
        }

        Object[] tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets == null || tagAssets.Length == 0)
        {
            Debug.LogError("[Setup] Không mở được TagManager để tạo layer.");
            return -1;
        }

        SerializedObject tagManager = new SerializedObject(tagAssets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[Setup] Đã tạo layer '{layerName}' (slot {i}).");
                return i;
            }
        }

        Debug.LogError("[Setup] Hết slot layer để tạo '" + layerName + "'.");
        return -1;
    }

    // ---------------------------------------------------------------- folders

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    // ----------------------------------------------------------------- sprites

    private static void ConfigureAllSprites()
    {
        foreach (CropSpec spec in CropSpecs)
        {
            ConfigureSprite($"{CropSpriteFolder}/{spec.id}-stage-2.png", 100f, FilterMode.Point);
            ConfigureSprite($"{CropSpriteFolder}/{spec.id}-stage-3.png", 100f, FilterMode.Point);
            ConfigureSprite($"{CropSpriteFolder}/item-{spec.id}.png", 100f, FilterMode.Point);
            ConfigureSprite($"{SeedSpriteFolder}/seed-{spec.id}.png", 100f, FilterMode.Bilinear);
        }

        foreach (MonsterSpec spec in MonsterSpecs)
        {
            // 32x32 art at 32 pixels-per-unit = exactly one tile wide.
            ConfigureSprite($"{MonsterSpriteFolder}/{spec.spriteFile}", 32f, FilterMode.Point);
        }
    }

    private static void ConfigureSprite(string path, float pixelsPerUnit, FilterMode filter)
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

        if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
        {
            importer.spritePixelsPerUnit = pixelsPerUnit;
            changed = true;
        }

        if (importer.filterMode != filter)
        {
            importer.filterMode = filter;
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

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite == null)
        {
            Debug.LogWarning($"[Setup] Không load được sprite: {path}");
        }

        return sprite;
    }

    // ------------------------------------------------------------------ crops

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

    private static int CreateCrops(List<ItemData> seedItems, List<ItemData> produceItems)
    {
        CropData template = AssetDatabase.LoadAssetAtPath<CropData>(TemplateCropPath);
        if (template == null)
        {
            Debug.LogWarning(
                "[Setup] Không tìm thấy TomatoCropData để copy cài đặt test-mode/icon sao. "
                + "Cây mới sẽ dùng giá trị mặc định."
            );
        }

        Sprite sprout = LoadSprite(CommonSproutPath);
        int made = 0;

        foreach (CropSpec spec in CropSpecs)
        {
            ItemData produce = LoadOrCreate<ItemData>($"{ItemDataFolder}/{spec.assetName}Data.asset");
            produce.itemName = spec.produceName;
            produce.icon = LoadSprite($"{CropSpriteFolder}/item-{spec.id}.png");
            produce.itemType = ItemType.Produce;
            produce.maxStack = 99;
            produce.purchasePrice = 0;
            produce.sellPrice = spec.sellPrice;
            produce.sellPriceMode = SellPriceMode.Fixed;
            EditorUtility.SetDirty(produce);

            CropData crop = LoadOrCreate<CropData>($"{CropDataFolder}/{spec.assetName}CropData.asset");
            crop.cropName = spec.cropName;
            crop.cropType = spec.type;
            crop.allowedSeasons = new[]
            {
                SeasonType.Spring, SeasonType.Summer, SeasonType.Autumn, SeasonType.Winter
            };
            crop.stage1Sprite = sprout;
            crop.stage2Sprite = LoadSprite($"{CropSpriteFolder}/{spec.id}-stage-2.png");
            crop.stage3Sprite = LoadSprite($"{CropSpriteFolder}/{spec.id}-stage-3.png");
            crop.harvestItem = produce;
            crop.harvestAmount = spec.harvestAmount;
            crop.stage1DurationMinutes = spec.stageMinutes;
            crop.stage2DurationMinutes = spec.stageMinutes;

            if (template != null)
            {
                // Match however the rest of your crops are currently tuned.
                crop.testMode = template.testMode;
                crop.testStage1DurationSeconds = template.testStage1DurationSeconds;
                crop.testStage2DurationSeconds = template.testStage2DurationSeconds;
                crop.normalQualityWeight = template.normalQualityWeight;
                crop.purpleStarWeight = template.purpleStarWeight;
                crop.goldStarWeight = template.goldStarWeight;
                crop.purpleStarIcon = template.purpleStarIcon;
                crop.goldStarIcon = template.goldStarIcon;
                crop.purpleStarLocalPosition = template.purpleStarLocalPosition;
                crop.goldStarLocalPosition = template.goldStarLocalPosition;
                crop.qualityIconLocalScale = template.qualityIconLocalScale;
            }

            crop.maxHealth = spec.maxHealth;
            crop.specialType = spec.special;
            crop.specialRequiresMature = spec.requiresMature;
            crop.specialPower = spec.power;
            crop.specialRadius = spec.radius;
            crop.specialInterval = spec.interval;
            EditorUtility.SetDirty(crop);

            ItemData seed = LoadOrCreate<ItemData>($"{ItemDataFolder}/{spec.assetName}SeedData.asset");
            seed.itemName = spec.seedName;
            seed.icon = LoadSprite($"{SeedSpriteFolder}/seed-{spec.id}.png");
            seed.itemType = ItemType.Seed;
            seed.maxStack = 99;
            seed.purchasePrice = spec.seedPrice;
            seed.sellPrice = 0;
            seed.cropToPlant = crop;
            EditorUtility.SetDirty(seed);

            seedItems.Add(seed);
            produceItems.Add(produce);
            made++;
        }

        AssetDatabase.SaveAssets();
        return made;
    }

    // --------------------------------------------------------------- monsters

    private static List<MonsterBase> CreateMonsterPrefabs(int layerIndex)
    {
        List<MonsterBase> result = new List<MonsterBase>();

        foreach (MonsterSpec spec in MonsterSpecs)
        {
            string prefabPath = $"{MonsterPrefabFolder}/{spec.prefabName}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existing != null)
            {
                MonsterBase existingMonster = existing.GetComponent<MonsterBase>();
                if (existingMonster != null)
                {
                    result.Add(existingMonster); // keep whatever you already tuned
                }

                continue;
            }

            GameObject go = new GameObject(spec.prefabName);

            if (layerIndex >= 0)
            {
                go.layer = layerIndex;
            }

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite($"{MonsterSpriteFolder}/{spec.spriteFile}");
            renderer.sortingOrder = 4;

            CircleCollider2D hitbox = go.AddComponent<CircleCollider2D>();
            hitbox.isTrigger = true;
            hitbox.radius = 0.4f;

            MonsterBase monster = (MonsterBase)go.AddComponent(spec.scriptType);

            SerializedObject so = new SerializedObject(monster);
            SetFloat(so, "maxHealth", spec.health);
            SetFloat(so, "moveSpeed", spec.speed);
            SetFloat(so, "attackDamage", spec.damage);
            SetFloat(so, "attackInterval", spec.interval);
            SetFloat(so, "attackReach", spec.reach);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            MonsterBase prefabMonster = prefab.GetComponent<MonsterBase>();
            if (prefabMonster != null)
            {
                result.Add(prefabMonster);
            }
        }

        return result;
    }

    private static void SetFloat(SerializedObject so, string propertyName, float value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    // ------------------------------------------------------------------ scene

    private static string WireScene(
        List<ItemData> seedItems,
        List<ItemData> produceItems,
        List<MonsterBase> monsterPrefabs
    )
    {
        List<string> report = new List<string>();

        CoinManager coinManager = Object.FindFirstObjectByType<CoinManager>();
        if (coinManager != null)
        {
            int seeds = AppendUnique(coinManager, "seedCatalog", seedItems);
            int produce = AppendUnique(coinManager, "produceCatalog", produceItems);
            report.Add($"Shop: +{seeds} seeds, +{produce} sellable produce.");
        }
        else
        {
            report.Add(
                "Shop: CoinManager not found in the open scene - open demo1 and run this again."
            );
        }

        DemoTrainerMenu trainer = Object.FindFirstObjectByType<DemoTrainerMenu>();
        if (trainer != null)
        {
            AppendUnique(trainer, "seeds", seedItems);

            SerializedProperty produceArray = new SerializedObject(trainer).FindProperty("produce");
            if (produceArray != null && produceArray.arraySize > 0)
            {
                AppendUnique(trainer, "produce", produceItems);
            }

            int monsters = AppendUnique(trainer, "monsterPrefabs", monsterPrefabs);
            report.Add(
                monsters >= 0
                    ? "Trainer (F1): seeds + monster buttons added."
                    : "Trainer (F1): seeds added (replace DemoTrainerMenu.cs for monster buttons)."
            );
        }

        MonsterSpawner spawner = Object.FindFirstObjectByType<MonsterSpawner>();
        if (spawner == null)
        {
            GameObject spawnerObject = new GameObject("MonsterSpawner");
            Undo.RegisterCreatedObjectUndo(spawnerObject, "Create MonsterSpawner");
            spawner = spawnerObject.AddComponent<MonsterSpawner>();
        }

        ConfigureSpawner(spawner, monsterPrefabs);
        report.Add("MonsterSpawner ready (monsters unlock on days 1 / 3 / 5 / 7).");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return string.Join("\n", report);
    }

    // Returns how many were added, or -1 if the property does not exist.
    private static int AppendUnique(
        Object target,
        string arrayName,
        IEnumerable<Object> items
    )
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

    private static void ConfigureSpawner(MonsterSpawner spawner, List<MonsterBase> monsterPrefabs)
    {
        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty entries = so.FindProperty("entries");

        // Only fill an empty list, so re-running never overwrites your tuning.
        if (entries != null && entries.arraySize == 0)
        {
            foreach (MonsterSpec spec in MonsterSpecs)
            {
                MonsterBase prefab = monsterPrefabs.Find(m => m != null && m.gameObject.name == spec.prefabName);
                if (prefab == null)
                {
                    continue;
                }

                entries.InsertArrayElementAtIndex(entries.arraySize);
                SerializedProperty entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                entry.FindPropertyRelative("unlockDay").intValue = spec.unlockDay;
                entry.FindPropertyRelative("baseCount").intValue = spec.baseCount;
                entry.FindPropertyRelative("extraPerDay").floatValue = spec.extraPerDay;
                entry.FindPropertyRelative("maxCount").intValue = spec.maxCount;
            }
        }

        so.ApplyModifiedProperties();
    }
}
#endif
