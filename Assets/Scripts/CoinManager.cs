using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    [Header("Wallet")]
    [SerializeField, Min(0)] private int startingCoins = 5000;
    [SerializeField] private ItemStorageController itemStorageController;

    [Header("Shop catalog")]
    [SerializeField] private ItemData[] seedCatalog;
    [SerializeField] private ItemData[] produceCatalog;

    [Header("UI (auto-created when empty)")]
    [SerializeField] private TextMeshProUGUI coinText;

    public int CurrentCoins { get; private set; }

    private void Awake()
    {
        CurrentCoins = Mathf.Max(0, startingCoins);

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
        if (seedItem == null || seedItem.itemType != ItemType.Seed
            || seedItem.cropToPlant == null || seedItem.purchasePrice <= 0
            || amount <= 0 || itemStorageController == null)
        {
            Debug.LogWarning("Dữ liệu mua hạt giống không hợp lệ.");
            return false;
        }

        long totalPrice = (long)seedItem.purchasePrice * amount;
        if (totalPrice > int.MaxValue || !CanAfford((int)totalPrice))
        {
            Debug.Log(
                $"Không đủ coin mua {amount} {seedItem.itemName}. "
                + $"Cần {totalPrice}, hiện có {CurrentCoins} coin."
            );
            return false;
        }

        int bought = 0;
        for (int i = 0; i < amount; i++)
        {
            if (!itemStorageController.TryAddItem(seedItem, 1))
            {
                break;
            }

            CurrentCoins -= seedItem.purchasePrice;
            bought++;
        }

        RefreshUI();

        if (bought == 0)
        {
            Debug.Log("Không thể mua: Hotbar và Inventory đã đầy.");
            return false;
        }

        Debug.Log(
            $"Đã mua {bought} {seedItem.itemName} với giá "
            + $"{bought * seedItem.purchasePrice} coin. Số dư: {CurrentCoins} coin."
        );
        return bought == amount;
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
        if (produceItem == null || produceItem.itemType != ItemType.Produce
            || produceItem.sellPrice <= 0 || amount <= 0
            || itemStorageController == null)
        {
            Debug.LogWarning("Dữ liệu bán nông sản không hợp lệ.");
            return false;
        }

        if (!itemStorageController.TryRemoveItem(produceItem, amount))
        {
            Debug.Log(
                $"Không đủ {produceItem.itemName} để bán {amount} sản phẩm."
            );
            return false;
        }

        int totalPrice = 0;
        for (int i = 0; i < amount; i++)
        {
            totalPrice += produceItem.CalculateSellPrice(Random.value);
        }

        CurrentCoins += totalPrice;
        RefreshUI();
        Debug.Log(
            $"Đã bán {amount} {produceItem.itemName}, nhận {totalPrice} coin. "
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

        GameObject textObject = new GameObject("Coin Text");
        textObject.transform.SetParent(canvas.transform, false);
        coinText = textObject.AddComponent<TextMeshProUGUI>();
        coinText.fontSize = 28f;
        coinText.alignment = TextAlignmentOptions.TopRight;
        coinText.color = new Color(1f, 0.82f, 0.18f);

        RectTransform rect = coinText.rectTransform;
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-24f, -18f);
        rect.sizeDelta = new Vector2(300f, 50f);
    }

    private void RefreshUI()
    {
        if (coinText != null)
        {
            coinText.text = $"Coin: {CurrentCoins:N0}";
        }
    }
}
