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
    
    private void Start()
    {
        keyboardController = FindObjectOfType<KeyboardController>();
        
        // Find all walls with the "Wall" tag
        FindWallsByTag();
        
        // Subscribe to wall unlock events
        foreach (Wall wall in walls)
        {
            if (wall != null)
            {
                wall.OnWallUnlocked.AddListener(() => OnWallUnlocked(wall));
            }
        }
        
        // Deactivate all walls at start
        DeactivateAllWalls();
        
        // Activate first wall if needed
        if (autoActivateFirstWall && walls.Count > 0)
        {
            ActivateWall(0);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[WallManager] Initialized with {walls.Count} walls");
            LogWallStatus();
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
        
        // Sort walls by their position (or name, or however you want)
        walls = walls.OrderBy(w => w.transform.position.z).ToList();
        
        Debug.Log($"<color=cyan>[WallManager] Found {walls.Count} wall(s) with tag '{wallTag}'</color>");
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
        int previousIndex = currentWallIndex;
        FindWallsByTag();
        
        if (previousIndex >= 0 && previousIndex < walls.Count)
        {
            ActivateWall(previousIndex);
        }
    }
    
    private void LogWallStatus()
    {
        Debug.Log("=== WALL STATUS ===");
        for (int i = 0; i < walls.Count; i++)
        {
            Wall wall = walls[i];
            string status = wall.IsActive() ? "🟢 ACTIVE" : (wall.IsUnlocked() ? "✅ UNLOCKED" : "⚪ INACTIVE");
            string keys = string.Join("+", wall.GetExpectedKeys());
            Debug.Log($"  Wall {i + 1}: {wall.gameObject.name} - {status} - Keys: [{keys}]");
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