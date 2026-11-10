using UnityEngine;

public class NPCPatrol : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float tileSize = 1f;
    public int patrolTiles = 5;

    private Vector3 startPosition;
    private float leftLimit;
    private float rightLimit;
    private int direction = -1;

    void Start()
    {
        startPosition = transform.position;

        // Giới hạn di chuyển sang trái và phải
        leftLimit = startPosition.x - patrolTiles * tileSize;
        rightLimit = startPosition.x;

        // Bắt đầu đi từ phải sang trái
        direction = -1;
        FlipNPC();
    }

    void Update()
    {
        transform.position += Vector3.right
            * direction * moveSpeed * Time.deltaTime;

        // Đến giới hạn trái thì quay sang phải
        if (transform.position.x <= leftLimit)
        {
            transform.position = new Vector3(
                leftLimit,
                transform.position.y,
                transform.position.z
            );

            direction = 1;
            FlipNPC();
        }
        // Đến giới hạn phải thì quay sang trái
        else if (transform.position.x >= rightLimit)
        {
            transform.position = new Vector3(
                rightLimit,
                transform.position.y,
                transform.position.z
            );

            direction = -1;
            FlipNPC();
        }
    }

    void FlipNPC()
    {
        Vector3 scale = transform.localScale;
        scale.x = -scale.x;
        transform.localScale = scale;
    }

}