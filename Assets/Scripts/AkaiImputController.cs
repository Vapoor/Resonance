using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class AkaiImputController : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip noteClip;
    public int referenceNote = 60;

    [Header("Polyphony")]
    public int maxVoices = 8;

    [Header("Pitch Settings")]
    [Range(-24, 24)]
    public int pitchOffset = 0;
    
    [Header("Wall Integration")]
    [SerializeField] private bool sendToWalls = true;
    [SerializeField] private WallManager wallManager;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private List<AudioSource> audioSourcePool;
    private int currentSourceIndex = 0;

    private HashSet<int> activeNotes = new HashSet<int>();
    private Dictionary<int, float> noteVelocities = new Dictionary<int, float>();

    void Awake()
    {
        audioSourcePool = new List<AudioSource>();
        for (int i = 0; i < maxVoices; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            audioSourcePool.Add(source);
        }
    }
    
    void Start()
    {
        if (wallManager == null)
        {
            wallManager = FindObjectOfType<WallManager>();
        }
        
        if (wallManager == null && sendToWalls)
        {
            Debug.LogWarning("[AkaiController] WallManager not found! MIDI input won't affect walls.");
        }
    }

    void OnEnable()
    {
        InputSystem.onEvent += OnInputSystemEvent;
    }

    void OnDisable()
    {
        InputSystem.onEvent -= OnInputSystemEvent;
    }

    void Update()
    {
        if (activeNotes.Count > 0 && showDebugInfo)
        {
            Debug.Log($"[MIDI] Active notes: {activeNotes.Count} - {string.Join(", ", activeNotes)}");
        }
    }

    void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (device == null || !device.description.deviceClass.Contains("MIDI"))
            return;

        foreach (var control in device.allControls)
        {
            if (control.name.StartsWith("note"))
            {
                var value = control.ReadValueAsObject();

                if (value is float floatValue)
                {
                    if (int.TryParse(control.name.Replace("note", ""), out int note))
                    {
                        if (floatValue > 0f && !activeNotes.Contains(note))
                        {
                            activeNotes.Add(note);
                            noteVelocities[note] = floatValue;
                            OnNotePressed(note, floatValue);
                        }
                        else if (floatValue == 0f && activeNotes.Contains(note))
                        {
                            activeNotes.Remove(note);
                            noteVelocities.Remove(note);
                            OnNoteReleased(note);
                        }
                    }
                }
            }
        }
    }

    void OnNotePressed(int note, float velocity)
    {
        bool isBlack = IsBlackKey(note);
        string keyType = isBlack ? "Noire" : "Blanche";

        if (showDebugInfo)
        {
            string noteName = GetNoteName(note);
            Debug.Log($"<color=cyan>[MIDI] Note ON: {note} ({noteName}) - Type: {keyType} - Velocity: {velocity:F2}</color>");
        }

        // Play audio feedback
        if (noteClip != null)
        {
            AudioSource source = GetNextAudioSource();
            float pitch = CalculatePitch(note);

            source.pitch = pitch;
            source.volume = velocity;
            source.PlayOneShot(noteClip);
        }
        
        // Send to walls
        if (sendToWalls && wallManager != null)
        {
            Wall currentWall = wallManager.GetCurrentWall();
            if (currentWall != null)
            {
                currentWall.OnMidiNotePressed(note, velocity);
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=yellow>[MIDI] Sent note {note} to wall: {currentWall.gameObject.name}</color>");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("[MIDI] No active wall to receive MIDI input!");
            }
        }
    }

    void OnNoteReleased(int note)
    {
        if (showDebugInfo)
        {
            string noteName = GetNoteName(note);
            Debug.Log($"<color=gray>[MIDI] Note OFF: {note} ({noteName})</color>");
        }
    }

    AudioSource GetNextAudioSource()
    {
        AudioSource source = audioSourcePool[currentSourceIndex];
        currentSourceIndex = (currentSourceIndex + 1) % maxVoices;
        return source;
    }

    float CalculatePitch(int midiNote)
    {
        int semitones = (midiNote - referenceNote) + pitchOffset;
        float pitch = Mathf.Pow(2f, semitones / 12f);
        return pitch;
    }

    bool IsBlackKey(int note)
    {
        int n = note % 12;
        return n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
    }
    
    string GetNoteName(int midiNote)
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int octave = (midiNote / 12) - 1;
        int noteIndex = midiNote % 12;
        return $"{noteNames[noteIndex]}{octave}";
    }
}