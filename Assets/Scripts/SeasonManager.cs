using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }

    [Header("Reference")]
    [SerializeField] private DayNightController dayNightController;

    [Header("Season length (days)")]
    [Min(1)] [SerializeField] private int springDays = 7;
    [Min(1)] [SerializeField] private int summerDays = 7;
    [Min(1)] [SerializeField] private int autumnDays = 7;
    [Min(1)] [SerializeField] private int winterDays = 7;

    [Header("Starting point")]
    [SerializeField] private SeasonType startingSeason = SeasonType.Spring;
    [Min(1)] [SerializeField] private int startingDayInSeason = 1;

    [Header("Test Mode")]
    [SerializeField] private bool testMode;
    [Min(1)] [SerializeField] private int testSpringDays = 1;
    [Min(1)] [SerializeField] private int testSummerDays = 1;
    [Min(1)] [SerializeField] private int testAutumnDays = 1;
    [Min(1)] [SerializeField] private int testWinterDays = 1;

    [Header("UI")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject seasonUIPrefab;
    [SerializeField] private SeasonUI seasonUI;
    [SerializeField] private Vector2 seasonUIPosition = new Vector2(0f, -22f);
    [SerializeField] private Vector2 seasonUISize = new Vector2(260f, 42f);
    [SerializeField] private Color panelColor = new Color(0.025f, 0.065f, 0.075f, 0.92f);
    [SerializeField] private Color textColor = new Color(1f, 0.86f, 0.42f, 1f);
    [SerializeField, Min(0.1f)] private float announcementSeconds = 2.5f;

    private TextMeshProUGUI seasonText;
    private TextMeshProUGUI announcementText;
    private Coroutine announcementRoutine;

    public SeasonType CurrentSeason { get; private set; }
    public int DayInSeason { get; private set; }
    public event Action<SeasonType, int> SeasonDayChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Đã có một SeasonManager khác trong scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        if (dayNightController == null)
        {
            dayNightController = FindFirstObjectByType<DayNightController>();
        }
        if (uiCanvas == null)
        {
            uiCanvas = FindFirstObjectByType<Canvas>();
        }

        CurrentSeason = startingSeason;
        DayInSeason = Mathf.Clamp(startingDayInSeason, 1, GetSeasonLength(CurrentSeason));
        CreateUI();
        RefreshUI();
        Debug.Log($"[Season] Bắt đầu {GetSeasonDisplayName(CurrentSeason)} - Ngày {DayInSeason}.");
    }

    private void OnEnable()
    {
        if (dayNightController == null)
        {
            dayNightController = FindFirstObjectByType<DayNightController>();
        }
        if (dayNightController != null)
        {
            dayNightController.DayStarted -= HandleNewDay;
            dayNightController.DayStarted += HandleNewDay;
        }
    }

    private void Start()
    {
        if (dayNightController != null)
        {
            dayNightController.DayStarted -= HandleNewDay;
            dayNightController.DayStarted += HandleNewDay;
        }
    }

    private void OnDisable()
    {
        if (dayNightController != null)
        {
            dayNightController.DayStarted -= HandleNewDay;
        }
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleNewDay(int absoluteDay)
    {
        DayInSeason++;
        if (DayInSeason > GetSeasonLength(CurrentSeason))
        {
            CurrentSeason = (SeasonType)(((int)CurrentSeason + 1) % 4);
            DayInSeason = 1;
            Debug.Log($"[Season] {GetSeasonDisplayName(CurrentSeason)} đã bắt đầu!");
            ShowAnnouncement($"{GetSeasonDisplayName(CurrentSeason)} đã bắt đầu!");
        }

        Debug.Log(
            $"[Season] Ngày mới: {GetSeasonDisplayName(CurrentSeason)} - "
            + $"Ngày {DayInSeason} (Ngày game {absoluteDay})."
        );
        RefreshUI();
        SeasonDayChanged?.Invoke(CurrentSeason, DayInSeason);
    }

    public int GetSeasonLength(SeasonType season)
    {
        switch (season)
        {
            case SeasonType.Spring: return Mathf.Max(1, testMode ? testSpringDays : springDays);
            case SeasonType.Summer: return Mathf.Max(1, testMode ? testSummerDays : summerDays);
            case SeasonType.Autumn: return Mathf.Max(1, testMode ? testAutumnDays : autumnDays);
            default: return Mathf.Max(1, testMode ? testWinterDays : winterDays);
        }
    }

    public static string GetSeasonDisplayName(SeasonType season)
    {
        switch (season)
        {
            case SeasonType.Spring: return "Mùa Xuân";
            case SeasonType.Summer: return "Mùa Hạ";
            case SeasonType.Autumn: return "Mùa Thu";
            default: return "Mùa Đông";
        }
    }

    private void CreateUI()
    {
        if (uiCanvas == null || seasonText != null || seasonUI != null)
        {
            return;
        }

#if UNITY_EDITOR
        if (seasonUIPrefab == null)
        {
            seasonUIPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/Season/SeasonUI.prefab"
            );
        }
#endif

        if (seasonUIPrefab != null)
        {
            GameObject instance = Instantiate(seasonUIPrefab, uiCanvas.transform);
            instance.name = "SeasonUI_Runtime";
            seasonUI = instance.GetComponent<SeasonUI>();
            if (seasonUI != null)
            {
                seasonUI.Bind(this);
            }
        }

        if (seasonUI == null)
        {
            GameObject panel = new GameObject("SeasonUI_Runtime", typeof(RectTransform), typeof(Image));
            panel.layer = 5;
            panel.transform.SetParent(uiCanvas.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(73f, -132f);
            panelRect.sizeDelta = seasonUISize;
            Image image = panel.GetComponent<Image>();
            image.color = panelColor;
            image.raycastTarget = false;

            seasonText = CreateText("SeasonText", panel.transform, 20f, textColor);
            seasonText.alignment = TextAlignmentOptions.Center;
            seasonText.rectTransform.anchorMin = Vector2.zero;
            seasonText.rectTransform.anchorMax = Vector2.one;
            seasonText.rectTransform.offsetMin = Vector2.zero;
            seasonText.rectTransform.offsetMax = Vector2.zero;
        }

        announcementText = CreateText("SeasonAnnouncement_Runtime", uiCanvas.transform, 32f, textColor);
        announcementText.alignment = TextAlignmentOptions.Center;
        RectTransform announcementRect = announcementText.rectTransform;
        announcementRect.anchorMin = new Vector2(0.5f, 0.5f);
        announcementRect.anchorMax = new Vector2(0.5f, 0.5f);
        announcementRect.pivot = new Vector2(0.5f, 0.5f);
        announcementRect.anchoredPosition = Vector2.zero;
        announcementRect.sizeDelta = new Vector2(600f, 80f);
        announcementText.gameObject.SetActive(false);
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float size, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.layer = 5;
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.outlineColor = new Color32(20, 18, 12, 255);
        text.outlineWidth = 0.12f;
        text.raycastTarget = false;
        return text;
    }

    private void RefreshUI()
    {
        if (seasonText != null)
        {
            seasonText.text = GetSeasonDisplayName(CurrentSeason);
        }
        seasonUI?.Refresh();
    }

    private void ShowAnnouncement(string message)
    {
        if (announcementText == null)
        {
            return;
        }
        if (announcementRoutine != null)
        {
            StopCoroutine(announcementRoutine);
        }
        announcementRoutine = StartCoroutine(ShowAnnouncementRoutine(message));
    }

    private IEnumerator ShowAnnouncementRoutine(string message)
    {
        announcementText.text = message;
        announcementText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, announcementSeconds));
        announcementText.gameObject.SetActive(false);
        announcementRoutine = null;
    }

    private void OnValidate()
    {
        springDays = Mathf.Max(1, springDays);
        summerDays = Mathf.Max(1, summerDays);
        autumnDays = Mathf.Max(1, autumnDays);
        winterDays = Mathf.Max(1, winterDays);
        testSpringDays = Mathf.Max(1, testSpringDays);
        testSummerDays = Mathf.Max(1, testSummerDays);
        testAutumnDays = Mathf.Max(1, testAutumnDays);
        testWinterDays = Mathf.Max(1, testWinterDays);
        startingDayInSeason = Mathf.Max(1, startingDayInSeason);
        announcementSeconds = Mathf.Max(0.1f, announcementSeconds);
    }
}
