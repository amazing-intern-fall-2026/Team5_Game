using UnityEngine;

// Deals direct damage and pushes whatever it hits away from itself.
// Shop "Power" upgrades push harder.
public class EarthDragon : ElementalDragon
{
    [Header("Earth")]
    [SerializeField, Min(0f)] private float hitDamage = 15f;
    [SerializeField] private float knockbackForce = 3f;

    public override DragonElement Element => DragonElement.Earth;

    protected override void ApplyElementEffect(IElementalTarget target, Vector3 targetPosition)
    {
        if (target == null)
        {
            return;
        }

        target.TakeDamage(hitDamage);

        Vector2 direction = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        target.ApplyKnockback(direction, knockbackForce * PowerMultiplier);
    }
}