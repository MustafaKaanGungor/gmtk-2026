using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EndGameMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public void GoToMainMenu()
    {
        // Oyun duraklatılmış veya TimeScale değiştirilmişse normale döndürür.
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // Unity Editor içerisinde Play modunu durdurur.
        EditorApplication.isPlaying = false;
#else
        // Build alınmış oyunu kapatır.
        Application.Quit();
#endif
    }
}