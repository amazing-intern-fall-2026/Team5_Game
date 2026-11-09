using UnityEngine;

// The passive ability of the 4 special crops, added automatically by CropPlant
// when its CropData has a Special Type. Driven entirely by CropData:
//
//   Thorns     - reflects damage back at any monster that bites this crop.
//   RepelAura  - slows every monster inside the radius (Special Power = slow 0-1).
//   HealAura   - heals every crop inside the radius each pulse.
//   Decoy      - no logic here: monsters prefer decoys (see MonsterBase.FindTarget)
//                and the crop simply has far more health.
//
// By default the effect only works once the crop is fully grown, so growing one
// to maturity is a real choice: keep it as a defence, or harvest it for money.
public sealed class SpecialCropEffect : MonoBehaviour
{
    private CropPlant crop;
    private float pulseTimer;

    private bool IsActive =>
        crop != null
        && crop.Data != null
        && crop.IsAlive
        && (!crop.Data.specialRequiresMature || crop.IsReadyToHarvest);

    public void Initialize(CropPlant owner)
    {
        if (crop != null)
        {
            crop.Damaged -= HandleDamaged;
        }

        crop = owner;
        crop.Damaged += HandleDamaged;
        pulseTimer = 0f;
    }

    private void OnDestroy()
    {
        if (crop != null)
        {
            crop.Damaged -= HandleDamaged;
        }
    }

    private void Update()
    {
        if (!IsActive)
        {
            return;
        }

        CropData data = crop.Data;

        if (data.specialType != SpecialCropType.RepelAura
            && data.specialType != SpecialCropType.HealAura)
        {
            return;
        }

        pulseTimer += Time.deltaTime;
        if (pulseTimer < data.specialInterval)
        {
            return;
        }

        pulseTimer = 0f;

        if (data.specialType == SpecialCropType.RepelAura)
        {
            PulseRepel(data);
        }
        else
        {
            PulseHeal(data);
        }
    }

    private void PulseRepel(CropData data)
    {
        float speedMultiplier = Mathf.Clamp01(1f - data.specialPower);
        float duration = data.specialInterval * 1.5f; // overlaps the next pulse: no gaps

        foreach (MonsterBase monster in MonsterBase.ActiveMonsters)
        {
            if (monster == null || monster.IsDead)
            {
                continue;
            }

            if (Vector2.Distance(transform.position, monster.transform.position)
                <= data.specialRadius)
            {
                monster.ApplySlow(speedMultiplier, duration);
            }
        }
    }

    private void PulseHeal(CropData data)
    {
        foreach (CropPlant other in CropPlant.ActiveCrops)
        {
            if (other == null || !other.IsAlive)
            {
                continue;
            }

            if (Vector2.Distance(transform.position, other.transform.position)
                <= data.specialRadius)
            {
                other.Heal(data.specialPower);
            }
        }
    }

    private void HandleDamaged(float amount, IElementalTarget attacker)
    {
        if (!IsActive
            || crop.Data.specialType != SpecialCropType.Thorns
            || attacker == null)
        {
            return;
        }

        attacker.TakeDamage(crop.Data.specialPower);
    }

    private void OnDrawGizmosSelected()
    {
        if (crop == null || crop.Data == null || crop.Data.specialRadius <= 0f)
        {
            return;
        }

        Gizmos.color = crop.Data.specialType == SpecialCropType.HealAura
            ? new Color(0.3f, 1f, 0.4f, 0.8f)
            : new Color(0.7f, 0.4f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, crop.Data.specialRadius);
    }
}
