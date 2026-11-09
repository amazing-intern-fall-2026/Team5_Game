using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DemoTrainerMenu : MonoBehaviour
{
    [SerializeField] private ItemStorageController itemStorage;
    [SerializeField, Min(1)] private int stackAmount = 10;
    [SerializeField] private ItemData[] tools;
    [SerializeField] private ItemData[] seeds;
    [SerializeField] private ItemData[] fertilizers;
    [Tooltip("Nông sản dùng để test chất lượng. Nếu để trống sẽ lấy từ CoinManager.")]
    [SerializeField] private ItemData[] produce;

    [Header("Spawning")]
    [SerializeField] private GameObject[] animalPrefabs;
    [SerializeField] private GameObject[] dragonPrefabs;
    [SerializeField] private GameObject[] dummyPrefabs;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnOffset = 1.5f;

    // [Quái] Test the night monsters without waiting for night.
    [Header("Quái vật")]
    [SerializeField] private MonsterBase[] monsterPrefabs;
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private float monsterSpawnDistance = 4f;

    // [Thú] Feed + animal products for testing the animal shop / satisfaction.
    [Header("Thú vật (test)")]
    [SerializeField] private ItemData[] animalItems;

    private bool isOpen;
    private Vector2 scrollPosition;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (dragonPrefabs == null || dragonPrefabs.Length == 0)
        {
            dragonPrefabs = LoadPrefabsByNames(new[]
            {
                "EarthDragon", "FireDragon", "IceDragon", "LightningDragon"
            });
        }

        if (dummyPrefabs == null || dummyPrefabs.Length == 0)
        {
            dummyPrefabs = LoadPrefabsByNames(new[] { "TrainingDummy" });
        }
    }

    private static GameObject[] LoadPrefabsByNames(string[] prefabNames)
    {
        List<GameObject> loaded = new List<GameObject>();
        foreach (string name in prefabNames)
        {
            string[] guids = AssetDatabase.FindAssets($"{name} t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.name == name)
                {
                    loaded.Add(prefab);
                    break;
                }
            }
        }
        return loaded.ToArray();
    }
