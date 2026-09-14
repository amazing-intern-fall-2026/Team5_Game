using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HotbarSlotUI : MonoBehaviour,
    IDropHandler,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Hotbar settings")]
    [SerializeField] private int hotbarNumber;
    [SerializeField] private PlayerFarmToolController playerToolController;

    private ItemData currentItem;
    private int amount;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private GameObject dragPreview;

    public ItemData CurrentItem => currentItem;
    public int Amount => amount;
    public bool IsEmpty => currentItem == null || amount <= 0;

    private void Awake()
    {
        hotbarNumber = transform.GetSiblingIndex() + 1;
        EnsureOwnedUIReferences();

        if (playerToolController == null)
        {
            playerToolController =
                FindFirstObjectByType<PlayerFarmToolController>();
        }

        rootCanvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        RefreshUI();
    }

    private void EnsureOwnedUIReferences()
    {
        bool ownsIcon = itemIcon != null
            && itemIcon.transform.IsChildOf(transform);

        if (!ownsIcon)
        {
            GameObject iconObject = new GameObject(
                "ItemIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            iconObject.transform.SetParent(transform, false);
            itemIcon = iconObject.GetComponent<Image>();

            RectTransform iconRect = itemIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(44f, 44f);
        }

        itemIcon.preserveAspect = true;
        itemIcon.raycastTarget = false;

        bool ownsCountText = countText != null
            && countText.transform.IsChildOf(transform);

        if (!ownsCountText)
        {
            if (countText != null)
            {
                countText.text = string.Empty;
            }

            GameObject countObject = new GameObject(
                "CountText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );
            countObject.transform.SetParent(transform, false);
            countText = countObject.GetComponent<TextMeshProUGUI>();

            RectTransform countRect = countText.rectTransform;
            countRect.anchorMin = new Vector2(1f, 0f);
            countRect.anchorMax = new Vector2(1f, 0f);
            countRect.pivot = new Vector2(1f, 0f);
            countRect.anchoredPosition = new Vector2(-3f, 3f);
            countRect.sizeDelta = new Vector2(30f, 24f);
            countText.fontSize = 18f;
            countText.fontStyle = FontStyles.Bold;
            countText.alignment = TextAlignmentOptions.BottomRight;
        }

        countText.raycastTarget = false;

        Image slotHitArea = GetComponent<Image>();
        if (slotHitArea != null)
        {
            slotHitArea.raycastTarget = true;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (WasThisSlotPressed())
        {
            SelectThisSlot();
        }
    }

    public int AddItem(ItemData itemToAdd, int amountToAdd)
    {
        if (itemToAdd == null || amountToAdd <= 0)
        {
            return amountToAdd;
        }

        if (!IsEmpty && currentItem != itemToAdd)
        {
            return amountToAdd;
        }

        if (IsEmpty)
        {
            currentItem = itemToAdd;
            amount = 0;
        }

        int maxStack = Mathf.Max(1, currentItem.maxStack);
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
            currentItem = null;
        }

        RefreshUI();
        return true;
    }

    public void ClearItem()
    {
        currentItem = null;
        amount = 0;
        RefreshUI();
    }

    private bool WasThisSlotPressed()
    {
        switch (hotbarNumber)
        {
            case 1: return Keyboard.current.digit1Key.wasPressedThisFrame;
            case 2: return Keyboard.current.digit2Key.wasPressedThisFrame;
            case 3: return Keyboard.current.digit3Key.wasPressedThisFrame;
            case 4: return Keyboard.current.digit4Key.wasPressedThisFrame;
            case 5: return Keyboard.current.digit5Key.wasPressedThisFrame;
            case 6: return Keyboard.current.digit6Key.wasPressedThisFrame;
            case 7: return Keyboard.current.digit7Key.wasPressedThisFrame;
            case 8: return Keyboard.current.digit8Key.wasPressedThisFrame;
            default: return false;
        }
    }

    public void SelectThisSlot()
    {
        HotbarSelector hotbarSelector =
            FindFirstObjectByType<HotbarSelector>();

        if (hotbarSelector != null)
        {
            hotbarSelector.SelectSlot(GetComponent<RectTransform>());
        }

        if (playerToolController == null)
        {
            Debug.LogWarning(
                "Không tìm thấy PlayerFarmToolController trên Player."
            );
            return;
        }

        if (IsEmpty)
        {
            playerToolController.SetSelectedHotbarSlot(null);

            Debug.Log(
                "Hotbar Slot " + hotbarNumber + " đang trống."
            );

            return;
        }

        playerToolController.SetSelectedHotbarSlot(this);

        Debug.Log(
            "Đã chọn Hotbar Slot: " + hotbarNumber
        );
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        InventoryItemUI draggedItem =
            eventData.pointerDrag.GetComponentInParent<InventoryItemUI>();

        if (draggedItem == null)
        {
            return;
        }

        if (draggedItem.IsEmpty)
        {
            Debug.LogWarning("Item Inventory đang trống. Không thể chuyển vào Hotbar.");
            return;
        }

        int remaining = AddItem(draggedItem.ItemData, 1);

        if (remaining == 0)
        {
            draggedItem.RemoveOne();
            Debug.Log("Đã chuyển 1 item từ Inventory vào Hotbar.");
        }
        else
        {
            Debug.LogWarning(
                "Hotbar Slot chứa item khác loại hoặc đã đầy. Item được giữ nguyên."
            );
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectThisSlot();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty || rootCanvas == null)
        {
            return;
        }

        canvasGroup.blocksRaycasts = false;
        CreateDragPreview(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragPreview == null)
        {
            return;
        }

        dragPreview.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }
    }

    private void CreateDragPreview(Vector2 screenPosition)
    {
        dragPreview = new GameObject(
            "Hotbar Drag Preview",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );
        dragPreview.transform.SetParent(rootCanvas.transform, false);
        dragPreview.transform.SetAsLastSibling();

        Image previewImage = dragPreview.GetComponent<Image>();
        previewImage.sprite = currentItem.icon;
        previewImage.color = Color.white;
        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;

        CanvasGroup previewGroup = dragPreview.GetComponent<CanvasGroup>();
        previewGroup.blocksRaycasts = false;
        previewGroup.interactable = false;

        RectTransform previewRect = dragPreview.GetComponent<RectTransform>();
        Vector2 previewSize = itemIcon.rectTransform.rect.size;
        if (previewSize.x <= 0f || previewSize.y <= 0f)
        {
            previewSize = new Vector2(56f, 56f);
        }

        previewRect.sizeDelta = previewSize;
        previewRect.position = screenPosition;
    }

    private void RefreshUI()
    {
        bool hasItem = currentItem != null && amount > 0;

        if (hasItem)
        {
            itemIcon.sprite = currentItem.icon;
            itemIcon.color = Color.white;
            itemIcon.enabled = true;
            countText.text = amount.ToString();
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            countText.text = "";
        }
    }
}
