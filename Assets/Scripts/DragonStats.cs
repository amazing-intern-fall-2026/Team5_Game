using System.Collections.Generic;
using UnityEngine;

public enum DragonElement
{
    Fire = 0,
    Ice = 1,
    Earth = 2,
    Lightning = 3
}

public enum DragonStat
{
    Power = 0,        // the element's own effect (burn damage / slow / push / paralyze)
    Range = 1,        // how far it can reach
    AttackSpeed = 2   // how often it attacks
}

// One upgrade line in the shop, e.g. "Fire dragon - Power".
[System.Serializable]
public class DragonUpgradeTrack
{
    public DragonElement element;
    public DragonStat stat;
    [Min(1)] public int maxLevel = 5;
    [Tooltip("Cost of the first upgrade.")]
    [Min(1)] public int baseCost = 800;
    [Tooltip("Each following level costs this many times more.")]
    [Min(1f)] public float costGrowth = 1.6f;
    [Tooltip("Bonus per level. 0.2 = +20% per level.")]
    [Min(0f)] public float bonusPerLevel = 0.2f;
    [Tooltip("Shown in the shop under the upgrade name.")]
    public string description;

    /// <summary>Cost to go from currentLevel to currentLevel + 1.</summary>
    public int CostForLevel(int currentLevel)
    {
        float raw = baseCost * Mathf.Pow(costGrowth, Mathf.Max(0, currentLevel));
        return Mathf.Max(10, Mathf.RoundToInt(raw / 10f) * 10);
    }
}

// Upgrade levels for the four dragons. Upgrades are per ELEMENT: buying
// "Fire - Power" strengthens every fire dragon on the farm at once.
//
// Dragons read their bonuses through DragonStats.GetMultiplier(...), which is
// safe to call even when no DragonStats exists (it returns 1 = no bonus). The
// shop creates this object automatically the first time you upgrade something.
//
// Note: this project has no save system yet, so levels reset when you stop
// Play Mode - exactly like your coins and inventory do.
public sealed class DragonStats : MonoBehaviour
{
    private static DragonStats instance;

    [SerializeField] private List<DragonUpgradeTrack> tracks = CreateDefaultTracks();

    private readonly Dictionary<int, int> levels = new Dictionary<int, int>();

    public IReadOnlyList<DragonUpgradeTrack> Tracks => tracks;

    /// <summary>Raised whenever any level changes.</summary>
    public event System.Action Changed;

