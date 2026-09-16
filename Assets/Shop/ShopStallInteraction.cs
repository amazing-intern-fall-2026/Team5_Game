using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class ShopStallInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private ShopUIController shopController;
    [SerializeField] private BoxCollider2D interactionArea;

    [Header("New shop sprites")]
    [SerializeField] private Sprite stallBackground;
    [SerializeField] private Sprite shopSign;
    [SerializeField] private Sprite seedCrate;
    [SerializeField] private Sprite produceCrate;
    [SerializeField] private int sortingOrder = 2;

    [Header("Interaction")]
    [SerializeField] private Key interactionKey = Key.E;
    [SerializeField] private bool logOutOfRangeAttempts;

    private void Awake()
    {
        if (interactionArea == null)
        {
            interactionArea = GetComponent<BoxCollider2D>();
        }

        if (shopController == null)
        {
            shopController = FindFirstObjectByType<ShopUIController>();
        }

        if (player == null)
        {
            PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
            player = movement != null ? movement.transform : null;
        }

        BuildVisuals();
    }

    private void Update()
    {
        if (Keyboard.current == null
            || !Keyboard.current[interactionKey].wasPressedThisFrame)
        {
            return;
        }

        if (player == null || shopController == null || interactionArea == null)
        {
            Debug.LogWarning("ShopStall thiếu Player, Shop Controller hoặc vùng tương tác.");
            return;
        }

        if (!interactionArea.OverlapPoint(player.position))
        {
            if (logOutOfRangeAttempts)
            {
                Debug.Log("Hãy đến gần quầy hàng để mở Shop.");
            }

            return;
        }

        shopController.OpenShop();
        Debug.Log("Đã mở Shop tại quầy hàng.");
    }

    private void BuildVisuals()
    {
        SpriteRenderer mainRenderer = GetComponent<SpriteRenderer>();
        mainRenderer.sprite = stallBackground;
        mainRenderer.sortingOrder = sortingOrder;

        CreateDecoration("ShopSign", shopSign, new Vector3(0f, 2.35f, 0f), 0.34f);
        CreateDecoration("SeedCrate", seedCrate, new Vector3(-1.65f, -1.55f, 0f), 0.32f);
        CreateDecoration("ProduceCrate", produceCrate, new Vector3(1.65f, -1.55f, 0f), 0.32f);
    }

    private void CreateDecoration(
        string objectName,
        Sprite sprite,
        Vector3 localPosition,
        float localScale
    )
    {
        if (sprite == null)
        {
            return;
        }

        Transform existing = transform.Find(objectName);
        GameObject decoration = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(SpriteRenderer));

        if (existing == null)
        {
            decoration.transform.SetParent(transform, false);
        }

        decoration.transform.localPosition = localPosition;
        decoration.transform.localRotation = Quaternion.identity;
        decoration.transform.localScale = Vector3.one * localScale;

        SpriteRenderer renderer = decoration.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder + 1;
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D area = interactionArea != null
            ? interactionArea
            : GetComponent<BoxCollider2D>();

        if (area == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.35f);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(area.offset, area.size);
        Gizmos.matrix = previousMatrix;
    }
}
