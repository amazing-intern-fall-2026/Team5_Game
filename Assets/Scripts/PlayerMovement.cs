using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D playerBody;
    private Animator animator;

    private Vector2 moveInput;

    // 0 = Down
    // 1 = Up
    // 2 = Right / Left
    private int lastDirection = 0;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // =========================
        // INPUT
        // =========================

        moveInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                moveInput.y += 1f;

            if (Keyboard.current.sKey.isPressed)
                moveInput.y -= 1f;

            if (Keyboard.current.aKey.isPressed)
                moveInput.x -= 1f;

            if (Keyboard.current.dKey.isPressed)
                moveInput.x += 1f;
        }

        // Không cho đi chéo nhanh hơn
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        // =========================
        // DIRECTION
        // =========================

        UpdateDirection();

        // =========================
        // ANIMATOR
        // =========================

        if (animator != null)
        {
            animator.SetFloat("Speed", moveInput.magnitude);
            animator.SetInteger("Direction", lastDirection);
        }
    }

    private void UpdateDirection()
    {
        if (moveInput == Vector2.zero)
            return;

        // =========================
        // LEFT / RIGHT
        // =========================

        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            lastDirection = 2;

            // D = Right
            // A = Left
            // Dùng chung animation Right + Flip X
            spriteRenderer.flipX = moveInput.x < 0f;
        }

        // =========================
        // UP / DOWN
        // =========================

        else
        {
            spriteRenderer.flipX = false;

            // W = Up
            if (moveInput.y > 0f)
            {
                lastDirection = 1;
            }

            // S = Down
            else if (moveInput.y < 0f)
            {
                lastDirection = 0;
            }
        }
    }

    private void FixedUpdate()
    {
        if (playerBody == null)
            return;

        playerBody.MovePosition(
            playerBody.position +
            moveInput * moveSpeed * Time.fixedDeltaTime
        );
    }
}