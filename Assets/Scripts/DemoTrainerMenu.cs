using UnityEngine;
using UnityEngine.InputSystem;

public class DemoTrainerMenu : MonoBehaviour
{
    [SerializeField] private ItemStorageController itemStorage;
    [SerializeField, Min(1)] private int stackAmount = 10;
    [SerializeField] private ItemData[] tools;
    [SerializeField] private ItemData[] seeds;
    [SerializeField] private ItemData[] fertilizers;

    private bool isOpen;
    private Vector2 scrollPosition;

    private void Awake()
    {
        if (itemStorage == null)
        {
            itemStorage = FindFirstObjectByType<ItemStorageController>();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null
            && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            isOpen = !isOpen;
        }
    }

    private void OnGUI()
    {
        if (!isOpen)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(15f, 15f, 330f, 520f), "TRAINER - F1 để đóng", GUI.skin.window);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        DrawSection("Công cụ", tools, true);
        DrawSection("Hạt giống", seeds, false);
        DrawSection("Phân bón", fertilizers, false);

        GUILayout.Space(12f);
        GUI.backgroundColor = new Color(1f, 0.45f, 0.35f);
        if (GUILayout.Button("XÓA HOTBAR + INVENTORY", GUILayout.Height(36f)))
        {
            itemStorage?.ClearAllItems();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawSection(
        string title,
        ItemData[] items,
        bool giveOneOnly
    )
    {
        GUILayout.Space(6f);
        GUILayout.Label(title);

        if (items == null)
        {
            return;
        }

        foreach (ItemData item in items)
        {
            if (item == null)
            {
                continue;
            }

            int amount = giveOneOnly ? 1 : stackAmount;
            string label = "+ " + amount + "  " + item.itemName;

            if (GUILayout.Button(label, GUILayout.Height(30f)))
            {
                GiveItem(item, amount);
            }
        }
    }

    private void GiveItem(ItemData item, int amount)
    {
        if (itemStorage == null)
        {
            Debug.LogError("[Trainer] Không tìm thấy ItemStorageController.");
            return;
        }

        if (itemStorage.TryAddItem(item, amount))
        {
            Debug.Log("[Trainer] Đã cấp " + amount + " x " + item.itemName + ".");
        }
        else
        {
            Debug.LogWarning("[Trainer] Không đủ chỗ để cấp " + item.itemName + ".");
        }
    }
}
