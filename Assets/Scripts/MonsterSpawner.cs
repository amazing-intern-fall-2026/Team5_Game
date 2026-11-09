using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// One row per monster type. Each type has its OWN first-appearance day.
[System.Serializable]
public class MonsterSpawnEntry
{
    public MonsterBase prefab;
    [Tooltip("First day this monster can appear.")]
    [Min(1)] public int unlockDay = 1;
    [Tooltip("How many appear on the night it unlocks.")]
    [Min(0)] public int baseCount = 2;
    [Tooltip("Extra monsters added for every day after it unlocks.")]
    [Min(0f)] public float extraPerDay = 0.5f;
    [Min(1)] public int maxCount = 12;

    public int CountForDay(int day)
    {
        if (prefab == null || day < unlockDay)
        {
            return 0;
        }

        int extra = Mathf.FloorToInt((day - unlockDay) * extraPerDay);
        return Mathf.Clamp(baseCount + extra, 0, Mathf.Max(1, maxCount));
    }
}

// Summons monsters when night falls and removes them at dawn. Difficulty grows
// with the day number in two ways:
//  - more monsters, and new monster types unlocking on their own days
//  - every monster gets more health / damage / speed (MonsterScaling), and each
//    type's special ability gets stronger the longer it has been unlocked.
public sealed class MonsterSpawner : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    [SerializeField] private DayNightController dayNightController;
    [SerializeField] private FarmTileController farmTileController;

    [Header("Monsters (one entry per type)")]
    [SerializeField] private List<MonsterSpawnEntry> entries = new List<MonsterSpawnEntry>();

    [Header("Where they appear")]
    [Tooltip("Optional. If empty, monsters appear on a ring around the farm plot.")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Min(1f)] private float spawnRadius = 9f;
    [SerializeField, Min(0.05f)] private float timeBetweenSpawns = 0.5f;

    [Header("Difficulty growth per day (added on top of 1.0)")]
    [SerializeField, Min(0f)] private float healthPerDay = 0.12f;
    [SerializeField, Min(0f)] private float damagePerDay = 0.08f;
    [SerializeField, Min(0f)] private float speedPerDay = 0.02f;
    [SerializeField, Min(1f)] private float maxSpeedMultiplier = 1.5f;

    private bool? lastIsDay;
    private Coroutine waveRoutine;

    private int CurrentDay => dayNightController != null
        ? Mathf.Max(1, dayNightController.DayNumber)
        : 1;

    private void Awake()
    {
        if (dayNightController == null)
        {
            dayNightController = FindFirstObjectByType<DayNightController>();
        }

        if (farmTileController == null)
        {
            farmTileController = FindFirstObjectByType<FarmTileController>();
        }
    }

    private void Update()
    {
        if (dayNightController == null)
        {
            return;
        }

        bool isDay = dayNightController.IsDay;

        if (lastIsDay == null)
        {
            lastIsDay = isDay;

            if (!isDay)
            {
                SpawnWaveNow(); // the game was loaded during the night
            }

            return;
        }

        if (isDay == lastIsDay.Value)
        {
            return;
        }

        lastIsDay = isDay;

        if (isDay)
        {
            EndNight();
        }
        else
        {
            SpawnWaveNow();
        }
    }

    /// <summary>Starts tonight's wave immediately (also used by the trainer menu).</summary>
    [ContextMenu("Spawn wave now")]
    public void SpawnWaveNow()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
        }

        waveRoutine = StartCoroutine(WaveRoutine(CurrentDay));
    }

    /// <summary>Spawns one specific monster at a position (used by the trainer menu).</summary>
    public MonsterBase SpawnMonster(MonsterBase prefab, Vector3 position)
    {
        if (prefab == null)
        {
            return null;
        }

        int day = CurrentDay;
        int unlockDay = 1;

        foreach (MonsterSpawnEntry entry in entries)
        {
            if (entry != null && entry.prefab == prefab)
            {
                unlockDay = entry.unlockDay;
                break;
            }
        }

        MonsterBase monster = Instantiate(prefab, position, Quaternion.identity);
        monster.Initialize(BuildScaling(day, Mathf.Max(0, day - unlockDay)));
        return monster;
    }

    private void EndNight()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        if (MonsterBase.ActiveMonsters.Count > 0)
        {
            Debug.Log("[Quái] Trời sáng - quái vật tan biến.");
        }

        MonsterBase.DespawnAll();
    }

    private IEnumerator WaveRoutine(int day)
    {
        List<MonsterSpawnEntry> queue = BuildSpawnQueue(day);

        Debug.Log($"[Quái] Đêm ngày {day}: triệu hồi {queue.Count} quái.");

        WaitForSeconds wait = new WaitForSeconds(timeBetweenSpawns);

        foreach (MonsterSpawnEntry entry in queue)
        {
            int tier = Mathf.Max(0, day - entry.unlockDay);
            MonsterBase monster = Instantiate(
                entry.prefab,
                GetSpawnPosition(),
                Quaternion.identity
            );
            monster.Initialize(BuildScaling(day, tier));

            yield return wait;
        }

        waveRoutine = null;
    }

    // One list entry per monster to spawn, shuffled so types arrive mixed together.
    private List<MonsterSpawnEntry> BuildSpawnQueue(int day)
    {
        List<MonsterSpawnEntry> queue = new List<MonsterSpawnEntry>();

        foreach (MonsterSpawnEntry entry in entries)
        {
            if (entry == null)
            {
                continue;
            }

            int count = entry.CountForDay(day);
            for (int i = 0; i < count; i++)
            {
                queue.Add(entry);
            }
        }

        for (int i = queue.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (queue[i], queue[swapIndex]) = (queue[swapIndex], queue[i]);
        }

        return queue;
    }

    private MonsterScaling BuildScaling(int day, int tier)
    {
        int daysIn = Mathf.Max(0, day - 1);

        return new MonsterScaling(
            day,
            tier,
            1f + daysIn * healthPerDay,
            1f + daysIn * damagePerDay,
            Mathf.Min(maxSpeedMultiplier, 1f + daysIn * speedPerDay)
        );
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

            if (point != null)
            {
                Vector2 jitter = Random.insideUnitCircle * 0.75f;
                return point.position + new Vector3(jitter.x, jitter.y, 0f);
            }
        }

        float angle = Random.value * Mathf.PI * 2f;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;
        return GetFarmCenter() + offset;
    }

    private Vector3 GetFarmCenter()
    {
        if (farmTileController == null)
        {
            return transform.position;
        }

        Vector3Int origin = farmTileController.FarmPlotOriginCell;
        Vector3Int centerCell = new Vector3Int(
            origin.x + farmTileController.FarmPlotWidth / 2,
            origin.y + farmTileController.FarmPlotHeight / 2,
            0
        );

        Vector3 center = farmTileController.GetCellCenterWorld(centerCell);
        center.z = 0f;
        return center;
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Vector3 center = farmTileController != null ? GetFarmCenter() : transform.position;
        Gizmos.DrawWireSphere(center, spawnRadius);
    }
}
