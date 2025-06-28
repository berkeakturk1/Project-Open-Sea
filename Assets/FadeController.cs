using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;

    private void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.enabled = false;
        }
    }

    public void FadeOutAndIn(System.Action onFadeMiddle)
    {
        StartCoroutine(FadeRoutine(onFadeMiddle));
    }

    private IEnumerator FadeRoutine(System.Action onFadeMiddle)
    {
        // Enable and start fully transparent
        if (fadeImage != null)
        {
            fadeImage.enabled = true;
            SetAlpha(0f);
        }

        // Fade out to black
        yield return StartCoroutine(Fade(0f, 1f));

        onFadeMiddle?.Invoke();

        yield return new WaitForSeconds(0.5f);

        // Fade in back to transparent
        yield return StartCoroutine(Fade(1f, 0f));

        // After fully transparent, disable image
        if (fadeImage != null)
        {
            SetAlpha(0f);
            fadeImage.enabled = false;
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            SetAlpha(alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
        }
    }
}