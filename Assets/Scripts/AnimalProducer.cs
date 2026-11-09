using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Gives a farm animal (Pig, Chicken, Cow, Sheep) two things:
//
//  1. SATISFACTION (0-100). It falls a little every new day. Press F next to
//     the animal to look after it:
//        - if you carry "Thức ăn chăn nuôi" it eats one (big boost)
//        - otherwise you pet it (small boost, short cooldown)
//     A bar above its head (shown when you are close) has a white tick marking
//     the MINIMUM satisfaction it needs.
//
//  2. PRODUCTION. Every few days the animal DROPS an item on the ground (egg,
//     milk, wool, truffle ...) - but only while its satisfaction is at or above
//     the minimum. Walk over the drop to pick it up, then sell it in the shop.
//     Higher satisfaction can also make the item a purple / gold star quality.
//
// Add it to the animal prefab next to RandomWanderAI. It does not touch the
// wander / animation logic at all, so animals keep moving exactly as before.
[DisallowMultipleComponent]
public sealed class AnimalProducer : MonoBehaviour
{
    private static readonly List<AnimalProducer> active = new List<AnimalProducer>();
    private static Sprite barSprite;
    private static PlayerMovement cachedPlayer;
    private static ItemStorageController cachedStorage;

    public static IReadOnlyList<AnimalProducer> Active => active;

    [Header("Display")]
    [Tooltip("Name used in messages. Falls back to the GameObject name.")]
    [SerializeField] private string displayName;

    [Header("Production")]
    [SerializeField] private ItemData dropItem;
    [SerializeField, Min(1)] private int dropAmount = 1;
    [Tooltip("Produces one batch every this many days (if satisfied enough).")]
    [SerializeField, Min(1)] private int dropEveryDays = 1;
    [Tooltip("Stops producing while this many of its drops are still lying on the ground.")]
    [SerializeField, Min(1)] private int maxUncollectedDrops = 3;

    [Header("Satisfaction (0-100)")]
    [SerializeField, Range(0f, 100f)] private float startingSatisfaction = 60f;
    [Tooltip("The animal only drops items while satisfaction is at least this.")]
    [SerializeField, Range(0f, 100f)] private float minSatisfactionToDrop = 50f;
    [Tooltip("Satisfaction lost every new day.")]
    [SerializeField, Min(0f)] private float dailyDecay = 12f;
    [Tooltip("Food item. Its 'Feed Satisfaction' value is how much one portion gives.")]
    [SerializeField] private ItemData feedItem;
    [SerializeField, Min(0f)] private float petGain = 8f;
    [SerializeField, Min(0f)] private float petCooldownSeconds = 20f;

    [Header("Quality (happier animal = better item)")]
    [SerializeField] private bool satisfactionAffectsQuality = true;
    [SerializeField, Range(0f, 100f)] private float purpleStarFrom = 75f;
    [SerializeField, Range(0f, 1f)] private float purpleStarChance = 0.25f;
    [SerializeField, Range(0f, 100f)] private float goldStarFrom = 90f;
    [SerializeField, Range(0f, 1f)] private float goldStarChance = 0.12f;

    [Header("Interaction")]
    [Tooltip("How close the player must be to press F on this animal.")]
    [SerializeField] private float careRadius = 1.6f;
    [Tooltip("The satisfaction bar only shows when the player is this close.")]
    [SerializeField] private float barShowRadius = 3.5f;
    [Tooltip("Only used if the scene has no DayNightController.")]
    [SerializeField] private float fallbackSecondsPerDay = 120f;

    private float satisfaction;
    private int daysSinceDrop;
    private bool productReady;
    private int uncollectedDrops;
    private float lastPetTime = -999f;
    private bool loggedBlocked;
    private float flashTimer;
    private float fallbackTimer;
    private ItemData sourceItem;
    private DayNightController dayNightController;
    private SpriteRenderer animalRenderer;

    private Transform barRoot;
    private SpriteRenderer barBack;
    private SpriteRenderer barFill;
    private SpriteRenderer barTick;

    private const float BarWidth = 1f;
    private const float BarHeight = 0.12f;

    // ---- read-only info (used by the shop and the validator) ---------------

    public ItemData SourceItem => sourceItem;
    public ItemData DropItem => dropItem;
    public ItemData FeedItem => feedItem;
    public int DropEveryDays => dropEveryDays;
    public float Satisfaction => satisfaction;
    public float MinSatisfactionToDrop => minSatisfactionToDrop;
    public bool IsSatisfied => satisfaction >= minSatisfactionToDrop;

    public string DisplayName => string.IsNullOrEmpty(displayName)
        ? gameObject.name.Replace("(Clone)", string.Empty).Trim()
        : displayName;

    public void SetSource(ItemData item)
    {
        sourceItem = item;
    }

