using UnityEngine;

public class MusicTriggerZone : MonoBehaviour
{
    [Header("Trigger Behavior")]
    [SerializeField] private TriggerAction onEnterAction = TriggerAction.StopMusic;
    [SerializeField] private TriggerAction onExitAction = TriggerAction.ResumeMusic;
    
    [Header("Music Settings")]
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float fadeInDuration = 1f;
    
    [Header("Optional: Change Music Instead of Stop")]
    [SerializeField] private AudioClip musicToPlayOnEnter;
    [SerializeField] private float crossfadeDuration = 2f;
    
    [Header("Wall Audio Hint Control")]
    [SerializeField] private bool controlWallHints = true;
    [SerializeField] private Wall targetWall;
    [SerializeField] private bool enableHintsOnEnter = true;
    [SerializeField] private bool disableHintsOnExit = true;
    
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);
    
    private bool hasTriggered = false;
    private bool playerInside = false;
    
    public enum TriggerAction
    {
        None,
        StopMusic,
        PauseMusic,
        ResumeMusic,
        ChangeMusicToClip
    }
    
    private void Start()
    {
        // Auto-find nearest wall if not assigned
        if (controlWallHints && targetWall == null)
        {
            targetWall = FindObjectOfType<Wall>();
            
            if (targetWall != null && showDebugInfo)
            {
                Debug.Log($"[MusicTrigger] Auto-assigned wall: {targetWall.gameObject.name}");
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;
        
        if (triggerOnce && hasTriggered)
            return;
        
        playerInside = true;
        hasTriggered = true;
        
        // Handle music
        ExecuteAction(onEnterAction, "Enter");
        
        // Handle wall hints
        if (controlWallHints && targetWall != null && enableHintsOnEnter)
        {
            targetWall.SetPlayerInAudioRange(true);
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=cyan>[MusicTrigger] Enabled wall hints for: {targetWall.gameObject.name}</color>");
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;
        
        if (!playerInside)
            return;
        
        playerInside = false;
        
        // Handle music
        ExecuteAction(onExitAction, "Exit");
        
        // Handle wall hints
        if (controlWallHints && targetWall != null && disableHintsOnExit)
        {
            targetWall.SetPlayerInAudioRange(false);
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=gray>[MusicTrigger] Disabled wall hints for: {targetWall.gameObject.name}</color>");
            }
        }
    }
    
    private void ExecuteAction(TriggerAction action, string triggerType)
    {
        if (BackgroundMusicManager.Instance == null)
        {
            Debug.LogWarning("[MusicTrigger] BackgroundMusicManager not found in scene!");
            return;
        }
        
        switch (action)
        {
            case TriggerAction.None:
                break;
                
            case TriggerAction.StopMusic:
                if (showDebugInfo)
                {
                    Debug.Log($"<color=yellow>[MusicTrigger] {triggerType} - Stopping music</color>");
                }
                BackgroundMusicManager.Instance.StopMusic(fadeOutDuration);
                break;
                
            case TriggerAction.PauseMusic:
                if (showDebugInfo)
                {
                    Debug.Log($"<color=yellow>[MusicTrigger] {triggerType} - Pausing music</color>");
                }
                BackgroundMusicManager.Instance.PauseMusic();
                break;
                
            case TriggerAction.ResumeMusic:
                if (showDebugInfo)
                {
                    Debug.Log($"<color=green>[MusicTrigger] {triggerType} - Resuming music</color>");
                }
                BackgroundMusicManager.Instance.ResumeMusic();
                break;
                
            case TriggerAction.ChangeMusicToClip:
                if (musicToPlayOnEnter != null)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"<color=cyan>[MusicTrigger] {triggerType} - Changing music to: {musicToPlayOnEnter.name}</color>");
                    }
                    BackgroundMusicManager.Instance.CrossfadeToMusic(musicToPlayOnEnter, crossfadeDuration);
                }
                else
                {
                    Debug.LogWarning("[MusicTrigger] ChangeMusicToClip selected but no clip assigned!");
                }
                break;
        }
    }
    
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider boxCol)
            {
                Gizmos.DrawCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawSphere(sphereCol.center, sphereCol.radius);
            }
            
            // Draw line to target wall
            if (controlWallHints && targetWall != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, targetWall.transform.position);
            }
        }
    }
}