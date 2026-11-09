using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    private enum ShopTab
    {
        BuySeeds,
        SellProduce,
        BuyAnimals,      // [Thú]
        DragonUpgrades   // [Rồng]
    }

    [Header("References")]
    [SerializeField] private CoinManager coinManager;
    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private Canvas parentCanvas;

    [Header("Shop visual sprites")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite buttonNormalSprite;
    [SerializeField] private Sprite buttonHoverSprite;

    [Header("Input")]
    [SerializeField] private Key toggleKey = Key.B;

    private GameObject shopInstance;
    private CanvasGroup shopCanvasGroup;
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI emptyText;
    private RectTransform contentRoot;
    private Button buyTabButton;
    private Button sellTabButton;
    private Button animalTabButton;   // [Thú]
    private Button upgradeTabButton;  // [Rồng]
    private ShopTab currentTab;
    private bool isOpen;
    private SeasonType displayedSeason;
    private bool hasDisplayedSeason;

    public static bool IsOpen { get; private set; }

    protected void Awake()
    {
        if (coinManager == null)
        {
            coinManager = FindFirstObjectByType<CoinManager>();
        }

#if UNITY_EDITOR
        // The currently open scene can keep an older in-memory version after
        // fields are added to this component. Resolve the existing prefab for
        // Editor Play Mode so the Shop still opens without reopening the scene.
        if (shopPrefab == null)
        {
            shopPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Shop/PixelFarmShopUI.prefab"
            );
        }
#endif

        if (parentCanvas == null)
        {
            parentCanvas = FindFirstObjectByType<Canvas>();
        }

        CreateShopUI();
        SetOpen(false);
    }

    protected void OnDisable()
    {
        SetOpen(false);
    }

    protected void Update()
    {
        if (Keyboard.current != null
            && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            SetOpen(!isOpen);
        }

        if (isOpen && SeasonManager.Instance != null
            && (!hasDisplayedSeason
                || displayedSeason != SeasonManager.Instance.CurrentSeason))
        {
            RefreshShop();
        }
    }

    public void OpenShop()
    {
        SetOpen(true);
    }

    public void CloseShop()
    {
        SetOpen(false);
    }

    public void ToggleShop()
    {
        SetOpen(!isOpen);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        IsOpen = open;

        if (shopCanvasGroup != null)
        {
            shopCanvasGroup.alpha = open ? 1f : 0f;
            shopCanvasGroup.interactable = open;
            shopCanvasGroup.blocksRaycasts = open;
        }

        if (open)
        {
            RefreshShop();
            shopInstance?.transform.SetAsLastSibling();
        }
    }

    private void CreateShopUI()
    {
        if (shopPrefab == null || parentCanvas == null)
        {
            Debug.LogError("Shop thiếu ShopUI prefab hoặc Canvas.");
            return;
        }

        shopInstance = Instantiate(shopPrefab, parentCanvas.transform);
        shopInstance.name = "ShopUI_Runtime";
        parentCanvas.pixelPerfect = true;

        RectTransform rootRect = shopInstance.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.localRotation = Quaternion.identity;
        rootRect.localScale = Vector3.one;

        shopCanvasGroup = shopInstance.GetComponent<CanvasGroup>();
        if (shopCanvasGroup == null)
        {
            shopCanvasGroup = shopInstance.AddComponent<CanvasGroup>();
        }

        Transform panelTransform = shopInstance.transform.Find("Panel");
        RectTransform panel = panelTransform != null
            ? panelTransform as RectTransform
            : rootRect;

        if (panel == null)
        {
            Debug.LogError("ShopUI prefab không có RectTransform làm khung Shop.");
            return;
        }

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null && panelSprite != null)
        {
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
        }

        BuildHeader(panel);
        BuildTabs(panel);
        BuildScrollView(panel);
    }

    private void BuildHeader(RectTransform panel)
    {
        TextMeshProUGUI title = CreateText("Title", panel, "CỬA HÀNG NÔNG TRẠI", 27f);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -38f), new Vector2(350f, 42f));
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;
        ApplyReadableText(title, new Color(1f, 0.88f, 0.54f), 0.12f);

        coinText = CreateText("CoinText", panel, "Coin: 0", 17f);
        SetRect(coinText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(112f, -78f), new Vector2(190f, 30f));
        coinText.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyReadableText(coinText, new Color(1f, 0.84f, 0.24f), 0.1f);

        Button closeButton = CreateButton("CloseButton", panel, "X");
        SetRect(closeButton.GetComponent<RectTransform>(), Vector2.one, Vector2.one,
            new Vector2(-29f, -34f), new Vector2(38f, 38f));
        closeButton.onClick.AddListener(() => SetOpen(false));
    }

    // [Thú][Rồng] Four tabs now: seeds, animals, dragon upgrades, sell.
    private void BuildTabs(RectTransform panel)
    {
        const float step = 144f;
        buyTabButton = CreateTab(panel, "BuySeedsTab", "HẠT GIỐNG", -1.5f * step, ShopTab.BuySeeds);
        animalTabButton = CreateTab(panel, "BuyAnimalsTab", "THÚ VẬT", -0.5f * step, ShopTab.BuyAnimals);
        upgradeTabButton = CreateTab(panel, "DragonUpgradesTab", "NÂNG CẤP RỒNG", 0.5f * step, ShopTab.DragonUpgrades);
        sellTabButton = CreateTab(panel, "SellProduceTab", "BÁN HÀNG", 1.5f * step, ShopTab.SellProduce);
    }

    private Button CreateTab(RectTransform panel, string objectName, string label, float x, ShopTab tab)
    {
        Button button = CreateButton(objectName, panel, label);
        SetRect(button.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(x, -118f), new Vector2(136f, 42f));

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 16f;
        }

        button.onClick.AddListener(() => SelectTab(tab));
        return button;
    }

    private void BuildScrollView(RectTransform panel)
    {
        GameObject viewportObject = CreateUIObject("Viewport", panel);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(26f, 24f);
        viewport.offsetMax = new Vector2(-26f, -150f);

        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0.025f, 0.055f, 0.065f, 0.94f);
        viewportObject.AddComponent<RectMask2D>();

        GameObject contentObject = CreateUIObject("Content", viewport);
        contentRoot = contentObject.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = viewportObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = contentRoot;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        emptyText = CreateText("EmptyText", contentRoot, "", 18f);
        emptyText.alignment = TextAlignmentOptions.Center;
        emptyText.gameObject.AddComponent<LayoutElement>().minHeight = 70f;
    }

    private void SelectTab(ShopTab tab)
    {
        currentTab = tab;
        RefreshShop();
    }

    private void RefreshShop()
    {
        if (SeasonManager.Instance != null)
        {
            displayedSeason = SeasonManager.Instance.CurrentSeason;
            hasDisplayedSeason = true;
        }

        if (coinText != null)
        {
            coinText.text = coinManager != null
                ? "Coin: " + coinManager.CurrentCoins.ToString("N0")
                : "Coin: ---";
        }

        if (contentRoot == null)
        {
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (child != emptyText.transform)
            {
                Destroy(child.gameObject);
            }
        }

        SetTabColor(buyTabButton, currentTab == ShopTab.BuySeeds);
        SetTabColor(animalTabButton, currentTab == ShopTab.BuyAnimals);
        SetTabColor(upgradeTabButton, currentTab == ShopTab.DragonUpgrades);
        SetTabColor(sellTabButton, currentTab == ShopTab.SellProduce);

        bool hasRows;
        string emptyMessage;
        switch (currentTab)
        {
            case ShopTab.BuyAnimals:
                hasRows = BuildAnimalRows();
                emptyMessage = "Chưa có thú vật nào trong cửa hàng.";
                break;
            case ShopTab.DragonUpgrades:
                hasRows = BuildDragonUpgradeRows();
                emptyMessage = "Chưa có dữ liệu nâng cấp rồng.";
                break;
            case ShopTab.SellProduce:
                hasRows = BuildProduceRows();
                emptyMessage = "Không có nông sản hoặc sản phẩm thú vật trong Hotbar/Inventory.";
                break;
            default:
                hasRows = BuildSeedRows();
                emptyMessage = "Chưa có dữ liệu hạt giống.";
                break;
        }

        emptyText.gameObject.SetActive(!hasRows);
        if (!hasRows)
        {
            emptyText.text = emptyMessage;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    private bool BuildSeedRows()
    {
        if (coinManager == null)
        {
            return false;
        }

        bool hasRows = false;
        hasRows |= BuildPurchaseCatalog(coinManager.SeedCatalog, ItemType.Seed);
        hasRows |= BuildPurchaseCatalog(
            coinManager.FertilizerCatalog,
            ItemType.Fertilizer
        );
        return hasRows;
    }

    private bool BuildPurchaseCatalog(ItemData[] catalog, ItemType expectedType)
    {
        if (catalog == null)
        {
            return false;
        }

        bool hasRows = false;
        foreach (ItemData item in catalog)
        {
            if (item == null || item.itemType != expectedType)
            {
                continue;
            }

            hasRows = true;
            CreateBuyRow(item);
        }

        return hasRows;
    }

    private void CreateBuyRow(ItemData seed)
    {
        bool unavailableInCurrentSeason = seed.itemType == ItemType.Seed
            && seed.cropToPlant != null
            && SeasonManager.Instance != null
            && !seed.cropToPlant.IsAllowedInSeason(SeasonManager.Instance.CurrentSeason);
        // [Thú] you can only own so many of each animal
        bool soldOut = seed.itemType == ItemType.Animal
            && AnimalProducer.CountOwned(seed) >= seed.maxOwned;

        GameObject row = CreateUIObject("BuySeed_" + seed.name, contentRoot);
        Image rowImage = row.AddComponent<Image>();
        rowImage.color = new Color(0.06f, 0.115f, 0.125f, 1f);

        Button rowButton = row.AddComponent<Button>();
        rowButton.targetGraphic = rowImage;
        ColorBlock rowColors = rowButton.colors;
        rowColors.normalColor = Color.white;
        rowColors.highlightedColor = new Color(1f, 0.94f, 0.72f, 1f);
        rowColors.pressedColor = new Color(0.78f, 0.72f, 0.56f, 1f);
        rowColors.selectedColor = Color.white;
        rowButton.colors = rowColors;
        rowButton.interactable = !unavailableInCurrentSeason && !soldOut;
        rowButton.onClick.AddListener(() =>
        {
            // [Thú] animals are summoned on the farm instead of going to the inventory
            if (seed.itemType == ItemType.Animal)
            {
                coinManager.TryBuyAnimal(seed);
            }
            else
            {
                coinManager.TryBuyItem(seed, 1);
            }

            RefreshShop();
        });

        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 78f;
        rowLayout.preferredHeight = 78f;

        GameObject iconFrame = CreateUIObject("IconFrame", row.transform);
        Image iconFrameImage = iconFrame.AddComponent<Image>();
        iconFrameImage.color = new Color(0.68f, 0.43f, 0.17f, 1f);
        iconFrameImage.raycastTarget = false;
        SetRect(iconFrameImage.rectTransform, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(45f, 0f), new Vector2(58f, 58f));

        GameObject iconObject = CreateUIObject("ItemIcon", row.transform);
        Image icon = iconObject.AddComponent<Image>();
        icon.sprite = seed.icon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetRect(icon.rectTransform, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(45f, 0f), new Vector2(48f, 48f));

        TextMeshProUGUI nameText = CreateText(
            "ItemName", row.transform, seed.itemName, 18f);
        SetRect(nameText.rectTransform, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(191f, 13f), new Vector2(220f, 27f));
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.fontStyle = FontStyles.Bold;
        ApplyReadableText(nameText, Color.white, 0.12f);

        TextMeshProUGUI typeText = CreateText(
            "ItemInfo",
            row.transform,
            GetBuyRowInfo(seed, unavailableInCurrentSeason, soldOut),
            14f);
        SetRect(typeText.rectTransform, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(231f, -15f), new Vector2(300f, 23f));
        typeText.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyReadableText(typeText, new Color(0.82f, 0.88f, 0.86f), 0.08f);

        if (unavailableInCurrentSeason || soldOut)
        {
            rowImage.color = new Color(0.045f, 0.055f, 0.06f, 0.78f);
            icon.color = new Color(0.45f, 0.45f, 0.45f, 0.75f);
            nameText.color = new Color(0.55f, 0.55f, 0.55f, 1f);
        }

        GameObject priceBox = CreateUIObject("PriceBox", row.transform);
        Image priceBoxImage = priceBox.AddComponent<Image>();
        priceBoxImage.color = new Color(0.69f, 0.43f, 0.16f, 1f);
        SetRect(priceBoxImage.rectTransform, new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-86f, 0f), new Vector2(142f, 48f));

        GameObject coin = CreateUIObject("Coin", priceBox.transform);
        Image coinImage = coin.AddComponent<Image>();
        coinImage.color = new Color(1f, 0.72f, 0.16f, 1f);
        coinImage.raycastTarget = false;
        SetRect(coinImage.rectTransform, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(23f, 0f), new Vector2(18f, 18f));

        TextMeshProUGUI priceText = CreateText(
            "Price", priceBox.transform, seed.purchasePrice.ToString("N0"), 19f);
        SetRect(priceText.rectTransform, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(88f, 0f), new Vector2(82f, 28f));
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.fontStyle = FontStyles.Bold;
        ApplyReadableText(priceText, new Color(1f, 0.93f, 0.58f), 0.12f);
    }

    // [Thú] Animals + animal feed. Animals are summoned on the farm when bought.
    private bool BuildAnimalRows()
    {
        if (coinManager == null)
        {
            return false;
        }

        bool hasRows = false;
        hasRows |= BuildPurchaseCatalog(coinManager.AnimalCatalog, ItemType.Animal);
        hasRows |= BuildPurchaseCatalog(coinManager.AnimalCatalog, ItemType.AnimalFeed);
        return hasRows;
    }

    private static string GetBuyRowInfo(ItemData item, bool unavailableInCurrentSeason, bool soldOut)
    {
        if (unavailableInCurrentSeason)
        {
            return "Không bán trong mùa này";
        }

        switch (item.itemType)
        {
            case ItemType.Fertilizer:
                return "Phân bón • Mua 1";

            case ItemType.AnimalFeed:
                return "Thức ăn • +" + item.feedSatisfaction + " hài lòng • Mua 1";

            case ItemType.Animal:
                int owned = AnimalProducer.CountOwned(item);
                if (soldOut)
                {
                    return "Đã nuôi tối đa " + owned + "/" + item.maxOwned;
                }

                string produces = string.Empty;
                AnimalProducer template = item.animalPrefab != null
                    ? item.animalPrefab.GetComponent<AnimalProducer>()
                    : null;
                if (template != null && template.DropItem != null)
                {
                    produces = " • Cho " + template.DropItem.itemName
                        + " mỗi " + template.DropEveryDays + " ngày";
                }

                return "Thú vật • Đang nuôi " + owned + "/" + item.maxOwned + produces;

            default:
                return "Hạt giống • Mua 1";
        }
    }

    // [Rồng] One row per upgrade (4 dragons x 3 stats). Bought with coins and
    // applied instantly to every dragon of that element.
    private bool BuildDragonUpgradeRows()
    {
        DragonStats stats = DragonStats.Instance;
        if (coinManager == null || stats == null)
        {
            return false;
        }

        bool hasRows = false;

        foreach (DragonUpgradeTrack track in stats.Tracks)
        {
            if (track == null)
            {
                continue;
            }

            hasRows = true;
            DragonElement element = track.element;
            DragonStat stat = track.stat;

            int level = stats.GetLevel(element, stat);
            bool maxed = stats.IsMaxed(element, stat);
            int cost = stats.GetNextCost(element, stat);

            int currentBonus = Mathf.RoundToInt(track.bonusPerLevel * level * 100f);
            int nextBonus = Mathf.RoundToInt(track.bonusPerLevel * (level + 1) * 100f);

            string level_text = "Cấp " + level + "/" + track.maxLevel
                + " • +" + currentBonus + "%"
                + (maxed ? string.Empty : " → +" + nextBonus + "%");

            string description = track.description;
            if (!ElementalDragon.HasDragon(element))
            {
                description = "Chưa có " + DragonStats.GetElementName(element).ToLower()
                    + " trên nông trại • " + description;
            }

            CreateUpgradeRow(
                ElementalDragon.GetIcon(element),
                DragonStats.GetElementName(element) + " • " + DragonStats.GetStatName(element, stat),
                level_text,
                description,
                maxed ? "TỐI ĐA" : "NÂNG CẤP\n" + cost.ToString("N0"),
                !maxed,
                () =>
                {
                    DragonStats.Instance.TryUpgrade(element, stat, coinManager);
                    RefreshShop();
                }
            );
        }

        return hasRows;
    }

    private void CreateUpgradeRow(
        Sprite icon,
        string title,
        string levelText,
        string description,
        string buttonLabel,
        bool canUpgrade,
        UnityEngine.Events.UnityAction onClick
    )
    {
        GameObject row = CreateUIObject("DragonUpgrade_" + title, contentRoot);
        row.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.17f, 0.96f);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 84f;
        rowLayout.preferredHeight = 84f;

        GameObject frame = CreateUIObject("IconFrame", row.transform);
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = new Color(0.68f, 0.43f, 0.17f, 1f);
        frameImage.raycastTarget = false;
        SetRect(frameImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(45f, 0f), new Vector2(58f, 58f));

        GameObject iconObject = CreateUIObject("Icon", row.transform);
        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        SetRect(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(45f, 0f), new Vector2(48f, 48f));

        TextMeshProUGUI nameText = CreateText("Name", row.transform, title, 18f);
        SetRect(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(231f, 25f), new Vector2(300f, 26f));
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        ApplyReadableText(nameText, Color.white, 0.12f);

        TextMeshProUGUI levelLabel = CreateText("Level", row.transform, levelText, 15f);
        SetRect(levelLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(231f, 0f), new Vector2(300f, 24f));
        levelLabel.alignment = TextAlignmentOptions.MidlineLeft;
        levelLabel.color = new Color(1f, 0.82f, 0.25f);

        TextMeshProUGUI descriptionText = CreateText("Description", row.transform, description, 12f);
        SetRect(descriptionText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(231f, -24f), new Vector2(300f, 22f));
        descriptionText.alignment = TextAlignmentOptions.MidlineLeft;
        descriptionText.color = new Color(0.78f, 0.86f, 0.84f);

        Button button = CreateButton("UpgradeButton", row.transform, buttonLabel);
        SetRect(button.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-86f, 0f), new Vector2(142f, 54f));
        button.interactable = canUpgrade;

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.enableAutoSizing = true;
            buttonText.fontSizeMin = 10f;
            buttonText.fontSizeMax = 16f;
        }

        button.onClick.AddListener(onClick);
    }

    private bool BuildProduceRows()
    {
        if (coinManager == null
            || coinManager.ItemStorage == null
            || coinManager.ProduceCatalog == null)
        {
            return false;
        }

        bool hasRows = false;
        foreach (ItemData produce in coinManager.ProduceCatalog)
        {
            if (produce == null || produce.itemType != ItemType.Produce)
            {
                continue;
            }

            foreach (CropQuality quality in System.Enum.GetValues(
                typeof(CropQuality)))
            {
                int owned = coinManager.ItemStorage.CountItem(produce, quality);
                if (owned <= 0)
                {
                    continue;
                }

                hasRows = true;
                ItemData selectedProduce = produce;
                CropQuality selectedQuality = quality;
                int sellAllAmount = owned;
                CreateItemRow(
                    selectedProduce,
                    CropQualityUtility.GetDisplayName(selectedQuality)
                        + " • x" + owned + "   "
                        + GetSellPriceText(selectedProduce, selectedQuality),
                    "Bán 1",
                    () =>
                    {
                        coinManager.TrySellProduce(
                            selectedProduce,
                            selectedQuality,
                            1
                        );
                        RefreshShop();
                    },
                    "Bán hết",
                    () =>
                    {
                        coinManager.TrySellProduce(
                            selectedProduce,
                            selectedQuality,
                            sellAllAmount
                        );
                        RefreshShop();
                    }
                );
            }
        }

        return hasRows;
    }

    private void CreateItemRow(
        ItemData item,
        string detail,
        string primaryLabel,
        UnityEngine.Events.UnityAction primaryAction,
        string secondaryLabel,
        UnityEngine.Events.UnityAction secondaryAction
    )
    {
        GameObject row = CreateUIObject("ShopItem_" + item.name, contentRoot);
        row.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.17f, 0.96f);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 74f;
        rowLayout.preferredHeight = 74f;

        GameObject iconObject = CreateUIObject("Icon", row.transform);
        Image icon = iconObject.AddComponent<Image>();
        icon.sprite = item.icon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(43f, 0f), new Vector2(56f, 56f));

        TextMeshProUGUI nameText = CreateText("Name", row.transform, item.itemName, 18f);
        SetRect(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(180f, 14f), new Vector2(210f, 28f));
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.fontStyle = FontStyles.Bold;

        TextMeshProUGUI detailText = CreateText("Detail", row.transform, detail, 15f);
        SetRect(detailText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(195f, -17f), new Vector2(240f, 25f));
        detailText.alignment = TextAlignmentOptions.MidlineLeft;
        detailText.color = new Color(1f, 0.82f, 0.25f);

        float primaryX = secondaryAction == null ? -58f : -138f;
        Button primary = CreateButton("PrimaryButton", row.transform, primaryLabel);
        SetRect(primary.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(primaryX, 0f), new Vector2(92f, 42f));
        primary.onClick.AddListener(primaryAction);

        if (secondaryAction != null)
        {
            Button secondary = CreateButton("SecondaryButton", row.transform, secondaryLabel);
            SetRect(secondary.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-50f, 0f), new Vector2(82f, 42f));
            secondary.onClick.AddListener(secondaryAction);
        }
    }

    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform));
        result.layer = 5;
        result.transform.SetParent(parent, false);
        return result;
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize
    )
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableAutoSizing = false;
        text.extraPadding = true;
        return text;
    }

    private static void ApplyReadableText(
        TextMeshProUGUI text,
        Color faceColor,
        float outlineWidth
    )
    {
        text.color = faceColor;
        text.fontStyle |= FontStyles.Bold;
        text.outlineColor = new Color32(15, 18, 16, 255);
        text.outlineWidth = outlineWidth;
    }

    private Button CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = CreateUIObject(objectName, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = buttonNormalSprite;
        image.type = buttonNormalSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = buttonNormalSprite != null
            ? Color.white
            : new Color(0.12f, 0.29f, 0.32f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (buttonNormalSprite != null && buttonHoverSprite != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState states = button.spriteState;
            states.highlightedSprite = buttonHoverSprite;
            states.selectedSprite = buttonHoverSprite;
            states.pressedSprite = buttonHoverSprite;
            button.spriteState = states;
        }
        else
        {
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.2f, 0.48f, 0.5f, 1f);
            colors.pressedColor = new Color(0.08f, 0.2f, 0.22f, 1f);
            button.colors = colors;
        }

        TextMeshProUGUI buttonText = CreateText("Text", buttonObject.transform, label, 16f);
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;
        ApplyReadableText(buttonText, Color.white, 0.1f);
        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private static void SetTabColor(Button button, bool selected)
    {
        if (button != null && button.targetGraphic is Image image)
        {
            image.color = selected
                ? new Color(0.75f, 0.46f, 0.12f, 1f)
                : new Color(0.12f, 0.29f, 0.32f, 1f);
        }
    }

    private static string GetSellPriceText(
        ItemData produce,
        CropQuality quality
    )
    {
        float qualityMultiplier = produce.GetQualityPriceMultiplier(quality);
        if (produce.sellPriceMode == SellPriceMode.HalfOrOneAndHalf)
        {
            int lowPrice = Mathf.RoundToInt(
                produce.sellPrice * 0.5f * qualityMultiplier);
            int highPrice = Mathf.RoundToInt(
                produce.sellPrice * 1.5f * qualityMultiplier);
            return lowPrice.ToString("N0") + " hoặc "
                + highPrice.ToString("N0") + " coin/cái";
        }

        int price = Mathf.RoundToInt(produce.sellPrice * qualityMultiplier);
        return price.ToString("N0") + " coin/cái";
    }
}
