using UnityEngine;

/// <summary>
/// Builds a three-sided fence around the configured farm plot at runtime.
/// The top edge is deliberately left open as the entrance.
/// </summary>
public class FarmPlotFenceController : MonoBehaviour
{
    [Header("Farm plot")]
    [SerializeField] private FarmTileController farmTileController;

    [Header("Fence sprites")]
    [SerializeField] private Sprite horizontalFenceSprite;
    [SerializeField] private Sprite verticalFenceSprite;
    [SerializeField] private int sortingOrder = 4;

    [Header("Collision")]
    [Min(0.05f)] [SerializeField] private float fenceThickness = 0.35f;

    private const string RuntimeRootName = "FarmPlotFence_Runtime";

    private void Awake()
    {
        BuildFence();
    }

    [ContextMenu("Rebuild Farm Plot Fence")]
    public void BuildFence()
    {
        if (farmTileController == null)
        {
            Debug.LogWarning("FarmPlotFenceController thiếu FarmTileController.", this);
            return;
        }

        Transform previous = transform.Find(RuntimeRootName);
        if (previous != null)
        {
            Destroy(previous.gameObject);
        }

        GameObject rootObject = new GameObject(RuntimeRootName);
        rootObject.transform.SetParent(transform, false);
        Transform root = rootObject.transform;

        Vector3Int origin = farmTileController.FarmPlotOriginCell;
        int width = farmTileController.FarmPlotWidth;
        int height = farmTileController.FarmPlotHeight;
        Vector3 cellSize = farmTileController.GetCellSize();

        Vector3 bottomLeftCenter = farmTileController.GetCellCenterWorld(origin);
        float leftX = bottomLeftCenter.x - cellSize.x * 0.5f;
        float rightX = bottomLeftCenter.x + (width - 0.5f) * cellSize.x;
        float bottomY = bottomLeftCenter.y - cellSize.y * 0.5f;

        for (int x = 0; x < width; x++)
        {
            Vector3 position = new Vector3(
                bottomLeftCenter.x + x * cellSize.x,
                bottomY,
                0f
            );
            CreatePiece(root, $"Bottom_{x + 1}", horizontalFenceSprite, position,
                new Vector2(cellSize.x, fenceThickness));
        }

        for (int y = 0; y < height; y++)
        {
            float yPosition = bottomLeftCenter.y + y * cellSize.y;
            CreatePiece(root, $"Left_{y + 1}", verticalFenceSprite,
                new Vector3(leftX, yPosition, 0f),
                new Vector2(fenceThickness, cellSize.y));
            CreatePiece(root, $"Right_{y + 1}", verticalFenceSprite,
                new Vector3(rightX, yPosition, 0f),
                new Vector2(fenceThickness, cellSize.y));
        }

        CreateCollider(rootObject, "LeftCollider",
            new Vector2(leftX, bottomLeftCenter.y + (height - 1) * cellSize.y * 0.5f),
            new Vector2(fenceThickness, height * cellSize.y));
        CreateCollider(rootObject, "RightCollider",
            new Vector2(rightX, bottomLeftCenter.y + (height - 1) * cellSize.y * 0.5f),
            new Vector2(fenceThickness, height * cellSize.y));
        CreateCollider(rootObject, "BottomCollider",
            new Vector2((leftX + rightX) * 0.5f, bottomY),
            new Vector2(width * cellSize.x + fenceThickness, fenceThickness));

        Debug.Log($"Đã tạo hàng rào 3 cạnh quanh vùng canh tác {width}x{height} ({width * height} ô).", this);
    }

    private void CreatePiece(
        Transform parent,
        string objectName,
        Sprite sprite,
        Vector3 worldPosition,
        Vector2 targetSize
    )
    {
        if (sprite == null)
        {
            return;
        }

        GameObject piece = new GameObject(objectName);
        piece.transform.SetParent(parent, true);
        piece.transform.position = worldPosition;

        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        Vector2 spriteSize = sprite.bounds.size;
        piece.transform.localScale = new Vector3(
            spriteSize.x > 0f ? targetSize.x / spriteSize.x : 1f,
            spriteSize.y > 0f ? targetSize.y / spriteSize.y : 1f,
            1f
        );
    }

    private static void CreateCollider(
        GameObject root,
        string objectName,
        Vector2 worldCenter,
        Vector2 size
    )
    {
        GameObject colliderObject = new GameObject(objectName);
        colliderObject.transform.SetParent(root.transform, true);
        colliderObject.transform.position = worldCenter;

        BoxCollider2D collider = colliderObject.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = false;
    }
}