    /// <summary>The existing instance, or null. Never creates one.</summary>
    public static DragonStats Current
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DragonStats>();
            }

            return instance;
        }
    }

    /// <summary>The existing instance, creating one if the scene has none.</summary>
    public static DragonStats Instance
    {
        get
        {
            if (Current == null)
            {
                GameObject holder = new GameObject("DragonStats");
                instance = holder.AddComponent<DragonStats>();
            }

            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    // ---- what dragons call -------------------------------------------------

    /// <summary>Bonus multiplier for a stat (1 = no bonus). Safe with no DragonStats.</summary>
    public static float GetMultiplier(DragonElement element, DragonStat stat)
    {
        DragonStats stats = Current;
        return stats != null ? stats.ComputeMultiplier(element, stat) : 1f;
    }

    public float ComputeMultiplier(DragonElement element, DragonStat stat)
    {
        DragonUpgradeTrack track = FindTrack(element, stat);
        if (track == null)
        {
            return 1f;
        }

        return 1f + track.bonusPerLevel * GetLevel(element, stat);
    }

    // ---- what the shop calls -----------------------------------------------

    public DragonUpgradeTrack FindTrack(DragonElement element, DragonStat stat)
    {
        foreach (DragonUpgradeTrack track in tracks)
        {
            if (track != null && track.element == element && track.stat == stat)
            {
                return track;
            }
        }

        return null;
    }

    public int GetLevel(DragonElement element, DragonStat stat)
    {
        return levels.TryGetValue(Key(element, stat), out int level) ? level : 0;
    }

    public bool IsMaxed(DragonElement element, DragonStat stat)
    {
        DragonUpgradeTrack track = FindTrack(element, stat);
        return track == null || GetLevel(element, stat) >= track.maxLevel;
    }

    /// <summary>Price of the next level, or -1 if there is none.</summary>
    public int GetNextCost(DragonElement element, DragonStat stat)
    {
        DragonUpgradeTrack track = FindTrack(element, stat);
        if (track == null || IsMaxed(element, stat))
        {
            return -1;
        }

        return track.CostForLevel(GetLevel(element, stat));
    }

    public bool TryUpgrade(DragonElement element, DragonStat stat, CoinManager coinManager)
    {
        DragonUpgradeTrack track = FindTrack(element, stat);
        if (track == null || coinManager == null)
        {
            Debug.LogWarning("[Rồng] Dữ liệu nâng cấp không hợp lệ.");
            return false;
        }

        if (IsMaxed(element, stat))
        {
            Debug.Log($"[Rồng] {GetElementName(element)} - {GetStatName(element, stat)} đã đạt cấp tối đa.");
            return false;
        }

        int cost = GetNextCost(element, stat);
        if (!coinManager.TrySpendCoins(cost))
        {
            return false;
        }

        int newLevel = GetLevel(element, stat) + 1;
        levels[Key(element, stat)] = newLevel;
        Changed?.Invoke();

        Debug.Log(
            $"[Rồng] Nâng cấp {GetElementName(element)} - {GetStatName(element, stat)} "
            + $"lên cấp {newLevel}/{track.maxLevel} (+{Mathf.RoundToInt(track.bonusPerLevel * newLevel * 100f)}%)."
        );
        return true;
    }

    [ContextMenu("Reset all levels")]
    public void ResetLevels()
    {
        levels.Clear();
        Changed?.Invoke();
    }

    // ---- display helpers ---------------------------------------------------

    public static string GetElementName(DragonElement element)
    {
        switch (element)
        {
            case DragonElement.Fire: return "Rồng Lửa";
            case DragonElement.Ice: return "Rồng Băng";
            case DragonElement.Earth: return "Rồng Đất";
            case DragonElement.Lightning: return "Rồng Sét";
            default: return "Rồng";
        }
    }

    public static string GetStatName(DragonElement element, DragonStat stat)
    {
        switch (stat)
        {
            case DragonStat.Range:
                return "Tầm đánh";
            case DragonStat.AttackSpeed:
                return "Tốc độ đánh";
            default:
                switch (element)
                {
                    case DragonElement.Fire: return "Sát thương cháy";
                    case DragonElement.Ice: return "Sức làm chậm";
                    case DragonElement.Earth: return "Lực đẩy lùi";
                    default: return "Thời gian tê liệt";
                }
        }
    }

    private static int Key(DragonElement element, DragonStat stat)
    {
        return (int)element * 10 + (int)stat;
    }

    private static List<DragonUpgradeTrack> CreateDefaultTracks()
    {
        List<DragonUpgradeTrack> result = new List<DragonUpgradeTrack>();

        AddElement(result, DragonElement.Fire, 1f, "Mỗi đòn cháy gây nhiều sát thương hơn");
        AddElement(result, DragonElement.Ice, 1f, "Làm chậm quái mạnh hơn và lâu hơn");
        AddElement(result, DragonElement.Earth, 1f, "Đẩy lùi quái xa hơn");
        AddElement(result, DragonElement.Lightning, 1.25f, "Làm quái tê liệt lâu hơn");

        return result;
    }

    private static void AddElement(
        List<DragonUpgradeTrack> list,
        DragonElement element,
        float costFactor,
        string powerDescription
    )
    {
        list.Add(new DragonUpgradeTrack
        {
            element = element, stat = DragonStat.Power,
            maxLevel = 5, baseCost = Mathf.RoundToInt(800 * costFactor),
            costGrowth = 1.6f, bonusPerLevel = 0.2f, description = powerDescription
        });
        list.Add(new DragonUpgradeTrack
        {
            element = element, stat = DragonStat.Range,
            maxLevel = 5, baseCost = Mathf.RoundToInt(600 * costFactor),
            costGrowth = 1.6f, bonusPerLevel = 0.1f,
            description = "Tăng tầm phát hiện và tấn công"
        });
        list.Add(new DragonUpgradeTrack
        {
            element = element, stat = DragonStat.AttackSpeed,
            maxLevel = 5, baseCost = Mathf.RoundToInt(700 * costFactor),
            costGrowth = 1.6f, bonusPerLevel = 0.1f,
            description = "Giảm thời gian hồi giữa các đòn đánh"
        });
    }
}
