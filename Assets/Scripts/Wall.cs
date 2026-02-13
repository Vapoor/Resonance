using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class Wall : MonoBehaviour
{
    [Header("Input Mode")]
    [SerializeField] private InputMode inputMode = InputMode.Keyboard;
    
    [Header("Keyboard Configuration")]
    [Tooltip("Expected key combination for this wall (e.g., single key or combo like Q+S)")]
    [SerializeField] private List<string> expectedKeys = new List<string>();
    [SerializeField] private KeyMatchMode keyMatchMode = KeyMatchMode.Any;
    
    [Header("MIDI Note Configuration")]
    [Tooltip("Expected MIDI note number (e.g., 60 = Middle C, 72 = C one octave higher)")]
    [SerializeField] private int expectedMidiNote = 60;
    
    [Header("Audio Hint System")]
    [SerializeField] private bool enableAudioHints = true;
    [SerializeField] private AudioClip hintSound;
    [SerializeField] private int referenceMidiNote = 60;
    [SerializeField] private float hintInterval = 5f;
    [SerializeField] private float hintVolume = 0.7f;
    
    [Header("Distortion Wall Linking")]
    [SerializeField] private string distortionWallSuffix = "_Distortion";
    [SerializeField] private GameObject linkedDistortionWall;
    [SerializeField] private bool keepDistortionAfterUnlock = true;
    
    [Header("Correct Input Behavior")]
    [SerializeField] private float greenIndicatorDuration = 0.5f;
    [SerializeField] private float maxDistortionDelay = 0.3f;
    
    [Header("Cooldown System")]
    [SerializeField] private float inputCooldown = 0.5f;
    private float lastInputTime = -999f;
    
    [Header("Distance to Speed Mapping")]
    [Tooltip("Close match = HIGH speed, Far match = LOW speed")]
    private float speedAtDistance1 = 1f;
    private float speedAtDistance3 = 0.5f;
    private float speedAtDistance5 = 0.1f;
    
    [Header("Shader Settings")]
    [SerializeField] private string noiseSpeedPropertyName = "_NoiseSpeed";
    
    [Header("Visual Feedback")]
    [SerializeField] private Color correctInputColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.gray;
    
    [Header("Wall State")]
    [SerializeField] private bool isActive = false;
    [SerializeField] private bool isUnlocked = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    [Header("Music After Clear")]
    [SerializeField] private AudioClip musicAfterClear;
    [SerializeField] private bool changeMusicOnClear = true;
    [SerializeField] private float musicCrossfadeDuration = 2f;
    
    [Header("Events")]
    public UnityEvent OnWallUnlocked = new UnityEvent();
    public UnityEvent<string> OnCorrectKeyPressed = new UnityEvent<string>();
    public UnityEvent<string> OnWrongKeyPressed = new UnityEvent<string>();
    public UnityEvent<int> OnCorrectNotePressed = new UnityEvent<int>();
    public UnityEvent<int> OnWrongNotePressed = new UnityEvent<int>();
    public UnityEvent<int> OnKeyDistanceCalculated = new UnityEvent<int>();
    
    // Private variables
    private Renderer wallRenderer;
    private Renderer distortionRenderer;
    private Material distortionMaterial;
    private Color originalColor;
    private int lastDistance = 0;
    private float currentNoiseSpeed = 0f;
    private KeyboardController keyboardController;
    private AudioSource audioSource;
    private Coroutine hintCoroutine;
    private bool isShowingCorrectSequence = false;
    private bool playerInAudioRange = false;
    
    // All 14 keys in physical order
    private static readonly List<string> allKeys = new List<string>
    {
        "VER", "Q", "S", "D", "F", "G", "H", "J", "K", "L", "M", "ù", "*", "ENTER"
    };
    
    public enum InputMode
    {
        Keyboard,
        MIDI
    }
    
    public enum KeyMatchMode
    {
        Any,
        All,
        Simultaneous
    }
    
    private void Awake()
    {
        SetupReferences();
        FindLinkedDistortionWall();
        SetupAudio();
    }
    
    private void SetupReferences()
    {
        wallRenderer = GetComponent<Renderer>();
        if (wallRenderer != null)
        {
            originalColor = wallRenderer.material.color;
        }
    }
    
    private void SetupAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = hintVolume;
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
            if (showDebugInfo)
            {
                Debug.Log($"<color=cyan>[Wall] Found distortion wall: {distortionWallName}</color>");
            }
        }
    }
    
    private void SetupDistortionWall(GameObject distortionWall)
    {
        if (distortionWall == null) return;
        
        distortionRenderer = distortionWall.GetComponent<Renderer>();
        
        if (distortionRenderer != null)
        {
            distortionMaterial = distortionRenderer.material;
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
            if (inputMode == InputMode.Keyboard)
            {
                Debug.Log($"[Wall] {gameObject.name} initialized | Mode: KEYBOARD | Keys: [{string.Join("+", expectedKeys)}]");
            }
            else
            {
                Debug.Log($"[Wall] {gameObject.name} initialized | Mode: MIDI | Expected Note: {expectedMidiNote} ({GetNoteName(expectedMidiNote)})");
            }
        }
    }
    
    // ==================== KEYBOARD INPUT ====================
    
    public void OnKeyPressed(string keyName)
    {
        if (inputMode != InputMode.Keyboard) return;
        
        if (!isActive || isUnlocked)
        {
            return;
        }
        
        if (Time.time - lastInputTime < inputCooldown)
        {
            if (showDebugInfo)
            {
                float remaining = inputCooldown - (Time.time - lastInputTime);
                Debug.Log($"[Wall] Cooldown active! Wait {remaining:F2}s");
            }
            return;
        }
        
        lastInputTime = Time.time;
        
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
        switch (keyMatchMode)
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
        
        StartCoroutine(CorrectInputSequence());
        
        StopAudioHints();
        
        isUnlocked = true;
        
        OnCorrectKeyPressed.Invoke(keyName);
        OnWallUnlocked.Invoke();
        OnKeyDistanceCalculated.Invoke(0);
    }
    
    private IEnumerator CorrectInputSequence()
    {
        isShowingCorrectSequence = true;
        
        // Step 1: Show green color IMMEDIATELY
        if (wallRenderer != null)
        {
            wallRenderer.material.color = correctInputColor;
            
            // Set albedo to green
            if (wallRenderer.material.HasProperty("_BaseColor"))
            {
                wallRenderer.material.SetColor("_BaseColor", correctInputColor);
            }
            else if (wallRenderer.material.HasProperty("_Color"))
            {
                wallRenderer.material.SetColor("_Color", correctInputColor);
            }
            
            Debug.Log($"<color=green>[Wall] 🟢 GREEN COLOR ACTIVATED!</color>");
        }
        
        // Disable all mesh colliders
        DisableAllMeshColliders();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=green>[Wall] Showing green indicator for {greenIndicatorDuration}s</color>");
        }
        
        yield return new WaitForSeconds(greenIndicatorDuration);
        
        // Step 2: Activate distortion wall
        if (linkedDistortionWall != null)
        {
            linkedDistortionWall.SetActive(true);
            Debug.Log($"<color=cyan>[Wall] Distortion wall activated</color>");
        }
        
        // Step 3: Small delay before max distortion
        yield return new WaitForSeconds(maxDistortionDelay);
        
        // Step 4: Set noise speed to maximum (1.0) and KEEP IT THERE
        currentNoiseSpeed = speedAtDistance1;
        UpdateNoiseSpeed(currentNoiseSpeed);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=magenta>[Wall] 💥 MAXIMUM DISTORTION LOCKED! Speed: {currentNoiseSpeed} - PERMANENT!</color>");
        }
        
        // Step 5: Change background music if configured
        if (changeMusicOnClear && musicAfterClear != null)
        {
            BackgroundMusicManager.Instance.CrossfadeToMusic(musicAfterClear, musicCrossfadeDuration);
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=cyan>[Wall] 🎵 Crossfading to new music: {musicAfterClear.name}</color>");
            }
        }
        
        isShowingCorrectSequence = false;
    }
    
    private void HandleWrongKey(string keyName)
    {
        int distance = CalculateKeyDistance(keyName);
        lastDistance = distance;
        
        if (distance > 5)
        {
            currentNoiseSpeed = 0f;
            
            if (linkedDistortionWall != null)
            {
                linkedDistortionWall.SetActive(false);
            }
            
            Debug.Log($"<color=yellow>[Wall] ❌ TOO FAR! Pressed: {keyName} | Distance: {distance} | Distortion HIDDEN</color>");
        }
        else
        {
            float noiseSpeed = CalculateNoiseSpeed(distance);
            currentNoiseSpeed = noiseSpeed;
            
            UpdateNoiseSpeed(currentNoiseSpeed);
            
            if (linkedDistortionWall != null)
            {
                linkedDistortionWall.SetActive(true);
            }
            
            string expectedDisplay = string.Join("+", expectedKeys);
            Debug.Log($"<color=yellow>[Wall] ❌ WRONG! Pressed: {keyName} | Expected: {expectedDisplay} | Distance: {distance} | Speed: {noiseSpeed:F2}</color>");
        }
        
        OnWrongKeyPressed.Invoke(keyName);
        OnKeyDistanceCalculated.Invoke(distance);
    }
    
    private int CalculateKeyDistance(string pressedKey)
    {
        int pressedIndex = allKeys.IndexOf(pressedKey);
        if (pressedIndex == -1)
        {
            Debug.LogWarning($"[Wall] Key '{pressedKey}' not found in key list!");
            return 6;
        }
        
        int minDistance = int.MaxValue;
        
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
    
    // ==================== MIDI INPUT ====================
    
    public void OnMidiNotePressed(int midiNote, float velocity)
    {
        if (inputMode != InputMode.MIDI) return;
        
        if (!isActive || isUnlocked)
        {
            return;
        }
        
        if (Time.time - lastInputTime < inputCooldown)
        {
            if (showDebugInfo)
            {
                float remaining = inputCooldown - (Time.time - lastInputTime);
                Debug.Log($"[Wall] Cooldown active! Wait {remaining:F2}s");
            }
            return;
        }
        
        lastInputTime = Time.time;
        
        bool isCorrect = CheckNoteMatch(midiNote);
        
        if (isCorrect)
        {
            HandleCorrectNote(midiNote, velocity);
        }
        else
        {
            HandleWrongNote(midiNote, velocity);
        }
    }
    
    private bool CheckNoteMatch(int midiNote)
    {
        return midiNote == expectedMidiNote;
    }
    
    private void HandleCorrectNote(int midiNote, float velocity)
    {
        Debug.Log($"<color=green>[Wall] {gameObject.name} - ✅ PERFECT NOTE! Played: {midiNote} ({GetNoteName(midiNote)})</color>");
        
        lastDistance = 0;
        
        StartCoroutine(CorrectInputSequence());
        
        StopAudioHints();
        
        isUnlocked = true;
        
        OnCorrectNotePressed.Invoke(midiNote);
        OnWallUnlocked.Invoke();
        OnKeyDistanceCalculated.Invoke(0);
    }
    
    private void HandleWrongNote(int midiNote, float velocity)
    {
        int distance = CalculateNoteDistance(midiNote);
        lastDistance = distance;
        
        if (distance >= 12)
        {
            currentNoiseSpeed = 0f;
            
            if (linkedDistortionWall != null)
            {
                linkedDistortionWall.SetActive(false);
            }
            
            Debug.Log($"<color=yellow>[Wall] ❌ TOO FAR! Played: {midiNote} ({GetNoteName(midiNote)}) | Expected: {expectedMidiNote} ({GetNoteName(expectedMidiNote)}) | Distance: {distance} semitones | Distortion HIDDEN</color>");
        }
        else
        {
            float noiseSpeed = CalculateNoiseSpeedMidi(distance);
            currentNoiseSpeed = noiseSpeed;
            
            UpdateNoiseSpeed(currentNoiseSpeed);
            
            if (linkedDistortionWall != null)
            {
                linkedDistortionWall.SetActive(true);
            }
            
            Debug.Log($"<color=yellow>[Wall] ❌ WRONG! Played: {midiNote} ({GetNoteName(midiNote)}) | Expected: {expectedMidiNote} ({GetNoteName(expectedMidiNote)}) | Distance: {distance} semitones | Speed: {noiseSpeed:F2}</color>");
        }
        
        OnWrongNotePressed.Invoke(midiNote);
        OnKeyDistanceCalculated.Invoke(distance);
    }
    
    private int CalculateNoteDistance(int playedNote)
    {
        int distance = Mathf.Abs(playedNote - expectedMidiNote);
        
        if (showDebugInfo)
        {
            Debug.Log($"[Distance] Played: {playedNote} | Expected: {expectedMidiNote} | Distance: {distance} semitones");
        }
        
        return distance;
    }
    
    // ==================== DISTANCE CALCULATION ====================
    
    private float CalculateNoiseSpeed(int distance)
    {
        if (distance == 0)
            return 1f;
        else if (distance > 5)
            return 0f;
        else if (distance == 1)
            return speedAtDistance1;
        else if (distance <= 3)
        {
            float t = (distance - 1) / 2f;
            return Mathf.Lerp(speedAtDistance1, speedAtDistance3, t);
        }
        else
        {
            float t = (distance - 3) / 2f;
            return Mathf.Lerp(speedAtDistance3, speedAtDistance5, t);
        }
    }
    
    private float CalculateNoiseSpeedMidi(int distance)
    {
        if (distance == 0)
            return 1f;
        else if (distance >= 12)
            return 0f;
        else if (distance <= 3)
        {
            float t = distance / 3f;
            return Mathf.Lerp(1f, 0.7f, t);
        }
        else if (distance <= 6)
        {
            float t = (distance - 3) / 3f;
            return Mathf.Lerp(0.7f, 0.5f, t);
        }
        else
        {
            float t = (distance - 6) / 6f;
            return Mathf.Lerp(0.5f, 0.1f, t);
        }
    }
    
    // ==================== AUDIO HINT SYSTEM ====================
    
    private void StartAudioHints()
    {
        if (inputMode != InputMode.MIDI) return;
        
        if (enableAudioHints && hintSound != null && !isUnlocked)
        {
            StopAudioHints();
            hintCoroutine = StartCoroutine(PlayAudioHintsRoutine());
        }
    }
    
    private void StopAudioHints()
    {
        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
            hintCoroutine = null;
        }
    }
    
    private IEnumerator PlayAudioHintsRoutine()
    {
        // Don't play hint immediately - wait for player to enter trigger
        
        while (isActive && !isUnlocked)
        {
            yield return new WaitForSeconds(hintInterval);
            
            if (isActive && !isUnlocked)
            {
                // Only play if player is in range
                if (playerInAudioRange)
                {
                    PlayHintSound();
                }
                else if (showDebugInfo)
                {
                    Debug.Log($"<color=gray>[Wall] Skipping hint - player not in range</color>");
                }
            }
        }
    }
    
    private void PlayHintSound()
    {
        if (hintSound == null || audioSource == null)
        {
            return;
        }
        
        float pitch = CalculatePitch(expectedMidiNote);
        
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(hintSound);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=yellow>[Wall] �� Playing hint for note {expectedMidiNote} ({GetNoteName(expectedMidiNote)}) | Pitch: {pitch:F2}</color>");
        }
    }
    
    private float CalculatePitch(int targetNote)
    {
        int semitones = targetNote - referenceMidiNote;
        float pitch = Mathf.Pow(2f, semitones / 12f);
        return pitch;
    }
    
    private string GetNoteName(int midiNote)
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int octave = (midiNote / 12) - 1;
        int noteIndex = midiNote % 12;
        return $"{noteNames[noteIndex]}{octave}";
    }
    
    // ==================== EXTERNAL AUDIO RANGE CONTROL ====================
    
    /// <summary>
    /// Called by MusicTriggerZone to enable/disable audio hints
    /// </summary>
    public void SetPlayerInAudioRange(bool inRange)
    {
        playerInAudioRange = inRange;
        
        if (showDebugInfo)
        {
            Debug.Log($"<color={(inRange ? "cyan" : "gray")}>[Wall] {gameObject.name} - Player audio range: {inRange}</color>");
        }
        
        // Play hint immediately when player enters (if wall is active)
        if (inRange && isActive && !isUnlocked && inputMode == InputMode.MIDI && enableAudioHints)
        {
            PlayHintSound();
        }
    }
    
    // ==================== VISUAL FEEDBACK ====================
    
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
                wallRenderer.material.color = correctInputColor;
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
            UpdateVisualState();
            
            if (inputMode == InputMode.MIDI)
            {
                StartAudioHints();
            }
            
            if (showDebugInfo)
            {
                if (inputMode == InputMode.Keyboard)
                {
                    Debug.Log($"<color=yellow>[Wall] {gameObject.name} ACTIVATED | Keys: [{string.Join("+", expectedKeys)}]</color>");
                }
                else
                {
                    Debug.Log($"<color=yellow>[Wall] {gameObject.name} ACTIVATED | Expected Note: {expectedMidiNote} ({GetNoteName(expectedMidiNote)})</color>");
                }
            }
        }
    }
    
    public void Deactivate()
    {
        if (isActive)
        {
            isActive = false;
            UpdateVisualState();
            StopAudioHints();
            
            // DON'T disable distortion if wall is unlocked and keepDistortionAfterUnlock is true
            if (linkedDistortionWall != null && !(isUnlocked && keepDistortionAfterUnlock))
            {
                linkedDistortionWall.SetActive(false);
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=gray>[Wall] Distortion disabled</color>");
                }
            }
            else if (isUnlocked && keepDistortionAfterUnlock)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"<color=green>[Wall] Distortion KEPT ACTIVE (unlocked wall)</color>");
                }
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
        lastInputTime = -999f;
        isShowingCorrectSequence = false;
        playerInAudioRange = false;
        
        StopAllCoroutines();
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
        
        if (showDebugInfo)
        {
            Debug.Log($"[Wall] {gameObject.name} reset");
        }
    }
    
    // ==================== COLLIDER MANAGEMENT ====================
    
    [ContextMenu("Remove All Colliders (Including Children)")]
    private void RemoveAllCollidersRecursive()
    {
        int totalRemoved = 0;
        
        totalRemoved += RemoveCollidersFromObject(gameObject);
        
        if (linkedDistortionWall != null)
        {
            totalRemoved += RemoveCollidersFromObject(linkedDistortionWall);
        }
        
        Debug.Log($"<color=green>✅ Removed {totalRemoved} collider(s) from {gameObject.name} and all children!</color>");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        if (linkedDistortionWall != null)
        {
            UnityEditor.EditorUtility.SetDirty(linkedDistortionWall);
        }
        #endif
    }
    
    private int RemoveCollidersFromObject(GameObject obj)
    {
        int count = 0;
        
        Collider[] colliders = obj.GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            Debug.Log($"  Removing {col.GetType().Name} from {obj.name}");
            #if UNITY_EDITOR
            DestroyImmediate(col);
            #else
            Destroy(col);
            #endif
            count++;
        }
        
        foreach (Transform child in obj.transform)
        {
            count += RemoveCollidersFromObject(child.gameObject);
        }
        
        return count;
    }
    
    private void DisableAllMeshColliders()
    {
        int count = 0;
        
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>(true);
        
        foreach (MeshCollider meshCol in meshColliders)
        {
            meshCol.enabled = false;
            count++;
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=cyan>[Wall] Disabled MeshCollider on {meshCol.gameObject.name}</color>");
            }
        }
        
        if (linkedDistortionWall != null)
        {
            MeshCollider[] distortionColliders = linkedDistortionWall.GetComponentsInChildren<MeshCollider>(true);
            
            foreach (MeshCollider meshCol in distortionColliders)
            {
                meshCol.enabled = false;
                count++;
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=cyan>[Wall] Disabled MeshCollider on distortion wall: {meshCol.gameObject.name}</color>");
                }
            }
        }
        
        Debug.Log($"<color=green>[Wall] ✅ Disabled {count} MeshCollider(s)</color>");
    }
    
    // ==================== GETTERS ====================
    
    public bool IsActive() => isActive;
    public bool IsUnlocked() => isUnlocked;
    public int GetLastDistance() => lastDistance;
    public float GetCurrentNoiseSpeed() => currentNoiseSpeed;
    public List<string> GetExpectedKeys() => new List<string>(expectedKeys);
    public int GetExpectedMidiNote() => expectedMidiNote;
    
    public void SetExpectedKeys(List<string> keys)
    {
        expectedKeys = new List<string>(keys);
    }
    
    public void SetExpectedMidiNote(int note)
    {
        expectedMidiNote = note;
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