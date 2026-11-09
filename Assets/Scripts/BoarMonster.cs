using System.Collections.Generic;
using UnityEngine;

// Appears from day 5. A heavy brute. Its own logic:
//  - CHARGE: when far from its target it bursts forward at ~3x speed, then has
//    to wait for a cooldown (the cooldown shrinks every tier, so it charges
//    more often as the nights go on).
//  - TRAMPLE: each hit also damages every crop around the target.
//  - HEAVY: resists most of the Earth dragon's knockback.
public class BoarMonster : MonsterBase
{
    [Header("Charge")]
    [SerializeField] private float chargeCooldown = 6f;
    [SerializeField] private float cooldownReductionPerTier = 0.4f;
    [SerializeField] private float minCooldown = 2.5f;
    [SerializeField] private float chargeDuration = 1.2f;
    [SerializeField] private float chargeSpeedMultiplier = 3f;
    [Tooltip("Only starts a charge when at least this far from its target.")]
    [SerializeField] private float chargeMinDistance = 2.5f;

    [Header("Trample")]
    [Tooltip("Crops within this distance of the target also get hit.")]
    [SerializeField] private float trampleRadius = 1.1f;
    [SerializeField, Range(0f, 1f)] private float trampleSplashFactor = 0.5f;

    private float chargeEndTime;
    private float nextChargeTime;

    private bool IsCharging => Time.time < chargeEndTime;

    protected override float KnockbackResistance => 0.6f;

    protected override void OnBeforeMove(Vector2 toTarget)
    {
        if (IsCharging || Time.time < nextChargeTime)
        {
            return;
        }

        if (toTarget.magnitude < chargeMinDistance)
        {
            return;
        }

        float cooldown = Mathf.Max(
            minCooldown,
            chargeCooldown - cooldownReductionPerTier * Tier
        );

        chargeEndTime = Time.time + chargeDuration;
        nextChargeTime = chargeEndTime + cooldown;
    }

    protected override float GetSpeedMultiplier()
    {
        return IsCharging ? chargeSpeedMultiplier : 1f;
    }

    protected override void AttackCrop(CropPlant crop)
    {
        Vector2 center = crop.transform.position;

        // Snapshot: a trampled crop can be destroyed while we loop.
        List<CropPlant> snapshot = new List<CropPlant>(CropPlant.ActiveCrops);

        foreach (CropPlant other in snapshot)
        {
            if (other == null || other == crop || !other.IsAlive)
            {
                continue;
            }

            if (Vector2.Distance(center, other.transform.position) > trampleRadius)
            {
                continue;
            }

            DamageCrop(other, EffectiveDamage * trampleSplashFactor);
        }

        DamageCrop(crop, EffectiveDamage);
    }
}
