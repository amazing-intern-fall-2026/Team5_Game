using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotbarSlotUI : MonoBehaviour, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;

    private ItemData currentItem;
    private int amount;

    private void Awake()
    {
        RefreshUI();
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItemUI draggedItem =
            eventData.pointerDrag.GetComponent<InventoryItemUI>();

        if (draggedItem == null || draggedItem.Amount <= 0)
            return;

        bool slotIsEmpty = currentItem == null;
        bool isSameItem = currentItem == draggedItem.ItemData;

        // Không cho đặt item khác loại vào slot đang có item.
        if (!slotIsEmpty && !isSameItem)
            return;

        // Slot trống: nhận loại item đang kéo.
        if (slotIsEmpty)
        {
            currentItem = draggedItem.ItemData;
        }

        // Chỉ trừ 1 item ở Inventory nếu thêm được vào Hotbar.
        if (draggedItem.RemoveOne())
        {
            amount++;
            RefreshUI();
        }
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