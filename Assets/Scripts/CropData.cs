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
    Lettuce = 7
}

[CreateAssetMenu(fileName = "NewCrop", menuName = "Farm Game/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Identity")]
    public string cropName;
    public CropType cropType = CropType.None;

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

    public float Stage1DurationSeconds => testMode
        ? Mathf.Max(0.01f, testStage1DurationSeconds)
        : Mathf.Max(0f, stage1DurationMinutes) * 60f;

    public float Stage2DurationSeconds => testMode
        ? Mathf.Max(0.01f, testStage2DurationSeconds)
        : Mathf.Max(0f, stage2DurationMinutes) * 60f;

    public float TotalGrowthDurationSeconds =>
        Stage1DurationSeconds + Stage2DurationSeconds;
}
