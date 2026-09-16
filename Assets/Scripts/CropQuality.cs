using UnityEngine;

public enum CropQuality
{
    Normal = 0,
    PurpleStar = 1,
    GoldStar = 2
}

public static class CropQualityUtility
{
    private const string PurpleStarResource =
        "CropQuality/crop-quality-purple-star";
    private const string GoldStarResource =
        "CropQuality/crop-quality-gold-star";

    public static string GetDisplayName(CropQuality quality)
    {
        switch (quality)
        {
            case CropQuality.PurpleStar: return "Sao Tím";
            case CropQuality.GoldStar: return "Sao Vàng";
            default: return "Thường";
        }
    }

    public static Sprite LoadDefaultIcon(CropQuality quality)
    {
        switch (quality)
        {
            case CropQuality.PurpleStar:
                return Resources.Load<Sprite>(PurpleStarResource);
            case CropQuality.GoldStar:
                return Resources.Load<Sprite>(GoldStarResource);
            default:
                return null;
        }
    }
}
