using UnityEngine;
using System.Collections;

public class EndScreenTrigger : MonoBehaviour
{
    public CanvasGroup endScreen;    // CanvasGroup de ton écran de fin
    public AudioSource music;        // AudioSource de la musique
    public float fadeDuration = 1f;  // Durée du fade

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag("Player")) // remplace "Player" par ton tag si besoin
        {
            triggered = true;
            StartCoroutine(ShowEndScreen());
        }
    }

    IEnumerator ShowEndScreen()
    {
        // Lance la musique
        if (music != null)
            music.Play();

        // Fade alpha du CanvasGroup
        float time = 0;
        float startAlpha = endScreen.alpha;
        float targetAlpha = 1f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            endScreen.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        endScreen.alpha = targetAlpha;
    }
}
