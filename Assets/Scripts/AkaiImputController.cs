using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

public class AkaiImputController : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip noteClip;
    public int referenceNote = 60;

    [Header("Polyphony")]
    public int maxVoices = 8; // Nombre de notes simultanées

    [Header("Pitch Settings")]
    [Range(-24, 24)]
    public int pitchOffset = 0;

    private List<AudioSource> audioSourcePool;
    private int currentSourceIndex = 0;

    private HashSet<int> activeNotes = new HashSet<int>();
    private Dictionary<int, float> noteVelocities = new Dictionary<int, float>();

    void Awake()
    {
        // Créer un pool d'AudioSources pour la polyphonie
        audioSourcePool = new List<AudioSource>();
        for (int i = 0; i < maxVoices; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            audioSourcePool.Add(source);
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
        if (activeNotes.Count > 0)
        {
            Debug.Log($"Notes actives : {activeNotes.Count} - {string.Join(", ", activeNotes)}");
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

    if (noteClip != null)
    {
        AudioSource source = GetNextAudioSource();
        float pitch = CalculatePitch(note);

        Debug.Log($"Note ON: {note} ({keyType}) - Vélocité: {velocity:F2} - Pitch: {pitch:F2}");

        source.pitch = pitch;
        source.volume = velocity;
        source.PlayOneShot(noteClip);
    }
    
    // NEW: Send note to walls
    MidiWallBridge bridge = FindObjectOfType<MidiWallBridge>();
    if (bridge != null)
    {
        bridge.OnMidiNotePressed(note, velocity);
    }
}

    void OnNoteReleased(int note)
    {
        Debug.Log($"Note OFF: {note}");
    }

    AudioSource GetNextAudioSource()
    {
        // Rotation circulaire dans le pool
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
}