#endif

    private void Awake()
    {
        if (itemStorage == null)
        {
            itemStorage = FindFirstObjectByType<ItemStorageController>();
        }

        if (produce == null || produce.Length == 0)
        {
            CoinManager coinManager = FindFirstObjectByType<CoinManager>();
            if (coinManager != null)
            {
                produce = coinManager.ProduceCatalog;
            }
        }

        if (spawnPoint == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                spawnPoint = player.transform;
            }
        }

        if (monsterSpawner == null)
        {
            monsterSpawner = FindFirstObjectByType<MonsterSpawner>();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null
            && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            isOpen = !isOpen;
        }
    }

    private void OnGUI()
    {
        if (!isOpen)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(15f, 15f, 330f, 520f), "TRAINER - F1 để đóng", GUI.skin.window);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        DrawSection("Công cụ", tools, true);
        DrawSection("Hạt giống", seeds, false);
        DrawSection("Phân bón", fertilizers, false);
        DrawQualitySection("Nông sản Sao Tím", produce, CropQuality.PurpleStar);
        DrawQualitySection("Nông sản Sao Vàng", produce, CropQuality.GoldStar);

        DrawSpawnSection("Sinh vật (Thú nuôi)", animalPrefabs, Color.white);
        DrawSpawnSection("Rồng (Dragons)", dragonPrefabs, new Color(1f, 0.55f, 0.25f));
        DrawSpawnSection("Bù nhìn (Dummies)", dummyPrefabs, new Color(0.55f, 0.85f, 1f));

        GUILayout.Space(6f);
        GUILayout.Label("Quái vật (test)");

        if (monsterPrefabs != null)
        {
            foreach (MonsterBase monsterPrefab in monsterPrefabs)
            {
                if (monsterPrefab == null)
                {
                    continue;
                }

                if (GUILayout.Button("+ " + monsterPrefab.name, GUILayout.Height(30f)))
                {
                    SpawnMonster(monsterPrefab);
                }
            }
        }

        if (monsterSpawner != null
            && GUILayout.Button("Triệu hồi đợt quái ngay", GUILayout.Height(30f)))
        {
            monsterSpawner.SpawnWaveNow();
        }

        if (GUILayout.Button("Xóa toàn bộ quái", GUILayout.Height(30f)))
        {
            MonsterBase.DespawnAll();
        }

        DrawSection("Vật phẩm thú vật", animalItems, false);

        GUILayout.Space(6f);
        GUILayout.Label("Thú vật / Rồng (test)");

        if (GUILayout.Button("Hài lòng tối đa (mọi thú)", GUILayout.Height(30f)))
        {
            AnimalProducer.DebugSetAllSatisfaction(100f);
        }

        if (GUILayout.Button("Hài lòng = 0 (mọi thú)", GUILayout.Height(30f)))
        {
            AnimalProducer.DebugSetAllSatisfaction(0f);
        }

        if (GUILayout.Button("Sang ngày mới cho thú (thử sinh sản)", GUILayout.Height(30f)))
        {
            AnimalProducer.DebugAdvanceDayAll();
        }

        if (GUILayout.Button("Reset nâng cấp rồng", GUILayout.Height(30f)))
        {
            DragonStats.Current?.ResetLevels();
        }

        GUILayout.Space(12f);
        GUI.backgroundColor = new Color(1f, 0.45f, 0.35f);
        if (GUILayout.Button("XÓA HOTBAR + INVENTORY", GUILayout.Height(36f)))
        {
            itemStorage?.ClearAllItems();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawSpawnSection(string title, GameObject[] prefabs, Color buttonColor)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return;
        }

        GUILayout.Space(6f);
        GUILayout.Label(title);

        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = buttonColor;

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            if (GUILayout.Button("+ " + prefab.name, GUILayout.Height(30f)))
            {
                SpawnPrefab(prefab);
            }
        }

        GUI.backgroundColor = previousColor;
    }

    private void DrawQualitySection(string title, ItemData[] items, CropQuality quality)
    {
        GUILayout.Space(6f);
        GUILayout.Label(title);

        if (items == null)
        {
            GUILayout.Label("Không tìm thấy Produce Catalog trong CoinManager.");
            return;
        }

        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = quality == CropQuality.PurpleStar
            ? new Color(0.72f, 0.42f, 1f)
            : new Color(1f, 0.78f, 0.2f);

        foreach (ItemData item in items)
        {
            if (item == null || item.itemType != ItemType.Produce)
            {
                continue;
            }

            string label = "+ " + stackAmount + "  " + item.itemName
                + " [" + CropQualityUtility.GetDisplayName(quality) + "]";

            if (GUILayout.Button(label, GUILayout.Height(30f)))
            {
                GiveItem(item, stackAmount, quality);
            }
        }

        GUI.backgroundColor = previousColor;
    }

    private void DrawSection(string title, ItemData[] items, bool giveOneOnly)
    {
        GUILayout.Space(6f);
        GUILayout.Label(title);

        if (items == null)
        {
            return;
        }

        foreach (ItemData item in items)
        {
            if (item == null)
            {
                continue;
            }

            int amount = giveOneOnly ? 1 : stackAmount;
            string label = "+ " + amount + "  " + item.itemName;

            if (GUILayout.Button(label, GUILayout.Height(30f)))
            {
                GiveItem(item, amount);
            }
        }
    }

    private void GiveItem(ItemData item, int amount)
    {
        GiveItem(item, amount, CropQuality.Normal);
    }

    private void GiveItem(ItemData item, int amount, CropQuality quality)
    {
        if (itemStorage == null)
        {
            Debug.LogError("[Trainer] Không tìm thấy ItemStorageController.");
            return;
        }

        if (itemStorage.TryAddItem(item, amount, quality))
        {
            Debug.Log(
                "[Trainer] Đã cấp " + amount + " x " + item.itemName
                + " [" + CropQualityUtility.GetDisplayName(quality) + "]."
            );
        }
        else
        {
            Debug.LogWarning("[Trainer] Không đủ chỗ để cấp " + item.itemName + ".");
        }
    }

    private void SpawnPrefab(GameObject prefab)
    {
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector2 randomOffset = Random.insideUnitCircle * spawnOffset;
        Vector3 spawnPosition = origin + new Vector3(randomOffset.x, randomOffset.y, 0f);

        Instantiate(prefab, spawnPosition, Quaternion.identity);
        Debug.Log("[Trainer] Đã triệu hồi " + prefab.name + ".");
    }

    private void SpawnMonster(MonsterBase monsterPrefab)
    {
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector2 direction = Random.insideUnitCircle.normalized;
        Vector3 position = origin + (Vector3)(direction * monsterSpawnDistance);

        // Going through the spawner gives it today's difficulty scaling.
        if (monsterSpawner != null)
        {
            monsterSpawner.SpawnMonster(monsterPrefab, position);
        }
        else
        {
            Instantiate(monsterPrefab, position, Quaternion.identity);
        }

        Debug.Log("[Trainer] Đã triệu hồi quái " + monsterPrefab.name + ".");
    }
}