using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("References")]
    private Rigidbody rb;
    private Vector3 moveDirection;
    
    // Public properties for animation system (future use)
    public Vector3 Velocity { get; private set; }
    public bool IsMoving { get; private set; }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Lock rotation so physics doesn't make the cylinder tumble
        rb.freezeRotation = true;
    }
    
    private void Update()
    {
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Arrow Keys
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Arrow Keys
        
        // Calculate movement direction relative to camera
        moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        
        // Update properties for future animation system
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
        // Apply movement using physics
        Vector3 targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Preserve gravity
        
        rb.linearVelocity = targetVelocity;
    }
    
    // Optional: Method to change speed (useful for animations like running)
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
}