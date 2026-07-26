using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class VentMinigameCompletionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private VentMinigameInteractable ventInteractable;

    [SerializeField]
    private BrushCursorController brushCursor;

    [SerializeField]
    private GameObject successPanel;

    [SerializeField]
    private CanvasGroup successCanvasGroup;

    [SerializeField]
    private RectTransform successRect;

    [Header("Animation")]
    [Min(0f)]
    [SerializeField]
    private float appearDuration = 0.25f;

    [Range(0.1f, 1f)]
    [SerializeField]
    private float startingScale = 0.85f;

    [Header("Closing")]
    [Min(0f)]
    [SerializeField]
    private float visibleDuration = 1.25f;

    [SerializeField]
    private bool closeAutomatically = true;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onTaskCompleted;

    private Coroutine completionRoutine;
    private bool completionStarted;

    private Vector3 successOriginalScale =
        Vector3.one;

    private void Awake()
    {
        if (successPanel != null)
        {
            if (successCanvasGroup == null)
            {
                successCanvasGroup =
                    successPanel.GetComponent<CanvasGroup>();
            }

            if (successRect == null)
            {
                successRect =
                    successPanel.GetComponent<RectTransform>();
            }
        }

        if (successRect != null)
        {
            successOriginalScale =
                successRect.localScale;
        }

        HideSuccessPanel();
    }

    private void OnEnable()
    {
        completionStarted = false;

        if (completionRoutine != null)
        {
            StopCoroutine(completionRoutine);
            completionRoutine = null;
        }

        HideSuccessPanel();

        if (brushCursor != null)
        {
            brushCursor.SetInteractionEnabled(true);
            brushCursor.SetBrushVisible(true);
        }
    }

    public void HandleCleaningCompleted()
    {
        if (completionStarted)
        {
            return;
        }

        completionStarted = true;

        if (ventInteractable != null)
        {
            ventInteractable.CompleteTask();
        }

        onTaskCompleted?.Invoke();

        completionRoutine =
            StartCoroutine(CompletionSequence());
    }

    private IEnumerator CompletionSequence()
    {
        if (brushCursor != null)
        {
            brushCursor.SetInteractionEnabled(false);
            brushCursor.SetBrushVisible(false);
        }

        if (successPanel != null)
        {
            successPanel.SetActive(true);
        }

        if (successCanvasGroup != null)
        {
            successCanvasGroup.alpha = 0f;
            successCanvasGroup.interactable = true;
            successCanvasGroup.blocksRaycasts = true;
        }

        if (successRect != null)
        {
            successRect.localScale =
                successOriginalScale *
                startingScale;
        }

        float elapsedTime = 0f;

        while (elapsedTime < appearDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                appearDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsedTime /
                        appearDuration
                    );

            float easedTime =
                1f -
                Mathf.Pow(
                    1f - normalizedTime,
                    3f
                );

            if (successCanvasGroup != null)
            {
                successCanvasGroup.alpha =
                    easedTime;
            }

            if (successRect != null)
            {
                float scaleMultiplier =
                    Mathf.Lerp(
                        startingScale,
                        1f,
                        easedTime
                    );

                successRect.localScale =
                    successOriginalScale *
                    scaleMultiplier;
            }

            yield return null;
        }

        if (successCanvasGroup != null)
        {
            successCanvasGroup.alpha = 1f;
        }

        if (successRect != null)
        {
            successRect.localScale =
                successOriginalScale;
        }

        yield return new WaitForSecondsRealtime(
            visibleDuration
        );

        if (closeAutomatically &&
            ventInteractable != null)
        {
            ventInteractable.CloseMinigame();
        }

        completionRoutine = null;
    }

    private void HideSuccessPanel()
    {
        if (successCanvasGroup != null)
        {
            successCanvasGroup.alpha = 0f;
            successCanvasGroup.interactable = false;
            successCanvasGroup.blocksRaycasts = false;
        }

        if (successRect != null)
        {
            successRect.localScale =
                successOriginalScale;
        }

        if (successPanel != null)
        {
            successPanel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (completionRoutine != null)
        {
            StopCoroutine(completionRoutine);
            completionRoutine = null;
        }
    }
}