using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class IsometricPlayerController : MonoBehaviour
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
    private float moveInputX;
    private float moveInputY;
    private bool jumpInput;

    // Ground and jump tracking
    private bool isGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int jumpCount;

    // Movement smoothing
    private float targetVelocityX;
    private float targetVelocityY;

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
        moveInputX = Input.GetAxisRaw("Horizontal");
        moveInputY = Input.GetAxisRaw("Vertical");

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
    }

    void FixedUpdate()
    {
        // Calculate target velocities based on input
        targetVelocityX = moveInputX * moveSpeed;
        targetVelocityY = moveInputY * moveSpeed;

        // Smooth Movement for X axis
        float speedDifferenceX = targetVelocityX - rb.velocity.x;
        float accelRateX = (Mathf.Abs(targetVelocityX) > 0.01f) ? acceleration : deceleration;
        float movementX = Mathf.Pow(Mathf.Abs(speedDifferenceX) * accelRateX, 0.9f) * Mathf.Sign(speedDifferenceX);

        // Smooth Movement for Y axis (if needed)
        float speedDifferenceY = targetVelocityY - rb.velocity.y;
        float accelRateY = (Mathf.Abs(targetVelocityY) > 0.01f) ? acceleration : deceleration;
        float movementY = Mathf.Pow(Mathf.Abs(speedDifferenceY) * accelRateY, 0.9f) * Mathf.Sign(speedDifferenceY);

        // Apply forces
        rb.AddForce(new Vector2(movementX, movementY), ForceMode2D.Force);

        // Optionally, clamp the velocity for better control
        float clampedX = Mathf.Clamp(rb.velocity.x, -moveSpeed, moveSpeed);
        float clampedY = Mathf.Clamp(rb.velocity.y, -moveSpeed, moveSpeed);
        rb.velocity = new Vector2(clampedX, clampedY);
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
