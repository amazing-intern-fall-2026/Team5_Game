using UnityEngine;

public class HotbarSelector : MonoBehaviour
{
    [SerializeField] private RectTransform selectionHighlight;
    [SerializeField] private RectTransform[] hotbarSlots;

    private void Start()
    {
        SelectSlot(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SelectSlot(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SelectSlot(6);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SelectSlot(7);
    }

    private void SelectSlot(int index)
    {
        if (selectionHighlight == null
            || index < 0
            || index >= hotbarSlots.Length
            || hotbarSlots[index] == null)
        {
            return;
        }

        selectionHighlight.position = hotbarSlots[index].position;
    }

    public void SelectSlot(RectTransform slot)
    {
        if (slot == null)
        {
            return;
        }

        selectionHighlight.position = slot.position;
    }
}
