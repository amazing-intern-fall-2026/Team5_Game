using System.Collections;
using UnityEngine;

// Deals direct hit damage and applies a burn damage-over-time effect.
// Shop "Power" upgrades raise the damage per tick.
public class FireDragon : ElementalDragon
{
    [Header("Fire")]
    [SerializeField, Min(0f)] private float hitDamage = 10f;
    [SerializeField] private float damagePerTick = 5f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private float dotDuration = 4f;

    public override DragonElement Element => DragonElement.Fire;

    protected override void ApplyElementEffect(IElementalTarget target, Vector3 targetPosition)
    {
        if (target == null)
        {
            return;
        }

        target.TakeDamage(hitDamage);
        StartCoroutine(BurnRoutine(target, damagePerTick * PowerMultiplier));
    }

    private IEnumerator BurnRoutine(IElementalTarget target, float tickDamage)
    {
        int ticks = Mathf.Max(1, Mathf.RoundToInt(dotDuration / tickInterval));

        for (int i = 0; i < ticks; i++)
        {
            if (target == null || target.Equals(null))
            {
                yield break;
            }

            target.TakeDamage(tickDamage);
            yield return new WaitForSeconds(tickInterval);
        }
    }
}