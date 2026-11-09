using UnityEngine;

// Deals direct damage and paralyzes whatever it hits for a duration.
// Shop "Power" upgrades paralyze for longer.
public class LightningDragon : ElementalDragon
{
    [Header("Lightning")]
    [SerializeField, Min(0f)] private float hitDamage = 12f;
    [SerializeField] private float paralyzeDuration = 2f;

    public override DragonElement Element => DragonElement.Lightning;

    protected override void ApplyElementEffect(IElementalTarget target, Vector3 targetPosition)
    {
        if (target == null)
        {
            return;
        }

        target.TakeDamage(hitDamage);
        target.ApplyParalyze(paralyzeDuration * PowerMultiplier);
    }
}