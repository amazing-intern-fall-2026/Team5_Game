using System;
using UnityEngine;

public class ItemStorageController : MonoBehaviour
{
    [Header("UI references")]
    [SerializeField] private Transform hotbarSlotGrid;
    [SerializeField] private Transform inventorySlotGrid;

    [Header("Prefab")]
    [SerializeField] private InventoryItemUI inventoryItemPrefab;

    [Header("Starting hotbar items")]
    [SerializeField] private ItemData[] startingHotbarItems;
    [SerializeField, Min(1)] private int startingItemAmount = 10;

    private HotbarSlotUI[] hotbarSlots;

    private void Awake()
    {
        hotbarSlots =
            hotbarSlotGrid.GetComponentsInChildren<HotbarSlotUI>(true);

        Array.Sort(
            hotbarSlots,
            (first, second) =>
                first.transform.GetSiblingIndex()
                .CompareTo(second.transform.GetSiblingIndex())
        );
    }

    private void Start()
    {
        if (startingHotbarItems == null)
        {
            return;
        }

        foreach (ItemData startingItem in startingHotbarItems)
        {
            if (startingItem != null)
            {
                TryAddItem(startingItem, startingItemAmount);
            }
        }
    }

    public bool TryAddItem(ItemData itemToAdd, int amountToAdd)
    {
        return TryAddItem(itemToAdd, amountToAdd, CropQuality.Normal);
    }

    public bool TryAddItem(
        ItemData itemToAdd,
        int amountToAdd,
        CropQuality quality
    )
    {
        if (itemToAdd == null || amountToAdd <= 0)
        {
            return false;
        }

        // Keep additions atomic. This prevents a crop from giving a partial
        // harvest and then being harvested again when the storage is full.
        if (!CanStoreItem(itemToAdd, amountToAdd, quality))
        {
            Debug.LogWarning(
                "Hotbar và Inventory không đủ chỗ để nhận: "
                + itemToAdd.itemName
            );
            return false;
        }

        int remaining = amountToAdd;

        // Ưu tiên Hotbar theo thứ tự 1 đến 8.
        foreach (HotbarSlotUI hotbarSlot in hotbarSlots)
        {
            remaining = hotbarSlot.AddItem(itemToAdd, remaining, quality);

            if (remaining <= 0)
            {
                return true;
            }
        }

        // Sau Hotbar mới đến 20 ô Inventory.
        for (int i = 0; i < inventorySlotGrid.childCount; i++)
        {
            Transform inventorySlot = inventorySlotGrid.GetChild(i);

            InventoryItemUI inventoryItem =
                inventorySlot.GetComponentInChildren<InventoryItemUI>(true);

            if (inventoryItem == null)
            {
                inventoryItem = Instantiate(
                    inventoryItemPrefab,
                    inventorySlot
                );

                RectTransform itemRect =
                    inventoryItem.GetComponent<RectTransform>();

                itemRect.anchoredPosition = Vector2.zero;
                itemRect.localScale = Vector3.one;
            }

            remaining = inventoryItem.AddItem(itemToAdd, remaining, quality);

            if (remaining <= 0)
            {
                return true;
            }
        }

        Debug.LogWarning(
            "Hotbar và Inventory đều đầy. Không thể nhận: "
            + itemToAdd.itemName
        );

        return false;
    }

    public bool CanStoreItem(ItemData item, int amount)
    {
        return CanStoreItem(item, amount, CropQuality.Normal);
    }

    public bool CanStoreItem(
        ItemData item,
        int amount,
        CropQuality quality
    )
    {
        if (item == null || amount <= 0)
        {
            return false;
        }

        int availableCapacity = 0;
        int maxStack = Mathf.Max(1, item.maxStack);

        foreach (HotbarSlotUI slot in hotbarSlots)
        {
            if (slot.IsEmpty)
            {
                availableCapacity += maxStack;
            }
            else if (slot.CurrentItem == item && slot.Quality == quality)
            {
                availableCapacity += Mathf.Max(0, maxStack - slot.Amount);
            }

            if (availableCapacity >= amount)
            {
                return true;
            }
        }

        for (int i = 0; i < inventorySlotGrid.childCount; i++)
        {
            InventoryItemUI inventoryItem = inventorySlotGrid.GetChild(i)
                .GetComponentInChildren<InventoryItemUI>(true);

            if (inventoryItem == null || inventoryItem.IsEmpty)
            {
                availableCapacity += maxStack;
            }
            else if (inventoryItem.ItemData == item
                && inventoryItem.Quality == quality)
            {
                availableCapacity += Mathf.Max(
                    0,
                    maxStack - inventoryItem.Amount
                );
            }

            if (availableCapacity >= amount)
            {
                return true;
            }
        }

        return false;
    }

