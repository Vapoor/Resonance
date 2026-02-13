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
    
    [Header("Distortion Wall Linking")]
    [SerializeField] private string distortionWallSuffix = "_Distortion";
    [SerializeField] private GameObject linkedDistortionWall;
    
    [Header("Unlock Behavior")]
    [SerializeField] private UnlockBehavior unlockBehavior = UnlockBehavior.Hide;
    [SerializeField] private float despawnDelay = 1f;
    
    [Header("Cooldown System")]
    [SerializeField] private float keyCooldown = 1f;
    private float lastKeyPressTime = -999f;
    
    [Header("Distance to Speed Mapping - INVERTED")]
    [Tooltip("Close match = HIGH speed (more distortion)")]
    [SerializeField] private float speedAtDistance0 = 1f;    // Perfect = MAX speed ✅
    [SerializeField] private float speedAtDistance3 = 0.5f;  // Medium
    [SerializeField] private float speedAtDistance6 = 0f;    // Far = NO speed ✅

    
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
    public UnityEvent<string> OnCorrectKeyPressed = new UnityEvent<string>();
    public UnityEvent<string> OnWrongKeyPressed = new UnityEvent<string>();
    public UnityEvent<int> OnKeyDistanceCalculated = new UnityEvent<int>();
    
    // Private variables
    private Renderer wallRenderer;
    private Renderer distortionRenderer;
    private Material distortionMaterial;
    private Color originalColor;
    private float feedbackTimer = 0f;
    private int lastDistance = 0;
    private float currentNoiseSpeed = 0f;
    private KeyboardController keyboardController;
    
    // All 14 keys in physical order
    private static readonly List<string> allKeys = new List<string>
    {
        "VER", "Q", "S", "D", "F", "G", "H", "J", "K", "L", "M", "ù", "*", "ENTER"
    };
    
    public enum KeyMatchMode
    {
        Any,
        All,
        Simultaneous
    }
    
    public enum UnlockBehavior
    {
        None,
        Hide,
        Destroy,
        HideWithDistortion
    }
    
    private void Awake()
    {
        SetupReferences();
        FindLinkedDistortionWall();
    }
    
    private void SetupReferences()
    {
        wallRenderer = GetComponent<Renderer>();
        if (wallRenderer != null)
        {
            originalColor = wallRenderer.material.color;
        }
    }
    
    private void FindLinkedDistortionWall()
    {
        if (linkedDistortionWall != null)
        {
            SetupDistortionWall(linkedDistortionWall);
            return;
        }
        
        string distortionWallName = gameObject.name + distortionWallSuffix;
        GameObject foundDistortionWall = GameObject.Find(distortionWallName);
        
        if (foundDistortionWall != null)
        {
            linkedDistortionWall = foundDistortionWall;
            SetupDistortionWall(linkedDistortionWall);
            Debug.Log($"<color=cyan>[Wall] Found distortion wall: {distortionWallName}</color>");
        }
        else
        {
            Debug.LogWarning($"[Wall] No distortion wall found with name '{distortionWallName}'.");
        }
    }
    
    private void SetupDistortionWall(GameObject distortionWall)
    {
        if (distortionWall == null) return;
        
        distortionRenderer = distortionWall.GetComponent<Renderer>();
        
        if (distortionRenderer != null)
        {
            distortionMaterial = distortionRenderer.material;
            
            if (showDebugInfo)
            {
                Debug.Log($"[Wall] Linked to distortion wall: {distortionWall.name}");
            }
        }
    }
    
    private void Start()
    {
        keyboardController = FindObjectOfType<KeyboardController>();
        
        if (linkedDistortionWall != null)
        {
            linkedDistortionWall.SetActive(false);
        }
        
        UpdateNoiseSpeed(0f);
        UpdateVisualState();
        
        if (showDebugInfo)
        {
            Debug.Log($"[Wall] {gameObject.name} initialized | Keys: [{string.Join("+", expectedKeys)}] | Mode: {matchMode}");
        }
    }
    
    private void Update()
    {
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
                return expectedKeys.Contains(keyName);
                
            case KeyMatchMode.Simultaneous:
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
    currentNoiseSpeed = speedAtDistance0; // Should be 1.0
    
    Debug.Log($"<color=magenta>[DEBUG] Distance=0 → NoiseSpeed={currentNoiseSpeed}</color>");
    
    UpdateNoiseSpeed(currentNoiseSpeed);
    ShowCorrectFeedback();
    
    // Show distortion on correct key
    if (linkedDistortionWall != null)
    {
        linkedDistortionWall.SetActive(true);
    }
    
    isUnlocked = true;
    
    OnCorrectKeyPressed.Invoke(keyName);
    OnWallUnlocked.Invoke();
    OnKeyDistanceCalculated.Invoke(0);
    
    HandleUnlockBehavior();
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
    
private void HandleWrongKey(string keyName)
{
    int distance = CalculateKeyDistance(keyName);
    lastDistance = distance;
    
    // Check if distance is too far (6 or more)
    if (distance >= 6)
    {
        // Too far off - hide distortion completely
        currentNoiseSpeed = 0f;
        
        if (linkedDistortionWall != null)
        {
            linkedDistortionWall.SetActive(false);
        }
        
        Debug.Log($"<color=red>[Wall] {gameObject.name} - ❌ TOO FAR! Pressed: {keyName} | Distance: {distance} | Distortion HIDDEN</color>");
    }
    else
    {
        // Close enough (0-5) - show distortion with calculated speed
        float noiseSpeed = CalculateNoiseSpeed(distance);
        currentNoiseSpeed = noiseSpeed;
        
        UpdateNoiseSpeed(currentNoiseSpeed);
        
        // Show distortion wall
        if (linkedDistortionWall != null)
        {
            linkedDistortionWall.SetActive(true);
        }
        
        string expectedDisplay = string.Join("+", expectedKeys);
        Debug.Log($"<color=red>[Wall] {gameObject.name} - ❌ WRONG! Pressed: {keyName} | Expected: {expectedDisplay} | Distance: {distance} | Speed: {noiseSpeed:F2}</color>");
        
        Debug.Log($"<color=magenta>[DEBUG] Distance={distance} → NoiseSpeed={noiseSpeed:F3} | Distortion VISIBLE</color>");
    }
    
    ShowWrongFeedback();
    
    OnWrongKeyPressed.Invoke(keyName);
    OnKeyDistanceCalculated.Invoke(distance);
}
    
   private float CalculateNoiseSpeed(int distance)
{
    // INVERTED: closer = MORE speed, farther = LESS speed
    // This is only called for distances 0-5
    // Distance 6+ is handled separately in HandleWrongKey
    
    if (distance == 0)
        return speedAtDistance0;  // 1.0
    else if (distance >= 5)
        return 0.1f;  // Very low speed at distance 5
    else if (distance == 3)
        return speedAtDistance3;  // 0.5
    else if (distance < 3)
    {
        // Interpolate between 0 and 3
        float t = distance / 3f;
        return Mathf.Lerp(speedAtDistance0, speedAtDistance3, t);
    }
    else // distance 4-5
    {
        // Interpolate between 3 and 5
        float t = (distance - 3) / 2f;
        return Mathf.Lerp(speedAtDistance3, 0.1f, t);
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
    
    private void HandleUnlockBehavior()
    {
        switch (unlockBehavior)
        {
            case UnlockBehavior.None:
                break;
                
            case UnlockBehavior.Hide:
                Invoke(nameof(HideWall), despawnDelay);
                break;
                
            case UnlockBehavior.Destroy:
                Invoke(nameof(DestroyWall), despawnDelay);
                break;
                
            case UnlockBehavior.HideWithDistortion:
                Invoke(nameof(HideWallKeepDistortion), despawnDelay);
                break;
        }
    }
    
    private void HideWall()
    {
        gameObject.SetActive(false);
        
        if (linkedDistortionWall != null)
        {
            linkedDistortionWall.SetActive(false);
        }
        
        Debug.Log($"<color=gray>[Wall] {gameObject.name} hidden</color>");
    }
    
    private void DestroyWall()
    {
        Debug.Log($"<color=gray>[Wall] {gameObject.name} destroyed</color>");
        
        if (linkedDistortionWall != null)
        {
            Destroy(linkedDistortionWall);
        }
        
        Destroy(gameObject);
    }
    
    private void HideWallKeepDistortion()
    {
        if (wallRenderer != null)
        {
            wallRenderer.enabled = false;
        }
        
        Debug.Log($"<color=gray>[Wall] {gameObject.name} hidden, distortion remains</color>");
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
    
    public void Activate()
    {
        if (!isActive)
        {
            isActive = true;
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
            
            if (linkedDistortionWall != null)
            {
                linkedDistortionWall.SetActive(false);
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
        
        CancelInvoke();
        
        gameObject.SetActive(true);
        if (wallRenderer != null)
        {
            wallRenderer.enabled = true;
        }
        
        UpdateNoiseSpeed(0f);
        UpdateVisualState();
        
        if (linkedDistortionWall != null)
        {
            linkedDistortionWall.SetActive(false);
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
    
    public void SetLinkedDistortionWall(GameObject distortionWall)
    {
        linkedDistortionWall = distortionWall;
        SetupDistortionWall(distortionWall);
    }
    
    public static List<string> GetAllAvailableKeys()
    {
        return new List<string>(allKeys);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.yellow : (isUnlocked ? Color.green : Color.gray);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        if (linkedDistortionWall != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, linkedDistortionWall.transform.position);
        }
    }
}