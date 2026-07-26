using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tum gorevler tamamlaninca bir UI ekrani gosterir ve roketin yanindaki
/// bekleme alanini aktif eder. Oyuncu o alana girince 2. sahneye gecer.
/// </summary>
[DisallowMultipleComponent]
public class LevelCompletionController : MonoBehaviour
{
    private const string DefaultAllTasksMessage =
        "All tasks complete!\nProceed to the marked area near the rocket.";

    [Header("References")]
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private RocketBoardingZone boardingZone;

    [Header("UI")]
    [Tooltip("Gorevler bitince acilacak panel (baslangicta kapali olmali).")]
    [SerializeField] private GameObject completionPanel;

    [SerializeField] private TMP_Text completionText;

    [Header("Messages")]
    [TextArea]
    [Tooltip("Gorevler bitince ekranda gosterilecek yazi.")]
    [SerializeField] private string allTasksMessage =
        DefaultAllTasksMessage;

    [Header("Scene Transition")]
    [Tooltip("Gecilecek 2. sahnenin adi. (Build Settings'e ekli olmali.)")]
    [SerializeField] private string nextSceneName;

    [Tooltip("Sahne adi bos ise bu Build index kullanilir. -1 ise bir sonraki sahne.")]
    [SerializeField] private int nextSceneBuildIndex = -1;

    [Tooltip("Alana girdikten sonra gecise kadar beklenecek sure (saniye).")]
    [Min(0f)]
    [SerializeField] private float transitionDelay = 0.5f;

    private bool tasksCompleted;
    private bool transitionStarted;

    private void OnEnable()
    {
        if (taskManager != null)
        {
            taskManager.AllTasksCompleted +=
                HandleAllTasksCompleted;
        }

        if (boardingZone != null)
        {
            boardingZone.PlayerEntered +=
                HandlePlayerEnteredZone;
        }
    }

    private void OnDisable()
    {
        if (taskManager != null)
        {
            taskManager.AllTasksCompleted -=
                HandleAllTasksCompleted;
        }

        if (boardingZone != null)
        {
            boardingZone.PlayerEntered -=
                HandlePlayerEnteredZone;
        }
    }

    private void Awake()
    {
        if (IsLegacyTurkishMessage(allTasksMessage))
        {
            allTasksMessage = DefaultAllTasksMessage;
        }

        // Baslangicta panel kapali, alan pasif.
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }

        if (boardingZone != null)
        {
            boardingZone.Deactivate();
        }
    }

    private bool IsLegacyTurkishMessage(string message)
    {
        return message ==
            "Tum gorevler tamamlandi!\n" +
            "Rokete gitmek icin isaretli alana yaklas." ||
            message ==
            "Tüm görevler tamamlandı!\n" +
            "Rokete gitmek için işaretli alana yaklaş.";
    }

    private void HandleAllTasksCompleted()
    {
        if (tasksCompleted)
        {
            return;
        }

        tasksCompleted = true;

        if (completionText != null)
        {
            completionText.text = allTasksMessage;
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
        }

        if (boardingZone != null)
        {
            boardingZone.Activate();
        }

        Debug.Log(
            "LevelCompletionController: Tum gorevler tamamlandi.",
            this
        );
    }

    private void HandlePlayerEnteredZone()
    {
        if (!tasksCompleted || transitionStarted)
        {
            return;
        }

        transitionStarted = true;

        if (transitionDelay > 0f)
        {
            Invoke(nameof(LoadNextScene), transitionDelay);
        }
        else
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        if (nextSceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(nextSceneBuildIndex);
            return;
        }

        // Ikisi de belirtilmemisse siradaki sahneyi yukle.
        int nextIndex =
            SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning(
                "LevelCompletionController: Gecilecek sahne " +
                "belirtilmemis ve siradaki sahne yok.",
                this
            );
        }
    }
}
