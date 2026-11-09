using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    [Header("Wallet")]
    [SerializeField, Min(0)] private int startingCoins = 5000;
    [SerializeField] private ItemStorageController itemStorageController;

    [Header("Shop catalog")]
    [SerializeField] private ItemData[] seedCatalog;
    [SerializeField] private ItemData[] fertilizerCatalog;
    [SerializeField] private ItemData[] produceCatalog;
    // [Thú] Animals + animal feed sold in the shop's animal tab.
    [SerializeField] private ItemData[] animalCatalog;

    [Header("UI (auto-created when empty)")]
    [SerializeField] private GameObject currencyUIPrefab;
    [SerializeField] private TextMeshProUGUI coinText;

    private GameObject currencyUIInstance;

    public int CurrentCoins { get; private set; }
    public ItemData[] SeedCatalog => seedCatalog;
    public ItemData[] FertilizerCatalog => fertilizerCatalog;
    public ItemData[] ProduceCatalog => produceCatalog;
    public ItemData[] AnimalCatalog => animalCatalog;
    public ItemStorageController ItemStorage => itemStorageController;

    private void Awake()
    {
        CurrentCoins = Mathf.Max(0, startingCoins);

#if UNITY_EDITOR
        if (currencyUIPrefab == null)
        {
            currencyUIPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/CurrencyUI.prefab"
            );
        }
