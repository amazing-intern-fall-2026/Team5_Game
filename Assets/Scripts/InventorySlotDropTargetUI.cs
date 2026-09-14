using UnityEngine;
using UnityEngine.EventSystems;

// Added at runtime to each Inventory slot by InventorySlotGridDropUI.
// Keeping the receiver on the slot makes drops reliable regardless of which
// child Image/Text is under the mouse.
public sealed class InventorySlotDropTargetUI : MonoBehaviour, IDropHandler
{
    private InventorySlotGridDropUI owner;
    private Transform inventorySlot;

    public void Initialize(
        InventorySlotGridDropUI dropOwner,
        Transform slot
    )
    {
        owner = dropOwner;
        inventorySlot = slot;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (owner != null && inventorySlot != null)
        {
            owner.HandleDrop(eventData, inventorySlot);
        }
    }
}
