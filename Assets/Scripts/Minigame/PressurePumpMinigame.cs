using System;
using UnityEngine;

/// <summary>
/// Basinc pompasi minigame'inin basari sistemi. PressurePumpController'i izler;
/// basinc yesil bolgede yeterince kalinca minigame'i tamamlar ve
/// (varsa) TaskManager'a ilgili gorevi tamamlandi olarak bildirir.
/// </summary>
[DisallowMultipleComponent]
public class PressurePumpMinigame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PressurePumpController pump;

    [Tooltip("Opsiyonel. Atanirsa basari aninda ilgili gorev tamamlanir.")]
    [SerializeField] private TaskManager taskManager;

    [Header("Task")]
    [Tooltip("Basarida TaskManager'da tamamlanacak gorevin kimligi.")]
    [SerializeField] private string taskId = "pressure_pump";

    [Header("Success Settings")]
    [Tooltip("Basincin yesil bolgede kalmasi gereken sure (saniye).")]
    [Min(0f)]
    [SerializeField] private float requiredHoldTime = 3f;

    [Tooltip("Acik: yesil bolgeden cikinca sayac sifirlanir (kesintisiz tutma). " +
        "Kapali: toplam sure birikir.")]
    [SerializeField] private bool requireContinuousHold = true;

    [Tooltip("Basarinca pompa mekanigini durdur.")]
    [SerializeField] private bool stopPumpOnSuccess = true;

    /// <summary>Minigame basariyla tamamlandiginda tetiklenir.</summary>
    public event Action Completed;

    private float holdTimer;

    public bool IsCompleted { get; private set; }

    // Basariya ne kadar yaklasildi (0-1 arasi). UI ilerleme cubugu icin.
    public float HoldProgress =>
        requiredHoldTime <= 0f
            ? 1f
            : Mathf.Clamp01(holdTimer / requiredHoldTime);

    private void Awake()
    {
        if (pump == null)
        {
            Debug.LogError(
                "PressurePumpMinigame: PressurePumpController atanmamis!",
                this
            );

            enabled = false;
        }
    }

    private void Update()
    {
        if (IsCompleted)
        {
            return;
        }

        if (pump.IsInGreenZone)
        {
            holdTimer += Time.deltaTime;
        }
        else if (requireContinuousHold)
        {
            holdTimer = 0f;
        }

        if (holdTimer >= requiredHoldTime)
        {
            CompleteMinigame();
        }
    }

    private void CompleteMinigame()
    {
        IsCompleted = true;

        if (stopPumpOnSuccess)
        {
            pump.StopPumping();
        }

        if (taskManager != null)
        {
            taskManager.CompleteTask(taskId);
        }

        GameSignals.Raise(GameSignals.PressureSuccess);

        Completed?.Invoke();

        Debug.Log(
            "PressurePumpMinigame: Basari! Gorev '" + taskId + "'.",
            this
        );
    }

    /// <summary>Minigame'i bastan baslatmak icin.</summary>
    public void ResetMinigame()
    {
        IsCompleted = false;
        holdTimer = 0f;
    }
}
