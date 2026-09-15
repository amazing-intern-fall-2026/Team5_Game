using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class PlayerCameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("Transform mà camera sẽ theo. Gán Player tại Inspector.")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Smoothing")]
    [Tooltip("Thời gian camera cần để tiến gần vị trí đích. Giá trị nhỏ bám nhanh hơn.")]
    [SerializeField, Min(0.01f)] private float smoothing = 0.18f;
    [SerializeField, Min(0f)] private float maximumFollowSpeed = 30f;
    [SerializeField] private bool snapToTargetOnStart = true;

    [Header("Follow Axes")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;

    [Header("Map Bounds")]
    [SerializeField] private bool useBounds = true;
    [Tooltip("Góc dưới-trái của vùng map theo tọa độ world.")]
    [SerializeField] private Vector2 minBounds = new Vector2(-14f, -9f);
    [Tooltip("Góc trên-phải của vùng map theo tọa độ world.")]
    [SerializeField] private Vector2 maxBounds = new Vector2(14f, 10f);
    [Tooltip("Tính kích thước viewport để mép camera không nhìn ra ngoài map.")]
    [SerializeField] private bool includeCameraViewport = true;

    [Header("2D Camera")]
    [SerializeField] private bool forceOrthographic = true;
    [SerializeField, Min(0.1f)] private float orthographicSize = 6f;

    private Camera attachedCamera;
    private Vector3 velocity;
    private float fixedZ;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();
        fixedZ = transform.position.z;

        if (forceOrthographic)
        {
            attachedCamera.orthographic = true;
            attachedCamera.orthographicSize = orthographicSize;
        }

        if (target == null)
        {
            Debug.LogWarning("PlayerCameraFollow chưa được gán Target.", this);
            enabled = false;
            return;
        }

        fixedZ = target.position.z + offset.z;

        if (snapToTargetOnStart)
        {
            transform.position = GetClampedTargetPosition();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 destination = GetClampedTargetPosition();
        Vector3 nextPosition = Vector3.SmoothDamp(
            transform.position,
            destination,
            ref velocity,
            Mathf.Max(0.01f, smoothing),
            Mathf.Max(0f, maximumFollowSpeed),
            Time.deltaTime
        );

        nextPosition.z = fixedZ;
        transform.position = nextPosition;
        transform.rotation = Quaternion.identity;
    }

    private Vector3 GetClampedTargetPosition()
    {
        Vector3 current = transform.position;
        Vector3 desired = target.position + offset;

        if (!followX) desired.x = current.x;
        if (!followY) desired.y = current.y;
        desired.z = fixedZ;

        if (!useBounds)
        {
            return desired;
        }

        float halfHeight = 0f;
        float halfWidth = 0f;
        if (includeCameraViewport && attachedCamera != null && attachedCamera.orthographic)
        {
            halfHeight = attachedCamera.orthographicSize;
            halfWidth = halfHeight * attachedCamera.aspect;
        }

        desired.x = ClampAxis(
            desired.x,
            minBounds.x + halfWidth,
            maxBounds.x - halfWidth
        );
        desired.y = ClampAxis(
            desired.y,
            minBounds.y + halfHeight,
            maxBounds.y - halfHeight
        );
        return desired;
    }

    private static float ClampAxis(float value, float minimum, float maximum)
    {
        if (minimum > maximum)
        {
            return (minimum + maximum) * 0.5f;
        }

        return Mathf.Clamp(value, minimum, maximum);
    }

    private void OnValidate()
    {
        smoothing = Mathf.Max(0.01f, smoothing);
        maximumFollowSpeed = Mathf.Max(0f, maximumFollowSpeed);
        orthographicSize = Mathf.Max(0.1f, orthographicSize);

        if (forceOrthographic)
        {
            Camera cameraComponent = GetComponent<Camera>();
            if (cameraComponent != null)
            {
                cameraComponent.orthographic = true;
                cameraComponent.orthographicSize = orthographicSize;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.9f);
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) * 0.5f,
            (minBounds.y + maxBounds.y) * 0.5f,
            0f
        );
        Vector3 size = new Vector3(
            Mathf.Abs(maxBounds.x - minBounds.x),
            Mathf.Abs(maxBounds.y - minBounds.y),
            0f
        );
        Gizmos.DrawWireCube(center, size);
    }
}
