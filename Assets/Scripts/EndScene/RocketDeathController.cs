using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RocketDeathController : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] private RocketMovement2D rocketMovement;
    [SerializeField] private MeteorSpawner meteorSpawner;

    [Header("Death Screen")]
    [SerializeField] private CanvasGroup deathScreenCanvasGroup;

    [Tooltip(
        "Normal meteor ölümünde gösterilecek yazı ve "
        + "Tekrar Oyna butonunun bulunduğu nesne."
    )]
    [SerializeField] private GameObject retryContentRoot;

    [Header("Fade Settings")]
    [Min(0.01f)]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Final Scene Transition")]
    [Tooltip(
        "Final ölümünden sonra yüklenecek sahnenin "
        + "Scene List içerisindeki tam adı."
    )]
    [SerializeField] private string finalSceneName =
        "StoryboardScene";

    [Tooltip(
        "Ekran tamamen karardıktan sonra yeni sahneye "
        + "geçmeden önce beklenecek süre."
    )]
    [Min(0f)]
    [SerializeField] private float finalBlackHoldDuration = 0.4f;

    private bool isDead;
    private bool isRestarting;
    private bool isLoadingFinalScene;

    public bool IsDead => isDead;

    private void Awake()
    {
        // Önceki sahne zamanı durdurmuş olabilir.
        Time.timeScale = 1f;

        isDead = false;
        isRestarting = false;
        isLoadingFinalScene = false;

        HideDeathScreenImmediately();
    }

    /// <summary>
    /// Normal meteor ölümüdür.
    /// Ölüm ekranını ve Tekrar Oyna butonunu gösterir.
    /// </summary>
    public void KillRocket()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        StopGameplay();

        StartCoroutine(
            ShowRetryDeathScreen()
        );
    }

    /// <summary>
    /// Final sekansındaki hikâye ölümüdür.
    /// Tekrar Oyna ekranını göstermeden sonraki sahneye geçer.
    /// </summary>
    public void KillRocketAndLoadFinalScene()
    {
        if (isDead || isLoadingFinalScene)
        {
            return;
        }

        isDead = true;
        isLoadingFinalScene = true;

        StopGameplay();

        StartCoroutine(
            FadeAndLoadFinalScene()
        );
    }

    private void StopGameplay()
    {
        if (rocketMovement != null)
        {
            rocketMovement.SetMovementEnabled(false);
        }

        if (meteorSpawner != null)
        {
            meteorSpawner.StopSpawning();
        }

        // Roket, arka plan ve düşen nesneler donar.
        Time.timeScale = 0f;
    }

    private IEnumerator ShowRetryDeathScreen()
    {
        if (retryContentRoot != null)
        {
            retryContentRoot.SetActive(true);
        }

        yield return FadeToBlack();

        if (deathScreenCanvasGroup != null)
        {
            deathScreenCanvasGroup.interactable = true;
            deathScreenCanvasGroup.blocksRaycasts = true;
        }
    }

    private IEnumerator FadeAndLoadFinalScene()
    {
        // Final ölümünde ölüm yazısı ve tekrar butonu görünmesin.
        if (retryContentRoot != null)
        {
            retryContentRoot.SetActive(false);
        }

        yield return FadeToBlack();

        if (finalBlackHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                finalBlackHoldDuration
            );
        }

        if (string.IsNullOrWhiteSpace(finalSceneName))
        {
            Debug.LogError(
                "RocketDeathController: Final Scene Name boş.",
                this
            );

            yield break;
        }

        // Yeni sahne donmuş başlamasın.
        Time.timeScale = 1f;

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(
                finalSceneName,
                LoadSceneMode.Single
            );

        if (loadOperation == null)
        {
            Debug.LogError(
                $"RocketDeathController: "
                + $"{finalSceneName} sahnesi yüklenemedi. "
                + "Sahne adını ve Build Profiles listesini kontrol et.",
                this
            );

            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator FadeToBlack()
    {
        if (deathScreenCanvasGroup == null)
        {
            Debug.LogError(
                "RocketDeathController: "
                + "Death Screen Canvas Group atanmamış.",
                this
            );

            yield break;
        }

        deathScreenCanvasGroup.gameObject.SetActive(true);

        deathScreenCanvasGroup.interactable = false;
        deathScreenCanvasGroup.blocksRaycasts = true;

        float startingAlpha =
            deathScreenCanvasGroup.alpha;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            // Time.timeScale sıfır olduğu için
            // unscaledDeltaTime kullanıyoruz.
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / fadeDuration
            );

            deathScreenCanvasGroup.alpha = Mathf.Lerp(
                startingAlpha,
                1f,
                progress
            );

            yield return null;
        }

        deathScreenCanvasGroup.alpha = 1f;
    }

    public void RestartLevel()
    {
        if (isRestarting)
        {
            return;
        }

        isRestarting = true;

        Time.timeScale = 1f;

        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.buildIndex,
            LoadSceneMode.Single
        );
    }

    private void HideDeathScreenImmediately()
    {
        if (deathScreenCanvasGroup != null)
        {
            deathScreenCanvasGroup.gameObject.SetActive(true);
            deathScreenCanvasGroup.alpha = 0f;
            deathScreenCanvasGroup.interactable = false;
            deathScreenCanvasGroup.blocksRaycasts = false;
        }

        if (retryContentRoot != null)
        {
            retryContentRoot.SetActive(false);
        }
    }
}