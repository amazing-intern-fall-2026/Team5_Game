using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RandomWanderAI : MonoBehaviour
{
    [Header("Grid")]
    [Tooltip("The Tilemap that defines the walkable area (e.g. GroundTilemap).")]
    [SerializeField] private Tilemap groundTilemap;

    [Header("Wander Settings")]
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;
    [SerializeField] private float moveSpeed = 1.5f;
    [Tooltip("How many tiles away from its spawn point this entity is allowed to wander.")]
    [SerializeField] private int wanderRadius = 3;

    [Header("Optional visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private string isWalkingParam = "IsWalking";

    [Header("Directional animation")]
    [Tooltip("Animator float parameters set to the last movement direction, e.g. (0,1)=up, (0,-1)=down, (1,0)=right, (-1,0)=left. Use these to drive a Blend Tree or direction-based transitions.")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [Tooltip("Flip the sprite on X for horizontal movement. Turn this off if you're using fully distinct Left/Right clips instead of mirroring a single side clip.")]
    [SerializeField] private bool mirrorHorizontalMovement = true;

    private Vector3Int homeCell;
    private Vector3Int currentCell;

    private static readonly Vector3Int[] Directions =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
    };

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (groundTilemap == null)
        {
            GameObject found = GameObject.Find("GroundTilemap");
            if (found != null)
                groundTilemap = found.GetComponent<Tilemap>();
        }

        if (groundTilemap == null)
        {
            Debug.LogError("RandomWanderAI: no Ground Tilemap assigned or found in the scene.");
            return;
        }

        homeCell = groundTilemap.WorldToCell(transform.position);
        currentCell = homeCell;
        transform.position = groundTilemap.GetCellCenterWorld(currentCell);

        StartCoroutine(WanderRoutine());
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));

            if (TryPickNextCell(out Vector3Int nextCell))
                yield return MoveToCell(nextCell);
        }
    }

    private bool TryPickNextCell(out Vector3Int result)
    {
        // Try a handful of random directions; if none pan out this cycle,
        // just skip the move and wait again next loop.
        for (int i = 0; i < 4; i++)
        {
            Vector3Int candidate = currentCell + Directions[Random.Range(0, Directions.Length)];

            bool inRadius = Vector3.Distance(candidate, homeCell) <= wanderRadius;
            bool walkable = groundTilemap.HasTile(candidate);

            if (inRadius && walkable)
            {
                result = candidate;
                return true;
            }
        }

        result = currentCell;
        return false;
    }

    private IEnumerator MoveToCell(Vector3Int targetCell)
    {
        Vector3Int delta = targetCell - currentCell;
        Vector2 direction = new Vector2(delta.x, delta.y);

        Vector3 startPos = transform.position;
        Vector3 targetPos = groundTilemap.GetCellCenterWorld(targetCell);

        if (mirrorHorizontalMovement && spriteRenderer != null && delta.x != 0)
            spriteRenderer.flipX = delta.x < 0;

        if (animator != null)
        {
            animator.SetFloat(moveXParam, direction.x);
            animator.SetFloat(moveYParam, direction.y);
            animator.SetBool(isWalkingParam, true);
        }

        float duration = Vector3.Distance(startPos, targetPos) / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }

        transform.position = targetPos;
        currentCell = targetCell;

        if (animator != null)
            animator.SetBool(isWalkingParam, false);
    }
}
