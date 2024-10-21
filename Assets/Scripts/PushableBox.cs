using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PushableBox : MonoBehaviour
{
    [Header("Box Properties")]
    public float weight = 1f; // Determines how much the box affects player movement
    public float pushResistance = 1f; // Determines how much the box resists being thrown

    private Rigidbody2D rb;
    private bool isBeingMoved;
    private Vector2 moveDirection;
    private float moveSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        Debug.Log($"PushableBox initialized with weight: {weight} and push resistance: {pushResistance}");
    }

    void FixedUpdate()
    {
        if (isBeingMoved)
        {
            Vector2 targetPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
            Debug.Log($"Moving box to {targetPosition}");
        }
    }

    public void StartMoving(Vector2 direction, float speed)
    {
        isBeingMoved = true;
        moveDirection = direction.normalized;
        moveSpeed = speed;
        Debug.Log($"StartMoving called with direction: {direction}, speed: {speed}");
    }

    public void StopMoving()
    {
        if (isBeingMoved)
        {
            isBeingMoved = false;
            Debug.Log("StopMoving called.");
        }
    }
}