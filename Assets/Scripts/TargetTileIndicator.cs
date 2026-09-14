using UnityEngine;

/// <summary>
/// Draws the Stardew-style action target.  It is created by the player at
/// runtime so scenes that already contain the player need no fragile prefab
/// changes or manually assigned sprite asset.
/// </summary>
[DisallowMultipleComponent]
public class TargetTileIndicator : MonoBehaviour
{
    [SerializeField] private Color borderColor = new Color(1f, 0.92f, 0.2f, 1f);
    [SerializeField, Range(1, 8)] private int borderPixels = 3;
    [SerializeField] private int sortingOrder = 100;

    private FarmTileController farmTileController;
    private GameObject indicatorObject;
    private SpriteRenderer indicatorRenderer;
    private Texture2D generatedTexture;

    public void Initialize(FarmTileController controller)
    {
        farmTileController = controller;
        EnsureIndicator();
        Hide();
    }

    public void SetTarget(bool shouldShow, Vector3 worldPosition)
    {
        if (!shouldShow || farmTileController == null
            || !farmTileController.TryGetGroundCell(worldPosition, out Vector3Int cell))
        {
            Hide();
            return;
        }

        EnsureIndicator();
        indicatorObject.transform.position = farmTileController.GetCellCenterWorld(cell);

        Vector3 cellSize = farmTileController.GetCellSize();
        indicatorObject.transform.localScale = new Vector3(
            Mathf.Abs(cellSize.x) * 0.94f,
            Mathf.Abs(cellSize.y) * 0.94f,
            1f
        );
        indicatorRenderer.enabled = true;
    }

    public void Hide()
    {
        if (indicatorRenderer != null)
        {
            indicatorRenderer.enabled = false;
        }
    }

    private void EnsureIndicator()
    {
        if (indicatorObject != null)
        {
            return;
        }

        indicatorObject = new GameObject("Farm Action Target");
        indicatorObject.transform.SetParent(transform, false);

        indicatorRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        indicatorRenderer.sprite = CreateBorderSprite();
        indicatorRenderer.color = borderColor;
        indicatorRenderer.sortingOrder = sortingOrder;
    }

    private Sprite CreateBorderSprite()
    {
        const int textureSize = 32;
        generatedTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            name = "Runtime Target Tile Border"
        };

        Color clear = Color.clear;
        Color[] pixels = new Color[textureSize * textureSize];
        int width = Mathf.Clamp(borderPixels, 1, textureSize / 2);

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool isBorder = x < width || x >= textureSize - width
                    || y < width || y >= textureSize - width;
                pixels[y * textureSize + x] = isBorder ? Color.white : clear;
            }
        }

        generatedTexture.SetPixels(pixels);
        generatedTexture.Apply();

        return Sprite.Create(
            generatedTexture,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize
        );
    }

    private void OnDestroy()
    {
        if (generatedTexture != null)
        {
            Destroy(generatedTexture);
        }
    }
}
