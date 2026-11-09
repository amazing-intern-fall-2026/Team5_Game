using UnityEngine;

// Anything a dragon can affect implements this - the dragons never need to
// know what they hit, just that it can take damage/slow/knockback/paralyze.
// Implement this on real enemies later exactly like TrainingDummy does.
public interface IElementalTarget
{
    void TakeDamage(float amount);
    void ApplySlow(float multiplier, float duration);
    void ApplyKnockback(Vector2 direction, float force);
    void ApplyParalyze(float duration);
}
