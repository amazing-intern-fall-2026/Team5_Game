using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base for a stationary elemental animal. Deliberately has no wander
// script - place it once in the scene and it stays put, periodically
// scanning for anything on Target Layer within range and applying its
// specific element to the nearest one. Subclasses only implement the
// element itself (Fire/Ice/Earth/Lightning).
//
// [Rồng] Shop upgrades: every value below is the BASE value. Range, attack
// speed and the element's own power are multiplied by the upgrade level the
// player bought in the shop (see DragonStats). With no DragonStats in the
// scene every multiplier is 1, so dragons behave exactly as before.
public abstract class ElementalDragon : MonoBehaviour
{
    private static readonly List<ElementalDragon> active = new List<ElementalDragon>();

    [Header("Targeting")]
    [SerializeField] private float range = 4f;
    [SerializeField] private float attackInterval = 2f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Optional visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerParam = "Attack";

    /// <summary>Which dragon this is. Decides which shop upgrades apply to it.</summary>
    public abstract DragonElement Element { get; }

    public static IReadOnlyList<ElementalDragon> Active => active;

    // Base values after shop upgrades.
    public float EffectiveRange =>
        range * DragonStats.GetMultiplier(Element, DragonStat.Range);

    public float EffectiveAttackInterval =>
        attackInterval / Mathf.Max(0.1f, DragonStats.GetMultiplier(Element, DragonStat.AttackSpeed));

    /// <summary>Multiplier for this element's own effect (burn damage, slow, push, paralyze).</summary>
    protected float PowerMultiplier =>
        DragonStats.GetMultiplier(Element, DragonStat.Power);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active.Clear();
    }

    /// <summary>Is at least one dragon of this element on the farm?</summary>
    public static bool HasDragon(DragonElement element)
    {
        foreach (ElementalDragon dragon in active)
        {
            if (dragon != null && dragon.Element == element)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The current sprite of a dragon of this element, for the shop icon.</summary>
    public static Sprite GetIcon(DragonElement element)
    {
        foreach (ElementalDragon dragon in active)
        {
            if (dragon == null || dragon.Element != element)
            {
                continue;
            }

            SpriteRenderer renderer = dragon.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                return renderer.sprite;
            }
        }

        return null;
    }

    private void OnEnable()
    {
        if (!active.Contains(this))
        {
            active.Add(this);
        }
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    private void Start()
    {
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            TryAttackNearestTarget();
            // Read each loop so an upgrade bought in the shop applies right away.
            yield return new WaitForSeconds(EffectiveAttackInterval);
        }
    }

    private void TryAttackNearestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, EffectiveRange, targetLayer);

        Collider2D nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit.GetComponent<IElementalTarget>() == null)
                continue;

            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = hit;
            }
        }

        if (nearest == null)
            return;

        IElementalTarget target = nearest.GetComponent<IElementalTarget>();
        ApplyElementEffect(target, nearest.transform.position);

        if (animator != null)
            animator.SetTrigger(attackTriggerParam);
    }

    protected abstract void ApplyElementEffect(IElementalTarget target, Vector3 targetPosition);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, EffectiveRange);
    }
}
