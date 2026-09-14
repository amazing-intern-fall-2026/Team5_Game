using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FarmTileController : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap farmSoilTilemap;

    [Header("Soil states")]
    [SerializeField] private TileBase tilledTile;
    [SerializeField] private TileBase wateredTile;

    [Header("Crop visuals")]
    [SerializeField] private Transform cropParent;
    [SerializeField] private int cropSortingOrder = 3;
    [SerializeField] private float cropVisualScale = 0.1f;

    [Header("Harvest")]
    [SerializeField] private ItemStorageController itemStorageController;

    private readonly Dictionary<Vector3Int, CropPlant> plantedCrops =
        new Dictionary<Vector3Int, CropPlant>();

    /// <summary>
    /// Converts a world point to the matching ground cell.  Keeping this
    /// conversion here ensures the target preview and farm actions always use
    /// exactly the same tile.
    /// </summary>
    public bool TryGetGroundCell(
        Vector3 worldPosition,
        out Vector3Int cellPosition
    )
    {
        cellPosition = Vector3Int.zero;

        if (groundTilemap == null)
        {
            return false;
        }

        cellPosition = groundTilemap.WorldToCell(worldPosition);
        return groundTilemap.GetTile(cellPosition) != null;
    }

    public Vector3 GetCellCenterWorld(Vector3Int cellPosition)
    {
        return groundTilemap.GetCellCenterWorld(cellPosition);
    }

    public bool TryGetAdjacentGroundCell(
        Vector3 originWorldPosition,
        Vector2 direction,
        out Vector3Int targetCell
    )
    {
        targetCell = Vector3Int.zero;

        if (groundTilemap == null)
        {
            return false;
        }

        Vector3Int originCell = groundTilemap.WorldToCell(originWorldPosition);
        targetCell = originCell + new Vector3Int(
            Mathf.RoundToInt(direction.x),
            Mathf.RoundToInt(direction.y),
            0
        );

        return groundTilemap.GetTile(targetCell) != null;
    }

    public Vector3 GetCellSize()
    {
        return groundTilemap.layoutGrid.cellSize;
    }

    public bool HoeAtWorldPosition(Vector3 worldPosition)
    {
        if (!TryGetGroundCell(worldPosition, out Vector3Int cellPosition))
        {
            return false;
        }

        if (farmSoilTilemap.GetTile(cellPosition) != null)
        {
            return false;
        }

        farmSoilTilemap.SetTile(cellPosition, tilledTile);
        return true;
    }

    public bool WaterAtWorldPosition(Vector3 worldPosition)
    {
        Vector3Int cellPosition =
            groundTilemap.WorldToCell(worldPosition);

        TileBase currentSoil =
            farmSoilTilemap.GetTile(cellPosition);

        if (currentSoil != tilledTile)
        {
            return false;
        }

        farmSoilTilemap.SetTile(cellPosition, wateredTile);
        return true;
    }

    public bool TryPlantAtWorldPosition(
        Vector3 worldPosition,
        CropData cropData
    )
    {
        if (cropData == null)
        {
            return false;
        }

        Vector3Int cellPosition =
            groundTilemap.WorldToCell(worldPosition);

        TileBase currentSoil =
            farmSoilTilemap.GetTile(cellPosition);

        // Chỉ trồng trên đất đã tưới.
        if (currentSoil != wateredTile)
        {
            return false;
        }

        // Một ô chỉ có một cây.
        if (plantedCrops.ContainsKey(cellPosition))
        {
            return false;
        }

        GameObject cropObject =
            new GameObject(cropData.cropName);

        Transform parent = cropParent != null
            ? cropParent
            : transform;

        cropObject.transform.SetParent(parent);
        cropObject.transform.position =
            groundTilemap.GetCellCenterWorld(cellPosition);

        cropObject.transform.localScale =
            Vector3.one * cropVisualScale;

        CropPlant cropPlant =
            cropObject.AddComponent<CropPlant>();

        cropPlant.Initialize(cropData, cropSortingOrder);

        plantedCrops.Add(cellPosition, cropPlant);

        return true;
    }

    public bool TryHarvestAtWorldPosition(Vector3 worldPosition)
    {
        Vector3Int cellPosition =
            groundTilemap.WorldToCell(worldPosition);

        if (!plantedCrops.TryGetValue(
            cellPosition,
            out CropPlant cropPlant
        ))
        {
            return false;
        }

        if (!cropPlant.TryHarvest(
            out ItemData harvestItem,
            out int harvestAmount
        ))
        {
            return false;
        }

        if (itemStorageController == null)
        {
            Debug.LogWarning(
                "Chưa gán Item Storage Controller."
            );
            return false;
        }

        if (!itemStorageController.TryAddItem(
            harvestItem,
            harvestAmount
        ))
        {
            return false;
        }

        plantedCrops.Remove(cellPosition);
        Destroy(cropPlant.gameObject);

        Debug.Log(
            $"Thu hoạch thành công {harvestAmount} {harvestItem.itemName}; "
            + "đã ưu tiên xếp vào Hotbar rồi Inventory."
        );

        return true;
    }

    public FertilizerApplicationResult ApplyFertilizerAtWorldPosition(
        Vector3 worldPosition,
        ItemData fertilizerItem,
        out string cropName,
        out float reducedSeconds
    )
    {
        cropName = string.Empty;
        reducedSeconds = 0f;

        if (!TryGetGroundCell(worldPosition, out Vector3Int cellPosition)
            || !plantedCrops.TryGetValue(cellPosition, out CropPlant cropPlant)
            || cropPlant == null)
        {
            return FertilizerApplicationResult.NoCrop;
        }

        cropName = cropPlant.CropName;
        return cropPlant.TryApplyFertilizer(
            fertilizerItem,
            out reducedSeconds
        );
    }
}
