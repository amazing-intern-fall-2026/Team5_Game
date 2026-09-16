using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class DayNightUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private DayNightController controller;

    [Header("New Day/Night art")]
    [SerializeField] private Sprite panelBackground;
    [SerializeField] private Sprite panelBorder;
    [SerializeField] private Sprite orbitCircle;
    [SerializeField] private Sprite sunIcon;
    [SerializeField] private Sprite moonIcon;

    [Header("Layout")]
    [SerializeField] private Vector2 screenPosition = new Vector2(18f, -18f);
    [SerializeField] private Vector2 panelSize = new Vector2(380f, 112f);
    [SerializeField] private Vector2 orbitCenter = new Vector2(190f, -56f);
    [SerializeField, Min(1f)] private float orbitRadius = 25f;
    [SerializeField, Min(0f)] private float orbitSmoothing = 12f;
    [SerializeField] private Vector2 celestialIconSize = new Vector2(27f, 27f);

    [Header("Colors")]
    [SerializeField] private Color dayAccent = new Color(1f, 0.82f, 0.25f, 1f);
    [SerializeField] private Color nightAccent = new Color(0.55f, 0.82f, 1f, 1f);
    [SerializeField] private Color inactiveIconColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color activeIconColor = Color.white;

    [Header("Runtime UI references (auto-created when empty)")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private RectTransform sunRect;
    [SerializeField] private RectTransform moonRect;
    [SerializeField] private Image sunImage;
    [SerializeField] private Image moonImage;
    private float displayedAngle;
    private bool angleInitialized;

    private void Awake()
    {
        EnsureVisualTree();
        if (controller == null)
        {
            controller = FindFirstObjectByType<DayNightController>();
        }
    }

    private void Update()
    {
        if (controller == null)
        {
            controller = FindFirstObjectByType<DayNightController>();
        }

        RefreshFromController();
    }

    public void Bind(DayNightController source)
    {
        controller = source;
        EnsureVisualTree();
        RefreshFromController();
    }

    public void RefreshFromController()
    {
        if (controller == null || dayText == null)
        {
            return;
        }

        bool isDay = controller.IsDay;
        dayText.text = $"Ngày {controller.DayNumber}";
        stateText.text = isDay ? "Ban ngày" : "Ban đêm";
        stateText.color = isDay ? dayAccent : nightAccent;

        float hour = controller.CurrentHour;
        int displayHour = Mathf.FloorToInt(hour);
        int displayMinute = Mathf.FloorToInt((hour - displayHour) * 60f);
        clockText.text = $"{displayHour:00}:{displayMinute:00}";
        clockText.color = isDay ? dayAccent : nightAccent;

        float targetAngle = controller.CelestialAngleDegrees;
        if (!angleInitialized)
        {
            displayedAngle = targetAngle;
            angleInitialized = true;
        }
        else
        {
            float blend = orbitSmoothing <= 0f
                ? 1f
                : 1f - Mathf.Exp(-orbitSmoothing * Time.unscaledDeltaTime);
            displayedAngle = Mathf.LerpAngle(displayedAngle, targetAngle, blend);
        }

        SetOrbitPosition(sunRect, displayedAngle);
        SetOrbitPosition(moonRect, displayedAngle + 180f);
        sunImage.color = isDay ? activeIconColor : inactiveIconColor;
        moonImage.color = isDay ? inactiveIconColor : activeIconColor;
    }

    private void SetOrbitPosition(RectTransform icon, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        icon.anchoredPosition = new Vector2(
            Mathf.Cos(radians) * orbitRadius,
            Mathf.Sin(radians) * orbitRadius
        );
    }

    private void EnsureVisualTree()
    {
        RectTransform root = (RectTransform)transform;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = screenPosition;
        root.sizeDelta = panelSize;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        Image background = GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }
        background.sprite = panelBackground;
        background.color = Color.white;
        background.raycastTarget = false;

        if (dayText != null)
        {
            return;
        }

        CreateStretchImage("PanelBorder", transform, panelBorder);
        dayText = CreateText("DayText", transform, new Vector2(75f, -23f),
            new Vector2(120f, 34f), 24f, TextAlignmentOptions.MidlineLeft);
        stateText = CreateText("StateText", transform, new Vector2(75f, -58f),
            new Vector2(120f, 28f), 17f, TextAlignmentOptions.MidlineLeft);

        RectTransform orbitRoot = CreateRect("Orbit", transform, orbitCenter,
            new Vector2(82f, 82f));
        Image orbitImage = orbitRoot.gameObject.AddComponent<Image>();
        orbitImage.sprite = orbitCircle;
        orbitImage.preserveAspect = true;
        orbitImage.raycastTarget = false;

        sunImage = CreateIcon("Sun", orbitRoot, sunIcon, out sunRect);
        moonImage = CreateIcon("Moon", orbitRoot, moonIcon, out moonRect);

        clockText = CreateText("ClockText", transform, new Vector2(315f, -56f),
            new Vector2(110f, 46f), 31f, TextAlignmentOptions.Center);
    }

    private Image CreateIcon(
        string objectName,
        Transform parent,
        Sprite sprite,
        out RectTransform rect
    )
    {
        rect = CreateRect(objectName, parent, Vector2.zero, celestialIconSize);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void CreateStretchImage(
        string objectName,
        Transform parent,
        Sprite sprite
    )
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        child.layer = 5;
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = child.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment
    )
    {
        RectTransform rect = CreateRect(objectName, parent, position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.outlineColor = new Color32(20, 18, 12, 255);
        text.outlineWidth = 0.12f;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size
    )
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = 5;
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
