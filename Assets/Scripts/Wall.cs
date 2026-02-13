using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class Wall : MonoBehaviour
{
    [Header("Key Configuration")]
    [Tooltip("Expected key combination for this wall (e.g., single key or combo like Q+S)")]
    [SerializeField] private List<string> expectedKeys = new List<string>();
    
    [Header("Key Matching Mode")]
    [SerializeField] private KeyMatchMode matchMode = KeyMatchMode.Any;
    
    [Header("References")]
    [SerializeField] private GameObject wallVisual;
    [SerializeField] private GameObject distortionVisual;
    [SerializeField] private Material distortionMaterial;
    
    [Header("Cooldown System")]
    [SerializeField] private float keyCooldown = 1f;
    private float lastKeyPressTime = -999f;
    
    [Header("Distance to Speed Mapping")]
    [SerializeField] private float speedAtDistance0 = 0f;    // Perfect
    [SerializeField] private float speedAtDistance3 = 0.5f;  // Medium
    [SerializeField] private float speedAtDistance6 = 1f;    // Maximum
    
    [Header("Shader Settings")]
    [SerializeField] private string noiseSpeedPropertyName = "_NoiseSpeed";
    
    [Header("Visual Feedback")]
    [SerializeField] private Color correctKeyColor = Color.green;
    [SerializeField] private Color wrongKeyColor = Color.red;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private float feedbackDuration = 0.5f;
    
    [Header("Wall State")]
    [SerializeField] private bool isActive = false;
    [SerializeField] private bool isUnlocked = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Events")]
    public UnityEvent OnWallUnlocked = new UnityEvent();
    public UnityEvent<int> OnKeyDistanceCalculated = new UnityEvent<int>();
    
    // Private variables
    private Renderer wallRenderer;
    private Renderer distortionRenderer;
    private Color originalColor;
    private float feedbackTimer = 0f;
    private int lastDistance = 0;
    private float currentNoiseSpeed = 0f;
    private KeyboardController keyboardController;
    
    // All 14 keys in physical order (what Unity detects - QWERTY layout)
    private static readonly List<string> allKeys = new List<string>
    {
        "VER",    // 0 - Caps Lock
        "Q",      // 1 - A key (shows Q on AZERTY)
        "S",      // 2 - S key
        "D",      // 3 - D key
        "F",      // 4 - F key
        "G",      // 5 - G key
        "H",      // 6 - H key
        "J",      // 7 - J key
        "K",      // 8 - K key
        "L",      // 9 - L key
        "M",      // 10 - Semicolon (shows M on AZERTY)
        "ù",      // 11 - Quote (shows ù on AZERTY)
        "*",      // 12 - Backslash (shows * on AZERTY)
        "ENTER"   // 13 - Return
    };
    
    public enum KeyMatchMode
    {
        Any,            // Any single key from expected keys
        All,            // All keys must be pressed (any order)
        Simultaneous    // All keys pressed at the same time (combo)
    }
    
    private void Awake()
    {
        SetupReferences();
    }
    
    private void SetupReferences()
    {
        // Find wall visual
        if (wallVisual == null)
        {
            Transform cubeTransform = transform.Find("Cube");
            if (cubeTransform != null)
            {
                wallVisual = cubeTransform.gameObject;
            }
        }
        
        // Find or create distortion visual
        if (distortionVisual == null)
        {
            Transform distortionTransform = transform.Find("DistortionVisual");
            if (distortionTransform != null)
            {
                distortionVisual = distortionTransform.gameObject;
            }
            else
            {
                // Create distortion duplicate if it doesn't exist
                CreateDistortionDuplicate();
            }
        }
        
        // Get renderers
        if (wallVisual != null)
        {
            wallRenderer = wallVisual.GetComponent<Renderer>();
            if (wallRenderer != null)
            {
                originalColor = wallRenderer.material.color;
            }
        }
        
        if (distortionVisual != null)
        {
            distortionRenderer = distortionVisual.GetComponent<Renderer>();
            
            // Apply distortion material
            if (distortionMaterial != null && distortionRenderer != null)
            {
                distortionRenderer.material = distortionMaterial;
            }
        }
    }
    
    private void CreateDistortionDuplicate()
    {
        if (wallVisual == null) return;
        
        // Duplicate the wall visual
        distortionVisual = Instantiate(wallVisual, transform);
        distortionVisual.name = "DistortionVisual";
        
        // Position it slightly in front
        distortionVisual.transform.localPosition = wallVisual.transform.localPosition + new Vector3(0, 0, -0.1f);
        
        // Apply distortion material
        Renderer renderer = distortionVisual.GetComponent<Renderer>();
        if (renderer != null && distortionMaterial != null)
        {
            renderer.material = distortionMaterial;
        }
        
        distortionRenderer = renderer;
        
        Debug.Log($"[Wall] Created distortion duplicate for {gameObject.name}");
    }
    
    private void Start()
    {
        keyboardController = FindObjectOfType<KeyboardController>();
        
        // Hide distortion visual initially
        if (distortionVisual != null)
        {
            distortionVisual.SetActive(false);
        }
        
        // Set initial noise speed to 0
        UpdateNoiseSpeed(0f);
        UpdateVisualState();
        
        if (showDebugInfo)
        {
            Debug.Log($"[Wall] {gameObject.name} initialized | Keys: [{string.Join("+", expectedKeys)}] | Mode: {matchMode}");
        }
    }
    
    private void Update()
    {
        // Handle visual feedback timer
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0f)
            {
                ResetVisualFeedback();
            }
        }
    }
    
    public void OnKeyPressed(string keyName)
    {
        if (!isActive || isUnlocked)
        {
            return;
        }
        
        // Check cooldown
        if (Time.time - lastKeyPressTime < keyCooldown)
        {
            if (showDebugInfo)
            {
                float remaining = keyCooldown - (Time.time - lastKeyPressTime);
                Debug.Log($"[Wall] Cooldown active! Wait {remaining:F2}s");
            }
            return;
        }
        
        lastKeyPressTime = Time.time;
        
        // Check if key matches
        bool isCorrect = CheckKeyMatch(keyName);
        
        if (isCorrect)
        {
            HandleCorrectKey(keyName);
        }
        else
        {
            HandleWrongKey(keyName);
        }
    }
    
    private bool CheckKeyMatch(string keyName)
    {
        switch (matchMode)
        {
            case KeyMatchMode.Any:
                return expectedKeys.Contains(keyName);
                
            case KeyMatchMode.All:
                // For simplicity, checking if the pressed key is one of the expected
                return expectedKeys.Contains(keyName);
                
            case KeyMatchMode.Simultaneous:
                // Check if all expected keys are currently held down
                if (keyboardController != null)
                {
                    foreach (string key in expectedKeys)
                    {
                        if (!keyboardController.IsKeyPressed(key))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
                
            default:
                return false;
        }
    }
    
    private void HandleCorrectKey(string keyName)
    {
        Debug.Log($"<color=green>[Wall] {gameObject.name} - ✅ PERFECT! Key: {keyName}</color>");
        
        lastDistance = 0;
        currentNoiseSpeed = speedAtDistance0;
        
        // Update shader to perfect (0)
        UpdateNoiseSpeed(currentNoiseSpeed);
        
        // Show correct feedback
        ShowCorrectFeedback();
        
        // Show distortion with perfect clarity
        if (distortionVisual != null)
        {
            distortionVisual.SetActive(true);
        }
        
        // Unlock wall
        isUnlocked = true;
        
        // Trigger event
        OnWallUnlocked.Invoke();
        OnKeyDistanceCalculated.Invoke(0);
    }
    
    private void HandleWrongKey(string keyName)
    {
        // Calculate distance
        int distance = CalculateKeyDistance(keyName);
        lastDistance = distance;
        
        // Calculate noise speed based on distance
        float noiseSpeed = CalculateNoiseSpeed(distance);
        currentNoiseSpeed = noiseSpeed;
        
        // Update shader
        UpdateNoiseSpeed(currentNoiseSpeed);
        
        // Show wrong feedback
        ShowWrongFeedback();
        
        // Show distortion with noise
        if (distortionVisual != null)
        {
            distortionVisual.SetActive(true);
        }
        
        string expectedDisplay = string.Join("+", expectedKeys);
        Debug.Log($"<color=red>[Wall] {gameObject.name} - ❌ WRONG! Pressed: {keyName} | Expected: {expectedDisplay} | Distance: {distance} | Speed: {noiseSpeed:F2}</color>");
        
        // Trigger event
        OnKeyDistanceCalculated.Invoke(distance);
    }
    
    private int CalculateKeyDistance(string pressedKey)
    {
        int pressedIndex = allKeys.IndexOf(pressedKey);
        if (pressedIndex == -1)
        {
            Debug.LogWarning($"[Wall] Key '{pressedKey}' not found in key list!");
            return 6; // Max distance
        }
        
        int minDistance = int.MaxValue;
        
        // Calculate distance to closest expected key
        foreach (string expectedKey in expectedKeys)
        {
            int expectedIndex = allKeys.IndexOf(expectedKey);
            if (expectedIndex == -1)
            {
                Debug.LogWarning($"[Wall] Expected key '{expectedKey}' not found in key list!");
                continue;
            }
            
            int distance = Mathf.Abs(pressedIndex - expectedIndex);
            minDistance = Mathf.Min(minDistance, distance);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[Distance] Pressed: {pressedKey} (idx {pressedIndex}) | Expected: {string.Join("+", expectedKeys)} | Distance: {minDistance}");
        }
        
        return minDistance;
    }
    
    private float CalculateNoiseSpeed(int distance)
    {
        // 0 distance = 0 speed (perfect)
        // 3 distance = 0.5 speed
        // 6+ distance = 1.0 speed (max)
        
        if (distance == 0)
            return speedAtDistance0;
        else if (distance >= 6)
            return speedAtDistance6;
        else if (distance == 3)
            return speedAtDistance3;
        else if (distance < 3)
        {
            // Interpolate between 0 and 3
            float t = distance / 3f;
            return Mathf.Lerp(speedAtDistance0, speedAtDistance3, t);
        }
        else // distance between 3 and 6
        {
            // Interpolate between 3 and 6
            float t = (distance - 3) / 3f;
            return Mathf.Lerp(speedAtDistance3, speedAtDistance6, t);
        }
    }
    
    private void UpdateNoiseSpeed(float speed)
    {
        if (distortionMaterial != null)
        {
            if (distortionMaterial.HasProperty(noiseSpeedPropertyName))
            {
                distortionMaterial.SetFloat(noiseSpeedPropertyName, speed);
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=cyan>[Shader] {noiseSpeedPropertyName} = {speed:F3}</color>");
                }
            }
            else
            {
                Debug.LogWarning($"[Wall] Material doesn't have property '{noiseSpeedPropertyName}'");
            }
        }
    }
    
    private void ShowCorrectFeedback()
    {
        if (wallRenderer != null)
        {
            wallRenderer.material.color = correctKeyColor;
            feedbackTimer = feedbackDuration;
        }
    }
    
    private void ShowWrongFeedback()
    {
        if (wallRenderer != null)
        {
            wallRenderer.material.color = wrongKeyColor;
            feedbackTimer = feedbackDuration;
        }
    }
    
    private void ResetVisualFeedback()
    {
        if (wallRenderer != null)
        {
            if (isUnlocked)
            {
                wallRenderer.material.color = correctKeyColor;
            }
            else if (!isActive)
            {
                wallRenderer.material.color = inactiveColor;
            }
            else
            {
                wallRenderer.material.color = originalColor;
            }
        }
    }
    
    private void UpdateVisualState()
    {
        if (wallRenderer != null)
        {
            if (!isActive)
            {
                wallRenderer.material.color = inactiveColor;
            }
            else if (isUnlocked)
            {
                wallRenderer.material.color = correctKeyColor;
            }
            else
            {
                wallRenderer.material.color = originalColor;
            }
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    public void Activate()
    {
        if (!isActive)
        {
            isActive = true;
            
            // Reactivate the wall visual
            if (wallVisual != null)
            {
                wallVisual.SetActive(true);
            }
            
            UpdateVisualState();
            Debug.Log($"<color=yellow>[Wall] {gameObject.name} ACTIVATED | Keys: [{string.Join("+", expectedKeys)}]</color>");
        }
    }
    
    public void Deactivate()
    {
        if (isActive)
        {
            isActive = false;
            UpdateVisualState();
            
            // Hide distortion when inactive
            if (distortionVisual != null)
            {
                distortionVisual.SetActive(false);
            }
            
            // Deactivate the entire wall GameObject
            if (wallVisual != null)
            {
                wallVisual.SetActive(false);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=gray>[Wall] {gameObject.name} DEACTIVATED</color>");
            }
        }
    }
    
    public void ResetWall()
    {
        isUnlocked = false;
        lastDistance = 0;
        currentNoiseSpeed = 0f;
        lastKeyPressTime = -999f;
        
        UpdateNoiseSpeed(0f);
        UpdateVisualState();
        
        if (distortionVisual != null)
        {
            distortionVisual.SetActive(false);
        }
        
        Debug.Log($"[Wall] {gameObject.name} reset");
    }
    
    public bool IsActive() => isActive;
    public bool IsUnlocked() => isUnlocked;
    public int GetLastDistance() => lastDistance;
    public float GetCurrentNoiseSpeed() => currentNoiseSpeed;
    public List<string> GetExpectedKeys() => new List<string>(expectedKeys);
    
    public void SetExpectedKeys(List<string> keys)
    {
        expectedKeys = new List<string>(keys);
    }
    
    public static List<string> GetAllAvailableKeys()
    {
        return new List<string>(allKeys);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.yellow : (isUnlocked ? Color.green : Color.gray);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}