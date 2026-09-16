using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CropPlant : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private CropData cropData;
    private float growthElapsedSeconds;
    private int currentStage;
    private int fertilizerApplicationCount;
    private bool mutationRolled;
    private CropQuality quality = CropQuality.Normal;
    private SpriteRenderer qualityOverlay;

    public bool IsReadyToHarvest => currentStage >= 3;
    public CropType CropType => cropData != null
        ? cropData.cropType
        : CropType.None;
    public string CropName => cropData != null
        ? cropData.cropName
        : "Unknown crop";
    public float RemainingGrowthSeconds => cropData == null
        ? 0f
        : Mathf.Max(0f, cropData.TotalGrowthDurationSeconds - growthElapsedSeconds);
    public CropQuality Quality => quality;

    public void Initialize(CropData newCropData, int sortingOrder)
    {
        cropData = newCropData;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = sortingOrder;

        growthElapsedSeconds = 0f;
        currentStage = 1;
        fertilizerApplicationCount = 0;
        mutationRolled = false;
        quality = CropQuality.Normal;

        RefreshSprite();
        RefreshQualityOverlay();
    }

    private void Update()
    {
        if (cropData == null)
        {
            return;
        }

        // Corn reaches its first growth checkpoint, then waits for nitrogen.
        // Waiting time is intentionally not banked while the crop is paused.
        bool cornIsWaitingForNitrogen = cropData.cropType == CropType.Corn
            && fertilizerApplicationCount == 0
            && growthElapsedSeconds >= cropData.Stage1DurationSeconds;

        if (!cornIsWaitingForNitrogen)
        {
            growthElapsedSeconds += Time.deltaTime;
        }

        UpdateGrowthStage();
    }

    private void UpdateGrowthStage()
    {
        float elapsedTime = growthElapsedSeconds;

        if (elapsedTime >= cropData.TotalGrowthDurationSeconds)
        {
            currentStage = 3;
        }
        else if (elapsedTime >= cropData.Stage1DurationSeconds)
        {
            currentStage = 2;
        }
        else
        {
            currentStage = 1;
        }

        RefreshSprite();
    }

    public FertilizerApplicationResult TryApplyFertilizer(
        ItemData fertilizerItem,
        out float reducedSeconds
    )
    {
        reducedSeconds = 0f;

        if (IsReadyToHarvest)
        {
            return FertilizerApplicationResult.CropMature;
        }

        if (!FertilizerRules.TryGetRule(
            CropType,
            fertilizerItem,
            out int maximumApplications,
            out reducedSeconds
        ))
        {
            reducedSeconds = 0f;
            return FertilizerApplicationResult.WrongType;
        }

        if (fertilizerApplicationCount >= maximumApplications)
        {
            reducedSeconds = 0f;
            return FertilizerApplicationResult.ApplicationLimitReached;
        }

        fertilizerApplicationCount++;

        if (!mutationRolled)
        {
            mutationRolled = true;
            quality = cropData.RollQuality(Random.value);
            RefreshQualityOverlay();
            Debug.Log(
                $"[Biến dị] {CropName} roll: "
                + CropQualityUtility.GetDisplayName(quality)
            );
        }

        growthElapsedSeconds = Mathf.Min(
            cropData.TotalGrowthDurationSeconds,
            growthElapsedSeconds + reducedSeconds
        );
        UpdateGrowthStage();

        return FertilizerApplicationResult.Success;
    }

    private void RefreshQualityOverlay()
    {
        if (cropData == null || quality == CropQuality.Normal)
        {
            if (qualityOverlay != null)
            {
                qualityOverlay.gameObject.SetActive(false);
            }
            return;
        }

        Sprite icon = cropData.GetQualityIcon(quality);
        if (icon == null)
        {
            Debug.LogWarning("Thiếu icon cho "
                + CropQualityUtility.GetDisplayName(quality));
            return;
        }

        if (qualityOverlay == null)
        {
            GameObject overlayObject = new GameObject("QualityStarOverlay");
            overlayObject.transform.SetParent(transform, false);
            qualityOverlay = overlayObject.AddComponent<SpriteRenderer>();
        }

        qualityOverlay.gameObject.SetActive(true);
        qualityOverlay.sprite = icon;
        qualityOverlay.sortingLayerID = spriteRenderer.sortingLayerID;
        qualityOverlay.sortingOrder = spriteRenderer.sortingOrder + 1;
        qualityOverlay.transform.localPosition = quality == CropQuality.PurpleStar
            ? (Vector3)cropData.purpleStarLocalPosition
            : (Vector3)cropData.goldStarLocalPosition;
        qualityOverlay.transform.localRotation = Quaternion.identity;
        qualityOverlay.transform.localScale =
            Vector3.one * Mathf.Max(0.001f, cropData.qualityIconLocalScale);
    }

    private void RefreshSprite()
    {
        if (spriteRenderer == null || cropData == null)
        {
            return;
        }

        if (currentStage == 1)
        {
            spriteRenderer.sprite = cropData.stage1Sprite;
        }
        else if (currentStage == 2)
        {
            spriteRenderer.sprite = cropData.stage2Sprite;
        }
        else
        {
            spriteRenderer.sprite = cropData.stage3Sprite;
        }
    }

    public bool TryHarvest(out ItemData harvestItem, out int amount)
    {
        return TryHarvest(out harvestItem, out amount, out _);
    }

    public bool TryHarvest(
        out ItemData harvestItem,
        out int amount,
        out CropQuality harvestQuality
    )
    {
        harvestItem = null;
        amount = 0;
        harvestQuality = CropQuality.Normal;

        if (!IsReadyToHarvest)
        {
            return false;
        }

        harvestItem = cropData.harvestItem;
        amount = cropData.harvestAmount;
        harvestQuality = quality;

        return harvestItem != null && amount > 0;
    }
}
