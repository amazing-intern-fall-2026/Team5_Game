using System.Collections;
using TMPro;
using UnityEngine;

// Stationary test target for the elemental dragons. Reacts visibly to all
// four effects: HP drops (and flashes) from damage, knockback actually
// moves it, slow/paralyze show as status text since a dummy has no
// movement/actions of its own to visibly interrupt.
public class TrainingDummy : MonoBehaviour, IElementalTarget
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Optional display")]
    [Tooltip("World-space TextMeshPro above the dummy, not UI. Leave empty to rely on Console logs only.")]
    [SerializeField] private TextMeshPro statusText;

    [Header("Optional visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.1f;

    private float currentHealth;
    private float slowEndTime;
    private float slowMultiplier = 1f;
    private float paralyzeEndTime;
    private Vector3 basePosition;
    private Color originalColor;

    private Coroutine knockbackRoutine;
    private Coroutine respawnRoutine;
    private Coroutine flashRoutine;

    public bool IsSlowed => Time.time < slowEndTime;
    public bool IsParalyzed => Time.time < paralyzeEndTime;

    private void Awake()
    {
        currentHealth = maxHealth;
        basePosition = transform.position;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Update()
    {
        UpdateStatusText();
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        FlashHit();

        Debug.Log($"[Dummy] Nhận {amount} sát thương. HP còn {currentHealth:0}/{maxHealth:0}.");

        if (currentHealth <= 0f)
        {
            Debug.Log($"[Dummy] Đã bị hạ. Hồi sinh sau {respawnDelay}s.");

            if (respawnRoutine != null)
                StopCoroutine(respawnRoutine);

            respawnRoutine = StartCoroutine(RespawnRoutine());
        }
    }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = Mathf.Clamp01(multiplier);
        slowEndTime = Mathf.Max(slowEndTime, Time.time + duration);
        Debug.Log($"[Dummy] Bị làm chậm còn x{slowMultiplier:0.0} trong {duration}s.");
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction, force));
        Debug.Log($"[Dummy] Bị đẩy lùi với lực {force}.");
    }

    public void ApplyParalyze(float duration)
    {
        paralyzeEndTime = Mathf.Max(paralyzeEndTime, Time.time + duration);
        Debug.Log($"[Dummy] Bị tê liệt trong {duration}s.");
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force)
    {
        Vector3 start = transform.position;
        Vector3 target = start + (Vector3)(direction.normalized * force);
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        transform.position = target;
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        currentHealth = maxHealth;
        transform.position = basePosition;
        slowMultiplier = 1f;
        slowEndTime = 0f;
        paralyzeEndTime = 0f;

        Debug.Log("[Dummy] Đã hồi sinh với đầy đủ HP.");
    }

    private void FlashHit()
    {
        if (spriteRenderer == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
            return;

        string status = $"HP: {currentHealth:0}/{maxHealth:0}";

        if (IsSlowed)
            status += $"\n<color=#66CCFF>SLOWED x{slowMultiplier:0.0}</color>";

        if (IsParalyzed)
            status += "\n<color=#FFCC00>PARALYZED</color>";

        statusText.text = status;
    }
}
