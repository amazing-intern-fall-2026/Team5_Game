using UnityEngine;

// An item an animal dropped on the farm. Walk over it to pick it up - it goes
// into your Hotbar/Inventory, then you sell it in the shop like any produce.
// If your inventory is full it simply waits on the ground until there is room.
public sealed class DroppedItem : MonoBehaviour
{
    private const float DisplaySize = 0.5f;   // world units, whatever the icon's pixel size
    private const float PickupRadius = 0.9f;

    private static Sprite fallbackSprite;

    private ItemData item;
    private int amount;
    private CropQuality quality;
    private AnimalProducer origin;
    private Vector3 basePosition;
    private float bobPhase;
    private float nextFullMessageTime;

    public static DroppedItem Spawn(
        ItemData item,
        int amount,
        CropQuality quality,
        Vector3 position,
        AnimalProducer origin
    )
    {
        if (item == null)
        {
            return null;
        }

        GameObject dropObject = new GameObject("Drop_" + item.itemName);
        dropObject.transform.position = position;

        SpriteRenderer renderer = dropObject.AddComponent<SpriteRenderer>();
        renderer.sprite = item.icon != null ? item.icon : GetFallbackSprite();
        renderer.sortingOrder = 6;

        // Icons can be any size (some are over 1000px); normalise to a small pickup.
        Vector3 size = renderer.sprite.bounds.size;
        float largest = Mathf.Max(size.x, size.y);
        if (largest > 0.0001f)
        {
            dropObject.transform.localScale = Vector3.one * (DisplaySize / largest);
        }

        if (quality == CropQuality.PurpleStar)
        {
            renderer.color = new Color(0.88f, 0.7f, 1f, 1f);
        }
        else if (quality == CropQuality.GoldStar)
        {
            renderer.color = new Color(1f, 0.92f, 0.55f, 1f);
        }

        DroppedItem drop = dropObject.AddComponent<DroppedItem>();
        drop.item = item;
        drop.amount = Mathf.Max(1, amount);
        drop.quality = quality;
        drop.origin = origin;
        drop.basePosition = position;
        drop.bobPhase = Random.value * Mathf.PI * 2f;
        return drop;
    }

    private void Update()
    {
        transform.position = basePosition
            + Vector3.up * (Mathf.Sin(Time.time * 3f + bobPhase) * 0.04f);

        PlayerMovement player = AnimalProducer.FindPlayer();
        if (player == null
            || Vector2.Distance(player.transform.position, basePosition) > PickupRadius)
        {
            return;
        }

        ItemStorageController storage = AnimalProducer.FindStorage();
        if (storage == null)
        {
            return;
        }

        if (storage.TryAddItem(item, amount, quality))
        {
            Debug.Log(
                $"[Thú] Nhặt {amount} {item.itemName} "
                + $"[{CropQualityUtility.GetDisplayName(quality)}]."
            );

            if (origin != null)
            {
                origin.NotifyDropCollected();
            }

            Destroy(gameObject);
        }
        else if (Time.time >= nextFullMessageTime)
        {
            nextFullMessageTime = Time.time + 3f;
            Debug.Log($"[Thú] Hành trang đã đầy - chưa nhặt được {item.itemName}.");
        }
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite == null)
        {
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 220, 120, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
        }

        return fallbackSprite;
    }
}
