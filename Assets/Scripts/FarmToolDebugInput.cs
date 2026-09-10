using UnityEngine;
using UnityEngine.InputSystem;

public class FarmToolDebugInput : MonoBehaviour
{
    private enum SelectedTool
    {
        Hoe,
        WateringCan
    }

    [SerializeField] private Camera worldCamera;
    [SerializeField] private FarmTileController farmTileController;

    [SerializeField] private SelectedTool selectedTool =
        SelectedTool.Hoe;

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            selectedTool = SelectedTool.Hoe;
            Debug.Log("Selected tool: Hoe");
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            selectedTool = SelectedTool.WateringCan;
            Debug.Log("Selected tool: Watering Can");
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            UseSelectedTool();
        }
    }

    private void UseSelectedTool()
    {
        Vector2 mouseScreenPosition =
            Mouse.current.position.ReadValue();

        Vector3 worldPosition =
            worldCamera.ScreenToWorldPoint(
                new Vector3(
                    mouseScreenPosition.x,
                    mouseScreenPosition.y,
                    -worldCamera.transform.position.z
                )
            );

        worldPosition.z = 0;

        if (selectedTool == SelectedTool.Hoe)
        {
            farmTileController.HoeAtWorldPosition(worldPosition);
        }

        if (selectedTool == SelectedTool.WateringCan)
        {
            farmTileController.WaterAtWorldPosition(worldPosition);
        }
    }
}