#endif

        if (itemStorageController == null)
        {
            itemStorageController = FindFirstObjectByType<ItemStorageController>();
        }

        EnsureCoinText();
        RefreshUI();
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && CurrentCoins >= amount;
    }

    public bool TrySpendCoins(int amount)
    {
        if (!CanAfford(amount))
        {
            Debug.Log($"Không đủ coin. Cần {amount}, hiện có {CurrentCoins} coin.");
            return false;
        }

        CurrentCoins -= amount;
        RefreshUI();
        Debug.Log($"Đã trừ {amount} coin. Số dư: {CurrentCoins} coin.");
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentCoins += amount;
        RefreshUI();
        Debug.Log($"Đã cộng {amount} coin. Số dư: {CurrentCoins} coin.");
    }

    public bool TryBuySeed(ItemData seedItem, int amount = 1)
    {
        if (seedItem == null || seedItem.itemType != ItemType.Seed)
        {
            Debug.LogWarning("Dữ liệu mua hạt giống không hợp lệ.");
            return false;
        }

        return TryBuyItem(seedItem, amount);
    }

    public bool TryBuyItem(ItemData item, int amount = 1)
    {
        bool isSeed = item != null
            && item.itemType == ItemType.Seed
            && item.cropToPlant != null;
        bool isFertilizer = item != null
            && item.itemType == ItemType.Fertilizer
            && item.fertilizerType != FertilizerType.None;
        // [Thú] animal feed goes into the inventory just like seeds do
        bool isFeed = item != null && item.itemType == ItemType.AnimalFeed;

        if (isSeed && SeasonManager.Instance != null
            && !item.cropToPlant.IsAllowedInSeason(SeasonManager.Instance.CurrentSeason))
        {
            Debug.Log(
                $"Không thể mua {item.itemName}: hạt giống không phù hợp với "
                + $"{SeasonManager.GetSeasonDisplayName(SeasonManager.Instance.CurrentSeason)}."
            );
            return false;
        }

        if ((!isSeed && !isFertilizer && !isFeed) || item.purchasePrice <= 0
            || amount <= 0 || itemStorageController == null)
        {
            Debug.LogWarning("Dữ liệu vật phẩm mua trong Shop không hợp lệ.");
            return false;
        }

        long totalPrice = (long)item.purchasePrice * amount;
        if (totalPrice > int.MaxValue || !CanAfford((int)totalPrice))
        {
            Debug.Log(
                $"Không đủ coin mua {amount} {item.itemName}. "
                + $"Cần {totalPrice}, hiện có {CurrentCoins} coin."
            );
            return false;
        }

        int bought = 0;
        for (int i = 0; i < amount; i++)
        {
            if (!itemStorageController.TryAddItem(item, 1))
            {
                break;
            }

            CurrentCoins -= item.purchasePrice;
            bought++;
        }

        RefreshUI();

        if (bought == 0)
        {
            Debug.Log("Không thể mua: Hotbar và Inventory đã đầy.");
            return false;
        }

        Debug.Log(
            $"Đã mua {bought} {item.itemName} với giá "
            + $"{bought * item.purchasePrice} coin. Số dư: {CurrentCoins} coin."
        );
        return bought == amount;
    }

    // [Thú] Buying an animal does not add an inventory item - it summons the
    // animal on the farm. Coins are only taken if the animal was really created.
    public bool TryBuyAnimal(ItemData animalItem)
    {
        if (animalItem == null || animalItem.itemType != ItemType.Animal
            || animalItem.animalPrefab == null || animalItem.purchasePrice <= 0)
        {
            Debug.LogWarning("Dữ liệu mua thú vật không hợp lệ.");
            return false;
        }

        if (!CanAfford(animalItem.purchasePrice))
        {
            Debug.Log(
                $"Không đủ coin mua {animalItem.itemName}. "
                + $"Cần {animalItem.purchasePrice}, hiện có {CurrentCoins} coin."
            );
            return false;
        }

        AnimalProducer animal = AnimalManager.Instance.SpawnPurchased(animalItem);
        if (animal == null)
        {
            return false; // reason already logged (e.g. owned the maximum)
        }

        CurrentCoins -= animalItem.purchasePrice;
        RefreshUI();
        Debug.Log(
            $"Đã mua {animalItem.itemName} với giá {animalItem.purchasePrice} coin. "
            + $"Số dư: {CurrentCoins} coin."
        );
        return true;
    }

    public bool TryBuySeedByIndex(int catalogIndex)
    {
        return seedCatalog != null
            && catalogIndex >= 0
            && catalogIndex < seedCatalog.Length
            && TryBuySeed(seedCatalog[catalogIndex]);
    }

    public bool TrySellProduce(ItemData produceItem, int amount = 1)
    {
        return TrySellProduce(produceItem, CropQuality.Normal, amount);
    }

    public bool TrySellProduce(
        ItemData produceItem,
        CropQuality quality,
        int amount = 1
    )
    {
        if (produceItem == null || produceItem.itemType != ItemType.Produce
            || produceItem.sellPrice <= 0 || amount <= 0
            || itemStorageController == null)
        {
            Debug.LogWarning("Dữ liệu bán nông sản không hợp lệ.");
            return false;
        }

        if (!itemStorageController.TryRemoveItem(
            produceItem,
            quality,
            amount
        ))
        {
            Debug.Log(
                $"Không đủ {produceItem.itemName} "
                + $"[{CropQualityUtility.GetDisplayName(quality)}] "
                + $"để bán {amount} sản phẩm."
            );
            return false;
        }

        int totalPrice = 0;
        for (int i = 0; i < amount; i++)
        {
            // Chili (HalfOrOneAndHalf) rolls 125/375 first, then quality applies.
            totalPrice += produceItem.CalculateSellPrice(Random.value, quality);
        }

        CurrentCoins += totalPrice;
        RefreshUI();
        Debug.Log(
            $"Đã bán {amount} {produceItem.itemName} "
            + $"[{CropQualityUtility.GetDisplayName(quality)}], "
            + $"nhận {totalPrice} coin. "
            + $"Số dư: {CurrentCoins} coin."
        );
        return true;
    }

    public bool TrySellProduceByIndex(int catalogIndex)
    {
        return produceCatalog != null
            && catalogIndex >= 0
            && catalogIndex < produceCatalog.Length
            && TrySellProduce(produceCatalog[catalogIndex]);
    }

    private void EnsureCoinText()
    {
        if (coinText != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("Không tìm thấy Canvas để hiển thị Coin.");
            return;
        }

        if (currencyUIPrefab == null)
        {
            Debug.LogWarning("CoinManager thiếu CurrencyUI prefab.");
            return;
        }

        currencyUIInstance = Instantiate(currencyUIPrefab, canvas.transform);
        currencyUIInstance.name = "CurrencyUI_Runtime";

        RectTransform rootRect = currencyUIInstance.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        RectTransform currencyBar = currencyUIInstance.transform.childCount > 0
            ? currencyUIInstance.transform.GetChild(0) as RectTransform
            : rootRect;
        currencyBar.anchorMin = Vector2.one;
        currencyBar.anchorMax = Vector2.one;
        currencyBar.pivot = Vector2.one;
        currencyBar.anchoredPosition = new Vector2(-24f, -18f);
        currencyBar.sizeDelta = new Vector2(300f, 38f);

        GameObject valueCover = new GameObject(
            "CoinValueBackground",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image)
        );
        valueCover.layer = 5;
        valueCover.transform.SetParent(currencyBar, false);
        UnityEngine.UI.Image coverImage = valueCover.GetComponent<UnityEngine.UI.Image>();
        coverImage.color = new Color(0.025f, 0.065f, 0.075f, 1f);
        coverImage.raycastTarget = false;
        RectTransform coverRect = valueCover.GetComponent<RectTransform>();
        coverRect.anchorMin = new Vector2(0f, 0f);
        coverRect.anchorMax = new Vector2(1f, 1f);
        coverRect.offsetMin = new Vector2(58f, 5f);
        coverRect.offsetMax = new Vector2(-8f, -5f);

        GameObject textObject = new GameObject("CoinValue", typeof(RectTransform));
        textObject.layer = 5;
        textObject.transform.SetParent(currencyBar, false);
        coinText = textObject.AddComponent<TextMeshProUGUI>();
        coinText.fontSize = 20f;
        coinText.fontStyle = FontStyles.Bold;
        coinText.alignment = TextAlignmentOptions.MidlineRight;
        coinText.color = new Color(1f, 0.88f, 0.42f);
        coinText.outlineColor = new Color32(20, 18, 12, 255);
        coinText.outlineWidth = 0.1f;
        coinText.raycastTarget = false;

        RectTransform rect = coinText.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(65f, 2f);
        rect.offsetMax = new Vector2(-16f, -2f);
    }

    private void RefreshUI()
    {
        if (coinText != null)
        {
            coinText.text = CurrentCoins.ToString("N0");
        }
    }
}
