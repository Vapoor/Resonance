using UnityEngine;

public class LevelEndTrigger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = Color.yellow;
    
    private LevelManager levelManager;
    private bool hasTriggered = false;
    
    private void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        
        if (levelManager == null)
        {
            Debug.LogError("[LevelEndTrigger] No LevelManager found in scene!");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        
        if (other.CompareTag(playerTag))
        {
            hasTriggered = true;
            
            if (levelManager != null)
            {
                Debug.Log("<color=green>[LevelEndTrigger] Level completed! Advancing to next level...</color>");
                levelManager.NextLevel();
            }
            
            // Reset after a delay to allow re-triggering if player comes back
            Invoke(nameof(ResetTrigger), 2f);
        }
    }
    
    private void ResetTrigger()
    {
        hasTriggered = false;
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        Gizmos.color = gizmoColor;
        
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
