using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Geri sayim sayaci. Belirlenen (genelde 2) sahnede calisir ve kalan sureyi
/// sahneler arasinda paylasir (static). Sayac bitene kadar TaskManager'daki tum
/// gorevler tamamlanmazsa GameOver sahnesine gecer. Gorevler zamaninda
/// tamamlanirsa sayac durur (basari).
///
/// UI yapisi MeteorCountdownUI'dan alindi (TMP text + sifir dolgulu format).
/// </summary>
[DisallowMultipleComponent]
public class TaskCountdownController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text countText;

    [Tooltip("2 secilirse sayilar 09, 08, 07 seklinde gorunur.")]
    [Range(1, 4)]
    [SerializeField] private int minimumDigits = 2;

    [Header("Countdown")]
    [Tooltip("Toplam sure (saniye). Belirlenen sahneler boyunca paylasilir.")]
    [Min(1f)]
    [SerializeField] private float totalSeconds = 120f;

    [Tooltip("Bu, geri sayimin BASLADIGI ilk sahne mi. Isaretliyse sahneye " +
        "her girildiginde sayac bastan baslar. Ikinci sahnede KAPALI birak " +
        "(kalan sureyle devam etsin).")]
    [SerializeField] private bool isFirstCountdownScene = false;

    [Header("References")]
    [Tooltip("Bu sahnenin gorevlerini tutan TaskManager.")]
    [SerializeField] private TaskManager taskManager;

    [Header("Game Over")]
    [Tooltip("Sure bitince gecilecek sahne adi. (Build Settings'e ekli olmali.)")]
    [SerializeField] private string gameOverSceneName = "GameOver";

    // Sahneler arasinda paylasilan kalan sure. -1 = henuz baslatilmadi.
    private static float sharedRemaining = -1f;

    private float remaining;
    private bool finished;

    // Editorde (domain reload kapaliyken) yeni oyunda temiz baslamak icin.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSharedState()
    {
        sharedRemaining = -1f;
    }

    private void Awake()
    {
        if (isFirstCountdownScene || sharedRemaining < 0f)
        {
            // Ilk sahne: sayaci bastan baslat. (Ya da hic baslatilmamissa.)
            sharedRemaining = totalSeconds;
        }

        remaining = sharedRemaining;

        UpdateUI();
    }

    private void Update()
    {
        if (finished)
        {
            return;
        }

        // Gorevler zamaninda bittiyse sayaci durdur (basari, gameover yok).
        if (taskManager != null && taskManager.AreAllTasksCompleted)
        {
            finished = true;
            return;
        }

        remaining -= Time.deltaTime;
        sharedRemaining = remaining;

        if (remaining <= 0f)
        {
            remaining = 0f;
            sharedRemaining = 0f;

            UpdateUI();

            finished = true;

            GameOver();
            return;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (countText == null)
        {
            return;
        }

        int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(remaining));

        countText.text = secondsLeft.ToString("D" + minimumDigits);
    }

    private void GameOver()
    {
        // Yeniden oynanista temiz baslasin.
        sharedRemaining = -1f;

        if (string.IsNullOrEmpty(gameOverSceneName))
        {
            Debug.LogError(
                "TaskCountdownController: GameOver sahne adi atanmamis!",
                this
            );

            return;
        }

        SceneManager.LoadScene(gameOverSceneName);
    }

    /// <summary>Kalan sureyi disaridan sifirlamak icin (ornegin menuden yeni oyun).</summary>
    public static void ResetCountdown()
    {
        sharedRemaining = -1f;
    }
}
