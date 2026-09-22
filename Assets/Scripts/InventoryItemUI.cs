using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryItemUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Item data")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount;
    [SerializeField] private CropQuality quality = CropQuality.Normal;
    [Tooltip("Temporarily clears this serialized test item at runtime without deleting its data or GameObject.")]
    [SerializeField] private bool hideInitialTestItem;

    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image qualityBadge;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;

    public ItemData ItemData => itemData;
    public int Amount => amount;
    public CropQuality Quality => quality;
    public bool IsEmpty => itemData == null || amount <= 0;

    private void Awake()
    {
        NormalizeItemRectTransform();
        EnsureOwnedUIReferences();

        if (hideInitialTestItem)
        {
            itemData = null;
            amount = 0;
            quality = CropQuality.Normal;
        }

        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        RefreshUI();
    }

    private void NormalizeItemRectTransform()
    {
        RectTransform itemRect = GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.localRotation = Quaternion.identity;
        itemRect.localScale = Vector3.one;
    }

    private void EnsureOwnedUIReferences()
    {
        bool ownsIcon = itemIcon != null
            && (itemIcon.transform == transform
                || itemIcon.transform.IsChildOf(transform));

        if (!ownsIcon)
        {
            itemIcon = GetComponent<Image>();

            if (itemIcon == null)
            {
                itemIcon = gameObject.AddComponent<Image>();
            }
        }

        itemIcon.preserveAspect = true;
        itemIcon.raycastTarget = true;

        bool ownsCountText = countText != null
            && countText.transform.IsChildOf(transform);

        if (!ownsCountText)
        {
            if (countText != null)
            {
                countText.text = string.Empty;
            }

            countText = GetComponentInChildren<TextMeshProUGUI>(true);

            if (countText == null
                || !countText.transform.IsChildOf(transform))
            {
                GameObject countObject = new GameObject(
                    "CountText",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI)
                );
                countObject.transform.SetParent(transform, false);
                countText = countObject.GetComponent<TextMeshProUGUI>();
            }
        }

        RectTransform countRect = countText.rectTransform;
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.anchoredPosition = new Vector2(-4f, 4f);
        countRect.sizeDelta = new Vector2(35f, 35f);
        countRect.localRotation = Quaternion.identity;
        countRect.localScale = Vector3.one;

        countText.fontSize = 22f;
        countText.fontStyle = FontStyles.Bold;
        countText.alignment = TextAlignmentOptions.BottomRight;
        countText.raycastTarget = false;

        if (qualityBadge == null || !qualityBadge.transform.IsChildOf(transform))
        {
            GameObject badgeObject = new GameObject(
                "QualityBadge",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            badgeObject.transform.SetParent(transform, false);
            qualityBadge = badgeObject.GetComponent<Image>();
        }

        RectTransform badgeRect = qualityBadge.rectTransform;
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = new Vector2(2f, -2f);
        badgeRect.sizeDelta = new Vector2(18f, 18f);
        qualityBadge.preserveAspect = true;
        qualityBadge.raycastTarget = false;
    }

    public int AddItem(ItemData itemToAdd, int amountToAdd)
    {
        return AddItem(itemToAdd, amountToAdd, CropQuality.Normal);
    }

    public int AddItem(
        ItemData itemToAdd,
        int amountToAdd,
        CropQuality qualityToAdd
    )
    {
        if (itemToAdd == null || amountToAdd <= 0)
        {
            return amountToAdd;
        }

        if (!IsEmpty && (itemData != itemToAdd || quality != qualityToAdd))
        {
            return amountToAdd;
        }

        if (IsEmpty)
        {
            itemData = itemToAdd;
            amount = 0;
            quality = qualityToAdd;
        }

        int maxStack = Mathf.Max(1, itemData.maxStack);
        int freeSpace = maxStack - amount;
        int addedAmount = Mathf.Min(freeSpace, amountToAdd);

        amount += addedAmount;
        RefreshUI();

        return amountToAdd - addedAmount;
    }

    public bool RemoveOne()
    {
        if (amount <= 0)
        {
            return false;
        }

        amount--;

        if (amount <= 0)
        {
            itemData = null;
            quality = CropQuality.Normal;
        }

        RefreshUI();
        return true;
    }

    public int RemoveItems(int amountToRemove)
    {
        int removed = Mathf.Min(Mathf.Max(0, amountToRemove), amount);
        amount -= removed;

        if (amount == 0)
        {
            itemData = null;
            quality = CropQuality.Normal;
        }

        RefreshUI();
        return removed;
    }

    public bool WasDraggedFrom(Transform slot)
    {
        return originalParent == slot;
    }

    public void ClearItem()
    {
        itemData = null;
        amount = 0;
        quality = CropQuality.Normal;
        RefreshUI();
    }

    private void RefreshUI()
    {
        bool hasItem = itemData != null && amount > 0;

        itemIcon.sprite = hasItem ? itemData.icon : null;
        itemIcon.enabled = hasItem;
        countText.text = hasItem ? amount.ToString() : "";
        Sprite badgeSprite = hasItem
            ? CropQualityUtility.LoadDefaultIcon(quality)
            : null;
        qualityBadge.sprite = badgeSprite;
        qualityBadge.enabled = badgeSprite != null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty || rootCanvas == null)
        {
            return;
        }

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPosition =
            ((RectTransform)transform).anchoredPosition;

        transform.SetParent(rootCanvas.transform);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (originalParent == null)
        {
            return;
        }

        canvasGroup.blocksRaycasts = true;
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);

        RectTransform itemRect = GetComponent<RectTransform>();
        itemRect.anchoredPosition = originalAnchoredPosition;
    }
}
