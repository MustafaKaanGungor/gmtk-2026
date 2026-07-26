using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ArrowDirection
{
    Left,
    Up,
    Right,
    Down
}

public enum MinigameFailureMode
{
    RestartCurrentRound,
    RestartFromFirstRound,
    CloseMinigame
}

[Serializable]
public class ArrowRoundSettings
{
    [Min(1)]
    public int arrowCount = 4;

    [Min(0.1f)]
    public float timeLimit = 5f;
}

public class ArrowSequenceMinigame : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject minigameRoot;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text sequenceText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Image timerFill;

    [Header("Round Settings")]
    [SerializeField] private List<ArrowRoundSettings> rounds =
        new List<ArrowRoundSettings>
        {
            new ArrowRoundSettings
            {
                arrowCount = 4,
                timeLimit = 5f
            },
            new ArrowRoundSettings
            {
                arrowCount = 5,
                timeLimit = 5f
            },
            new ArrowRoundSettings
            {
                arrowCount = 6,
                timeLimit = 5f
            }
        };

    [Header("Failure Settings")]
    [SerializeField]
    private MinigameFailureMode failureMode =
        MinigameFailureMode.RestartCurrentRound;

    [SerializeField] private float failureMessageDuration = 0.8f;
    [SerializeField] private float roundCompletedMessageDuration = 0.8f;
    [SerializeField] private float minigameCompletedMessageDuration = 1.5f;

    [Header("Game Behaviour")]
    [Tooltip("Açıkken oyun dünyasını durdurur. Sayaç etkilenmez.")]
    [SerializeField] private bool pauseGameWhileOpen = true;

    [Tooltip("Minigame açıkken kapatılacak scriptler. PlayerMovement gibi.")]
    [SerializeField] private MonoBehaviour[] behavioursToDisable;

    [Header("Arrow Colors")]
    [SerializeField] private Color pendingArrowColor = Color.white;
    [SerializeField] private Color currentArrowColor = Color.yellow;
    [SerializeField] private Color completedArrowColor = Color.green;
    [SerializeField] private Color wrongArrowColor = Color.red;

    [Header("Events")]
    public UnityEvent onMinigameCompleted;
    public UnityEvent onAttemptFailed;
    public UnityEvent onMinigameOpened;
    public UnityEvent onMinigameClosed;

    public bool IsOpen
    {
        get { return isOpen; }
    }

    private readonly List<ArrowDirection> currentSequence =
        new List<ArrowDirection>();

    private bool[] previousBehaviourStates;

    private bool isOpen;
    private bool acceptingInput;

    private int currentRoundIndex;
    private int currentSequenceIndex;

    private float timeRemaining;
    private float currentRoundDuration;
    private float previousTimeScale = 1f;

    private Coroutine flowCoroutine;

    private void Awake()
    {
        if (minigameRoot != null)
        {
            minigameRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            CloseMinigame();
            return;
        }

        if (!acceptingInput)
        {
            return;
        }

        float deltaTime = pauseGameWhileOpen
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        timeRemaining -= deltaTime;

        if (timeRemaining < 0f)
        {
            timeRemaining = 0f;
        }

        UpdateTimerUI();

        if (timeRemaining <= 0f)
        {
            FailCurrentRound("SÜRE DOLDU!");
            return;
        }

        ArrowDirection? pressedDirection = ReadPressedDirection();

        if (pressedDirection.HasValue)
        {
            CheckDirection(pressedDirection.Value);
        }
    }

    public void OpenMinigame()
    {
        if (isOpen)
        {
            return;
        }

        if (rounds == null || rounds.Count == 0)
        {
            Debug.LogWarning("Minigame için en az bir tur ayarlanmalı.");
            return;
        }

        isOpen = true;

        if (minigameRoot != null)
        {
            minigameRoot.SetActive(true);
        }

        SaveAndDisableBehaviours();

        if (pauseGameWhileOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        currentRoundIndex = 0;

        onMinigameOpened?.Invoke();

        StartCurrentRound();
    }

    public void CloseMinigame()
    {
        if (!isOpen)
        {
            return;
        }

        acceptingInput = false;
        isOpen = false;

        StopAllCoroutines();
        flowCoroutine = null;

        if (pauseGameWhileOpen)
        {
            Time.timeScale = previousTimeScale;
        }

        RestoreBehaviours();

        if (minigameRoot != null)
        {
            minigameRoot.SetActive(false);
        }

        onMinigameClosed?.Invoke();
    }

    private void StartCurrentRound()
    {
        if (!isOpen)
        {
            return;
        }

        if (currentRoundIndex < 0 || currentRoundIndex >= rounds.Count)
        {
            return;
        }

        ArrowRoundSettings settings = rounds[currentRoundIndex];

        int arrowCount = Mathf.Max(1, settings.arrowCount);

        currentSequence.Clear();

        for (int i = 0; i < arrowCount; i++)
        {
            ArrowDirection randomDirection =
                (ArrowDirection)UnityEngine.Random.Range(0, 4);

            currentSequence.Add(randomDirection);
        }

        currentSequenceIndex = 0;

        currentRoundDuration = Mathf.Max(0.1f, settings.timeLimit);
        timeRemaining = currentRoundDuration;

        acceptingInput = true;

        if (roundText != null)
        {
            roundText.text =
                "AŞAMA " +
                (currentRoundIndex + 1) +
                " / " +
                rounds.Count;
        }

        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }

        RefreshSequenceText();
        UpdateTimerUI();
    }

    private ArrowDirection? ReadPressedDirection()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return null;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            return ArrowDirection.Left;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            return ArrowDirection.Up;
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            return ArrowDirection.Right;
        }

        if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            return ArrowDirection.Down;
        }

        return null;
    }

    private void CheckDirection(ArrowDirection pressedDirection)
    {
        if (currentSequenceIndex >= currentSequence.Count)
        {
            return;
        }

        ArrowDirection expectedDirection =
            currentSequence[currentSequenceIndex];

        if (pressedDirection == expectedDirection)
        {
            currentSequenceIndex++;

            RefreshSequenceText();

            if (currentSequenceIndex >= currentSequence.Count)
            {
                acceptingInput = false;

                flowCoroutine =
                    StartCoroutine(RoundCompletedRoutine());
            }

            return;
        }

        RefreshSequenceText(currentSequenceIndex);
        FailCurrentRound("YANLIŞ TUŞ!");
    }

    private void FailCurrentRound(string message)
    {
        if (!acceptingInput)
        {
            return;
        }

        acceptingInput = false;

        if (feedbackText != null)
        {
            feedbackText.text = message;
        }

        onAttemptFailed?.Invoke();

        flowCoroutine = StartCoroutine(FailureRoutine());
    }

    private IEnumerator FailureRoutine()
    {
        yield return new WaitForSecondsRealtime(failureMessageDuration);

        if (!isOpen)
        {
            yield break;
        }

        switch (failureMode)
        {
            case MinigameFailureMode.RestartCurrentRound:
                StartCurrentRound();
                break;

            case MinigameFailureMode.RestartFromFirstRound:
                currentRoundIndex = 0;
                StartCurrentRound();
                break;

            case MinigameFailureMode.CloseMinigame:
                CloseMinigame();
                break;
        }
    }

    private IEnumerator RoundCompletedRoutine()
    {
        if (feedbackText != null)
        {
            feedbackText.text = "AŞAMA TAMAMLANDI!";
        }

        yield return new WaitForSecondsRealtime(
            roundCompletedMessageDuration
        );

        if (!isOpen)
        {
            yield break;
        }

        currentRoundIndex++;

        if (currentRoundIndex >= rounds.Count)
        {
            flowCoroutine =
                StartCoroutine(MinigameCompletedRoutine());

            yield break;
        }

        StartCurrentRound();
    }

    private IEnumerator MinigameCompletedRoutine()
    {
        acceptingInput = false;

        if (feedbackText != null)
        {
            feedbackText.text = "GÖREV TAMAMLANDI!";
        }

        onMinigameCompleted?.Invoke();

        yield return new WaitForSecondsRealtime(
            minigameCompletedMessageDuration
        );

        CloseMinigame();
    }

    private void UpdateTimerUI()
{
    if (timerText != null)
    {
        int totalTenths = Mathf.CeilToInt(timeRemaining * 10f);

        int minutes = totalTenths / 600;
        int seconds = (totalTenths / 10) % 60;
        int tenths = totalTenths % 10;

        timerText.text =
            $"{minutes:00}:{seconds:00}.{tenths}";
    }

    if (timerFill != null)
    {
        if (currentRoundDuration <= 0f)
        {
            timerFill.fillAmount = 0f;
        }
        else
        {
            timerFill.fillAmount = Mathf.Clamp01(
                timeRemaining / currentRoundDuration
            );
        }
    }
}

    private void RefreshSequenceText(int wrongIndex = -1)
    {
        if (sequenceText == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < currentSequence.Count; i++)
        {
            Color arrowColor;

            if (i == wrongIndex)
            {
                arrowColor = wrongArrowColor;
            }
            else if (i < currentSequenceIndex)
            {
                arrowColor = completedArrowColor;
            }
            else if (i == currentSequenceIndex)
            {
                arrowColor = currentArrowColor;
            }
            else
            {
                arrowColor = pendingArrowColor;
            }

            string htmlColor =
                ColorUtility.ToHtmlStringRGBA(arrowColor);

            builder.Append("<color=#");
            builder.Append(htmlColor);
            builder.Append(">");
            builder.Append(GetArrowCharacter(currentSequence[i]));
            builder.Append("</color>");

            if (i < currentSequence.Count - 1)
            {
                builder.Append("   ");
            }
        }

        sequenceText.text = builder.ToString();
    }

    private string GetArrowCharacter(ArrowDirection direction)
    {
        switch (direction)
        {
            case ArrowDirection.Left:
                return "←";

            case ArrowDirection.Up:
                return "↑";

            case ArrowDirection.Right:
                return "→";

            case ArrowDirection.Down:
                return "↓";

            default:
                return "?";
        }
    }

    private void SaveAndDisableBehaviours()
    {
        if (behavioursToDisable == null)
        {
            return;
        }

        previousBehaviourStates =
            new bool[behavioursToDisable.Length];

        for (int i = 0; i < behavioursToDisable.Length; i++)
        {
            MonoBehaviour behaviour = behavioursToDisable[i];

            if (behaviour == null)
            {
                continue;
            }

            previousBehaviourStates[i] = behaviour.enabled;
            behaviour.enabled = false;
        }
    }

    private void RestoreBehaviours()
    {
        if (behavioursToDisable == null ||
            previousBehaviourStates == null)
        {
            return;
        }

        int count = Mathf.Min(
            behavioursToDisable.Length,
            previousBehaviourStates.Length
        );

        for (int i = 0; i < count; i++)
        {
            MonoBehaviour behaviour = behavioursToDisable[i];

            if (behaviour == null)
            {
                continue;
            }

            behaviour.enabled = previousBehaviourStates[i];
        }
    }

    private void OnDestroy()
    {
        if (isOpen && pauseGameWhileOpen)
        {
            Time.timeScale = previousTimeScale;
        }

        RestoreBehaviours();
    }
}