    /// <summary>How many animals bought from this shop item are on the farm.</summary>
    public static int CountOwned(ItemData shopItem)
    {
        int count = 0;

        foreach (AnimalProducer animal in active)
        {
            if (animal != null && animal.sourceItem == shopItem)
            {
                count++;
            }
        }

        return count;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active.Clear();
        barSprite = null;
        cachedPlayer = null;
        cachedStorage = null;
    }

    // ---- lifecycle ------------------------------------------------------------

    private void Awake()
    {
        animalRenderer = GetComponentInChildren<SpriteRenderer>();
        satisfaction = Mathf.Clamp(startingSatisfaction, 0f, 100f);
        CreateBar();
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
        dayNightController = FindFirstObjectByType<DayNightController>();

        if (dayNightController != null)
        {
            dayNightController.DayStarted += HandleNewDay;
        }
    }

    private void OnDestroy()
    {
        if (dayNightController != null)
        {
            dayNightController.DayStarted -= HandleNewDay;
        }

        if (barRoot != null)
        {
            Destroy(barRoot.gameObject);
        }
    }

    private void Update()
    {
        if (dayNightController == null)
        {
            fallbackTimer += Time.deltaTime;

            if (fallbackTimer >= fallbackSecondsPerDay)
            {
                fallbackTimer = 0f;
                HandleNewDay(0);
            }
        }

        HandleCareInput();
        UpdateBar();
    }

    // ---- daily cycle ----------------------------------------------------------

    private void HandleNewDay(int dayNumber)
    {
        satisfaction = Mathf.Max(0f, satisfaction - dailyDecay);
        daysSinceDrop++;
        loggedBlocked = false;

        if (daysSinceDrop >= dropEveryDays)
        {
            productReady = true;
        }

        TryProduce();
    }

    private bool TryProduce()
    {
        if (!productReady || dropItem == null)
        {
            return false;
        }

        if (satisfaction < minSatisfactionToDrop)
        {
            if (!loggedBlocked)
            {
                loggedBlocked = true;
                Debug.Log(
                    $"[Thú] {DisplayName} chưa đủ hài lòng "
                    + $"({satisfaction:0}/{minSatisfactionToDrop:0}) nên chưa tạo {dropItem.itemName}. "
                    + "Hãy chăm sóc (phím F)."
                );
            }

            return false;
        }

        if (uncollectedDrops >= maxUncollectedDrops)
        {
            return false; // stays ready; tries again once the player collects
        }

        Drop();
        productReady = false;
        daysSinceDrop = 0;
        loggedBlocked = false;
        return true;
    }

    private void Drop()
    {
        CropQuality quality = RollQuality();
        Vector2 offset = Random.insideUnitCircle * 0.6f;
        Vector3 position = transform.position + new Vector3(offset.x, offset.y - 0.2f, 0f);

        DroppedItem.Spawn(dropItem, dropAmount, quality, position, this);
        uncollectedDrops++;

        Debug.Log(
            $"[Thú] {DisplayName} tạo ra {dropAmount} {dropItem.itemName} "
            + $"[{CropQualityUtility.GetDisplayName(quality)}] (hài lòng {satisfaction:0})."
        );
    }

    private CropQuality RollQuality()
    {
        if (!satisfactionAffectsQuality)
        {
            return CropQuality.Normal;
        }

        if (satisfaction >= goldStarFrom && Random.value < goldStarChance)
        {
            return CropQuality.GoldStar;
        }

        if (satisfaction >= purpleStarFrom && Random.value < purpleStarChance)
        {
            return CropQuality.PurpleStar;
        }

        return CropQuality.Normal;
    }

    /// <summary>Called by DroppedItem when the player picks one of this animal's drops up.</summary>
    public void NotifyDropCollected()
    {
        uncollectedDrops = Mathf.Max(0, uncollectedDrops - 1);
        TryProduce();
    }

    // ---- looking after the animal -------------------------------------------

    private void HandleCareInput()
    {
        if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
        {
            return;
        }

        // Every animal sees the key press; only the nearest one in reach reacts.
        if (GetNearestAnimalInReach() == this)
        {
            Care();
        }
    }

