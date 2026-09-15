using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public enum DayNightTimeUnit
{
    Minutes,
    Seconds
}

public sealed class DayNightController : MonoBehaviour
{
    [Header("Cycle Duration")]
    [SerializeField, Min(0.01f)] private float dayDuration = 8f;
    [SerializeField, Min(0.01f)] private float nightDuration = 4f;
    [SerializeField] private DayNightTimeUnit durationUnit = DayNightTimeUnit.Minutes;

    [Header("Start / Control")]
    [SerializeField] private bool startAtNight;
    [SerializeField, Range(0f, 23.99f)] private float startingHour = 6f;
    [SerializeField] private bool isPaused;
    [SerializeField, Min(0f)] private float timeScaleMultiplier = 1f;

    [Header("Test Mode")]
    [SerializeField] private bool testMode;
    [SerializeField, Min(0.1f)] private float testDaySeconds = 20f;
    [SerializeField, Min(0.1f)] private float testNightSeconds = 10f;

    [Header("2D Global Light")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Color dawnColor = new Color(1f, 0.62f, 0.42f);
    [SerializeField] private Color dayColor = Color.white;
    [SerializeField] private Color duskColor = new Color(0.88f, 0.38f, 0.3f);
    [SerializeField] private Color nightColor = new Color(0.24f, 0.3f, 0.55f);
    [SerializeField, Range(0f, 2f)] private float dawnIntensity = 0.65f;
    [SerializeField, Range(0f, 2f)] private float dayIntensity = 1f;
    [SerializeField, Range(0f, 2f)] private float duskIntensity = 0.55f;
    [SerializeField, Range(0f, 2f)] private float nightIntensity = 0.22f;
    [SerializeField, Range(0.05f, 0.4f)] private float dawnPortionOfDay = 0.18f;
    [SerializeField, Range(0.05f, 0.4f)] private float duskPortionOfDay = 0.18f;

    [Header("Clock Mapping")]
    [SerializeField, Range(0f, 23.99f)] private float dayStartHour = 6f;
    [SerializeField, Range(0f, 23.99f)] private float nightStartHour = 18f;

    [Header("UI (auto-created if empty)")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI clockText;

    private float elapsedInCycle;
    private int dayNumber = 1;

    private float DaySeconds => testMode
        ? Mathf.Max(0.1f, testDaySeconds)
        : ConvertToSeconds(dayDuration);

    private float NightSeconds => testMode
        ? Mathf.Max(0.1f, testNightSeconds)
        : ConvertToSeconds(nightDuration);

    private void Awake()
    {
        ResolveReferences();
        InitializeStartTime();
        CreateClockUIIfNeeded();
        ApplyCurrentState();
    }

    private void Update()
    {
        if (!isPaused && timeScaleMultiplier > 0f)
        {
            AdvanceTime(Time.deltaTime * timeScaleMultiplier);
        }

        ApplyCurrentState();
    }

    private float ConvertToSeconds(float value)
    {
        float safeValue = Mathf.Max(0.01f, value);
        return durationUnit == DayNightTimeUnit.Minutes
            ? safeValue * 60f
            : safeValue;
    }

    private void ResolveReferences()
    {
        if (globalLight == null)
        {
            globalLight = FindFirstObjectByType<Light2D>();
        }

        if (globalLight == null)
        {
            globalLight = gameObject.AddComponent<Light2D>();
        }

        globalLight.lightType = Light2D.LightType.Global;

        if (uiCanvas == null)
        {
            uiCanvas = FindFirstObjectByType<Canvas>();
        }
    }

    private void InitializeStartTime()
    {
        if (startAtNight)
        {
            elapsedInCycle = DaySeconds;
            return;
        }

        float hour = Mathf.Repeat(startingHour, 24f);
        bool beginsInDay = IsHourInRange(hour, dayStartHour, nightStartHour);

        if (beginsInDay)
        {
            float dayClockHours = Mathf.Repeat(nightStartHour - dayStartHour, 24f);
            float progressedHours = Mathf.Repeat(hour - dayStartHour, 24f);
            elapsedInCycle = DaySeconds * Mathf.Clamp01(progressedHours / dayClockHours);
        }
        else
        {
            float nightClockHours = 24f - Mathf.Repeat(nightStartHour - dayStartHour, 24f);
            float progressedHours = Mathf.Repeat(hour - nightStartHour, 24f);
            elapsedInCycle = DaySeconds
                + NightSeconds * Mathf.Clamp01(progressedHours / nightClockHours);
        }
    }

    private void AdvanceTime(float delta)
    {
        float dayEnd = DaySeconds;
        float cycleEnd = dayEnd + NightSeconds;

        while (delta > 0f)
        {
            bool currentlyDay = elapsedInCycle < dayEnd;
            float boundary = currentlyDay ? dayEnd : cycleEnd;
            float step = Mathf.Min(delta, boundary - elapsedInCycle);
            elapsedInCycle += step;
            delta -= step;

            if (elapsedInCycle + 0.0001f < boundary)
            {
                break;
            }

            if (currentlyDay)
            {
                elapsedInCycle = dayEnd;
                Debug.Log($"[DayNight] Ngày {dayNumber}: bắt đầu Ban đêm.");
            }
            else
            {
                elapsedInCycle = 0f;
                dayNumber++;
                Debug.Log($"[DayNight] Bắt đầu Ngày {dayNumber}.");
            }
        }
    }

    private void ApplyCurrentState()
    {
        float daySeconds = DaySeconds;
        bool isDay = elapsedInCycle < daySeconds;

        if (globalLight != null)
        {
            if (isDay)
            {
                ApplyDayLight(Mathf.Clamp01(elapsedInCycle / daySeconds));
            }
            else
            {
                globalLight.color = nightColor;
                globalLight.intensity = nightIntensity;
            }
        }

        UpdateClockUI(isDay);
    }

    private void ApplyDayLight(float progress)
    {
        float dawnEnd = Mathf.Clamp(dawnPortionOfDay, 0.05f, 0.4f);
        float duskStart = 1f - Mathf.Clamp(duskPortionOfDay, 0.05f, 0.4f);

        if (progress < dawnEnd)
        {
            float t = Mathf.SmoothStep(0f, 1f, progress / dawnEnd);
            if (t < 0.5f)
            {
                SetLight(nightColor, dawnColor, nightIntensity, dawnIntensity, t * 2f);
            }
            else
            {
                SetLight(dawnColor, dayColor, dawnIntensity, dayIntensity, (t - 0.5f) * 2f);
            }
        }
        else if (progress < duskStart)
        {
            globalLight.color = dayColor;
            globalLight.intensity = dayIntensity;
        }
        else
        {
            float t = Mathf.SmoothStep(0f, 1f, (progress - duskStart) / (1f - duskStart));
            if (t < 0.5f)
            {
                SetLight(dayColor, duskColor, dayIntensity, duskIntensity, t * 2f);
            }
            else
            {
                SetLight(duskColor, nightColor, duskIntensity, nightIntensity, (t - 0.5f) * 2f);
            }
        }
    }

    private void SetLight(
        Color fromColor,
        Color toColor,
        float fromIntensity,
        float toIntensity,
        float t
    )
    {
        globalLight.color = Color.Lerp(fromColor, toColor, t);
        globalLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, t);
    }

    private void UpdateClockUI(bool isDay)
    {
        float hour;
        if (isDay)
        {
            float dayHours = Mathf.Repeat(nightStartHour - dayStartHour, 24f);
            hour = dayStartHour + dayHours * (elapsedInCycle / DaySeconds);
        }
        else
        {
            float nightHours = 24f - Mathf.Repeat(nightStartHour - dayStartHour, 24f);
            float nightProgress = (elapsedInCycle - DaySeconds) / NightSeconds;
            hour = nightStartHour + nightHours * nightProgress;
        }

        hour = Mathf.Repeat(hour, 24f);
        int displayHour = Mathf.FloorToInt(hour);
        int displayMinute = Mathf.FloorToInt((hour - displayHour) * 60f);

        if (dayText != null) dayText.text = $"Ngày {dayNumber}";
        if (stateText != null) stateText.text = isDay ? "Ban ngày" : "Ban đêm";
        if (clockText != null) clockText.text = $"{displayHour:00}:{displayMinute:00}";
    }

    private void CreateClockUIIfNeeded()
    {
        if (uiCanvas == null || (dayText != null && stateText != null && clockText != null))
        {
            return;
        }

        GameObject panel = new GameObject("DayNightUI", typeof(RectTransform), typeof(Image));
        panel.layer = 5;
        panel.transform.SetParent(uiCanvas.transform, false);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.025f, 0.065f, 0.075f, 0.9f);
        panelImage.raycastTarget = false;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(18f, -18f);
        panelRect.sizeDelta = new Vector2(210f, 82f);

        dayText = CreateText("DayText", panel.transform, new Vector2(12f, -8f), 18f);
        stateText = CreateText("StateText", panel.transform, new Vector2(12f, -32f), 14f);
        clockText = CreateText("ClockText", panel.transform, new Vector2(108f, -13f), 25f);
        clockText.alignment = TextAlignmentOptions.TopRight;
        clockText.color = new Color(1f, 0.85f, 0.35f);
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        Vector2 position,
        float fontSize
    )
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.layer = 5;
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(90f, 28f);
        return text;
    }

    private static bool IsHourInRange(float hour, float start, float end)
    {
        if (start <= end)
        {
            return hour >= start && hour < end;
        }

        return hour >= start || hour < end;
    }

    private void OnValidate()
    {
        dayDuration = Mathf.Max(0.01f, dayDuration);
        nightDuration = Mathf.Max(0.01f, nightDuration);
        testDaySeconds = Mathf.Max(0.1f, testDaySeconds);
        testNightSeconds = Mathf.Max(0.1f, testNightSeconds);
        timeScaleMultiplier = Mathf.Max(0f, timeScaleMultiplier);
    }
}
