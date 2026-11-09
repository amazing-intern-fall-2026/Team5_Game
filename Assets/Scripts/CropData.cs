using UnityEngine;

public enum CropType
{
    None = 0,
    Rice = 1,
    Corn = 2,
    Tomato = 3,
    Chili = 4,
    Strawberry = 5,
    Okra = 6,
    Lettuce = 7,
    // [Quái] Special defensive crops
    Thornvine = 8,
    Lavender = 9,
    HealingAloe = 10,
    DecoyPumpkin = 11
}

public enum SeasonType
{
    Spring = 0,
    Summer = 1,
    Autumn = 2,
    Winter = 3
}

// [Quái] Passive ability a crop can have while it grows.
public enum SpecialCropType
{
    None = 0,
    Thorns = 1,      // reflects damage back at monsters that bite it
    RepelAura = 2,   // slows monsters inside its radius
    HealAura = 3,    // heals crops inside its radius
    Decoy = 4        // monsters prefer to attack it; very durable
}

[CreateAssetMenu(fileName = "NewCrop", menuName = "Farm Game/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Identity")]
    public string cropName;
    public CropType cropType = CropType.None;

    [Header("Season")]
    public SeasonType[] allowedSeasons = { SeasonType.Spring };

    [Header("Growth sprites")]
    public Sprite stage1Sprite;
    public Sprite stage2Sprite;
    public Sprite stage3Sprite;

    [Header("Growth time mode")]
    public bool testMode;

    [Header("Balanced growth time (minutes)")]
    [Min(0f)] public float stage1DurationMinutes = 5f;
    [Min(0f)] public float stage2DurationMinutes = 5f;

    [Header("Test growth time (seconds)")]
    [Min(0.01f)] public float testStage1DurationSeconds = 2f;
    [Min(0.01f)] public float testStage2DurationSeconds = 2f;

    [Header("Harvest")]
    public ItemData harvestItem;
    [Min(1)] public int harvestAmount = 1;

    // [Quái] Monsters attack crops, so every crop has health.
    [Header("Durability (monsters attack crops)")]
    [Min(1f)] public float maxHealth = 30f;

    // [Quái] Optional passive ability (used by the 4 special crops).
    [Header("Special effect (optional)")]
    public SpecialCropType specialType = SpecialCropType.None;
    [Tooltip("If true, the effect only works once the crop is fully grown.")]
    public bool specialRequiresMature = true;
    [Tooltip("Thorns: damage reflected per hit. RepelAura: slow strength 0-1 (0.5 = half speed). HealAura: HP restored per pulse.")]
    [Min(0f)] public float specialPower = 5f;
    [Tooltip("Aura radius in world units (RepelAura / HealAura).")]
    [Min(0f)] public float specialRadius = 2.5f;
    [Tooltip("Seconds between aura pulses.")]
    [Min(0.1f)] public float specialInterval = 1.5f;

    [Header("Mutation chance after first valid fertilizer")]
    [Min(0f)] public float normalQualityWeight = 94f;
    [Min(0f)] public float purpleStarWeight = 5f;
    [Min(0f)] public float goldStarWeight = 1f;

    [Header("Mutation overlay")]
    public Sprite purpleStarIcon;
    public Sprite goldStarIcon;
    public Vector2 purpleStarLocalPosition = new Vector2(-4f, 5f);
    public Vector2 goldStarLocalPosition = new Vector2(4f, 5f);
    [Min(0.001f)] public float qualityIconLocalScale = 0.08f;

    public bool HasSpecialEffect => specialType != SpecialCropType.None;

    public float Stage1DurationSeconds => testMode
        ? Mathf.Max(0.01f, testStage1DurationSeconds)
        : Mathf.Max(0f, stage1DurationMinutes) * 60f;

    public float Stage2DurationSeconds => testMode
        ? Mathf.Max(0.01f, testStage2DurationSeconds)
        : Mathf.Max(0f, stage2DurationMinutes) * 60f;

    public float TotalGrowthDurationSeconds =>
        Stage1DurationSeconds + Stage2DurationSeconds;

    public bool IsAllowedInSeason(SeasonType season)
    {
        if (allowedSeasons == null || allowedSeasons.Length == 0)
        {
            return false;
        }

        foreach (SeasonType allowedSeason in allowedSeasons)
        {
            if (allowedSeason == season)
            {
                return true;
            }
        }

        return false;
    }

    public CropQuality RollQuality(float random01)
    {
        float normal = Mathf.Max(0f, normalQualityWeight);
        float purple = Mathf.Max(0f, purpleStarWeight);
        float gold = Mathf.Max(0f, goldStarWeight);
        float total = normal + purple + gold;

        if (total <= 0f)
        {
            return CropQuality.Normal;
        }

        float roll = Mathf.Clamp01(random01) * total;
        if (roll < normal)
        {
            return CropQuality.Normal;
        }

        return roll < normal + purple
            ? CropQuality.PurpleStar
            : CropQuality.GoldStar;
    }

    public Sprite GetQualityIcon(CropQuality quality)
    {
        if (quality == CropQuality.PurpleStar)
        {
            return purpleStarIcon != null
                ? purpleStarIcon
                : CropQualityUtility.LoadDefaultIcon(quality);
        }

        if (quality == CropQuality.GoldStar)
        {
            return goldStarIcon != null
                ? goldStarIcon
                : CropQualityUtility.LoadDefaultIcon(quality);
        }

        return null;
    }
}
