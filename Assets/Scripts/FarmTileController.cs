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

    public bool HoeAtWorldPosition(Vector3 worldPosition)
    {
        Vector3Int cellPosition =
            groundTilemap.WorldToCell(worldPosition);

        // Không cho cuốc ra ngoài vùng nền.
        if (groundTilemap.GetTile(cellPosition) == null)
            return false;

        // Đã có đất canh tác thì không cuốc lần nữa.
        if (farmSoilTilemap.GetTile(cellPosition) != null)
            return false;

        farmSoilTilemap.SetTile(cellPosition, tilledTile);
        return true;
    }

    public bool WaterAtWorldPosition(Vector3 worldPosition)
    {
        Vector3Int cellPosition =
            groundTilemap.WorldToCell(worldPosition);

        TileBase currentSoil =
            farmSoilTilemap.GetTile(cellPosition);

        // Chỉ tưới ô đã cuốc.
        if (currentSoil != tilledTile)
            return false;

        farmSoilTilemap.SetTile(cellPosition, wateredTile);
        return true;
    }
}