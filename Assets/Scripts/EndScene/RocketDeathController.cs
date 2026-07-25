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

    [Header("Fade Settings")]
    [Min(0.01f)]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isDead;
    private bool isRestarting;

    public bool IsDead => isDead;

    private void Awake()
    {
        // Sahne yeniden yüklendiğinde oyun duraklatılmış başlamasın.
        Time.timeScale = 1f;

        isDead = false;
        isRestarting = false;

        HideDeathScreenImmediately();
    }

    public void KillRocket()
    {
        // Aynı anda birden fazla meteor çarparsa
        // ölüm sistemi yalnızca bir kez çalışsın.
        if (isDead)
        {
            return;
        }

        isDead = true;

        // Oyuncunun roket kontrolünü kapat.
        if (rocketMovement != null)
        {
            rocketMovement.SetMovementEnabled(false);
        }

        // Yeni meteor üretilmesini durdur.
        if (meteorSpawner != null)
        {
            meteorSpawner.StopSpawning();
        }

        // Roketi, mevcut meteorları ve arka planı dondur.
        Time.timeScale = 0f;

        StartCoroutine(ShowDeathScreen());
    }

    private IEnumerator ShowDeathScreen()
    {
        if (deathScreenCanvasGroup == null)
        {
            Debug.LogError(
                "RocketDeathController: Death Screen Canvas Group atanmamış.",
                this
            );

            yield break;
        }

        deathScreenCanvasGroup.gameObject.SetActive(true);

        // Fade sırasında oyuncu arka taraftaki UI ile etkileşemesin.
        deathScreenCanvasGroup.blocksRaycasts = true;
        deathScreenCanvasGroup.interactable = false;

        float elapsedTime = 0f;
        float startingAlpha = deathScreenCanvasGroup.alpha;

        while (elapsedTime < fadeDuration)
        {
            // Time.timeScale sıfır olduğu için
            // Time.deltaTime yerine unscaledDeltaTime kullanıyoruz.
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
        deathScreenCanvasGroup.interactable = true;
        deathScreenCanvasGroup.blocksRaycasts = true;
    }

    public void RestartLevel()
    {
        if (isRestarting)
        {
            return;
        }

        isRestarting = true;

        // Yeni yüklenen sahne donmuş başlamasın.
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.buildIndex,
            LoadSceneMode.Single
        );
    }

    private void HideDeathScreenImmediately()
    {
        if (deathScreenCanvasGroup == null)
        {
            return;
        }

        // Nesne aktif kalacak ama görünmez ve tıklanamaz olacak.
        deathScreenCanvasGroup.gameObject.SetActive(true);
        deathScreenCanvasGroup.alpha = 0f;
        deathScreenCanvasGroup.interactable = false;
        deathScreenCanvasGroup.blocksRaycasts = false;
    }
}