using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFarmToolController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FarmTileController farmTileController;
    [SerializeField] private FarmActionSFX farmActionSFX;

    private Animator animator;
    private Vector2 facingDirection = Vector2.down;
    private HotbarSlotUI selectedHotbarSlot;
    private TargetTileIndicator targetTileIndicator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        targetTileIndicator = GetComponent<TargetTileIndicator>();

        if (targetTileIndicator == null)
        {
            targetTileIndicator = gameObject.AddComponent<TargetTileIndicator>();
        }

        targetTileIndicator.Initialize(farmTileController);

        if (farmActionSFX == null)
        {
            farmActionSFX = GetComponent<FarmActionSFX>();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        UpdateFacingDirection();

        if (PixelFarmShopController.IsOpen)
        {
            if (targetTileIndicator != null)
            {
                targetTileIndicator.SetTarget(false, transform.position);
            }

            return;
        }

        UpdateTargetTile();
        UseSelectedItem();
        HarvestCrop();
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

    private int GetAnimatorDirection()
    {
        if (facingDirection == Vector2.down)
            return 0;

        if (facingDirection == Vector2.up)
            return 1;

        return 2; // Right / Left
    }

    private void PlayHoeAnimation()
    {
        if (animator == null)
            return;

        animator.SetInteger(
            "Direction",
            GetAnimatorDirection()
        );

        animator.ResetTrigger("Water");
        animator.SetTrigger("Hoe");
    }

    private void PlayWaterAnimation()
    {
        if (animator == null)
            return;

        animator.SetInteger(
            "Direction",
            GetAnimatorDirection()
        );

        animator.ResetTrigger("Hoe");
        animator.SetTrigger("Water");
    }

    public void SetSelectedHotbarSlot(HotbarSlotUI hotbarSlot)
    {
        selectedHotbarSlot = hotbarSlot;

        if (selectedHotbarSlot != null
            && !selectedHotbarSlot.IsEmpty)
        {
            Debug.Log(
                "Đã chọn: "
                + selectedHotbarSlot.CurrentItem.itemName
            );
        }
    }

    private void UpdateTargetTile()
    {
        if (targetTileIndicator == null)
        {
            return;
        }

        bool canTarget = selectedHotbarSlot != null
            && !selectedHotbarSlot.IsEmpty
            && IsFarmActionItem(selectedHotbarSlot.CurrentItem);

        Vector3Int targetCell = Vector3Int.zero;
        bool hasTarget = farmTileController != null
            && farmTileController.TryGetAdjacentGroundCell(
                transform.position,
                facingDirection,
                out targetCell
            );

        targetTileIndicator.SetTarget(
            canTarget && hasTarget,
            hasTarget
                ? farmTileController.GetCellCenterWorld(targetCell)
                : transform.position
        );
    }

    private static bool IsFarmActionItem(ItemData item)
    {
        return item != null
            && (item.canHoe
                || item.canWater
                || item.cropToPlant != null
                || item.fertilizerType != FertilizerType.None);
    }

    private void UseSelectedItem()
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

        if (selectedHotbarSlot == null
            || selectedHotbarSlot.IsEmpty)
        {
            Debug.Log("Hãy chọn item trong Hotbar.");
            return;
        }

        ItemData selectedItem =
            selectedHotbarSlot.CurrentItem;

        if (!TryGetTargetPosition(out Vector3 targetPosition))
        {
            if (selectedItem.fertilizerType != FertilizerType.None)
            {
                Debug.Log("Không có cây đang trồng ở ô phía trước.");
            }

            return;
        }

        if (selectedItem.canHoe)
        {
            PlayHoeAnimation();
            if (farmTileController.HoeAtWorldPosition(targetPosition))
            {
                farmActionSFX?.PlayTill();
            }
        }
        else if (selectedItem.canWater)
        {
            PlayWaterAnimation();
            if (farmTileController.WaterAtWorldPosition(targetPosition))
            {
                farmActionSFX?.PlayWater();
            }
        }
        else if (selectedItem.cropToPlant != null)
        {
            bool planted = farmTileController.TryPlantAtWorldPosition(
                targetPosition,
                selectedItem.cropToPlant
            );

            if (planted)
            {
                selectedHotbarSlot.RemoveOne();
                farmActionSFX?.PlayPlant();
            }
        }
        else if (selectedItem.fertilizerType != FertilizerType.None)
        {
            ApplyFertilizer(selectedItem, targetPosition);
        }
        else
        {
            Debug.Log(
                selectedItem.itemName
                + " không thể sử dụng ở nông trại."
            );
        }
    }

    private void HarvestCrop()
    {
        if (!Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

        if (farmTileController == null)
        {
            return;
        }

        if (!TryGetTargetPosition(out Vector3 targetPosition))
        {
            return;
        }

        if (farmTileController.TryHarvestAtWorldPosition(targetPosition))
        {
            farmActionSFX?.PlayHarvest();
        }
    }

    private void ApplyFertilizer(ItemData selectedItem, Vector3 targetPosition)
    {
        FertilizerApplicationResult result =
            farmTileController.ApplyFertilizerAtWorldPosition(
                targetPosition,
                selectedItem,
                out string cropName,
                out float reducedSeconds
            );

        switch (result)
        {
            case FertilizerApplicationResult.Success:
                selectedHotbarSlot.RemoveOne();
                farmActionSFX?.PlayFertilize();
                string reductionText = selectedItem.fertilizerTestMode
                    ? $"{reducedSeconds:0.##} giây"
                    : $"{reducedSeconds / 60f:0.#} phút";
                Debug.Log(
                    $"Bón {selectedItem.itemName} cho {cropName} thành công. "
                    + $"Giảm {reductionText} phát triển."
                );
                break;

            case FertilizerApplicationResult.NoCrop:
                Debug.Log("Không có cây đang trồng ở ô phía trước.");
                break;

            case FertilizerApplicationResult.WrongType:
                Debug.Log(
                    $"Không thể bón {selectedItem.itemName} cho {cropName}: sai loại phân."
                );
                break;

            case FertilizerApplicationResult.ApplicationLimitReached:
                Debug.Log(
                    $"Không thể bón thêm cho {cropName}: cây đã đạt số lần bón tối đa."
                );
                break;

            case FertilizerApplicationResult.CropMature:
                Debug.Log(
                    $"Không thể bón {selectedItem.itemName}: {cropName} đã trưởng thành."
                );
                break;

            case FertilizerApplicationResult.OutOfSeason:
                Debug.Log(
                    $"Không thể bón phân cho {cropName}: cây đang tạm dừng vì trái mùa."
                );
                break;
        }
    }

    private bool TryGetTargetPosition(out Vector3 targetPosition)
    {
        targetPosition = transform.position;

        if (farmTileController == null
            || !farmTileController.TryGetAdjacentGroundCell(
                transform.position,
                facingDirection,
                out Vector3Int targetCell
            ))
        {
            return false;
        }

        targetPosition = farmTileController.GetCellCenterWorld(targetCell);
        return true;
    }
}