    private static AnimalProducer GetNearestAnimalInReach()
    {
        PlayerMovement player = FindPlayer();
        if (player == null)
        {
            return null;
        }

        AnimalProducer best = null;
        float bestDistance = float.MaxValue;

        foreach (AnimalProducer animal in active)
        {
            if (animal == null)
            {
                continue;
            }

            float distance = Vector2.Distance(
                player.transform.position,
                animal.transform.position
            );

            if (distance <= animal.careRadius && distance < bestDistance)
            {
                best = animal;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void Care()
    {
        ItemStorageController storage = FindStorage();

        if (feedItem != null
            && storage != null
            && storage.CountItem(feedItem) > 0
            && storage.TryRemoveItem(feedItem, 1))
        {
            AddSatisfaction(Mathf.Max(1, feedItem.feedSatisfaction));
            Debug.Log($"[Thú] Cho {DisplayName} ăn: hài lòng {satisfaction:0}/100.");
            return;
        }

        float cooldownLeft = petCooldownSeconds - (Time.time - lastPetTime);

        if (cooldownLeft > 0f)
        {
            Debug.Log(
                $"[Thú] {DisplayName} vừa được vuốt ve, chờ {cooldownLeft:0}s "
                + "(hoặc cho ăn Thức ăn chăn nuôi)."
            );
            return;
        }

        lastPetTime = Time.time;
        AddSatisfaction(petGain);
        Debug.Log($"[Thú] Vuốt ve {DisplayName}: hài lòng {satisfaction:0}/100.");
    }

    private void AddSatisfaction(float amount)
    {
        satisfaction = Mathf.Clamp(satisfaction + amount, 0f, 100f);
        flashTimer = 0.3f;
        loggedBlocked = false;
        TryProduce(); // an animal that was waiting for care drops its item right away
    }

    // ---- shared lookups (also used by DroppedItem and AnimalManager) --------

    public static PlayerMovement FindPlayer()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<PlayerMovement>();
        }

        return cachedPlayer;
    }

    public static ItemStorageController FindStorage()
    {
        if (cachedStorage == null)
        {
            cachedStorage = FindFirstObjectByType<ItemStorageController>();
        }

        return cachedStorage;
    }

    // ---- test helpers (used by the F1 trainer menu) ---------------------------

    public static void DebugSetAllSatisfaction(float value)
    {
        foreach (AnimalProducer animal in new List<AnimalProducer>(active))
        {
            if (animal == null)
            {
                continue;
            }

            animal.satisfaction = Mathf.Clamp(value, 0f, 100f);
            animal.flashTimer = 0.3f;
            animal.loggedBlocked = false;
            animal.TryProduce();
        }
    }

    public static void DebugAdvanceDayAll()
    {
        foreach (AnimalProducer animal in new List<AnimalProducer>(active))
        {
            if (animal != null)
            {
                animal.HandleNewDay(0);
            }
        }
    }

    // ---- satisfaction bar -----------------------------------------------------

    private void CreateBar()
    {
        if (barSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            barSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0f, 0.5f),
                1f
            );
        }

        GameObject root = new GameObject(gameObject.name + "_SatisfactionBar");
        barRoot = root.transform;

        barBack = CreateBarPart("Back", new Color(0.05f, 0.06f, 0.07f, 0.9f), 60);
        barFill = CreateBarPart("Fill", Color.green, 61);
        barTick = CreateBarPart("MinTick", Color.white, 62);

        root.SetActive(false);
    }

    private SpriteRenderer CreateBarPart(string partName, Color color, int sortingOrder)
    {
        GameObject part = new GameObject(partName);
        part.transform.SetParent(barRoot, false);

        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = barSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void UpdateBar()
    {
        if (barRoot == null)
        {
            return;
        }

        PlayerMovement player = FindPlayer();
        bool nearPlayer = player != null
            && Vector2.Distance(player.transform.position, transform.position) <= barShowRadius;
        bool needsCare = productReady && satisfaction < minSatisfactionToDrop;
        bool show = nearPlayer || needsCare || flashTimer > 0f;

        if (barRoot.gameObject.activeSelf != show)
        {
            barRoot.gameObject.SetActive(show);
        }

        if (!show)
        {
            return;
        }

        Vector3 top = animalRenderer != null
            ? new Vector3(animalRenderer.bounds.center.x, animalRenderer.bounds.max.y + 0.2f, transform.position.z)
            : transform.position + new Vector3(0f, 0.9f, 0f);
        barRoot.position = top;

        float fraction = Mathf.Clamp01(satisfaction / 100f);
        float minFraction = Mathf.Clamp01(minSatisfactionToDrop / 100f);

        barBack.transform.localPosition = new Vector3(-BarWidth * 0.5f - 0.02f, 0f, 0f);
        barBack.transform.localScale = new Vector3(BarWidth + 0.04f, BarHeight + 0.04f, 1f);

        barFill.transform.localPosition = new Vector3(-BarWidth * 0.5f, 0f, 0f);
        barFill.transform.localScale = new Vector3(Mathf.Max(0.001f, BarWidth * fraction), BarHeight, 1f);

        barTick.transform.localPosition = new Vector3(-BarWidth * 0.5f + BarWidth * minFraction - 0.015f, 0f, 0f);
        barTick.transform.localScale = new Vector3(0.03f, BarHeight + 0.08f, 1f);

        Color fillColor = satisfaction < minSatisfactionToDrop
            ? new Color(0.92f, 0.27f, 0.22f)
            : satisfaction < 75f
                ? new Color(0.96f, 0.8f, 0.26f)
                : new Color(0.4f, 0.86f, 0.36f);

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            fillColor = Color.Lerp(fillColor, Color.white, 0.6f);
        }

        barFill.color = fillColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, careRadius);
    }
}
