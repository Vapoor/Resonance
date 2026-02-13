using UnityEngine;
using System.Collections;

public class BackgroundMusicManager : MonoBehaviour
{
    [Header("Singleton")]
    private static BackgroundMusicManager instance;
    public static BackgroundMusicManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BackgroundMusicManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("BackgroundMusicManager");
                    instance = obj.AddComponent<BackgroundMusicManager>();
                }
            }
            return instance;
        }
    }
    
    [Header("Initial Music")]
    [SerializeField] private AudioClip initialMusic;
    [SerializeField] private bool playOnStart = true;
    
    [Header("Audio Settings")]
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private bool loop = true;  // ← Make sure this is TRUE
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float crossfadeDuration = 2f;
    
    [Header("Current State")]
    [SerializeField] private bool isMusicEnabled = true;
    [SerializeField] private AudioClip currentClip;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private AudioSource primaryAudioSource;
    private AudioSource secondaryAudioSource;
    private Coroutine fadeCoroutine;
    private bool isCrossfading = false;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        SetupAudioSources();
    }
    
    private void SetupAudioSources()
    {
        primaryAudioSource = gameObject.AddComponent<AudioSource>();
        primaryAudioSource.playOnAwake = false;
        primaryAudioSource.loop = loop;  // Set loop
        primaryAudioSource.volume = 0f;
        primaryAudioSource.spatialBlend = 0f;
        
        secondaryAudioSource = gameObject.AddComponent<AudioSource>();
        secondaryAudioSource.playOnAwake = false;
        secondaryAudioSource.loop = loop;  // Set loop
        secondaryAudioSource.volume = 0f;
        secondaryAudioSource.spatialBlend = 0f;
        
        if (showDebugInfo)
        {
            Debug.Log($"[MusicManager] Audio sources created | Loop: {loop}");
        }
    }
    
    private void Start()
    {
        if (playOnStart && initialMusic != null)
        {
            PlayMusic(initialMusic, fadeInDuration);
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    public void PlayMusic(AudioClip clip, float fadeDuration = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[MusicManager] Cannot play null AudioClip!");
            return;
        }
        
        if (currentClip == clip && primaryAudioSource.isPlaying)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[MusicManager] Music '{clip.name}' is already playing");
            }
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[MusicManager] 🎵 Playing music: {clip.name} | Loop: {loop}</color>");
        }
        
        currentClip = clip;
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(FadeInMusic(clip, fadeDuration));
    }
    
    public void CrossfadeToMusic(AudioClip newClip, float duration = 2f)
    {
        if (newClip == null)
        {
            Debug.LogWarning("[MusicManager] Cannot crossfade to null AudioClip!");
            return;
        }
        
        if (currentClip == newClip && primaryAudioSource.isPlaying)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[MusicManager] Music '{newClip.name}' is already playing");
            }
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[MusicManager] 🎵 Crossfading to: {newClip.name} | Loop: {loop}</color>");
        }
        
        currentClip = newClip;
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(CrossfadeMusic(newClip, duration));
    }
    
    public void StopMusic(float fadeDuration = 1f)
    {
        if (showDebugInfo)
        {
            Debug.Log($"<color=yellow>[MusicManager] ⏸️ Stopping music</color>");
        }
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(FadeOutMusic(fadeDuration));
    }
    
    public void PauseMusic()
    {
        if (showDebugInfo)
        {
            Debug.Log($"<color=yellow>[MusicManager] ⏸️ Pausing music</color>");
        }
        
        primaryAudioSource.Pause();
        secondaryAudioSource.Pause();
    }
    
    public void ResumeMusic()
    {
        if (showDebugInfo)
        {
            Debug.Log($"<color=green>[MusicManager] ▶️ Resuming music</color>");
        }
        
        primaryAudioSource.UnPause();
        secondaryAudioSource.UnPause();
    }
    
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        
        if (!isCrossfading)
        {
            primaryAudioSource.volume = volume;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[MusicManager] Volume set to {volume:F2}");
        }
    }
    
    public void SetLooping(bool shouldLoop)
    {
        loop = shouldLoop;
        primaryAudioSource.loop = loop;
        secondaryAudioSource.loop = loop;
        
        if (showDebugInfo)
        {
            Debug.Log($"[MusicManager] Looping set to {loop}");
        }
    }
    
    public void SetMusicEnabled(bool enabled)
    {
        isMusicEnabled = enabled;
        
        if (enabled)
        {
            if (currentClip != null && !primaryAudioSource.isPlaying)
            {
                PlayMusic(currentClip, fadeInDuration);
            }
        }
        else
        {
            StopMusic(fadeOutDuration);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=yellow>[MusicManager] Music {(enabled ? "enabled" : "disabled")}</color>");
        }
    }
    
    // ==================== COROUTINES ====================
    
    private IEnumerator FadeInMusic(AudioClip clip, float duration)
    {
        // Stop and reset secondary source
        secondaryAudioSource.Stop();
        secondaryAudioSource.volume = 0f;
        
        // Setup primary source
        primaryAudioSource.clip = clip;
        primaryAudioSource.loop = loop;  // ← ENSURE LOOP IS SET!
        primaryAudioSource.volume = 0f;
        primaryAudioSource.Play();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=green>[MusicManager] Started playing: {clip.name} | Loop: {primaryAudioSource.loop} | Length: {clip.length}s</color>");
        }
        
        // Fade in
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            primaryAudioSource.volume = Mathf.Lerp(0f, volume, elapsed / duration);
            yield return null;
        }
        
        primaryAudioSource.volume = volume;
        fadeCoroutine = null;
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=green>[MusicManager] Fade in complete | isPlaying: {primaryAudioSource.isPlaying} | isLooping: {primaryAudioSource.loop}</color>");
        }
    }
    
    private IEnumerator FadeOutMusic(float duration)
    {
        float startVolume = primaryAudioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            primaryAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            secondaryAudioSource.volume = Mathf.Lerp(secondaryAudioSource.volume, 0f, elapsed / duration);
            yield return null;
        }
        
        primaryAudioSource.volume = 0f;
        primaryAudioSource.Stop();
        
        secondaryAudioSource.volume = 0f;
        secondaryAudioSource.Stop();
        
        fadeCoroutine = null;
    }
    
    private IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
    {
        isCrossfading = true;
        
        // Swap audio sources
        AudioSource fadeOutSource = primaryAudioSource;
        AudioSource fadeInSource = secondaryAudioSource;
        
        // Setup fade in source
        fadeInSource.clip = newClip;
        fadeInSource.loop = loop;  // ← ENSURE LOOP IS SET!
        fadeInSource.volume = 0f;
        fadeInSource.Play();
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[MusicManager] Crossfade started | New clip: {newClip.name} | Loop: {fadeInSource.loop}</color>");
        }
        
        float startVolumeOut = fadeOutSource.volume;
        float elapsed = 0f;
        
        // Crossfade
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            fadeOutSource.volume = Mathf.Lerp(startVolumeOut, 0f, t);
            fadeInSource.volume = Mathf.Lerp(0f, volume, t);
            
            yield return null;
        }
        
        // Finalize
        fadeOutSource.volume = 0f;
        fadeOutSource.Stop();
        
        fadeInSource.volume = volume;
        
        // Swap references
        primaryAudioSource = fadeInSource;
        secondaryAudioSource = fadeOutSource;
        
        isCrossfading = false;
        fadeCoroutine = null;
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[MusicManager] Crossfade complete | isPlaying: {primaryAudioSource.isPlaying} | isLooping: {primaryAudioSource.loop}</color>");
        }
    }
    
    // ==================== GETTERS ====================
    
    public bool IsMusicPlaying()
    {
        return primaryAudioSource.isPlaying || secondaryAudioSource.isPlaying;
    }
    
    public bool IsMusicEnabled()
    {
        return isMusicEnabled;
    }
    
    public AudioClip GetCurrentClip()
    {
        return currentClip;
    }
    
    public float GetVolume()
    {
        return volume;
    }
    
    public bool IsLooping()
    {
        return loop;
    }
    
    // ==================== DEBUG ====================
    
    [ContextMenu("Debug - Print Music Status")]
    private void DebugPrintStatus()
    {
        Debug.Log("=== MUSIC MANAGER STATUS ===");
        Debug.Log($"Current Clip: {(currentClip != null ? currentClip.name : "None")}");
        Debug.Log($"Primary Source:");
        Debug.Log($"  - isPlaying: {primaryAudioSource.isPlaying}");
        Debug.Log($"  - loop: {primaryAudioSource.loop}");
        Debug.Log($"  - volume: {primaryAudioSource.volume}");
        Debug.Log($"  - clip: {(primaryAudioSource.clip != null ? primaryAudioSource.clip.name : "None")}");
        Debug.Log($"  - time: {primaryAudioSource.time:F2}s / {(primaryAudioSource.clip != null ? primaryAudioSource.clip.length : 0):F2}s");
        Debug.Log($"Secondary Source:");
        Debug.Log($"  - isPlaying: {secondaryAudioSource.isPlaying}");
        Debug.Log($"  - loop: {secondaryAudioSource.loop}");
        Debug.Log($"  - volume: {secondaryAudioSource.volume}");
        Debug.Log("===========================");
    }
}