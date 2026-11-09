using UnityEngine;

// Deals direct damage and slows whatever it hits for a duration.
// Shop "Power" upgrades make the slow both stronger and longer.
public class IceDragon : ElementalDragon
{
    [Header("Ice")]
    [SerializeField, Min(0f)] private float hitDamage = 10f;
    [SerializeField, Range(0f, 1f)] private float slowMultiplier = 0.4f;
    [SerializeField] private float slowDuration = 3f;

    public override DragonElement Element => DragonElement.Ice;

    protected override void ApplyElementEffect(IElementalTarget target, Vector3 targetPosition)
    {
        if (target == null)
        {
            return;
        }

        target.TakeDamage(hitDamage);

        // Slow Multiplier 0.4 means "moves at 40% speed" = 60% slow strength.
        float baseStrength = 1f - slowMultiplier;
        float strength = Mathf.Min(0.9f, baseStrength * PowerMultiplier);

        target.ApplySlow(1f - strength, slowDuration * PowerMultiplier);
    }
}