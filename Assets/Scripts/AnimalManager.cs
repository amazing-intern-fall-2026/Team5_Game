using UnityEngine;

// Summons the animals the player buys in the shop onto the farm. Created
// automatically on the first purchase, so no scene setup is needed. If you want
// bought animals to appear in a pen instead of next to the player, add this
// component to a GameObject yourself and fill in Spawn Points.
public sealed class AnimalManager : MonoBehaviour
{
    private static AnimalManager instance;

    [Header("Where bought animals appear")]
    [Tooltip("Optional. If empty, new animals appear next to the player.")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Min(0.5f)] private float spawnDistanceFromPlayer = 2f;

    public static AnimalManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AnimalManager>();
            }

            if (instance == null)
            {
                GameObject holder = new GameObject("AnimalManager");
                instance = holder.AddComponent<AnimalManager>();
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

    /// <summary>False (with a reason) when the animal cannot be bought right now.</summary>
    public bool CanSpawn(ItemData animalItem, out string reason)
    {
        reason = string.Empty;

        if (animalItem == null || animalItem.animalPrefab == null)
        {
            reason = "Thú vật này chưa có prefab.";
            return false;
        }

        int owned = AnimalProducer.CountOwned(animalItem);
        if (owned >= animalItem.maxOwned)
        {
            reason = $"Đã nuôi tối đa {animalItem.maxOwned} {animalItem.itemName}.";
            return false;
        }

        return true;
    }

    /// <summary>Creates the animal on the farm. Returns null if it could not be created.</summary>
    public AnimalProducer SpawnPurchased(ItemData animalItem)
    {
        if (!CanSpawn(animalItem, out string reason))
        {
            Debug.Log("[Thú] " + reason);
            return null;
        }

        GameObject spawned = Instantiate(
            animalItem.animalPrefab,
            GetSpawnPosition(),
            Quaternion.identity
        );
        spawned.name = animalItem.animalPrefab.name;

        AnimalProducer producer = spawned.GetComponent<AnimalProducer>();
        if (producer == null)
        {
            Debug.LogWarning(
                $"[Thú] Prefab {spawned.name} chưa có AnimalProducer - "
                + "con vật sẽ không tạo vật phẩm. Chạy Tools > Farm Game > Setup Animal Shop + Dragon Upgrades."
            );
            producer = spawned.AddComponent<AnimalProducer>();
        }

        producer.SetSource(animalItem);
        return producer;
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

        PlayerMovement player = AnimalProducer.FindPlayer();
        Vector3 origin = player != null ? player.transform.position : transform.position;

        Vector2 direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.right;
        }

        return origin + (Vector3)(direction.normalized * spawnDistanceFromPlayer);
    }
}
