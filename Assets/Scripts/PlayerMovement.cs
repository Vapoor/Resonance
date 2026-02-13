using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float modelRotationOffset = -90f; // Adjust if model faces wrong direction
    
    [Header("Ground Check")]
    [SerializeField] private bool isGrounded = true;
    
    private Rigidbody rb;
    private Animator animator;
    private Vector3 moveDirection;
    private bool isFirstFrame = true;
    
    // Public properties for animation system (future use)
    public Vector3 Velocity { get; private set; }
    public bool IsMoving { get; private set; }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        
        if (rb != null)
        {
            // Lock rotation so physics doesn't make the cylinder tumble
            rb.freezeRotation = true;
            rb.useGravity = true;
        }
    }
    
    private void Start()
    {
        // Set initial rotation to face X- direction (after Animator initialization)
        transform.rotation = Quaternion.Euler(0f, modelRotationOffset, 0f);
    }
    
    private void Update()
    {
        // Force rotation on first frame
        if (isFirstFrame)
        {
            transform.rotation = Quaternion.Euler(0f, modelRotationOffset, 0f);
            isFirstFrame = false;
            
            // Disable Apply Root Motion if Animator exists
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
        }
        
        // Get input - ARROW KEYS ONLY
        float horizontal = Input.GetAxisRaw("Horizontal"); // Left/Right Arrow
        float vertical = Input.GetAxisRaw("Vertical");     // Up/Down Arrow
        
        moveDirection = new Vector3(-vertical, 0f, horizontal).normalized;
        
        // Update properties
        Velocity = rb.linearVelocity;
        IsMoving = moveDirection.magnitude > 0.1f;
        
        // Update animation
        if (animator != null)
        {
            animator.SetBool("isWalking", IsMoving);
        }
        
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