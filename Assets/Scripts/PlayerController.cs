using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    // Movement variables
    [Header("Movement")]
    public float baseMoveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 50f;
    public float gravityMultiplier = 2.5f;

    // Jumping variables
    [Header("Jumping")]
    public float jumpForce = 16f;
    public float jumpCutMultiplier = 0.5f;
    public int maxJumpCount = 1;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;

    // Ground check variables
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    // Interaction variables
    [Header("Interaction")]
    public float interactionDistance = 1.5f;
    public KeyCode latchKey = KeyCode.F;
    public KeyCode throwKey = KeyCode.G;

    [Header("Throw")]
    public float throwStrength = 10f;

    // Components
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator animator;

    // Input variables
    private float moveInput;
    private bool jumpInput;

    // Ground and jump tracking
    private bool isGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int jumpCount;

    // Push/Pull variables
    private PushableBox latchedBox;
    private bool isLatched;
    private Vector3 offsetToBox;

    // Movement speed affected by box weight
    private float currentMoveSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>() ?? GetComponentInChildren<BoxCollider2D>();
        animator = GetComponent<Animator>();

        if (groundCheck == null)
        {
            Debug.LogError("GroundCheck is not assigned in the Inspector.");
        }

        if (boxCollider == null)
        {
            Debug.LogError("BoxCollider2D is not assigned or missing on the Player GameObject.");
        }
        else
        {
            Debug.Log("BoxCollider2D successfully assigned.");
        }

        if (animator == null)
        {
            Debug.LogWarning("Animator component is missing. Animations won't play.");
        }

        currentMoveSpeed = baseMoveSpeed;
    }

    void Update()
    {
        // Handle Input
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && !isLatched)
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
        if (jumpBufferCounter > 0 && (coyoteTimeCounter > 0 || jumpCount < maxJumpCount) && !isLatched)
        {
            Jump();
            jumpBufferCounter = 0;
        }

        // Variable Jump Height
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0 && !isLatched)
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

        // Handle Latching
        HandleLatch();

        // Handle Throw
        HandleThrow();

        // Update Animator Parameters
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isLatched && latchedBox != null)
        {
            // Move the player with the box
            transform.position = latchedBox.transform.position - offsetToBox;
            
            // Move the box based on player input
            Vector2 boxMovement = new Vector2(moveInput * currentMoveSpeed, 0) * Time.fixedDeltaTime;
            latchedBox.GetComponent<Rigidbody2D>().MovePosition(latchedBox.GetComponent<Rigidbody2D>().position + boxMovement);
        }
        else
        {
            // Apply gravity multiplier when descending
            if (rb.velocity.y < 0)
            {
                rb.gravityScale = gravityMultiplier;
            }
            else
            {
                rb.gravityScale = 1f;
            }

            // Smooth Movement
            float targetSpeed = moveInput * currentMoveSpeed;
            float speedDifference = targetSpeed - rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float movement = Mathf.Pow(Mathf.Abs(speedDifference) * accelRate, 0.9f) * Mathf.Sign(speedDifference);

            rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

            // Optionally, clamp the velocity for better control
            float clampedX = Mathf.Clamp(rb.velocity.x, -currentMoveSpeed, currentMoveSpeed);
            rb.velocity = new Vector2(clampedX, rb.velocity.y);
        }
    }

    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
        jumpCount++;
        Debug.Log("Player Jumped");
    }

    void HandleLatch()
    {
        if (Input.GetKeyDown(latchKey))
        {
            if (!isLatched)
            {
                AttemptLatch();
            }
            else
            {
                Unlatch();
            }
        }
    }

    void AttemptLatch()
    {
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Vector2 interactionOrigin = (Vector2)transform.position + direction * (boxCollider.bounds.size.x / 2);
        Vector2 boxSize = new Vector2(interactionDistance, boxCollider.bounds.size.y);

        Collider2D hit = Physics2D.OverlapBox(interactionOrigin, boxSize, 0f, LayerMask.GetMask("Pushable"));

        if (hit != null && hit.CompareTag("BoxWall"))
        {
            PushableBox box = hit.GetComponentInParent<PushableBox>();
            if (box != null)
            {
                latchedBox = box;
                isLatched = true;
                currentMoveSpeed = baseMoveSpeed / latchedBox.weight;
                offsetToBox = latchedBox.transform.position - transform.position;
                Debug.Log($"Successfully latched onto a box with weight: {latchedBox.weight}");
            }
            else
            {
                Debug.Log("No PushableBox component found on the parent of the detected wall collider.");
            }
        }
        else
        {
            Debug.Log("No BoxWall detected to latch.");
        }
    }

    void Unlatch()
    {
        if (isLatched && latchedBox != null)
        {
            isLatched = false;
            latchedBox.StopMoving();
            currentMoveSpeed = baseMoveSpeed;
            latchedBox = null;
            Debug.Log("Unlatched from the box.");
        }
    }

    void HandleThrow()
    {
        if (isLatched && latchedBox != null)
        {
            if (Input.GetKeyDown(throwKey))
            {
                Vector2 throwDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                float finalThrowForce = throwStrength * latchedBox.pushResistance;

                Rigidbody2D boxRb = latchedBox.GetComponent<Rigidbody2D>();
                if (boxRb != null)
                {
                    boxRb.AddForce(throwDirection * finalThrowForce, ForceMode2D.Impulse);
                    Debug.Log($"Applied throw force {finalThrowForce} to the box in direction {throwDirection}");
                }
                else
                {
                    Debug.LogWarning("PushableBox does not have a Rigidbody2D component.");
                }

                Unlatch();
            }
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalVelocity", rb.velocity.y);
        animator.SetBool("IsPushingOrPulling", isLatched && latchedBox != null);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        else
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
            Debug.LogWarning("GroundCheck Transform is not assigned in the PlayerController.");
        }

        if (boxCollider != null)
        {
            Gizmos.color = Color.yellow;
            Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            Vector2 interactionOrigin = (Vector2)transform.position + direction * (boxCollider.bounds.size.x / 2);
            Vector2 boxSize = new Vector2(interactionDistance, boxCollider.bounds.size.y);
            Gizmos.DrawWireCube(interactionOrigin, boxSize);
        }
        else
        {
            Debug.LogWarning("BoxCollider2D is not assigned or missing on the Player GameObject.");
        }
    }
}