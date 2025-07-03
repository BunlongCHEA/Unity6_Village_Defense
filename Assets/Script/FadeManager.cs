using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    [Header("Fade Components")]
    public CanvasGroup fadeCanvasGroup;
    public Image fadeImage;

    [Header("Fade Settings")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1f;
    public Color fadeColor = Color.black;

    [Header("Fade States")]
    public bool startWithFadeIn = true;
    public bool isFading = false;

    public static FadeManager Instance { get; private set; }

    // Events for fade completion
    public System.Action OnFadeInComplete;
    public System.Action OnFadeOutComplete;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFade();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (startWithFadeIn)
        {
            StartCoroutine(FadeInFromBlack());
        }
    }

    private void InitializeFade()
    {
        // Setup fade image
        if (fadeImage != null)
        {
            fadeImage.color = fadeColor;
            fadeImage.raycastTarget = false;
        }

        // Setup canvas group
        if (fadeCanvasGroup == null && fadeImage != null)
        {
            fadeCanvasGroup = fadeImage.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Set initial state
        if (startWithFadeIn)
        {
            SetFadeAlpha(1f); // Start with black screen
        }
        else
        {
            SetFadeAlpha(0f); // Start with clear screen
        }
    }

    // Fade Methods
    // Fades to black (fade out)
    public void FadeToBlack(System.Action onComplete = null)
    {
        if (isFading) return;
        StartCoroutine(FadeCoroutine(0f, 1f, fadeOutDuration, onComplete));
    }

    // Fades from black to clear (fade in)
    public void FadeFromBlack(System.Action onComplete = null)
    {
        if (isFading) return;
        StartCoroutine(FadeCoroutine(1f, 0f, fadeInDuration, onComplete));
    }

    // Custom fade with specific parameters
    public void CustomFade(float fromAlpha, float toAlpha, float duration, System.Action onComplete = null)
    {
        if (isFading) return;
        StartCoroutine(FadeCoroutine(fromAlpha, toAlpha, duration, onComplete));
    }

    // Instant fade to specific alpha
    public void SetFadeAlpha(float alpha)
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    // Complete fade transition: Fade to black -> Action -> Fade from black
    // Perfect for scene transitions
    public void FadeTransition(System.Action actionDuringBlack, float pauseDuration = 0.5f)
    {
        if (isFading) return;
        StartCoroutine(FadeTransitionCoroutine(actionDuringBlack, pauseDuration));
    }

    // Coroutines
    private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration, System.Action onComplete)
    {
        isFading = true;

        if (fadeCanvasGroup == null)
        {
            isFading = false;
            onComplete?.Invoke();
            yield break;
        }

        float elapsedTime = 0f;
        fadeCanvasGroup.alpha = fromAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Use unscaled time for UI
            float progress = elapsedTime / duration;

            // Smooth fade curve
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            fadeCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, smoothProgress);

            yield return null;
        }

        // Ensure final alpha is set
        fadeCanvasGroup.alpha = toAlpha;
        isFading = false;

        // Trigger events
        if (toAlpha >= 1f)
        {
            OnFadeOutComplete?.Invoke();
        }
        else if (toAlpha <= 0f)
        {
            OnFadeInComplete?.Invoke();
        }

        onComplete?.Invoke();
    }

    private IEnumerator FadeTransitionCoroutine(System.Action actionDuringBlack, float pauseDuration)
    {
        // Fade to black
        yield return StartCoroutine(FadeCoroutine(fadeCanvasGroup.alpha, 1f, fadeOutDuration, null));

        // Execute action during black screen
        actionDuringBlack?.Invoke();

        // Wait for specified duration
        if (pauseDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(pauseDuration);
        }

        // Fade from black
        yield return StartCoroutine(FadeCoroutine(1f, 0f, fadeInDuration, null));
    }

    private IEnumerator FadeInFromBlack()
    {
        yield return new WaitForSecondsRealtime(0.1f); // Small delay for initialization
        yield return StartCoroutine(FadeCoroutine(1f, 0f, fadeInDuration, null));
    }

    // Utility Methods
    public bool IsFading()
    {
        return isFading;
    }

    public float GetCurrentAlpha()
    {
        return fadeCanvasGroup != null ? fadeCanvasGroup.alpha : 0f;
    }
}