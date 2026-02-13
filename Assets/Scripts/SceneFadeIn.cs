using UnityEngine;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    public CanvasGroup whiteFade;
    public float fadeDuration = 1f;

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float start = whiteFade.alpha;
        float time = 0;

        while(time < fadeDuration)
        {
            time += Time.deltaTime;
            whiteFade.alpha = Mathf.Lerp(start, 0, time / fadeDuration);
            yield return null;
        }

        whiteFade.alpha = 0;
    }
}
