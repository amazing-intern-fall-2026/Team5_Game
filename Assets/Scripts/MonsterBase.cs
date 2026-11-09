using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// How strong a monster is on a given night. Built by MonsterSpawner.
public readonly struct MonsterScaling
{
    public readonly int day;       // current day number
    public readonly int tier;      // nights since this monster type unlocked (0 = its first night)
    public readonly float health;  // multipliers applied to the prefab's base stats
    public readonly float damage;
    public readonly float speed;

    public MonsterScaling(int day, int tier, float health, float damage, float speed)
    {
        this.day = day;
        this.tier = tier;
        this.health = health;
        this.damage = damage;
        this.speed = speed;
    }

    public static MonsterScaling None => new MonsterScaling(1, 0, 1f, 1f, 1f);
}

// Shared brain for every night monster: finds the nearest crop (preferring
// decoys), walks to it, and bites it on a timer. Monsters only ever attack
// crops - never the player. Because it implements IElementalTarget, all four
// dragons can slow / push / paralyze / burn it with no extra code.
//
// Each monster type is a thin subclass that overrides movement and/or the
// attack (see LocustMonster, BlightSlimeMonster, BoarMonster, NightWraithMonster).
public abstract class MonsterBase : MonoBehaviour, IElementalTarget
{
    private static readonly List<MonsterBase> activeMonsters = new List<MonsterBase>();
    public static IReadOnlyList<MonsterBase> ActiveMonsters => activeMonsters;

    [Header("Base stats (scaled up by the day)")]
    [SerializeField] protected float maxHealth = 40f;
    [SerializeField] protected float moveSpeed = 1.5f;
    [SerializeField] protected float attackDamage = 4f;
    [SerializeField] protected float attackInterval = 1.2f;
    [Tooltip("How close (world units) it must get to a crop before it bites.")]
    [SerializeField] protected float attackReach = 0.6f;

    [Header("Targeting")]
    [Tooltip("Decoy crops count as this fraction of their real distance, so monsters prefer them.")]
    [SerializeField, Range(0.05f, 1f)] private float decoyDistanceFactor = 0.35f;
    [Tooltip("Layer that lets dragons see this monster. Must be included in each dragon's Target Layer.")]
    [SerializeField] private string targetLayerName = "Targetable";

    [Header("Optional visuals")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOrder = 4;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Animator animator;
    [SerializeField] private string isMovingParam = "IsMoving";

    protected MonsterScaling scaling = MonsterScaling.None;

    private float currentHealth;
    private float maxHealthEffective;
    private CropPlant targetCrop;
    private float nextAttackTime;
    private float retargetTimer;
    private float slowMultiplier = 1f;
    private float slowEndTime;
    private float paralyzeEndTime;
    private bool isDead;
    private bool isKnockedBack;
    private bool animatorHasMovingParam;
    private Color baseColor = Color.white;
    private Coroutine knockbackRoutine;
    private Coroutine flashRoutine;

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public bool IsSlowed => Time.time < slowEndTime;
    public bool IsParalyzed => Time.time < paralyzeEndTime;

    protected int Tier => scaling.tier;
    protected int Day => scaling.day;
    protected CropPlant TargetCrop => targetCrop;
    protected float EffectiveDamage => attackDamage * scaling.damage;
    protected float EffectiveSpeed =>
        moveSpeed * scaling.speed * (IsSlowed ? slowMultiplier : 1f);

    // Subclasses can make a monster heavier / more ethereal.
    protected virtual float KnockbackResistance => 0f;
    protected virtual float ParalyzeDurationMultiplier => 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeMonsters.Clear();
    }