    public int CountItem(ItemData item)
    {
        if (item == null)
        {
            return 0;
        }

        int total = 0;

        foreach (HotbarSlotUI slot in hotbarSlots)
        {
            if (!slot.IsEmpty && slot.CurrentItem == item)
            {
                total += slot.Amount;
            }
        }

        foreach (InventoryItemUI inventoryItem in
            inventorySlotGrid.GetComponentsInChildren<InventoryItemUI>(true))
        {
            if (!inventoryItem.IsEmpty && inventoryItem.ItemData == item)
            {
                total += inventoryItem.Amount;
            }
        }

        return total;
    }

    public int CountItem(ItemData item, CropQuality quality)
    {
        if (item == null)
        {
            return 0;
        }

        int total = 0;
        foreach (HotbarSlotUI slot in hotbarSlots)
        {
            if (!slot.IsEmpty
                && slot.CurrentItem == item
                && slot.Quality == quality)
            {
                total += slot.Amount;
            }
        }

        foreach (InventoryItemUI inventoryItem in
            inventorySlotGrid.GetComponentsInChildren<InventoryItemUI>(true))
        {
            if (!inventoryItem.IsEmpty
                && inventoryItem.ItemData == item
                && inventoryItem.Quality == quality)
            {
                total += inventoryItem.Amount;
            }
        }

        return total;
    }

    public bool TryRemoveItem(
        ItemData item,
        CropQuality quality,
        int amountToRemove
    )
    {
        if (item == null || amountToRemove <= 0
            || CountItem(item, quality) < amountToRemove)
        {
            return false;
        }

        int remaining = amountToRemove;
        foreach (HotbarSlotUI slot in hotbarSlots)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (!slot.IsEmpty
                && slot.CurrentItem == item
                && slot.Quality == quality)
            {
                remaining -= slot.RemoveItems(remaining);
            }
        }

        foreach (InventoryItemUI inventoryItem in
            inventorySlotGrid.GetComponentsInChildren<InventoryItemUI>(true))
        {
            if (remaining <= 0)
            {
                break;
            }

            if (!inventoryItem.IsEmpty
                && inventoryItem.ItemData == item
                && inventoryItem.Quality == quality)
            {
                remaining -= inventoryItem.RemoveItems(remaining);
            }
        }

        return remaining == 0;
    }

    public bool TryRemoveItem(ItemData item, int amountToRemove)
    {
        if (item == null || amountToRemove <= 0
            || CountItem(item) < amountToRemove)
        {
            return false;
        }

        int remaining = amountToRemove;

        foreach (HotbarSlotUI slot in hotbarSlots)
        {
            while (remaining > 0
                && !slot.IsEmpty
                && slot.CurrentItem == item)
            {
                slot.RemoveOne();
                remaining--;
            }
        }

        foreach (InventoryItemUI inventoryItem in
            inventorySlotGrid.GetComponentsInChildren<InventoryItemUI>(true))
        {
            while (remaining > 0
                && !inventoryItem.IsEmpty
                && inventoryItem.ItemData == item)
            {
                inventoryItem.RemoveOne();
                remaining--;
            }
        }

        return remaining == 0;
    }

    public void ClearAllItems()
    {
        foreach (HotbarSlotUI slot in hotbarSlots)
        {
            slot.ClearItem();
        }

        foreach (InventoryItemUI inventoryItem in
            inventorySlotGrid.GetComponentsInChildren<InventoryItemUI>(true))
        {
            inventoryItem.ClearItem();
        }

        Debug.Log("[Trainer] Đã xóa toàn bộ item khỏi Hotbar và Inventory.");
    }
}
