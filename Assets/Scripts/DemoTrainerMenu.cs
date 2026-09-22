using UnityEngine;
using UnityEngine.InputSystem;

public class DemoTrainerMenu : MonoBehaviour
{
    [SerializeField] private ItemStorageController itemStorage;
    [SerializeField, Min(1)] private int stackAmount = 10;
    [SerializeField] private ItemData[] tools;
    [SerializeField] private ItemData[] seeds;
    [SerializeField] private ItemData[] fertilizers;
    [Tooltip("Nông sản dùng để test chất lượng. Nếu để trống sẽ lấy từ CoinManager.")]
    [SerializeField] private ItemData[] produce;

    [SerializeField] private GameObject[] animalPrefabs;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnOffset = 1.5f;

    private bool isOpen;
    private Vector2 scrollPosition;

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

        GUILayout.Space(6f);
        GUILayout.Label("Sinh vật");

        if (animalPrefabs != null)
        {
            foreach (GameObject animalPrefab in animalPrefabs)
            {
                if (animalPrefab == null)
                {
                    continue;
                }

                if (GUILayout.Button("+ " + animalPrefab.name, GUILayout.Height(30f)))
                {
                    SpawnAnimal(animalPrefab);
                }
            }
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

    private void DrawQualitySection(
        string title,
        ItemData[] items,
        CropQuality quality
    )
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

    private void DrawSection(
        string title,
        ItemData[] items,
        bool giveOneOnly
    )
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

    private void GiveItem(
        ItemData item,
        int amount,
        CropQuality quality
    )
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

    private void SpawnAnimal(GameObject animalPrefab)
    {
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 randomOffset = Random.insideUnitCircle * spawnOffset;
        Vector3 spawnPosition = origin + new Vector3(randomOffset.x, randomOffset.y, 0f);

        Instantiate(animalPrefab, spawnPosition, Quaternion.identity);
        Debug.Log("[Trainer] Đã triệu hồi " + animalPrefab.name + ".");
    }
}
