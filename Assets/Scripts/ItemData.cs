using UnityEngine;

public enum FertilizerType
{
    None = 0,
    Nitrogen = 1,
    NPK = 2,
    Phosphate = 3,
    Potassium = 4
}

public enum ItemType
{
    General = 0,
    Tool = 1,
    Seed = 2,
    Produce = 3,
    Fertilizer = 4
}

public enum SellPriceMode
{
    Fixed = 0,
    HalfOrOneAndHalf = 1
}

[System.Serializable]
public class FertilizerEffect
{
    public CropType cropType;
    [Min(1)] public int maximumApplications = 1;
    [Min(0f)] public float reductionMinutes;
    [Min(0f)] public float testReductionSeconds = 1f;
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Farm Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic information")]
    public string itemName;
    public Sprite icon;
    public int maxStack = 99;
    public ItemType itemType = ItemType.General;

    [Header("Economy")]
    [Min(0)] public int purchasePrice;
    [Min(0)] public int sellPrice;
    public SellPriceMode sellPriceMode = SellPriceMode.Fixed;

    [Header("Farm actions")]
    public bool canHoe;
    public bool canWater;

    [Header("Seed")]
    public CropData cropToPlant;

    [Header("Fertilizer")]
    public FertilizerType fertilizerType = FertilizerType.None;
    public bool fertilizerTestMode;
    public FertilizerEffect[] fertilizerEffects;

    public bool TryGetFertilizerEffect(
        CropType cropType,
        out int maximumApplications,
        out float reductionSeconds
    )
    {
        maximumApplications = 0;
        reductionSeconds = 0f;

        if (fertilizerType == FertilizerType.None || fertilizerEffects == null)
        {
            return false;
        }

        foreach (FertilizerEffect effect in fertilizerEffects)
        {
            if (effect != null && effect.cropType == cropType)
            {
                maximumApplications = Mathf.Max(1, effect.maximumApplications);
                reductionSeconds = fertilizerTestMode
                    ? Mathf.Max(0f, effect.testReductionSeconds)
                    : Mathf.Max(0f, effect.reductionMinutes) * 60f;
                return true;
            }
        }

        return false;
    }

    public int CalculateSellPrice(float randomValue)
    {
        if (sellPriceMode == SellPriceMode.HalfOrOneAndHalf)
        {
            float multiplier = randomValue < 0.5f ? 0.5f : 1.5f;
            return Mathf.RoundToInt(sellPrice * multiplier);
        }

        return sellPrice;
    }
}
