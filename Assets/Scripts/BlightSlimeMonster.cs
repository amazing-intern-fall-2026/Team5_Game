using UnityEngine;

// Appears from day 3. Slow and sturdy. Its effect is BLIGHT: every bite leaves a
// damage-over-time on the crop that keeps ticking even after you kill the slime
// (the crop turns green while blighted). Later nights make the blight hit
// harder and last longer, so you must kill it fast or heal the crop.
public class BlightSlimeMonster : MonsterBase
{
    [Header("Blight")]
    [SerializeField] private float blightDamagePerSecond = 2.5f;
    [SerializeField] private float blightDuration = 6f;
    [SerializeField] private float extraDpsPerTier = 0.4f;
    [SerializeField] private float extraDurationPerTier = 1f;

    protected override void AttackCrop(CropPlant crop)
    {
        DamageCrop(crop, EffectiveDamage);

        if (crop == null || !crop.IsAlive)
        {
            return;
        }

        float dps = (blightDamagePerSecond + extraDpsPerTier * Tier) * scaling.damage;
        float duration = blightDuration + extraDurationPerTier * Tier;
        crop.ApplyDamageOverTime(dps, duration);
    }
}
