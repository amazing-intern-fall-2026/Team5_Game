using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class SeasonUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private SeasonManager manager;

    [Header("Visual assets")]
    [SerializeField] private Sprite panelBackground;
    [SerializeField] private Sprite panelBorder;

    [Header("Layout")]
    [SerializeField] private Vector2 screenPosition = new Vector2(73f, -132f);
    [SerializeField] private Vector2 panelSize = new Vector2(270f, 54f);
    [SerializeField] private Color textColor = new Color(1f, 0.88f, 0.42f, 1f);

    private TextMeshProUGUI label;

    private void Awake()
    {
        BuildVisuals();
        Refresh();
    }

    public void Bind(SeasonManager seasonManager)
    {
        manager = seasonManager;
        BuildVisuals();
        Refresh();
    }

    public void Refresh()
    {
        if (label == null)
        {
            BuildVisuals();
        }
        if (label != null && manager != null)
        {
            label.text = SeasonManager.GetSeasonDisplayName(manager.CurrentSeason);
        }
    }

    private void BuildVisuals()
    {
        RectTransform root = GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = screenPosition;
        root.sizeDelta = panelSize;
        root.localScale = Vector3.one;

        Transform oldVisual = transform.Find("Visual");
        if (oldVisual != null)
        {
            label = oldVisual.GetComponentInChildren<TextMeshProUGUI>(true);
            return;
        }

        GameObject visual = CreateUIObject("Visual", transform);
        Stretch(visual.GetComponent<RectTransform>());

        Image background = visual.AddComponent<Image>();
        background.sprite = panelBackground;
        background.type = panelBackground != null ? Image.Type.Sliced : Image.Type.Simple;
        background.color = panelBackground != null
            ? Color.white
            : new Color(0.025f, 0.065f, 0.075f, 0.94f);
        background.raycastTarget = false;

        GameObject borderObject = CreateUIObject("Border", visual.transform);
        Stretch(borderObject.GetComponent<RectTransform>());
        Image border = borderObject.AddComponent<Image>();
        border.sprite = panelBorder;
        border.type = panelBorder != null ? Image.Type.Sliced : Image.Type.Simple;
        border.color = panelBorder != null ? Color.white : new Color(0.68f, 0.43f, 0.17f, 1f);
        border.raycastTarget = false;

        GameObject textObject = CreateUIObject("SeasonText", visual.transform);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 5f);
        textRect.offsetMax = new Vector2(-18f, -5f);
        label = textObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = 20f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = textColor;
        label.outlineColor = new Color32(20, 18, 12, 255);
        label.outlineWidth = 0.12f;
        label.raycastTarget = false;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.layer = 5;
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
