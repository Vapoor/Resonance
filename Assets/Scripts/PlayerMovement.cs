using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("References")]
    private Rigidbody rb;
    private Vector3 moveDirection;
    [SerializeField] private Animator animator; // Glisser l'Animator ici

    // Public properties for external use
    public Vector3 Velocity { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Empêche le player de tomber sur le côté
    }

    private void Update()
    {
        // --- INPUT ---
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // --- CALCUL DU MOUVEMENT ALIGNÉ AVEC LA CAMÉRA (X- ET Z) ---
        // Camera looks toward X-, so:
        // Vertical (Z/S) should move along X axis (X-)
        // Horizontal (Q/D) should move along Z axis
        moveDirection = new Vector3(-vertical, 0f, horizontal).normalized;

        // --- ÉTAT DU PLAYER ---
        IsMoving = moveDirection.magnitude > 0.1f;

        // --- ROTATION DU PLAYER ---
        if (IsMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- MISE À JOUR DE L'ANIMATOR ---
        if (animator != null)
        {
            animator.SetBool("isWalking", IsMoving);
        }
    }

    private void FixedUpdate()
    {
        // --- VITESSE SELON WALK OU RUN ---
        float currentSpeed = IsRunning ? runSpeed : walkSpeed;

        Vector3 targetVelocity = moveDirection * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Conserve la gravité

        rb.linearVelocity = targetVelocity;

        // --- MISE À JOUR VELOCITY ---
        Velocity = rb.linearVelocity;
    }
}
