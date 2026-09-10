using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Item data")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount;

    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalAnchoredPosition;

    public ItemData ItemData => itemData;
    public int Amount => amount;
    public Sprite IconSprite => itemData != null ? itemData.icon : null;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        RefreshUI();
    }

    public bool RemoveOne()
    {
        if (amount <= 0)
            return false;

        amount--;
        RefreshUI();
        return true;
    }

    private void RefreshUI()
    {
        bool hasItem = itemData != null && amount > 0;

        itemIcon.sprite = itemData != null ? itemData.icon : null;
        itemIcon.enabled = hasItem;
        countText.text = hasItem ? amount.ToString() : "";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (amount <= 0 || itemData == null)
            return;

        originalParent = transform.parent;
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
        canvasGroup.blocksRaycasts = true;
        transform.SetParent(originalParent);

        RectTransform itemRect = GetComponent<RectTransform>();
        itemRect.anchoredPosition = originalAnchoredPosition;
    }
}