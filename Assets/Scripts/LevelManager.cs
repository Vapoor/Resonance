using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [System.Serializable]
    public class Level
    {
        public string levelName;
        public GameObject terrain;
        public Material skybox;
    }
    
    [Header("Level Configuration")]
    [SerializeField] private List<Level> levels = new List<Level>();
    
    [Header("Player Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private bool useCustomSpawnPosition = false;
    
    [Header("Transition Settings")]
    [SerializeField] private float transitionDelay = 0.5f;
    
    [Header("Current State")]
    [SerializeField] private int currentLevelIndex = -1;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Events")]
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();
    public UnityEvent OnAllLevelsCompleted = new UnityEvent();
    
    private WallManager wallManager;
    
    private void Start()
    {
        // Find WallManager
        wallManager = FindObjectOfType<WallManager>();
        
        // Validate levels configuration
        ValidateLevelsConfiguration();
        
        // Save spawn position from player's initial position if not using custom
        if (player != null && !useCustomSpawnPosition)
        {
            spawnPosition = player.position;
        }
        
        // Initialize first level
        InitializeLevels();
        LoadLevel(0);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[LevelManager] Initialized with {levels.Count} levels</color>");
        }
    }
    
    private void ValidateLevelsConfiguration()
    {
        bool hasErrors = false;
        
        Debug.Log($"[LevelManager] === CONFIGURATION CHECK === Total levels: {levels.Count}");
        
        for (int i = 0; i < levels.Count; i++)
        {
            string terrainInfo = levels[i].terrain != null ? levels[i].terrain.name : "❌ NULL";
            string nameInfo = !string.IsNullOrEmpty(levels[i].levelName) ? levels[i].levelName : "(empty)";
            string skyboxInfo = levels[i].skybox != null ? levels[i].skybox.name : "NULL";
            
            Debug.Log($"[LevelManager] Level {i}: Name='{nameInfo}' | Terrain={terrainInfo} | Skybox={skyboxInfo}");
            
            if (levels[i].terrain == null)
            {
                Debug.LogError($"[LevelManager] ❌ Level {i} ({nameInfo}) has NO TERRAIN assigned! Please assign it in the Inspector.");
                hasErrors = true;
            }
            
            if (string.IsNullOrEmpty(levels[i].levelName))
            {
                Debug.LogWarning($"[LevelManager] ⚠️ Level {i} has no name.");
            }
            
            if (levels[i].skybox == null)
            {
                Debug.LogWarning($"[LevelManager] ⚠️ Level {i} ({nameInfo}) has no skybox assigned.");
            }
        }
        
        Debug.Log("[LevelManager] === END CONFIGURATION CHECK ===");
        
        if (hasErrors)
        {
            Debug.LogError("[LevelManager] ⛔ CONFIGURATION ERROR! Fix the missing terrain assignments in the Inspector before playing.");
        }
    }
    
    private void InitializeLevels()
    {
        // Hide all terrains initially
        foreach (Level level in levels)
        {
            if (level.terrain != null)
            {
                level.terrain.SetActive(false);
            }
        }
    }
    
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Count)
        {
            Debug.LogWarning($"[LevelManager] Invalid level index: {levelIndex}");
            return;
        }
        
        // Hide current level terrain
        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
        {
            if (levels[currentLevelIndex].terrain != null)
            {
                levels[currentLevelIndex].terrain.SetActive(false);
                if (showDebugInfo)
                {
                    Debug.Log($"[LevelManager] Disabled terrain: {levels[currentLevelIndex].terrain.name}");
                }
            }
        }
        
        // Update current level
        currentLevelIndex = levelIndex;
        Level currentLevel = levels[currentLevelIndex];
        
        if (showDebugInfo)
        {
            Debug.Log($"[LevelManager] Current level terrain: {(currentLevel.terrain != null ? currentLevel.terrain.name : "NULL")}");
        }
        
        // Show new terrain FIRST (before respawning player)
        if (currentLevel.terrain != null)
        {
            currentLevel.terrain.SetActive(true);
            if (showDebugInfo)
            {
                Debug.Log($"[LevelManager] Enabled terrain: {currentLevel.terrain.name} - Active: {currentLevel.terrain.activeSelf}");
            }
        }
        else
        {
            Debug.LogError($"[LevelManager] Level {levelIndex} has NULL terrain!");
        }
        
        // Change skybox
        if (currentLevel.skybox != null)
        {
            RenderSettings.skybox = currentLevel.skybox;
            DynamicGI.UpdateEnvironment();
        }
        
        // Wait one frame to ensure terrain is loaded before respawning
        StartCoroutine(RespawnAfterFrame());
        
        // Refresh walls for new level
        if (wallManager != null)
        {
            // Wait a bit more to ensure terrain colliders are ready
            StartCoroutine(RefreshWallsAfterDelay(0.1f));
        }
        
        // Trigger event
        OnLevelChanged.Invoke(currentLevelIndex);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=yellow>[LevelManager] Loaded Level {currentLevelIndex + 1}/{levels.Count}: {currentLevel.levelName}</color>");
        }
    }
    
    private System.Collections.IEnumerator RespawnAfterFrame()
    {
        // Wait for physics to update
        yield return new WaitForFixedUpdate();
        RespawnPlayer();
    }
    
    private System.Collections.IEnumerator RefreshWallsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (wallManager != null)
        {
            wallManager.RefreshWalls();
            
            if (showDebugInfo)
            {
                Debug.Log("[LevelManager] Refreshed walls for new level");
            }
        }
    }
    
    public void NextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        
        if (nextIndex < levels.Count)
        {
            if (showDebugInfo)
            {
                Debug.Log($"<color=green>[LevelManager] Advancing to level {nextIndex + 1}...</color>");
            }
            
            Invoke(nameof(LoadNextLevel), transitionDelay);
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log("<color=cyan>[LevelManager] 🎉 All levels completed! 🎉</color>");
            }
            OnAllLevelsCompleted.Invoke();
        }
    }
    
    private void LoadNextLevel()
    {
        LoadLevel(currentLevelIndex + 1);
    }
    
    public void PreviousLevel()
    {
        int prevIndex = currentLevelIndex - 1;
        
        if (prevIndex >= 0)
        {
            LoadLevel(prevIndex);
        }
    }
    
    public void RestartLevel()
    {
        LoadLevel(currentLevelIndex);
    }
    
    public void RespawnPlayer()
    {
        if (player != null)
        {
            // Disable character controller if present
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }
            
            // Reset position
            player.position = spawnPosition;
            
            // Reset rotation
            player.rotation = Quaternion.identity;
            
            // Reset velocity if rigidbody present
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Re-enable character controller
            if (controller != null)
            {
                controller.enabled = true;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[LevelManager] Player respawned at {spawnPosition}");
            }
        }
    }
    
    public void SetSpawnPosition(Vector3 position)
    {
        spawnPosition = position;
        useCustomSpawnPosition = true;
    }
    
    // Public getters
    public int GetCurrentLevelIndex() => currentLevelIndex;
    public int GetTotalLevels() => levels.Count;
    public Level GetCurrentLevel()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
        {
            return levels[currentLevelIndex];
        }
        return null;
    }
    
    public string GetCurrentLevelName()
    {
        Level level = GetCurrentLevel();
        return level != null ? level.levelName : "Unknown";
    }
    
    // Editor helper
    [ContextMenu("Save Current Player Position as Spawn")]
    private void SaveCurrentPositionAsSpawn()
    {
        if (player != null)
        {
            spawnPosition = player.position;
            useCustomSpawnPosition = true;
            Debug.Log($"[LevelManager] Spawn position saved: {spawnPosition}");
        }
    }
}