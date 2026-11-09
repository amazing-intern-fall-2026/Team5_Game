using System.Collections.Generic;
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

    // [Quái] --- health / monster interaction ---------------------------
    private static readonly List<CropPlant> activeCrops = new List<CropPlant>();
    private float currentHealth;
    private bool isDying;
    private float dotDamagePerSecond;
    private float dotRemainingSeconds;
    private float dotTickTimer;

    public static IReadOnlyList<CropPlant> ActiveCrops => activeCrops;

    /// <summary>Raised when monsters destroy this crop. Harvesting does NOT raise it.</summary>
    public event System.Action<CropPlant> Died;

    /// <summary>Raised on every hit: amount, and the attacker (null for damage-over-time).</summary>
    public event System.Action<float, IElementalTarget> Damaged;

    public CropData Data => cropData;
    public float MaxHealth => cropData != null ? Mathf.Max(1f, cropData.maxHealth) : 1f;
    public float CurrentHealth => currentHealth;
    public bool IsAlive => !isDying && currentHealth > 0f;
    public bool IsPoisoned => dotRemainingSeconds > 0f;
    public bool IsDecoy => cropData != null
        && cropData.specialType == SpecialCropType.Decoy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeCrops.Clear();
    }

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

        // [Quái]
        currentHealth = MaxHealth;
        isDying = false;
        dotRemainingSeconds = 0f;
        dotTickTimer = 0f;
        if (!activeCrops.Contains(this))
        {
            activeCrops.Add(this);
        }
        AttachSpecialEffect();

        RefreshSprite();
        RefreshQualityOverlay();
        RefreshTint();
    }

    private void Update()
    {
        if (cropData == null)
        {
            return;
        }

        // [Quái] Blight keeps ticking even when the crop is out of season.
        UpdateDamageOverTime();
        if (!IsAlive)
        {
            return;
        }

        if (!IsReadyToHarvest
            && SeasonManager.Instance != null
            && !cropData.IsAllowedInSeason(SeasonManager.Instance.CurrentSeason))
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

    // [Quái] ------------------------------------------------------------
    public void TakeDamage(float amount, IElementalTarget attacker = null)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        RefreshTint();
        Damaged?.Invoke(amount, attacker);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        RefreshTint();
    }

    /// <summary>Blight: damage over time that keeps ticking even if the attacker dies.</summary>
    public void ApplyDamageOverTime(float damagePerSecond, float duration)
    {
        if (!IsAlive || damagePerSecond <= 0f || duration <= 0f)
        {
            return;
        }

        dotDamagePerSecond = Mathf.Max(
            IsPoisoned ? dotDamagePerSecond : 0f,
            damagePerSecond
        );
        dotRemainingSeconds = Mathf.Max(dotRemainingSeconds, duration);
        RefreshTint();
    }

    /// <summary>Wraith drain: rewinds growth by a fraction (0-1) of the total growth time.</summary>
    public void DrainGrowth(float fractionOfTotalGrowth)
    {
        if (cropData == null || !IsAlive)
        {
            return;
        }

        float seconds = cropData.TotalGrowthDurationSeconds
            * Mathf.Clamp01(fractionOfTotalGrowth);
        growthElapsedSeconds = Mathf.Max(0f, growthElapsedSeconds - seconds);
        UpdateGrowthStage();
    }

    private void UpdateDamageOverTime()
    {
        if (!IsAlive || dotRemainingSeconds <= 0f)
        {
            return;
        }

        float step = Mathf.Min(Time.deltaTime, dotRemainingSeconds);
        dotRemainingSeconds -= step;
        dotTickTimer += step;

        if (dotTickTimer >= 0.5f || dotRemainingSeconds <= 0f)
        {
            float tickDamage = dotDamagePerSecond * dotTickTimer;
            dotTickTimer = 0f;
            TakeDamage(tickDamage, null);
            if (IsAlive && dotRemainingSeconds <= 0f)
            {
                RefreshTint();
            }
        }
    }

    private void Die()
    {
        if (isDying)
        {
            return;
        }

        isDying = true;
        activeCrops.Remove(this);
        Debug.Log($"[Quái] {CropName} đã bị phá hủy.");
        Died?.Invoke(this);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        activeCrops.Remove(this);
    }

    private void RefreshTint()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        float fraction = Mathf.Clamp01(currentHealth / MaxHealth);
        Color tint = Color.Lerp(new Color(1f, 0.4f, 0.4f, 1f), Color.white, fraction);
        if (IsPoisoned)
        {
            tint *= new Color(0.75f, 1f, 0.6f, 1f); // sickly green while blighted
        }

        spriteRenderer.color = tint;
    }

    private void AttachSpecialEffect()
    {
        if (cropData == null || !cropData.HasSpecialEffect)
        {
            return;
        }

        SpecialCropEffect effect = GetComponent<SpecialCropEffect>();
        if (effect == null)
        {
            effect = gameObject.AddComponent<SpecialCropEffect>();
        }

        effect.Initialize(this);
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

        if (SeasonManager.Instance != null
            && !cropData.IsAllowedInSeason(SeasonManager.Instance.CurrentSeason))
        {
            return FertilizerApplicationResult.OutOfSeason;
        }

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
