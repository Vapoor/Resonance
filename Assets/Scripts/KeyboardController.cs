using UnityEngine;
using System.Collections.Generic;

public class KeyboardController : MonoBehaviour
{
    [Header("Key Detection Settings")]
    [SerializeField] private bool detectKeyDown = true;  // Detect single press
    [SerializeField] private bool detectKeyHold = false; // Detect holding
    [SerializeField] private bool debugMode = false;     // Show detailed debug info
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject targetCube;      // The cube to change color
    
    // Dictionary to store key states (useful for game logic later)
    // Maps QWERTY KeyCodes to AZERTY key names
    private Dictionary<KeyCode, string> keyMappings = new Dictionary<KeyCode, string>();
    
    // Dictionary to map each key to a specific color
    private Dictionary<string, Color> keyColors = new Dictionary<string, Color>();
    
    private Renderer cubeRenderer;
    
    private void Awake()
    {
        // Initialize key mappings
        InitializeKeyMappings();
        InitializeKeyColors();
        
        // Find the cube renderer
        if (targetCube != null)
        {
            cubeRenderer = targetCube.GetComponent<Renderer>();
        }
        else
        {
            // Try to find a cube in the scene
            GameObject cube = GameObject.FindGameObjectWithTag("Cube");
            if (cube != null)
            {
                targetCube = cube;
                cubeRenderer = cube.GetComponent<Renderer>();
                Debug.Log("[KeyboardController] Found cube automatically");
            }
            else
            {
                Debug.LogWarning("[KeyboardController] No cube assigned! Please assign a cube in the Inspector.");
            }
        }
    }
    
    private void InitializeKeyMappings()
    {
        // AZERTY keyboard second row (physical keys)
        // Mapping QWERTY KeyCodes to AZERTY key names
        
        keyMappings.Add(KeyCode.CapsLock, "VER");     // VERROUILLAGE MAJUSCULE
        keyMappings.Add(KeyCode.A, "Q");              // A key = Q on AZERTY
        keyMappings.Add(KeyCode.S, "S");              // S key = S on AZERTY
        keyMappings.Add(KeyCode.D, "D");              // D key = D on AZERTY
        keyMappings.Add(KeyCode.F, "F");              // F key = F on AZERTY
        keyMappings.Add(KeyCode.G, "G");              // G key = G on AZERTY
        keyMappings.Add(KeyCode.H, "H");              // H key = H on AZERTY
        keyMappings.Add(KeyCode.J, "J");              // J key = J on AZERTY
        keyMappings.Add(KeyCode.K, "K");              // K key = K on AZERTY
        keyMappings.Add(KeyCode.L, "L");              // L key = L on AZERTY
        keyMappings.Add(KeyCode.Semicolon, "M");      // ; key = M on AZERTY
        keyMappings.Add(KeyCode.Quote, "ù");          // ' key = ù on AZERTY
        keyMappings.Add(KeyCode.Backslash, "*");      // \ key = * on AZERTY
        keyMappings.Add(KeyCode.Return, "ENTER");     // Enter key
        
        Debug.Log("[KeyboardController] Initialized for AZERTY keyboard - 14 keys mapped");
    }
    
    private void InitializeKeyColors()
    {
        // Assign a unique color to each key
        keyColors.Add("VER", new Color(1f, 0f, 0f));        // Red
        keyColors.Add("Q", new Color(1f, 0.5f, 0f));        // Orange
        keyColors.Add("S", new Color(1f, 1f, 0f));          // Yellow
        keyColors.Add("D", new Color(0.5f, 1f, 0f));        // Yellow-Green
        keyColors.Add("F", new Color(0f, 1f, 0f));          // Green
        keyColors.Add("G", new Color(0f, 1f, 0.5f));        // Cyan-Green
        keyColors.Add("H", new Color(0f, 1f, 1f));          // Cyan
        keyColors.Add("J", new Color(0f, 0.5f, 1f));        // Light Blue
        keyColors.Add("K", new Color(0f, 0f, 1f));          // Blue
        keyColors.Add("L", new Color(0.5f, 0f, 1f));        // Purple
        keyColors.Add("M", new Color(1f, 0f, 1f));          // Magenta
        keyColors.Add("ù", new Color(1f, 0f, 0.5f));        // Pink
        keyColors.Add("*", new Color(1f, 1f, 1f));          // White
        keyColors.Add("ENTER", new Color(0.2f, 0.2f, 0.2f)); // Dark Gray
    }
    
    private void Update()
    {
        // Check all mapped keys
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
    
    // Called when a key is pressed (single press)
    private void OnKeyPressed(string keyName)
    {
        Debug.Log($"<color=green>✓ Key Pressed: {keyName}</color>");
        
        // Change cube color
        ChangeCubeColor(keyName);
        
        // Trigger game action
        TriggerGameAction(keyName);
    }
    
    // Called when a key is held down
    private void OnKeyHeld(string keyName)
    {
        Debug.Log($"[KeyboardController] Key Held: {keyName}");
    }
    
    // Change the cube's color based on the key pressed
    private void ChangeCubeColor(string keyName)
    {
        if (cubeRenderer != null && keyColors.ContainsKey(keyName))
        {
            Color newColor = keyColors[keyName];
            cubeRenderer.material.color = newColor;
            
            Debug.Log($"<color=cyan>[Color] Changed cube to {keyName} color: RGB({newColor.r:F2}, {newColor.g:F2}, {newColor.b:F2})</color>");
        }
        else if (cubeRenderer == null)
        {
            Debug.LogWarning("[KeyboardController] No cube renderer found!");
        }
    }
    
    // Public method to check if a specific key is currently pressed
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
    
    // Method you can call from other scripts to handle key events
    public void TriggerGameAction(string keyName)
    {
        // Add your game-specific logic here
        switch (keyName)
        {
            case "VER":
                Debug.Log("[Action] VERROUILLAGE MAJUSCULE pressed");
                break;
            case "Q":
                Debug.Log("[Action] Q pressed");
                break;
            case "S":
                Debug.Log("[Action] S pressed");
                break;
            case "D":
                Debug.Log("[Action] D pressed");
                break;
            case "F":
                Debug.Log("[Action] F pressed");
                break;
            case "G":
                Debug.Log("[Action] G pressed");
                break;
            case "H":
                Debug.Log("[Action] H pressed");
                break;
            case "J":
                Debug.Log("[Action] J pressed");
                break;
            case "K":
                Debug.Log("[Action] K pressed");
                break;
            case "L":
                Debug.Log("[Action] L pressed");
                break;
            case "M":
                Debug.Log("[Action] M pressed");
                break;
            case "ù":
                Debug.Log("[Action] ù pressed");
                break;
            case "*":
                Debug.Log("[Action] * pressed");
                break;
            case "ENTER":
                Debug.Log("[Action] ENTER pressed");
                break;
        }
    }
    
    // Get list of all available keys
    public List<string> GetAllKeys()
    {
        List<string> keys = new List<string>();
        foreach (var key in keyMappings)
        {
            keys.Add(key.Value);
        }
        return keys;
    }
    
    // Public method to get the color for a specific key
    public Color GetKeyColor(string keyName)
    {
        if (keyColors.ContainsKey(keyName))
        {
            return keyColors[keyName];
        }
        return Color.white;
    }
}