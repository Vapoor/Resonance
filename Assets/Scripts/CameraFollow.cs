using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("Camera Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -7f);
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool lookAtTarget = true;
    
    [Header("Optional: Mouse Look")]
    [SerializeField] private bool enableMouseLook = false;
    [SerializeField] private float mouseSensitivity = 2f;
    
    private float currentRotationY = 180f;
    
    private void Start()
    {
        // If no target assigned, try to find the player
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        // Lock cursor for mouse look (optional)
        if (enableMouseLook)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        // Handle mouse look rotation (optional)
        if (enableMouseLook)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            currentRotationY += mouseX;
            
            // Rotate offset around the target
            Quaternion rotation = Quaternion.Euler(0f, currentRotationY, 0f);
            offset = rotation * new Vector3(0f, 5f, -7f);
        }
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly move camera to desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
        
        // Always look towards X- (negative X axis)
        transform.rotation = Quaternion.Euler(0f, -90f, 0f);
    }
    
    // Toggle mouse look at runtime (press Escape)
    private void Update()
    {
        if (enableMouseLook && Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked 
                ? CursorLockMode.None 
                : CursorLockMode.Locked;
        }
    }
}