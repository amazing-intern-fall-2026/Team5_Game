using UnityEngine;

// Appears from day 7. An ethereal drainer. Its own logic:
//  - BLINK: when far from its target it teleports right next to it, with a
//    cooldown that shrinks every tier.
//  - DRAIN: each bite rewinds the crop's growth (so a ripe crop can wither back
//    to an earlier stage) and heals the wraith for part of the damage dealt.
//  - ETHEREAL: Lightning dragon paralysis only lasts half as long on it.
public class NightWraithMonster : MonsterBase
{
    [Header("Blink")]
    [SerializeField] private float blinkCooldown = 6f;
    [SerializeField] private float cooldownReductionPerTier = 0.5f;
    [SerializeField] private float minCooldown = 2.5f;
    [Tooltip("Only blinks when at least this far from its target.")]
    [SerializeField] private float blinkMinDistance = 3f;
    [Tooltip("How far from the crop it lands.")]
    [SerializeField] private float blinkArriveDistance = 1.2f;

    [Header("Drain")]
    [Tooltip("Fraction (0-1) of the crop's TOTAL growth time rewound per bite.")]
    [SerializeField, Range(0f, 1f)] private float drainGrowthFraction = 0.12f;
    [SerializeField, Range(0f, 0.2f)] private float extraDrainPerTier = 0.02f;
    [Tooltip("Fraction of bite damage the wraith heals itself for.")]
    [SerializeField, Range(0f, 1f)] private float lifestealFraction = 0.5f;

    private float nextBlinkTime;

    protected override float ParalyzeDurationMultiplier => 0.5f;

    protected override void Awake()
    {
        base.Awake();
        nextBlinkTime = Time.time + 2f; // don't blink the instant it spawns
    }

    protected override void OnBeforeMove(Vector2 toTarget)
    {
        if (Time.time < nextBlinkTime || toTarget.magnitude < blinkMinDistance)
        {
            return;
        }

        Vector2 targetPosition = TargetCrop.transform.position;
        Vector2 arrival = targetPosition - toTarget.normalized * blinkArriveDistance;
        transform.position = new Vector3(arrival.x, arrival.y, transform.position.z);

        nextBlinkTime = Time.time + Mathf.Max(
            minCooldown,
            blinkCooldown - cooldownReductionPerTier * Tier
        );
    }

    protected override void AttackCrop(CropPlant crop)
    {
        float damage = EffectiveDamage;
        DamageCrop(crop, damage);

        if (crop != null && crop.IsAlive)
        {
            crop.DrainGrowth(drainGrowthFraction + extraDrainPerTier * Tier);
        }

        HealSelf(damage * lifestealFraction);
    }
}
