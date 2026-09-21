public enum FertilizerApplicationResult
{
    Success,
    NoCrop,
    WrongType,
    ApplicationLimitReached,
    CropMature,
    OutOfSeason
}

/// <summary>
/// Compatibility is read from ItemData so designers can tune fertilizer
/// timing and Test Mode directly in the Inspector.
/// </summary>
public static class FertilizerRules
{
    public static bool TryGetRule(
        CropType cropType,
        ItemData fertilizerItem,
        out int maximumApplications,
        out float reductionSeconds
    )
    {
        if (fertilizerItem == null)
        {
            maximumApplications = 0;
            reductionSeconds = 0f;
            return false;
        }

        return fertilizerItem.TryGetFertilizerEffect(
            cropType,
            out maximumApplications,
            out reductionSeconds
        );
    }
}