    protected virtual void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
            baseColor = spriteRenderer.color;
        }

        int layer = LayerMask.NameToLayer(targetLayerName);
        if (layer >= 0)
        {
            gameObject.layer = layer;
        }
        else
        {
            Debug.LogWarning(
                $"[Quái] Layer '{targetLayerName}' chưa tồn tại - rồng sẽ không thấy {name}."
            );
        }

        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D hitbox = gameObject.AddComponent<CircleCollider2D>();
            hitbox.isTrigger = true;
            hitbox.radius = 0.4f;
        }

        CacheAnimatorParameter();

        maxHealthEffective = maxHealth;
        currentHealth = maxHealthEffective;
    }

    protected virtual void OnEnable()
    {
        if (!activeMonsters.Contains(this))
        {
            activeMonsters.Add(this);
        }
    }

    protected virtual void OnDisable()
    {
        activeMonsters.Remove(this);
    }

    /// <summary>Called by MonsterSpawner right after the monster is created.</summary>
    public void Initialize(MonsterScaling newScaling)
    {
        scaling = newScaling;
        maxHealthEffective = maxHealth * scaling.health;
        currentHealth = maxHealthEffective;
        OnScalingApplied();
    }

    protected virtual void OnScalingApplied() { }

    protected virtual void Update()
    {
        if (isDead)
        {
            return;
        }

        if (IsParalyzed || isKnockedBack)
        {
            SetMoving(false);
            return;
        }

        retargetTimer -= Time.deltaTime;
        if (targetCrop == null || !targetCrop.IsAlive || retargetTimer <= 0f)
        {
            targetCrop = FindTarget();
            retargetTimer = 0.75f;
        }

        if (targetCrop == null)
        {
            SetMoving(false);
            return;
        }

        Vector2 toTarget =
            (Vector2)targetCrop.transform.position - (Vector2)transform.position;

        if (toTarget.magnitude > attackReach)
        {
            OnBeforeMove(toTarget);
            Move(toTarget);
        }
        else
        {
            SetMoving(false);
            TryAttack();
        }
    }

    // ---- hooks the four monster types override -----------------------------

    /// <summary>Runs every frame before moving (charge / blink decisions).</summary>
    protected virtual void OnBeforeMove(Vector2 toTarget) { }

    /// <summary>Direction to walk this frame. Default: straight at the target.</summary>
    protected virtual Vector2 GetMoveDirection(Vector2 toTarget) => toTarget.normalized;

    /// <summary>Extra speed factor this frame (e.g. a boar charge).</summary>
    protected virtual float GetSpeedMultiplier() => 1f;

    /// <summary>What a bite actually does. Each monster type implements its own.</summary>
    protected abstract void AttackCrop(CropPlant crop);

    protected virtual void OnDied() { }

    // ---- shared behaviour ----------------------------------------------------

    protected virtual CropPlant FindTarget()
    {
        CropPlant best = null;
        float bestScore = float.MaxValue;

        foreach (CropPlant crop in CropPlant.ActiveCrops)
        {
            if (crop == null || !crop.IsAlive)
            {
                continue;
            }

            float distance =
                Vector2.Distance(transform.position, crop.transform.position);
            float score = crop.IsDecoy ? distance * decoyDistanceFactor : distance;

            if (score < bestScore)
            {
                bestScore = score;
                best = crop;
            }
        }

        return best;
    }

    protected void DamageCrop(CropPlant crop, float amount)
    {
        if (crop != null && crop.IsAlive)
        {
            crop.TakeDamage(amount, this);
        }
    }

    protected void HealSelf(float amount)
    {
        if (!isDead && amount > 0f)
        {
            currentHealth = Mathf.Min(maxHealthEffective, currentHealth + amount);
        }
    }

    private void Move(Vector2 toTarget)
    {
        Vector2 direction = GetMoveDirection(toTarget);
        float speed = EffectiveSpeed * GetSpeedMultiplier();

        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.05f)
        {
            spriteRenderer.flipX = direction.x < 0f;
        }

        SetMoving(true);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackInterval;
        AttackCrop(targetCrop);
    }

    // ---- IElementalTarget (how the dragons affect monsters) ----------------

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        currentHealth -= amount;
        Flash();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void ApplySlow(float multiplier, float duration)
    {
        if (isDead)
        {
            return;
        }

        float clamped = Mathf.Clamp(multiplier, 0.05f, 1f);
        slowMultiplier = IsSlowed ? Mathf.Min(slowMultiplier, clamped) : clamped;
        slowEndTime = Mathf.Max(slowEndTime, Time.time + duration);
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (isDead)
        {
            return;
        }

        float scaledForce = force * (1f - Mathf.Clamp01(KnockbackResistance));
        if (scaledForce <= 0.01f)
        {
            return;
        }

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        knockbackRoutine = StartCoroutine(
            KnockbackRoutine(direction.normalized * scaledForce)
        );
    }

    public void ApplyParalyze(float duration)
    {
        if (isDead)
        {
            return;
        }

        paralyzeEndTime = Mathf.Max(
            paralyzeEndTime,
            Time.time + duration * ParalyzeDurationMultiplier
        );
    }

    // ---- lifecycle -----------------------------------------------------------

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Debug.Log($"[Quái] {name} đã bị hạ.");
        OnDied();
        Destroy(gameObject);
    }

    /// <summary>Removes the monster without it counting as a kill (used at dawn).</summary>
    public void Despawn()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Destroy(gameObject);
    }

    public static void DespawnAll()
    {
        List<MonsterBase> snapshot = new List<MonsterBase>(activeMonsters);
        foreach (MonsterBase monster in snapshot)
        {
            if (monster != null)
            {
                monster.Despawn();
            }
        }
    }

    private IEnumerator KnockbackRoutine(Vector2 offset)
    {
        isKnockedBack = true;

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)offset;
        const float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        isKnockedBack = false;
        knockbackRoutine = null;
    }

    private void Flash()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = baseColor;
        flashRoutine = null;
    }

    // Only touch Animator parameters that actually exist, so a missing one can
    // never spam "Parameter does not exist" warnings.
    private void CacheAnimatorParameter()
    {
        animatorHasMovingParam = false;

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == isMovingParam
                && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animatorHasMovingParam = true;
                return;
            }
        }
    }

    private void SetMoving(bool moving)
    {
        if (animatorHasMovingParam)
        {
            animator.SetBool(isMovingParam, moving);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackReach);
    }
}
