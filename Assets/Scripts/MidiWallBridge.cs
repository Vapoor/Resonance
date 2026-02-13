using UnityEngine;

public class MidiWallBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AkaiImputController midiController;
    [SerializeField] private WallManager wallManager;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void Start()
    {
        if (midiController == null)
        {
            midiController = FindObjectOfType<AkaiImputController>();
        }
        
        if (wallManager == null)
        {
            wallManager = FindObjectOfType<WallManager>();
        }
    }
    
    // Call this method from AkaiImputController when a note is pressed
    public void OnMidiNotePressed(int midiNote, float velocity)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[MidiBridge] Note pressed: {midiNote} | Velocity: {velocity:F2}");
        }
        
        if (wallManager != null)
        {
            Wall currentWall = wallManager.GetCurrentWall();
            if (currentWall != null)
            {
                currentWall.OnMidiNotePressed(midiNote, velocity);
            }
        }
    }
}