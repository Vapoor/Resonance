using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class Wall : MonoBehaviour
{
    [Header("Key Configuration")]
    [Tooltip("The key(s) that are expected for this wall")]
    [SerializeField] private List<string> expectedKeys = new List<string>();
    
    [Header("Key Matching Mode")]
    [SerializeField] private KeyMatchMode matchMode = KeyMatchMode.Any;
    
    [Header("References")]
    [SerializeField] private GameObject wallCube;
    [SerializeField] private GameObject heatDistortionSphere;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color correctKeyColor = Color.green;
    [SerializeField] private Color wrongKeyColor = Color.red;
    [SerializeField] private Color defaultWallColor = Color.white;
    [SerializeField] private Color inactiveWallColor = Color.gray;
    [SerializeField] private float feedbackDuration = 0.5f;
    
    [Header("Heat Distortion Settings")]
    [SerializeField] private bool activateDistortionOnCorrectKey = true;
    [SerializeField] private Material shaderGraphFXMaterial;
    [SerializeField] private string noiseSpeedPropertyName = "_NoiseSpeed";
    
    [Header("Distance Penalty System")]
    [SerializeField] private int maxDistanceForMaxSpeed = 6;  // Max distance on keyboard (half of 13)
    [SerializeField] private AnimationCurve distanceSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float perfectNoiseSpeed = 0f;
    [SerializeField] private float maxNoiseSpeed = 1f;
    [SerializeField] private bool useClosestExpectedKey = true;  // For multi-key walls
    
    [Header("Wall State")]
    [SerializeField] private bool isActive = false;
    [SerializeField] private bool isUnlocked = false;
    
    [Header("Debug Info")]
    [SerializeField] private bool showDistanceInfo = true;
    
    [Header("Events")]
    public UnityEvent<string> OnCorrectKeyPressed = new UnityEvent<string>();
    public UnityEvent<string> OnWrongKeyPressed = new UnityEvent<string>();
    public UnityEvent OnWallUnlocked = new UnityEvent();
    public UnityEvent OnWallActivated = new UnityEvent();
    public UnityEvent OnWallDeactivated = new UnityEvent();
    public UnityEvent<int> OnDistanceChanged = new UnityEvent<int>();
    
    // Private variables
    private Renderer wallRenderer;
    private Renderer sphereRenderer;
    private Material wallMaterial;
    private Material sphereMaterial;
    private KeyboardController keyboardController;
    private List<string> pressedKeys = new List<string>();
    private float feedbackTimer = 0f;
    private Color originalWallColor;
    
    // Distance tracking
    private int lastKeyDistance = 0;
    private float currentNoiseSpeed = 0f;
    
    // Key position mapping (14 keys in order)
    private static readonly List<string> keyOrder = new List<string>
    {
        "VER", "Q", "S", "D", "F", "G", "H", "J", "K", "L", "M", "ù", "*", "ENTER"
    };
    
    public enum KeyMatchMode
    {
        Any,
        All,
        Sequence,
        Simultaneous
    }
    
    private void Awake()
    {
        // Find references if not assigned
        if (wallCube == null)
        {
            wallCube = transform.Find("Cube")?.gameObject;
            if (wallCube == null && transform.childCount > 0)
            {
                wallCube = transform.GetChild(0).gameObject;
            }
        }
        
        if (heatDistortionSphere == null)
        {
            heatDistortionSphere = transform.Find("HeatDistortionSphere")?.gameObject;
            if (heatDistortionSphere == null)
            {
                foreach (Transform child in transform.GetComponentsInChildren<Transform>())
                {
                    if (child.GetComponent<SphereCollider>() != null || child.name.Contains("Sphere"))
                    {
                        heatDistortionSphere = child.gameObject;
                        break;
                    }
                }
            }
        }
        
        // Get renderers
        if (wallCube != null)
        {
            wallRenderer = wallCube.GetComponent<Renderer>();
            if (wallRenderer != null)
            {
                wallMaterial = wallRenderer.material;
                originalWallColor = wallMaterial.color;
            }
        }
        
        if (heatDistortionSphere != null)
        {
            sphereRenderer = heatDistortionSphere.GetComponent<Renderer>();
            if (sphereRenderer != null)
            {
                sphereMaterial = sphereRenderer.material;
                
                // If no shader graph material is assigned, try to use the sphere's material
                if (shaderGraphFXMaterial == null)
                {
                    shaderGraphFXMaterial = sphereMaterial;
                }
            }
        }
    }
    
    private void Start()
    {
        // Find the keyboard controller
        keyboardController = FindObjectOfType<KeyboardController>();
        
        if (keyboardController == null)
        {
            Debug.LogWarning("[Wall] KeyboardController not found in scene!");
        }
        
        // Initialize heat distortion
        if (heatDistortionSphere != null)
        {
            heatDistortionSphere.SetActive(false);
        }
        
        // Initialize shader properties
        UpdateNoiseSpeed();
        
        // Set initial visual state
        UpdateVisualState();
        
        Debug.Log($"[Wall] {gameObject.name} initialized with {expectedKeys.Count} expected key(s): {string.Join(", ", expectedKeys)} | Active: {isActive}");
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
        
        bool isCorrectKey = false;
        
        switch (matchMode)
        {
            case KeyMatchMode.Any:
                isCorrectKey = CheckAnyKeyMatch(keyName);
                break;
                
            case KeyMatchMode.All:
                isCorrectKey = CheckAllKeysMatch(keyName);
                break;
                
            case KeyMatchMode.Sequence:
                isCorrectKey = CheckSequenceMatch(keyName);
                break;
                
            case KeyMatchMode.Simultaneous:
                isCorrectKey = CheckSimultaneousMatch(keyName);
                break;
        }
        
        if (isCorrectKey)
        {
            HandleCorrectKey(keyName);
        }
        else
        {
            HandleWrongKey(keyName);
        }
    }
    
    private bool CheckAnyKeyMatch(string keyName)
    {
        return expectedKeys.Contains(keyName);
    }
    
    private bool CheckAllKeysMatch(string keyName)
    {
        if (expectedKeys.Contains(keyName) && !pressedKeys.Contains(keyName))
        {
            pressedKeys.Add(keyName);
            Debug.Log($"[Wall] {gameObject.name} - Key collected: {keyName} ({pressedKeys.Count}/{expectedKeys.Count})");
        }
        
        if (pressedKeys.Count == expectedKeys.Count)
        {
            bool allMatch = true;
            foreach (string key in expectedKeys)
            {
                if (!pressedKeys.Contains(key))
                {
                    allMatch = false;
                    break;
                }
            }
            return allMatch;
        }
        
        return false;
    }
    
    private bool CheckSequenceMatch(string keyName)
    {
        int currentIndex = pressedKeys.Count;
        
        if (currentIndex < expectedKeys.Count && expectedKeys[currentIndex] == keyName)
        {
            pressedKeys.Add(keyName);
            Debug.Log($"[Wall] {gameObject.name} - Sequence progress: {keyName} ({pressedKeys.Count}/{expectedKeys.Count})");
            
            return pressedKeys.Count == expectedKeys.Count;
        }
        else
        {
            if (expectedKeys.Contains(keyName))
            {
                Debug.Log($"[Wall] {gameObject.name} - Wrong order! Resetting sequence.");
            }
            pressedKeys.Clear();
            return false;
        }
    }
    
    private bool CheckSimultaneousMatch(string keyName)
    {
        if (keyboardController != null)
        {
            int matchCount = 0;
            foreach (string key in expectedKeys)
            {
                if (keyboardController.IsKeyPressed(key))
                {
                    matchCount++;
                }
            }
            return matchCount == expectedKeys.Count;
        }
        return false;
    }
    
    private void HandleCorrectKey(string keyName)
    {
        Debug.Log($"<color=green>[Wall] {gameObject.name} - ✓ PERFECT! Key: {keyName}</color>");
        
        // Set distance to 0 (perfect)
        lastKeyDistance = 0;
        
        // Visual feedback
        ShowCorrectKeyFeedback();
        
        // Set noise speed to perfect (0)
        currentNoiseSpeed = perfectNoiseSpeed;
        UpdateNoiseSpeed();
        
        // Activate heat distortion with perfect clarity
        if (activateDistortionOnCorrectKey && heatDistortionSphere != null)
        {
            heatDistortionSphere.SetActive(true);
        }
        
        // Mark as unlocked
        isUnlocked = true;
        
        // Trigger events
        OnCorrectKeyPressed.Invoke(keyName);
        OnWallUnlocked.Invoke();
    }
    
    private void HandleWrongKey(string keyName)
    {
        // Calculate distance from expected key
        int distance = CalculateKeyDistance(keyName);
        lastKeyDistance = distance;
        
        // Calculate noise speed based on distance
        CalculateNoiseSpeedFromDistance(distance);
        
        // Update the shader
        UpdateNoiseSpeed();
        
        // Get expected key for display
        string expectedKeyDisplay = GetExpectedKeyForDisplay();
        
        string distanceInfo = showDistanceInfo ? $" | Distance: {distance} keys away | Noise Speed: {currentNoiseSpeed:F2}" : "";
        Debug.Log($"<color=red>[Wall] {gameObject.name} - ✗ WRONG KEY: {keyName} (Expected: {expectedKeyDisplay}){distanceInfo}</color>");
        
        // Visual feedback
        ShowWrongKeyFeedback();
        
        // Activate heat distortion with noise based on distance
        if (heatDistortionSphere != null)
        {
            heatDistortionSphere.SetActive(true);
        }
        
        // Trigger events
        OnWrongKeyPressed.Invoke(keyName);
        OnDistanceChanged.Invoke(distance);
    }
    
    /// <summary>
    /// Calculate the distance between pressed key and expected key(s)
    /// </summary>
    private int CalculateKeyDistance(string pressedKey)
    {
        int pressedKeyIndex = keyOrder.IndexOf(pressedKey);
        
        if (pressedKeyIndex == -1)
        {
            Debug.LogWarning($"[Wall] Key '{pressedKey}' not found in key order list!");
            return maxDistanceForMaxSpeed;
        }
        
        int minDistance = int.MaxValue;
        
        // Calculate distance to each expected key
        foreach (string expectedKey in expectedKeys)
        {
            int expectedKeyIndex = keyOrder.IndexOf(expectedKey);
            
            if (expectedKeyIndex == -1)
            {
                Debug.LogWarning($"[Wall] Expected key '{expectedKey}' not found in key order list!");
                continue;
            }
            
            // Calculate absolute distance
            int distance = Mathf.Abs(pressedKeyIndex - expectedKeyIndex);
            
            if (useClosestExpectedKey)
            {
                // Use the closest expected key
                minDistance = Mathf.Min(minDistance, distance);
            }
            else
            {
                // Use the first expected key (for sequence mode)
                minDistance = distance;
                break;
            }
        }
        
        if (showDistanceInfo)
        {
            Debug.Log($"[Distance] Pressed: {pressedKey} (index {pressedKeyIndex}) | Expected: {string.Join(", ", expectedKeys)} | Distance: {minDistance}");
        }
        
        return minDistance;
    }
    
    private string GetExpectedKeyForDisplay()
    {
        if (matchMode == KeyMatchMode.Sequence && pressedKeys.Count < expectedKeys.Count)
        {
            // For sequence, show the next expected key
            return expectedKeys[pressedKeys.Count];
        }
        else
        {
            // Show all expected keys
            return string.Join(", ", expectedKeys);
        }
    }
    
    private void CalculateNoiseSpeedFromDistance(int distance)
    {
        // Normalize distance (0 to 1)
        float normalizedDistance = Mathf.Clamp01((float)distance / maxDistanceForMaxSpeed);
        
        // Evaluate the curve
        float curveValue = distanceSpeedCurve.Evaluate(normalizedDistance);
        
        // Calculate final noise speed
        currentNoiseSpeed = Mathf.Lerp(perfectNoiseSpeed, maxNoiseSpeed, curveValue);
        
        if (showDistanceInfo)
        {
            Debug.Log($"[Noise] Distance: {distance} | Normalized: {normalizedDistance:F2} | Curve: {curveValue:F2} | Speed: {currentNoiseSpeed:F3}");
        }
    }
    
    private void UpdateNoiseSpeed()
    {
        if (shaderGraphFXMaterial != null)
        {
            // Check if the property exists
            if (shaderGraphFXMaterial.HasProperty(noiseSpeedPropertyName))
            {
                shaderGraphFXMaterial.SetFloat(noiseSpeedPropertyName, currentNoiseSpeed);
                
                if (showDistanceInfo)
                {
                    Debug.Log($"<color=cyan>[Shader] {noiseSpeedPropertyName} set to: {currentNoiseSpeed:F3}</color>");
                }
            }
            else
            {
                Debug.LogWarning($"[Wall] Material doesn't have property '{noiseSpeedPropertyName}'. Available properties:");
                
                // List all available properties for debugging
                Shader shader = shaderGraphFXMaterial.shader;
                int propertyCount = shader.GetPropertyCount();
                for (int i = 0; i < propertyCount; i++)
                {
                    Debug.Log($"  - {shader.GetPropertyName(i)} ({shader.GetPropertyType(i)})");
                }
            }
        }
        else
        {
            Debug.LogWarning("[Wall] Shader Graph FX Material not assigned!");
        }
    }
    
    private void ShowCorrectKeyFeedback()
    {
        if (wallMaterial != null)
        {
            wallMaterial.color = correctKeyColor;
            feedbackTimer = feedbackDuration;
        }
    }
    
    private void ShowWrongKeyFeedback()
    {
        if (wallMaterial != null)
        {
            wallMaterial.color = wrongKeyColor;
            feedbackTimer = feedbackDuration;
        }
    }
    
    private void ResetVisualFeedback()
    {
        if (wallMaterial != null)
        {
            if (isUnlocked)
            {
                wallMaterial.color = correctKeyColor;
            }
            else if (!isActive)
            {
                wallMaterial.color = inactiveWallColor;
            }
            else
            {
                wallMaterial.color = originalWallColor;
            }
        }
    }
    
    private void UpdateVisualState()
    {
        if (wallMaterial != null)
        {
            if (!isActive)
            {
                wallMaterial.color = inactiveWallColor;
            }
            else if (isUnlocked)
            {
                wallMaterial.color = correctKeyColor;
            }
            else
            {
                wallMaterial.color = originalWallColor;
            }
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    public void Activate()
    {
        if (!isActive)
        {
            isActive = true;
            UpdateVisualState();
            OnWallActivated.Invoke();
            Debug.Log($"<color=yellow>[Wall] {gameObject.name} ACTIVATED - Listening for keys: {string.Join(", ", expectedKeys)}</color>");
        }
    }
    
    public void Deactivate()
    {
        if (isActive)
        {
            isActive = false;
            UpdateVisualState();
            OnWallDeactivated.Invoke();
            Debug.Log($"<color=gray>[Wall] {gameObject.name} DEACTIVATED</color>");
        }
    }
    
    public void SetExpectedKeys(List<string> keys)
    {
        expectedKeys = new List<string>(keys);
        pressedKeys.Clear();
        Debug.Log($"[Wall] {gameObject.name} - Expected keys set to: {string.Join(", ", expectedKeys)}");
    }
    
    public void AddExpectedKey(string key)
    {
        if (!expectedKeys.Contains(key))
        {
            expectedKeys.Add(key);
            Debug.Log($"[Wall] {gameObject.name} - Added expected key: {key}");
        }
    }
    
    public void RemoveExpectedKey(string key)
    {
        if (expectedKeys.Contains(key))
        {
            expectedKeys.Remove(key);
            Debug.Log($"[Wall] {gameObject.name} - Removed expected key: {key}");
        }
    }
    
    public void ResetWall()
    {
        isUnlocked = false;
        pressedKeys.Clear();
        lastKeyDistance = 0;
        currentNoiseSpeed = perfectNoiseSpeed;
        
        UpdateNoiseSpeed();
        ResetVisualFeedback();
        
        if (heatDistortionSphere != null)
        {
            heatDistortionSphere.SetActive(false);
        }
        
        Debug.Log($"[Wall] {gameObject.name} has been reset");
    }
    
    public void SetActive(bool active)
    {
        if (active)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
    }
    
    public bool IsActive()
    {
        return isActive;
    }
    
    public bool IsUnlocked()
    {
        return isUnlocked;
    }
    
    public int GetLastKeyDistance()
    {
        return lastKeyDistance;
    }
    
    public float GetCurrentNoiseSpeed()
    {
        return currentNoiseSpeed;
    }
    
    public List<string> GetExpectedKeys()
    {
        return new List<string>(expectedKeys);
    }
    
    // Get the position of a key in the keyboard layout
    public static int GetKeyPosition(string keyName)
    {
        return keyOrder.IndexOf(keyName);
    }
    
    // Get total number of keys
    public static int GetTotalKeyCount()
    {
        return keyOrder.Count;
    }
    
    // Manually test distance calculation
    public void TestKeyDistance(string pressedKey)
    {
        int distance = CalculateKeyDistance(pressedKey);
        CalculateNoiseSpeedFromDistance(distance);
        UpdateNoiseSpeed();
        
        Debug.Log($"<color=magenta>[TEST] Pressed: {pressedKey} | Distance: {distance} | Noise Speed: {currentNoiseSpeed:F3}</color>");
    }
    
    // Gizmo to show wall info in editor
    private void OnDrawGizmos()
    {
        if (isActive && !isUnlocked)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        else if (isUnlocked)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        else
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
    
    // Validate in editor
    private void OnValidate()
    {
        // Ensure max distance is reasonable (max is 13 keys)
        maxDistanceForMaxSpeed = Mathf.Clamp(maxDistanceForMaxSpeed, 1, 13);
        
        // Clamp speeds
        perfectNoiseSpeed = Mathf.Clamp01(perfectNoiseSpeed);
        maxNoiseSpeed = Mathf.Clamp01(maxNoiseSpeed);
    }
}