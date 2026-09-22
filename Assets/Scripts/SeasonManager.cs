using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SeasonType
{
    Spring,
    Summer,
    Autumn,
    Winter
}

public sealed class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }


[Header("Reference")]
    [SerializeField] private DayNightController dayNightController;

    [Header("Season Length")]
    [Min(1)][SerializeField] private int springDays = 7;
    [Min(1)][SerializeField] private int summerDays = 7;
    [Min(1)][SerializeField] private int autumnDays = 7;
    [Min(1)][SerializeField] private int winterDays = 7;

    [Header("Starting Point")]
    [SerializeField] private SeasonType startingSeason = SeasonType.Spring;
    [Min(1)][SerializeField] private int startingDayInSeason = 1;

    [Header("Test Mode")]
    [SerializeField] private bool testMode;

    [Min(1)][SerializeField] private int testSpringDays = 1;
    [Min(1)][SerializeField] private int testSummerDays = 1;
    [Min(1)][SerializeField] private int testAutumnDays = 1;
    [Min(1)][SerializeField] private int testWinterDays = 1;

    [Header("Season UI")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject seasonUIPrefab;

    [SerializeField]
    private Vector2 seasonUIPosition =
        new Vector2(73f, -132f);

    [SerializeField]
    private Vector2 seasonUISize =
        new Vector2(270f, 54f);

    [SerializeField]
    private Color panelColor =
        new Color(0.025f, 0.065f, 0.075f, 0.92f);

    [SerializeField]
    private Color textColor =
        new Color(1f, 0.86f, 0.42f, 1f);

    [SerializeField, Min(0.1f)]
    private float announcementSeconds = 2.5f;

    private TextMeshProUGUI seasonText;
    private TextMeshProUGUI announcementText;

    private Coroutine announcementRoutine;

    public SeasonType CurrentSeason { get; private set; }

    public int DayInSeason { get; private set; }

    // Event khi ngày thay đổi
    public event Action<SeasonType, int> SeasonDayChanged;

    // Event khi mùa thay đổi
    public event Action<SeasonType> OnSeasonChanged;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "Đã có một SeasonManager khác trong scene.",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;

        if (dayNightController == null)
        {
            dayNightController =
                FindFirstObjectByType<DayNightController>();
        }

        if (uiCanvas == null)
        {
            uiCanvas =
                FindFirstObjectByType<Canvas>();
        }

        CurrentSeason = startingSeason;

        DayInSeason = Mathf.Clamp(
            startingDayInSeason,
            1,
            GetSeasonLength(CurrentSeason)
        );

        CreateUI();
        RefreshUI();

        Debug.Log(
            $"[Season] Bắt đầu " +
            $"{GetSeasonDisplayName(CurrentSeason)} - " +
            $"Ngày {DayInSeason}"
        );
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    // =========================================================
    // DAY / SEASON
    // =========================================================




    public void NextDay()
    {
        NextDay(-1);
    }


    private void NextDay(int absoluteDay)
    {
        DayInSeason++;

        bool seasonChanged = false;

        if (DayInSeason > GetSeasonLength(CurrentSeason))
        {
            // Spring -> Summer -> Autumn -> Winter -> Spring
            CurrentSeason =
                (SeasonType)(((int)CurrentSeason + 1) % 4);

            DayInSeason = 1;

            seasonChanged = true;

            string seasonName =
                GetSeasonDisplayName(CurrentSeason);

            Debug.Log(
                $"[Season] {seasonName} đã bắt đầu!"
            );

            ShowAnnouncement(
                $"{seasonName} đã bắt đầu!"
            );
        }

        Debug.Log(
            $"[Season] Ngày mới: " +
            $"{GetSeasonDisplayName(CurrentSeason)} - " +
            $"Ngày {DayInSeason}" +
            (absoluteDay >= 0
                ? $" | Ngày game {absoluteDay}"
                : "")
        );

        RefreshUI();

        // Báo cho các hệ thống khác
        SeasonDayChanged?.Invoke(
            CurrentSeason,
            DayInSeason
        );

        // Chỉ gọi khi thực sự sang mùa
        if (seasonChanged)
        {
            OnSeasonChanged?.Invoke(CurrentSeason);
        }
    }


    // =========================================================
    // SEASON LENGTH
    // =========================================================

    public int GetSeasonLength(SeasonType season)
    {
        switch (season)
        {
            case SeasonType.Spring:
                return Mathf.Max(
                    1,
                    testMode
                        ? testSpringDays
                        : springDays
                );

            case SeasonType.Summer:
                return Mathf.Max(
                    1,
                    testMode
                        ? testSummerDays
                        : summerDays
                );

            case SeasonType.Autumn:
                return Mathf.Max(
                    1,
                    testMode
                        ? testAutumnDays
                        : autumnDays
                );

            case SeasonType.Winter:
                return Mathf.Max(
                    1,
                    testMode
                        ? testWinterDays
                        : winterDays
                );
        }

        return 7;
    }


    public static string GetSeasonDisplayName(
        SeasonType season
    )
    {
        switch (season)
        {
            case SeasonType.Spring:
                return "Mùa Xuân";

            case SeasonType.Summer:
                return "Mùa Hạ";

            case SeasonType.Autumn:
                return "Mùa Thu";

            case SeasonType.Winter:
                return "Mùa Đông";
        }

        return "Mùa Xuân";
    }


    // =========================================================
    // UI
    // =========================================================

    private void CreateUI()
    {
        if (uiCanvas == null)
        {
            Debug.LogWarning(
                "SeasonManager: Không tìm thấy Canvas."
            );

            return;
        }

        // Nếu đã có UI thì không tạo thêm
        if (seasonText != null)
        {
            return;
        }


    // -----------------------------------------------------
    // LOAD SEASON UI PREFAB
    // -----------------------------------------------------

#if UNITY_EDITOR

    if (seasonUIPrefab == null)
        {
            seasonUIPrefab =
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/Season/SeasonUI.prefab"
                );
        }


#endif

    // -----------------------------------------------------
    // USE PREFAB
    // -----------------------------------------------------

    if (seasonUIPrefab != null)
        {
            GameObject instance =
                Instantiate(
                    seasonUIPrefab,
                    uiCanvas.transform
                );

            instance.name =
                "SeasonUI_Runtime";

            RectTransform rect =
                instance.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchorMin =
                    new Vector2(0f, 1f);

                rect.anchorMax =
                    new Vector2(0f, 1f);

                rect.pivot =
                    new Vector2(0f, 1f);

                rect.anchoredPosition =
                    seasonUIPosition;

                rect.sizeDelta =
                    seasonUISize;
            }

            // Tìm TMP bên trong prefab
            seasonText =
                instance.GetComponentInChildren<
                    TextMeshProUGUI
                >();

            if (seasonText != null)
            {
                seasonText.text =
                    GetSeasonDisplayName(
                        CurrentSeason
                    );
            }
        }


        // -----------------------------------------------------
        // FALLBACK UI
        // -----------------------------------------------------

        if (seasonText == null)
        {
            CreateFallbackSeasonUI();
        }


        // -----------------------------------------------------
        // ANNOUNCEMENT
        // -----------------------------------------------------

        CreateAnnouncementUI();
    }


    private void CreateFallbackSeasonUI()
    {
        GameObject panel =
            new GameObject(
                "SeasonUI_Runtime",
                typeof(RectTransform),
                typeof(Image)
            );

        panel.layer = 5;

        panel.transform.SetParent(
            uiCanvas.transform,
            false
        );

        RectTransform panelRect =
            panel.GetComponent<RectTransform>();

        panelRect.anchorMin =
            new Vector2(0f, 1f);

        panelRect.anchorMax =
            new Vector2(0f, 1f);

        panelRect.pivot =
            new Vector2(0f, 1f);

        panelRect.anchoredPosition =
            seasonUIPosition;

        panelRect.sizeDelta =
            seasonUISize;

        Image image =
            panel.GetComponent<Image>();

        image.color = panelColor;

        image.raycastTarget = false;

        seasonText =
            CreateText(
                "SeasonText",
                panel.transform,
                20f,
                textColor
            );

        seasonText.alignment =
            TextAlignmentOptions.Center;

        seasonText.rectTransform.anchorMin =
            Vector2.zero;

        seasonText.rectTransform.anchorMax =
            Vector2.one;

        seasonText.rectTransform.offsetMin =
            Vector2.zero;

        seasonText.rectTransform.offsetMax =
            Vector2.zero;
    }


    private void CreateAnnouncementUI()
    {
        if (announcementText != null)
        {
            return;
        }

        announcementText =
            CreateText(
                "SeasonAnnouncement_Runtime",
                uiCanvas.transform,
                32f,
                textColor
            );

        announcementText.alignment =
            TextAlignmentOptions.Center;

        RectTransform rect =
            announcementText.rectTransform;

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            Vector2.zero;

        rect.sizeDelta =
            new Vector2(600f, 80f);

        announcementText.gameObject.SetActive(false);
    }


    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        float fontSize,
        Color color
    )
    {
        GameObject textObject =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        textObject.layer = 5;

        textObject.transform.SetParent(
            parent,
            false
        );

        TextMeshProUGUI text =
            textObject.AddComponent<
                TextMeshProUGUI
            >();

        text.fontSize = fontSize;

        text.fontStyle =
            FontStyles.Bold;

        text.color = color;

        text.outlineColor =
            new Color32(
                20,
                18,
                12,
                255
            );

        text.outlineWidth = 0.12f;

        text.raycastTarget = false;

        return text;
    }


    private void RefreshUI()
    {
        if (seasonText != null)
        {
            seasonText.text =
                GetSeasonDisplayName(
                    CurrentSeason
                );
        }
    }


    // =========================================================
    // ANNOUNCEMENT
    // =========================================================

    private void ShowAnnouncement(
        string message
    )
    {
        if (announcementText == null)
        {
            return;
        }

        if (announcementRoutine != null)
        {
            StopCoroutine(
                announcementRoutine
            );
        }

        announcementRoutine =
            StartCoroutine(
                ShowAnnouncementRoutine(
                    message
                )
            );
    }


    private IEnumerator ShowAnnouncementRoutine(
        string message
    )
    {
        announcementText.text =
            message;

        announcementText.gameObject.SetActive(
            true
        );

        yield return new WaitForSecondsRealtime(
            Mathf.Max(
                0.1f,
                announcementSeconds
            )
        );

        announcementText.gameObject.SetActive(
            false
        );

        announcementRoutine = null;
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private void OnValidate()
    {
        springDays =
            Mathf.Max(1, springDays);

        summerDays =
            Mathf.Max(1, summerDays);

        autumnDays =
            Mathf.Max(1, autumnDays);

        winterDays =
            Mathf.Max(1, winterDays);

        testSpringDays =
            Mathf.Max(1, testSpringDays);

        testSummerDays =
            Mathf.Max(1, testSummerDays);

        testAutumnDays =
            Mathf.Max(1, testAutumnDays);

        testWinterDays =
            Mathf.Max(1, testWinterDays);

        startingDayInSeason =
            Mathf.Max(1, startingDayInSeason);

        announcementSeconds =
            Mathf.Max(
                0.1f,
                announcementSeconds
            );
    }


}
