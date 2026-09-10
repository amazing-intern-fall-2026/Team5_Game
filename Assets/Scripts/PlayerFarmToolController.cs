using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFarmToolController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FarmTileController farmTileController;

    [Header("Tool settings")]
    [SerializeField] private float toolReach = 0.6f;

    private Vector2 facingDirection = Vector2.down;
    private FarmTool selectedTool = FarmTool.Hoe;

    private enum FarmTool
    {
        Hoe,
        WateringCan
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        UpdateFacingDirection();
        SelectTool();
        UseTool();
    }

    private void UpdateFacingDirection()
    {
        if (Keyboard.current.wKey.isPressed)
        {
            facingDirection = Vector2.up;
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            facingDirection = Vector2.down;
        }
        else if (Keyboard.current.aKey.isPressed)
        {
            facingDirection = Vector2.left;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            facingDirection = Vector2.right;
        }
    }

    private void SelectTool()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            selectedTool = FarmTool.Hoe;
            Debug.Log("Đã chọn: Cuốc");
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            selectedTool = FarmTool.WateringCan;
            Debug.Log("Đã chọn: Bình tưới");
        }
    }

    private void UseTool()
    {
        if (!Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        if (farmTileController == null)
        {
            Debug.LogWarning("Chưa gán Farm Tile Controller.");
            return;
        }

        Vector3 targetPosition =
            transform.position + (Vector3)(facingDirection * toolReach);

        if (selectedTool == FarmTool.Hoe)
        {
            farmTileController.HoeAtWorldPosition(targetPosition);
        }
        else
        {
            farmTileController.WaterAtWorldPosition(targetPosition);
        }
    }
}