using System.Collections;
using UnityEngine;

public sealed class SkyTransition : MonoBehaviour
{
    [Header("Backgrounds")]
    [SerializeField] private SpriteRenderer normalSky;
    [SerializeField] private SpriteRenderer redSky;

    [Header("Automatic transition")]
    [SerializeField] private bool playAutomatically;
    [Min(0f)]
    [SerializeField] private float startDelay = 45f;

    [Header("Fade")]
    [Min(0.01f)]
    [SerializeField] private float fadeDuration = 8f;

    [Tooltip("Aktifse oyun duraklatıldığında bile geçiş devam eder.")]
    [SerializeField] private bool useUnscaledTime;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (normalSky == null || redSky == null)
        {
            Debug.LogError(
                $"{name}: Normal Sky veya Red Sky atanmamış.",
                this
            );

            enabled = false;
            return;
        }

        normalSky.gameObject.SetActive(true);
        redSky.gameObject.SetActive(true);

        SetAlpha(normalSky, 1f);
        SetAlpha(redSky, 0f);
    }

    private void Start()
    {
        if (playAutomatically)
        {
            StartCoroutine(StartAfterDelay());
        }
    }

    private IEnumerator StartAfterDelay()
    {
        float elapsed = 0f;

        while (elapsed < startDelay)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }

        StartTransition();
    }

    public void StartTransition()
    {
        if (!isActiveAndEnabled || fadeCoroutine != null)
        {
            return;
        }

        fadeCoroutine = StartCoroutine(FadeToRed());
    }

    public void ResetTransition()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        normalSky.gameObject.SetActive(true);
        redSky.gameObject.SetActive(true);

        SetAlpha(normalSky, 1f);
        SetAlpha(redSky, 0f);
    }

    private IEnumerator FadeToRed()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += GetDeltaTime();

            float linearT = Mathf.Clamp01(elapsed / fadeDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, linearT);

            // Normal arka plan görünür kalır.
            // Kırmızı arka plan onun üzerinde belirmeye başlar.
            SetAlpha(redSky, smoothT);

            yield return null;
        }

        SetAlpha(redSky, 1f);
        fadeCoroutine = null;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
    }

    private static void SetAlpha(
        SpriteRenderer spriteRenderer,
        float alpha
    )
    {
        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }
}