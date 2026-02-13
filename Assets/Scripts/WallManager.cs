using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WallManager : MonoBehaviour
{
    [Header("Wall Detection")]
    [SerializeField] private string wallTag = "Wall";
    
    [Header("Settings")]
    [SerializeField] private bool autoActivateFirstWall = true;
    [SerializeField] private bool autoAdvanceOnUnlock = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private List<Wall> walls = new List<Wall>();
    private int currentWallIndex = -1;
    private KeyboardController keyboardController;
    
    private void Awake()
    {
        keyboardController = FindObjectOfType<KeyboardController>();
    }
    
    private void Start()
    {
        // Delay to ensure terrain is loaded
        Invoke(nameof(LoadWallsForCurrentLevel), 0.1f);
    }
    
    public void LoadWallsForCurrentLevel()
    {
        if (showDebugInfo)
        {
            Debug.Log("[WallManager] Loading walls for current level...");
        }
        
        // Find all walls with the "Wall" tag
        FindWallsByTag();
        
        if (walls.Count == 0)
        {
            Debug.LogWarning("[WallManager] No walls found! Make sure walls have the 'Wall' tag.");
            return;
        }
        
        // Unsubscribe from previous events to avoid duplicates
        foreach (Wall wall in walls)
        {
            if (wall != null)
            {
                wall.OnWallUnlocked.RemoveAllListeners();
            }
        }
        
        // Subscribe to wall unlock events
        foreach (Wall wall in walls)
        {
            if (wall != null)
            {
                wall.OnWallUnlocked.AddListener(() => OnWallUnlocked(wall));
            }
        }
        
        // IMPORTANT: Ensure all wall GameObjects are active first
        EnableAllWallGameObjects();
        
        // Deactivate all walls (sets internal state, not GameObject active state)
        DeactivateAllWalls();
        
        // Activate first wall if needed
        if (autoActivateFirstWall && walls.Count > 0)
        {
            ActivateWall(0);
        }
        
        // Notify KeyboardController to refresh its wall list
        if (keyboardController != null)
        {
            keyboardController.RefreshWalls();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[WallManager] Loaded {walls.Count} walls for current level");
            LogWallStatus();
        }
    }
    
    private void EnableAllWallGameObjects()
    {
        // Make sure all wall GameObjects are active
        foreach (Wall wall in walls)
        {
            if (wall != null && !wall.gameObject.activeSelf)
            {
                wall.gameObject.SetActive(true);
                if (showDebugInfo)
                {
                    Debug.Log($"[WallManager] Enabled GameObject: {wall.gameObject.name}");
                }
            }
        }
    }
    
    private void FindWallsByTag()
    {
        walls.Clear();
        
        GameObject[] wallObjects = GameObject.FindGameObjectsWithTag(wallTag);
        
        if (wallObjects.Length == 0)
        {
            Debug.LogWarning($"[WallManager] No GameObjects found with tag '{wallTag}'!");
            return;
        }
        
        foreach (GameObject wallObj in wallObjects)
        {
            Wall wall = wallObj.GetComponent<Wall>();
            if (wall != null)
            {
                walls.Add(wall);
            }
            else
            {
                Debug.LogWarning($"[WallManager] GameObject '{wallObj.name}' has '{wallTag}' tag but no Wall component!");
            }
        }
        
        // Sort walls by X position (left to right)
        // Use OrderBy for ascending (left to right), or OrderByDescending for right to left
        walls = walls.OrderByDescending(w => w.transform.position.x).ToList();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[WallManager] Found {walls.Count} wall(s) with tag '{wallTag}'</color>");
            for (int i = 0; i < walls.Count; i++)
            {
                Debug.Log($"  {i + 1}. {walls[i].gameObject.name} at X={walls[i].transform.position.x:F2}");
            }
        }
    }
    
    private void OnWallUnlocked(Wall wall)
    {
        int wallIndex = walls.IndexOf(wall);
        Debug.Log($"<color=green>[WallManager] Wall {wallIndex + 1}/{walls.Count} unlocked: {wall.gameObject.name}</color>");
        
        if (autoAdvanceOnUnlock)
        {
            // Wait a moment then advance to next wall
            Invoke(nameof(ActivateNextWall), 1f);
        }
    }
    
    public void ActivateWall(int index)
    {
        if (index < 0 || index >= walls.Count)
        {
            Debug.LogWarning($"[WallManager] Invalid wall index: {index}");
            return;
        }
        
        // Deactivate current wall
        if (currentWallIndex >= 0 && currentWallIndex < walls.Count)
        {
            walls[currentWallIndex].Deactivate();
        }
        
        // Activate new wall
        currentWallIndex = index;
        
        // Make sure GameObject is active before activating wall script
        if (!walls[currentWallIndex].gameObject.activeSelf)
        {
            walls[currentWallIndex].gameObject.SetActive(true);
            if (showDebugInfo)
            {
                Debug.Log($"[WallManager] Enabled GameObject for wall: {walls[currentWallIndex].gameObject.name}");
            }
        }
        
        walls[currentWallIndex].Activate();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=yellow>[WallManager] Activated Wall {currentWallIndex + 1}/{walls.Count}: {walls[currentWallIndex].gameObject.name}</color>");
            LogWallStatus();
        }
    }
    
    public void ActivateNextWall()
    {
        int nextIndex = currentWallIndex + 1;
        
        if (nextIndex < walls.Count)
        {
            ActivateWall(nextIndex);
        }
        else
        {
            Debug.Log("<color=cyan>[WallManager] 🎉 All walls completed! 🎉</color>");
        
        }
    }
    
    public void ActivatePreviousWall()
    {
        int prevIndex = currentWallIndex - 1;
        
        if (prevIndex >= 0)
        {
            ActivateWall(prevIndex);
        }
    }
    
    public void DeactivateAllWalls()
    {
        foreach (Wall wall in walls)
        {
            if (wall != null)
            {
                wall.Deactivate();
            }
        }
        currentWallIndex = -1;
    }
    
    public void ResetAllWalls()
    {
        DeactivateAllWalls();
        
        foreach (Wall wall in walls)
        {
            if (wall != null)
            {
                wall.ResetWall();
            }
        }
        
        if (autoActivateFirstWall && walls.Count > 0)
        {
            ActivateWall(0);
        }
        
        Debug.Log("[WallManager] All walls reset");
    }
    
    public void RefreshWalls()
    {
        LoadWallsForCurrentLevel();
    }
    
    private void LogWallStatus()
    {
        Debug.Log("=== WALL STATUS ===");
        for (int i = 0; i < walls.Count; i++)
        {
            Wall wall = walls[i];
            string activeState = wall.gameObject.activeSelf ? "GameObject✅" : "GameObject❌";
            string status = wall.IsActive() ? "🟢 ACTIVE" : (wall.IsUnlocked() ? "✅ UNLOCKED" : "⚪ INACTIVE");
            string keys = string.Join("+", wall.GetExpectedKeys());
            Debug.Log($"  Wall {i + 1}: {wall.gameObject.name} - {activeState} - {status} - Keys: [{keys}]");
        }
        Debug.Log("==================");
    }
    
    public Wall GetCurrentWall()
    {
        if (currentWallIndex >= 0 && currentWallIndex < walls.Count)
        {
            return walls[currentWallIndex];
        }
        return null;
    }
    
    public int GetCurrentWallIndex()
    {
        return currentWallIndex;
    }
    
    public int GetTotalWalls()
    {
        return walls.Count;
    }
    
    public List<Wall> GetAllWalls()
    {
        return new List<Wall>(walls);
    }
}