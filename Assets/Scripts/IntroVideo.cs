using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroVideo : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public CanvasGroup whiteFade;
    public float fadeDuration = 1f;

    void Start()
    {
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += EndReached;
    }

    void OnPrepared(VideoPlayer vp)
    {
        vp.Play();
    }

    void EndReached(VideoPlayer vp)
    {
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        yield return Fade(1); // Fade vers blanc
        SceneManager.LoadScene("Terrain");
    }

    IEnumerator Fade(float target)
    {
        float start = whiteFade.alpha;
        float time = 0;

        while(time < fadeDuration)
        {
            time += Time.deltaTime;
            whiteFade.alpha = Mathf.Lerp(start, target, time / fadeDuration);
            yield return null;
        }

        whiteFade.alpha = target;
    }
}
