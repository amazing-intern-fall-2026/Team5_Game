using UnityEngine;

// Appears from day 1. Fast, fragile, and always comes in swarms. Its own logic:
//  - zigzags toward its target instead of walking straight
//  - bites rapidly, and bites MORE times per attack the longer it has been
//    unlocked (tier), so late-game swarms strip crops much faster.
public class LocustMonster : MonsterBase
{
    [Header("Locust")]
    [SerializeField] private float zigzagAmplitude = 0.7f;
    [SerializeField] private float zigzagFrequency = 7f;
    [Tooltip("One extra bite per attack every this many nights since it unlocked.")]
    [SerializeField, Min(1)] private int tiersPerExtraBite = 3;

    private float phase;

    protected override void Awake()
    {
        base.Awake();
        phase = Random.value * Mathf.PI * 2f; // so a swarm doesn't wobble in sync
    }

    protected override Vector2 GetMoveDirection(Vector2 toTarget)
    {
        Vector2 forward = toTarget.normalized;
        Vector2 side = new Vector2(-forward.y, forward.x);

        // Wobble fades out near the crop so it can actually land and bite.
        float settle = Mathf.Clamp01(toTarget.magnitude / 2f);
        float wobble =
            Mathf.Sin(Time.time * zigzagFrequency + phase) * zigzagAmplitude * settle;

        return (forward + side * wobble).normalized;
    }

    protected override void AttackCrop(CropPlant crop)
    {
        int bites = 1 + Tier / tiersPerExtraBite;

        for (int i = 0; i < bites; i++)
        {
            DamageCrop(crop, EffectiveDamage);
        }
    }
}
