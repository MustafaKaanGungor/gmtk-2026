using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Bir VideoPlayer'in videosu bitince belirtilen sahneye gecer.
/// VideoPlayer'in loopPointReached olayini dinler (video sonuna ulasinca).
/// </summary>
[DisallowMultipleComponent]
public class VideoEndSceneLoader : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Bitisi dinlenecek VideoPlayer. Bos ise ayni objeden alinir.")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Scene")]
    [Tooltip("Video bitince gecilecek sahne adi. (Build Settings'e ekli olmali.)")]
    [SerializeField] private string nextSceneName;

    [Tooltip("Sahne adi bos ise bu Build index kullanilir. -1 ise siradaki sahne.")]
    [SerializeField] private int nextSceneBuildIndex = -1;

    [Tooltip("Video bittikten sonra gecise kadar beklenecek sure (saniye).")]
    [Min(0f)]
    [SerializeField] private float delayAfterVideo = 0f;

    private bool transitionStarted;

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }
    }

    private void OnEnable()
    {
        if (videoPlayer == null)
        {
            Debug.LogError(
                "VideoEndSceneLoader: VideoPlayer atanmamis!",
                this
            );

            return;
        }

        // Video donguye alinmissa loopPointReached her dongude tetiklenir;
        // bir kez gecmek icin donguyu kapatiyoruz.
        videoPlayer.isLooping = false;

        videoPlayer.loopPointReached += HandleVideoFinished;
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleVideoFinished;
        }
    }

    private void HandleVideoFinished(VideoPlayer source)
    {
        if (transitionStarted)
        {
            return;
        }

        transitionStarted = true;

        if (delayAfterVideo > 0f)
        {
            Invoke(nameof(LoadNextScene), delayAfterVideo);
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

        int nextIndex =
            SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning(
                "VideoEndSceneLoader: Gecilecek sahne belirtilmemis " +
                "ve siradaki sahne yok.",
                this
            );
        }
    }
}
