using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(VideoPlayer))]
public class MainMenuVideoTransition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip introVideo;
    [SerializeField] private Camera videoCamera;
    [SerializeField] private Button playButton;
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "TaskTest";

    [Header("Playback")]
    [SerializeField] private bool prepareOnStart = true;
    [SerializeField] private bool configureCameraOutput = true;

    [Header("Skipping")]
    [SerializeField] private bool allowSkip = true;

    [Min(0f)]
    [SerializeField] private float minimumSkipDelay = 0.75f;

    [SerializeField] private bool hideCursorDuringVideo = true;

    private bool preparing;
    private bool playRequested;
    private bool playbackStarted;
    private bool transitionStarted;

    private float playbackStartedAt;
    private float previousTimeScale = 1f;

    private float previousMenuAlpha = 1f;
    private bool previousMenuInteractable = true;
    private bool previousMenuBlocksRaycasts = true;

    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockMode;

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoCamera == null)
        {
            videoCamera = Camera.main;
        }

        if (menuCanvasGroup != null)
        {
            previousMenuAlpha = menuCanvasGroup.alpha;
            previousMenuInteractable = menuCanvasGroup.interactable;
            previousMenuBlocksRaycasts = menuCanvasGroup.blocksRaycasts;
        }

        ConfigureVideoPlayer();
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += HandlePrepareCompleted;
            videoPlayer.loopPointReached += HandlePlaybackCompleted;
            videoPlayer.errorReceived += HandleVideoError;
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(PlayIntro);
        }
    }

    private void Start()
    {
        if (prepareOnStart)
        {
            PrepareVideo();
        }
    }

    private void Update()
    {
        if (!allowSkip ||
            !playbackStarted ||
            transitionStarted ||
            Time.unscaledTime - playbackStartedAt < minimumSkipDelay)
        {
            return;
        }

        if (SkipPressed())
        {
            LoadNextScene();
        }
    }

    public void PlayIntro()
    {
        if (playRequested || transitionStarted)
        {
            return;
        }

        if (!HasPlayableVideo())
        {
            Debug.LogError(
                "MainMenuVideoTransition: Intro Video is not assigned.",
                this
            );

            LoadNextScene();
            return;
        }

        playRequested = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;

        if (playButton != null)
        {
            playButton.interactable = false;
        }

        HideMenu();
        HideCursor();

        if (videoPlayer.isPrepared)
        {
            BeginPlayback();
        }
        else
        {
            PrepareVideo();
        }
    }

    private void ConfigureVideoPlayer()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;

        if (introVideo != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = introVideo;
        }

        if (configureCameraOutput && videoCamera != null)
        {
            videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            videoPlayer.targetCamera = videoCamera;
            videoPlayer.targetCameraAlpha = 1f;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        }
    }

    private void PrepareVideo()
    {
        if (videoPlayer == null ||
            videoPlayer.isPrepared ||
            preparing ||
            !HasPlayableVideo())
        {
            return;
        }

        preparing = true;
        videoPlayer.Prepare();
    }

    private bool HasPlayableVideo()
    {
        if (videoPlayer == null)
        {
            return false;
        }

        return videoPlayer.clip != null ||
            !string.IsNullOrWhiteSpace(videoPlayer.url);
    }

    private void HandlePrepareCompleted(VideoPlayer source)
    {
        preparing = false;

        if (playRequested && !transitionStarted)
        {
            BeginPlayback();
        }
    }

    private void BeginPlayback()
    {
        if (videoPlayer == null ||
            playbackStarted ||
            transitionStarted)
        {
            return;
        }

        playbackStarted = true;
        playbackStartedAt = Time.unscaledTime;
        videoPlayer.Play();
    }

    private void HandlePlaybackCompleted(VideoPlayer source)
    {
        LoadNextScene();
    }

    private void HandleVideoError(VideoPlayer source, string message)
    {
        preparing = false;

        Debug.LogError(
            "MainMenuVideoTransition: Video could not be played. " +
            message,
            this
        );

        if (playRequested)
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        if (transitionStarted)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName) ||
            !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError(
                "MainMenuVideoTransition: Next Scene is empty or is not " +
                "included in Build Profiles: " + nextSceneName,
                this
            );

            RestoreMenu();
            RestoreCursor();
            Time.timeScale = previousTimeScale;

            playRequested = false;
            playbackStarted = false;

            if (playButton != null)
            {
                playButton.interactable = true;
            }

            return;
        }

        transitionStarted = true;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        RestoreCursor();
        Time.timeScale = 1f;

        SceneManager.LoadSceneAsync(
            nextSceneName,
            LoadSceneMode.Single
        );
    }

    private void HideMenu()
    {
        if (menuCanvasGroup == null)
        {
            return;
        }

        menuCanvasGroup.alpha = 0f;
        menuCanvasGroup.interactable = false;
        menuCanvasGroup.blocksRaycasts = false;
    }

    private void RestoreMenu()
    {
        if (menuCanvasGroup == null)
        {
            return;
        }

        menuCanvasGroup.alpha = previousMenuAlpha;
        menuCanvasGroup.interactable = previousMenuInteractable;
        menuCanvasGroup.blocksRaycasts = previousMenuBlocksRaycasts;
    }

    private void HideCursor()
    {
        if (!hideCursorDuringVideo)
        {
            return;
        }

        previousCursorVisible = Cursor.visible;
        previousCursorLockMode = Cursor.lockState;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
    }

    private void RestoreCursor()
    {
        if (!hideCursorDuringVideo)
        {
            return;
        }

        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockMode;
    }

    private bool SkipPressed()
    {
        #if ENABLE_INPUT_SYSTEM
        bool keyboardPressed =
            Keyboard.current != null &&
            (
                Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.escapeKey.wasPressedThisFrame
            );

        bool mousePressed =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        return keyboardPressed || mousePressed;
        #else
        return
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetMouseButtonDown(0);
        #endif
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepareCompleted;
            videoPlayer.loopPointReached -= HandlePlaybackCompleted;
            videoPlayer.errorReceived -= HandleVideoError;
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(PlayIntro);
        }
    }
}
