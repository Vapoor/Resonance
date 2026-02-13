using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Ground Check")]
    [SerializeField] private bool isGrounded = true;
    
    private Rigidbody rb;
    private Vector3 moveDirection;
    
    // Public properties for animation system (future use)
    public Vector3 Velocity { get; private set; }
    public bool IsMoving { get; private set; }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            // Lock rotation so physics doesn't make the cylinder tumble
            rb.freezeRotation = true;
            rb.useGravity = true;
        }
    }
    
    private void Update()
    {
        // Get input - ARROW KEYS ONLY
        float horizontal = Input.GetAxisRaw("Horizontal"); // Left/Right Arrow
        float vertical = Input.GetAxisRaw("Vertical");     // Up/Down Arrow
        
        moveDirection = new Vector3(-vertical, 0f, horizontal).normalized;
        
        // Update properties
        Velocity = rb.linearVelocity;
        IsMoving = moveDirection.magnitude > 0.1f;
        
        // Rotate character to face movement direction
        if (IsMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    private void FixedUpdate()
    {
        if (rb == null) return;
        
        // Apply movement using physics
        Vector3 targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Preserve gravity/vertical velocity
        
        rb.linearVelocity = targetVelocity;
    }
}