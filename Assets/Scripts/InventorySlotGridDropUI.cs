using UnityEngine;
using UnityEngine.EventSystems;

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
            InventorySlotDropTargetUI dropTarget =
                slot.GetComponent<InventorySlotDropTargetUI>();

            if (dropTarget == null)
            {
                dropTarget = slot.gameObject.AddComponent<InventorySlotDropTargetUI>();
            }

            dropTarget.Initialize(this, slot);
        }
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

        HotbarSlotUI hotbarItem =
            eventData.pointerDrag.GetComponentInParent<HotbarSlotUI>();

        if (hotbarItem == null)
        {
            return;
        }

        if (hotbarItem.IsEmpty)
        {
            Debug.LogWarning("Hotbar Slot đang trống. Không có item để chuyển.");
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

        int remaining = inventoryItem.AddItem(
            hotbarItem.CurrentItem,
            1
        );

        if (remaining == 0)
        {
            hotbarItem.RemoveOne();
            Debug.Log("Đã chuyển 1 item từ Hotbar vào Inventory.");
        }
        else
        {
            Debug.LogWarning(
                "Ô Inventory chứa item khác loại hoặc đã đầy. Item được giữ nguyên."
            );
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
