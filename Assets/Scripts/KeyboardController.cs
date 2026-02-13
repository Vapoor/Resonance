using UnityEngine;
using System.Collections.Generic;

public class KeyboardController : MonoBehaviour
{
    [Header("Key Detection Settings")]
    [SerializeField] private bool detectKeyDown = true;
    [SerializeField] private bool detectKeyHold = false;
    [SerializeField] private bool debugMode = false;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject targetCube;
    
    private Dictionary<KeyCode, string> keyMappings = new Dictionary<KeyCode, string>();
    private Dictionary<string, Color> keyColors = new Dictionary<string, Color>();
    private Renderer cubeRenderer;
    
    // Reference to all walls in the scene
    private List<Wall> allWalls = new List<Wall>();
    
    private void Awake()
    {
        InitializeKeyMappings();
        InitializeKeyColors();
        
        if (targetCube != null)
        {
            cubeRenderer = targetCube.GetComponent<Renderer>();
        }
    }
    
    private void Start()
    {
        // Don't refresh walls here - let WallManager do it after loading walls
        // RefreshWalls();
    }
    
    private void InitializeKeyMappings()
    {
        keyMappings.Add(KeyCode.CapsLock, "VER");
        keyMappings.Add(KeyCode.A, "Q");
        keyMappings.Add(KeyCode.S, "S");
        keyMappings.Add(KeyCode.D, "D");
        keyMappings.Add(KeyCode.F, "F");
        keyMappings.Add(KeyCode.G, "G");
        keyMappings.Add(KeyCode.H, "H");
        keyMappings.Add(KeyCode.J, "J");
        keyMappings.Add(KeyCode.K, "K");
        keyMappings.Add(KeyCode.L, "L");
        keyMappings.Add(KeyCode.Semicolon, "M");
        keyMappings.Add(KeyCode.Quote, "ù");
        keyMappings.Add(KeyCode.Backslash, "*");
        keyMappings.Add(KeyCode.Return, "ENTER");
        
        Debug.Log("[KeyboardController] Initialized for AZERTY keyboard - 14 keys mapped");
    }
    
    private void InitializeKeyColors()
    {
        keyColors.Add("VER", new Color(1f, 0f, 0f));
        keyColors.Add("Q", new Color(1f, 0.5f, 0f));
        keyColors.Add("S", new Color(1f, 1f, 0f));
        keyColors.Add("D", new Color(0.5f, 1f, 0f));
        keyColors.Add("F", new Color(0f, 1f, 0f));
        keyColors.Add("G", new Color(0f, 1f, 0.5f));
        keyColors.Add("H", new Color(0f, 1f, 1f));
        keyColors.Add("J", new Color(0f, 0.5f, 1f));
        keyColors.Add("K", new Color(0f, 0f, 1f));
        keyColors.Add("L", new Color(0.5f, 0f, 1f));
        keyColors.Add("M", new Color(1f, 0f, 1f));
        keyColors.Add("ù", new Color(1f, 0f, 0.5f));
        keyColors.Add("*", new Color(1f, 1f, 1f));
        keyColors.Add("ENTER", new Color(0.2f, 0.2f, 0.2f));
    }
    
    private void Update()
    {
        foreach (var key in keyMappings)
        {
            if (detectKeyDown && Input.GetKeyDown(key.Key))
            {
                OnKeyPressed(key.Value);
                
                if (debugMode)
                {
                    Debug.Log($"[Debug] KeyCode: {key.Key} -> AZERTY: {key.Value}");
                }
            }
            else if (detectKeyHold && Input.GetKey(key.Key))
            {
                OnKeyHeld(key.Value);
            }
        }
    }
    
    private void OnKeyPressed(string keyName)
    {
        Debug.Log($"<color=green>✓ Key Pressed: {keyName}</color>");
        
        ChangeCubeColor(keyName);
        
        // Notify ONLY ACTIVE walls about the key press
        NotifyActiveWalls(keyName);
        
        TriggerGameAction(keyName);
    }
    
    private void OnKeyHeld(string keyName)
    {
        Debug.Log($"[KeyboardController] Key Held: {keyName}");
    }
    
    private void NotifyActiveWalls(string keyName)
    {
        int activeWallCount = 0;
        
        foreach (Wall wall in allWalls)
        {
            if (wall != null && wall.IsActive() && !wall.IsUnlocked())
            {
                wall.OnKeyPressed(keyName);
                activeWallCount++;
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"[KeyboardController] Notified {activeWallCount} active wall(s)");
        }
    }
    
    private void ChangeCubeColor(string keyName)
    {
        if (cubeRenderer != null && keyColors.ContainsKey(keyName))
        {
            Color newColor = keyColors[keyName];
            cubeRenderer.material.color = newColor;
        }
    }
    
    public bool IsKeyPressed(string keyName)
    {
        foreach (var key in keyMappings)
        {
            if (key.Value == keyName)
            {
                return Input.GetKey(key.Key);
            }
        }
        return false;
    }
    
    public void TriggerGameAction(string keyName)
    {
        // Your game-specific logic here
    }
    
    public List<string> GetAllKeys()
    {
        List<string> keys = new List<string>();
        foreach (var key in keyMappings)
        {
            keys.Add(key.Value);
        }
        return keys;
    }
    
    public Color GetKeyColor(string keyName)
    {
        if (keyColors.ContainsKey(keyName))
        {
            return keyColors[keyName];
        }
        return Color.white;
    }
    
    // Public method to refresh wall list (call if you spawn new walls)
    public void RefreshWalls()
    {
        allWalls = new List<Wall>(FindObjectsOfType<Wall>());
        Debug.Log($"[KeyboardController] Found {allWalls.Count} wall(s) in scene");
    }
    
    // Get all active walls
    public List<Wall> GetActiveWalls()
    {
        List<Wall> activeWalls = new List<Wall>();
        foreach (Wall wall in allWalls)
        {
            if (wall != null && wall.IsActive())
            {
                activeWalls.Add(wall);
            }
        }
        return activeWalls;
    }
}