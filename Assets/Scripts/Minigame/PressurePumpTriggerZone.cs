using UnityEngine;

/// <summary>
/// Oyunun belli bir alani. Oyuncu (PlayerMovement2D) iceri girince basinc
/// pompasi minigame'ini acar ve baslatir; alandan cikinca durdurur ve gizler.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class PressurePumpTriggerZone : MonoBehaviour
{
    [Header("Minigame References")]
    [Tooltip("Acilip kapanacak minigame kok objesi (ornegin UI paneli).")]
    [SerializeField] private GameObject minigameRoot;

    [SerializeField] private PressurePumpController pump;

    [Tooltip("Opsiyonel. Basari sistemini sifirlamak icin.")]
    [SerializeField] private PressurePumpMinigame minigame;

    [Tooltip("Opsiyonel. Atanmazsa alana giren oyuncudan otomatik alinir. " +
        "Pompalarken pump (anxious) animasyonunu tetiklemek icin.")]
    [SerializeField] private PlayerAnimationController playerAnimation;

    [Header("Settings")]
    [Tooltip("Alana her girildiginde basinci ve minigame'i sifirla.")]
    [SerializeField] private bool resetOnEnter = true;

    [Tooltip("Minigame zaten tamamlandiysa tekrar acma.")]
    [SerializeField] private bool skipIfCompleted = true;

    [Header("Debug")]
    [SerializeField] private bool printMessages = true;

    public bool IsOpen { get; private set; }

    private void Reset()
    {
        Collider2D colliderComponent = GetComponent<Collider2D>();

        if (colliderComponent != null)
        {
            colliderComponent.isTrigger = true;
        }
    }

    private void Awake()
    {
        Collider2D colliderComponent = GetComponent<Collider2D>();

        if (!colliderComponent.isTrigger)
        {
            colliderComponent.isTrigger = true;

            Debug.LogWarning(
                "PressurePumpTriggerZone: Collider'in Is Trigger ayari " +
                "otomatik acildi.",
                this
            );
        }

        // Baslangicta minigame kapali.
        CloseMinigame();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        // Atanmamissa giren oyuncudan animasyon kontrolcusunu al.
        if (playerAnimation == null)
        {
            playerAnimation =
                other.GetComponentInParent<PlayerAnimationController>();
        }

        OpenMinigame();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        CloseMinigame();
    }

    private bool IsPlayer(Collider2D other)
    {
        return other.GetComponentInParent<PlayerMovement2D>() != null;
    }

    private void OpenMinigame()
    {
        if (IsOpen)
        {
            return;
        }

        if (
            skipIfCompleted &&
            minigame != null &&
            minigame.IsCompleted)
        {
            return;
        }

        IsOpen = true;

        if (minigameRoot != null)
        {
            minigameRoot.SetActive(true);
        }

        if (resetOnEnter)
        {
            if (pump != null)
            {
                pump.ResetPressure();
            }

            if (minigame != null)
            {
                minigame.ResetMinigame();
            }
        }

        if (pump != null)
        {
            pump.StartPumping();
        }

        // Pompalarken anxious (pump) animasyonunu ac.
        if (playerAnimation != null)
        {
            playerAnimation.SetPumping(true);
        }

        if (printMessages)
        {
            Debug.Log(
                "PressurePumpTriggerZone: Minigame acildi.",
                this
            );
        }
    }

    private void CloseMinigame()
    {
        IsOpen = false;

        if (pump != null)
        {
            pump.StopPumping();
        }

        if (playerAnimation != null)
        {
            playerAnimation.SetPumping(false);
        }

        if (minigameRoot != null)
        {
            minigameRoot.SetActive(false);
        }
    }
}
