using UnityEngine;
using System.Collections.Generic;

public class WallManager : MonoBehaviour
{
    [Header("Wall Management")]
    [SerializeField] private List<Wall> walls = new List<Wall>();
    [SerializeField] private bool autoActivateFirstWall = true;
    [SerializeField] private bool autoAdvanceOnUnlock = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private int currentWallIndex = -1;
    private KeyboardController keyboardController;
    
    private void Start()
    {
        keyboardController = FindObjectOfType<KeyboardController>();
        
        // Find all walls if list is empty
        if (walls.Count == 0)
        {
            walls = new List<Wall>(FindObjectsOfType<Wall>());
            Debug.Log($"[WallManager] Auto-found {walls.Count} walls");
        }
        
        // Deactivate all walls at start
        DeactivateAllWalls();
        
        // Subscribe to wall unlock events
        foreach (Wall wall in walls)
        {
            if (wall != null)
            {
                wall.OnWallUnlocked.AddListener(() => OnWallUnlocked(wall));
            }
        }
        
        // Activate first wall if needed
        if (autoActivateFirstWall && walls.Count > 0)
        {
            ActivateWall(0);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[WallManager] Initialized with {walls.Count} walls");
        }
    }
    
    private void OnWallUnlocked(Wall wall)
    {
        Debug.Log($"<color=green>[WallManager] Wall unlocked: {wall.gameObject.name}</color>");
        
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
            Debug.Log("<color=cyan>[WallManager] All walls completed! 🎉</color>");
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
    
    // Add a wall to the manager
    public void AddWall(Wall wall)
    {
        if (!walls.Contains(wall))
        {
            walls.Add(wall);
            wall.OnWallUnlocked.AddListener(() => OnWallUnlocked(wall));
        }
    }
    
    // UI Testing methods (can be called from buttons)
    public void TestNextWall()
    {
        ActivateNextWall();
    }
    
    public void TestPreviousWall()
    {
        ActivatePreviousWall();
    }
    
    public void TestReset()
    {
        ResetAllWalls();
    }
}