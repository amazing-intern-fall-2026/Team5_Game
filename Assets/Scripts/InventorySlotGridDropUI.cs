using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotGridDropUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private InventoryItemUI inventoryItemPrefab;

    private void Awake()
    {
        // Give every slot its own drop receiver. This avoids depending on the
        // GraphicRaycaster choosing a particular icon/background child.
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform slot = transform.GetChild(i);
            EnsureSlotRaycastTarget(slot);

            InventorySlotDropTargetUI dropTarget =
                slot.GetComponent<InventorySlotDropTargetUI>();

            if (dropTarget == null)
            {
                dropTarget = slot.gameObject.AddComponent<InventorySlotDropTargetUI>();
            }

            dropTarget.Initialize(this, slot);
        }
    }

    private static void EnsureSlotRaycastTarget(Transform slot)
    {
        // Empty slots in the prefab have their Image disabled. An IDropHandler
        // alone cannot receive EventSystem raycasts, so drops on an empty slot
        // never reached HandleDrop. Keep the hit area transparent while making
        // it a valid GraphicRaycaster target.
        Image hitArea = slot.GetComponent<Image>();

        if (hitArea == null)
        {
            hitArea = slot.gameObject.AddComponent<Image>();
        }

        hitArea.enabled = true;
        hitArea.sprite = null;
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Transform inventorySlot = FindInventorySlotAtPointer(eventData);

        if (inventorySlot == null)
        {
            Debug.LogWarning("Thả item ngoài ô Inventory hợp lệ. Item được giữ nguyên.");
            return;
        }

        HandleDrop(eventData, inventorySlot);
    }

    public void HandleDrop(PointerEventData eventData, Transform inventorySlot)
    {
        if (eventData == null || eventData.pointerDrag == null)
        {
            return;
        }

        HotbarSlotUI hotbarSource =
            eventData.pointerDrag.GetComponentInParent<HotbarSlotUI>();
        InventoryItemUI inventorySource =
            eventData.pointerDrag.GetComponentInParent<InventoryItemUI>();

        if (hotbarSource == null && inventorySource == null)
        {
            return;
        }

        if (inventorySource != null && inventorySource.WasDraggedFrom(inventorySlot))
        {
            return;
        }

        InventoryItemUI inventoryItem =
            inventorySlot.GetComponentInChildren<InventoryItemUI>(true);

        if (inventoryItem == null)
        {
            if (inventoryItemPrefab == null)
            {
                Debug.LogWarning("Chưa gán Inventory Item Prefab cho SlotGrid.");
                return;
            }

            inventoryItem = Instantiate(
                inventoryItemPrefab,
                inventorySlot
            );

            RectTransform itemRect =
                inventoryItem.GetComponent<RectTransform>();

            itemRect.anchoredPosition = Vector2.zero;
            itemRect.localScale = Vector3.one;
        }

        if (hotbarSource != null)
        {
            TransferFromHotbar(hotbarSource, inventoryItem);
        }
        else
        {
            TransferFromInventory(inventorySource, inventoryItem);
        }
    }

    private static void TransferFromHotbar(
        HotbarSlotUI source,
        InventoryItemUI destination
    )
    {
        if (source.IsEmpty)
        {
            return;
        }

        int remaining = destination.AddItem(
            source.CurrentItem,
            source.Amount,
            source.Quality
        );
        int transferred = source.Amount - remaining;

        if (transferred > 0)
        {
            source.RemoveItems(transferred);
        }
    }

    private static void TransferFromInventory(
        InventoryItemUI source,
        InventoryItemUI destination
    )
    {
        if (source == destination || source.IsEmpty)
        {
            return;
        }

        int remaining = destination.AddItem(
            source.ItemData,
            source.Amount,
            source.Quality
        );
        int transferred = source.Amount - remaining;

        if (transferred > 0)
        {
            source.RemoveItems(transferred);
        }
    }

    private Transform FindInventorySlotAtPointer(PointerEventData eventData)
    {
        GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;

        if (targetObject != null)
        {
            Transform slot = FindInventorySlot(targetObject.transform);

            if (slot != null)
            {
                return slot;
            }
        }

        Camera eventCamera = eventData.pressEventCamera;

        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform slotRect = transform.GetChild(i) as RectTransform;

            if (slotRect != null && RectTransformUtility.RectangleContainsScreenPoint(
                    slotRect,
                    eventData.position,
                    eventCamera
                ))
            {
                return slotRect;
            }
        }

        return null;
    }

    private Transform FindInventorySlot(Transform target)
    {
        Transform current = target;

        while (current != null && current.parent != transform)
        {
            current = current.parent;
        }

        if (current != null && current.parent == transform)
        {
            return current;
        }

        return null;
    }
}
