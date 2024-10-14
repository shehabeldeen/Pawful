using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    // Movement variables
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 50f;

    // Jumping variables
    [Header("Jumping")]
    public float jumpForce = 16f;
    public float jumpCutMultiplier = 0.5f;
    public int maxJumpCount = 1; // For double jump, set to 2
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;

    // Ground check variables
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    // Components
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    // Input variables
    private float moveInput;
    private bool jumpInput;

    // Movement smoothing
    private float currentVelocity;

    // Ground and jump tracking
    private bool isGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int jumpCount;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (groundCheck == null)
        {
            Debug.LogError("GroundCheck is not assigned in the Inspector.");
        }
    }

    void Update()
    {
        // Handle Input
        moveInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump"))
        {
            jumpInput = true;
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Check if grounded
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            jumpCount = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Handle Jump Input with Coyote Time and Jump Buffer
        if (jumpBufferCounter > 0 && (coyoteTimeCounter > 0 || jumpCount < maxJumpCount))
        {
            Jump();
            jumpBufferCounter = 0;
        }

        // Variable Jump Height
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
        }

        // Flip Sprite based on movement direction
        if (moveInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (moveInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void FixedUpdate()
    {
        // Smooth Movement
        float targetSpeed = moveInput * moveSpeed;
        float speedDifference = targetSpeed - rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDifference) * accelRate, 0.9f) * Mathf.Sign(speedDifference);

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // Optionally, clamp the velocity for better control
        float clampedX = Mathf.Clamp(rb.velocity.x, -moveSpeed, moveSpeed);
        rb.velocity = new Vector2(clampedX, rb.velocity.y);
    }

    void Jump()
    {
        // Reset Y velocity before jumping for consistent jump height
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
        jumpCount++;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
