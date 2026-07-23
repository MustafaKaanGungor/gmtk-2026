using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class ThrowGameController : MonoBehaviour
{
    private enum GameState
    {
        Preparing,
        Aiming,
        SelectingPower,
        BagFlying,
        WaitingForNextThrow,
        GameOver
    }

    [Header("Gameplay References")]
    [SerializeField] private AimController aimController;
    [SerializeField] private PowerController powerController;
    [SerializeField] private BagThrower bagThrower;
    [SerializeField] private BagSpawner bagSpawner;
    [SerializeField] private RocketTarget rocketTarget;

    [Header("UI Reference")]
    [SerializeField] private GameUIController gameUIController;

    [Header("Game Flow")]
    [Min(0f)]
    [SerializeField] private float nextThrowDelay = 1.25f;

    [Header("Miss Detection")]
    [Tooltip(
        "Bu süreden önce çantanın durmuş olup olmadığı kontrol edilmez."
    )]
    [Min(0f)]
    [SerializeField] private float minimumFlightTime = 1f;

    [Tooltip(
        "Çanta bu hızın altında kaldığında durmuş sayılmaya yaklaşır."
    )]
    [Min(0f)]
    [SerializeField] private float stoppedSpeedThreshold = 0.3f;

    [Tooltip(
        "Çanta düşük hızda bu kadar süre kalırsa atış kaçmış sayılır."
    )]
    [Min(0f)]
    [SerializeField] private float stoppedDurationRequired = 0.6f;

    [Tooltip(
        "Atış bu süreden uzun sürerse otomatik olarak kaçmış sayılır."
    )]
    [Min(1f)]
    [SerializeField] private float maximumFlightTime = 8f;

    [Header("World Bounds")]
    [SerializeField] private float minimumWorldX = -12f;
    [SerializeField] private float maximumWorldX = 12f;
    [SerializeField] private float minimumWorldY = -7f;
    [SerializeField] private float maximumWorldY = 10f;

    [Header("Failed Bag")]
    [Tooltip(
        "Kaçan çantanın yok edilmeden önce sahnede kalacağı süre."
    )]
    [Min(0f)]
    [SerializeField] private float failedBagLifetime = 2f;

    private GameState currentState;

    private BagProjectile activeThrownBag;

    private float flightTimer;
    private float stoppedTimer;
    private float nextThrowTimer;

    private bool throwResultHandled;

    private void OnEnable()
    {
        if (rocketTarget != null)
        {
            rocketTarget.BagDelivered +=
                HandleBagDelivered;
        }
    }

    private void OnDisable()
    {
        if (rocketTarget != null)
        {
            rocketTarget.BagDelivered -=
                HandleBagDelivered;
        }
    }

    private void Start()
    {
        if (!ReferencesAreValid())
        {
            enabled = false;
            return;
        }

        StartGame();
    }

    private void Update()
    {
        switch (currentState)
        {
            case GameState.Aiming:
            case GameState.SelectingPower:
                CheckSelectionInput();
                break;

            case GameState.BagFlying:
                UpdateFlyingBag();
                break;

            case GameState.WaitingForNextThrow:
                UpdateNextThrowTimer();
                break;
        }
    }

    private bool ReferencesAreValid()
    {
        bool referencesValid = true;

        if (aimController == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "AimController atanmamış.",
                this
            );

            referencesValid = false;
        }

        if (powerController == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "PowerController atanmamış.",
                this
            );

            referencesValid = false;
        }

        if (bagThrower == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "BagThrower atanmamış.",
                this
            );

            referencesValid = false;
        }

        if (bagSpawner == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "BagSpawner atanmamış.",
                this
            );

            referencesValid = false;
        }

        if (rocketTarget == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "RocketTarget atanmamış.",
                this
            );

            referencesValid = false;
        }

        if (gameUIController == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "GameUIController atanmamış.",
                this
            );

            referencesValid = false;
        }

        return referencesValid;
    }

    public void StartGame()
    {
        activeThrownBag = null;
        throwResultHandled = false;

        bagSpawner.ResetSpawner();

        gameUIController.ResetGame(
            bagSpawner.RemainingBagCount
        );

        PrepareNextBag();
    }

    private void PrepareNextBag()
    {
        currentState = GameState.Preparing;

        if (!bagSpawner.HasRemainingBags)
        {
            FinishGame();
            return;
        }

        aimController.gameObject.SetActive(true);
        aimController.ResetAim();
        aimController.StartAiming();

        powerController.gameObject.SetActive(true);
        powerController.ResetPower();
        powerController.HidePowerBar();

        bool bagPrepared =
            bagSpawner.TryPrepareNextBag(
                out BagProjectile preparedBag
            );

        if (!bagPrepared || preparedBag == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "Yeni çanta hazırlanamadı.",
                this
            );

            FinishGame();
            return;
        }

        activeThrownBag = null;
        throwResultHandled = false;

        flightTimer = 0f;
        stoppedTimer = 0f;
        nextThrowTimer = 0f;

        currentState = GameState.Aiming;

        gameUIController.SetInstruction(
            "Atış açısını seç ve SPACE, " +
            "Enter veya sol tık ile durdur."
        );

        gameUIController.SetRemainingBagCount(
            bagSpawner.RemainingBagCount
        );
    }

    private void CheckSelectionInput()
    {
        if (!ConfirmPressed())
        {
            return;
        }

        if (currentState == GameState.Aiming)
        {
            LockAimAndStartPower();
        }
        else if (
            currentState ==
            GameState.SelectingPower)
        {
            LockPowerAndThrow();
        }
    }

    private void LockAimAndStartPower()
    {
        aimController.StopAiming();

        powerController.ShowPowerBar();
        powerController.ResetPower();
        powerController.StartSelectingPower();

        currentState =
            GameState.SelectingPower;

        gameUIController.SetInstruction(
            "Atış gücünü seç ve SPACE, " +
            "Enter veya sol tık ile durdur."
        );
    }

    private void LockPowerAndThrow()
    {
        float selectedPower =
            powerController.LockPower();

        Vector2 selectedDirection =
            aimController.AimDirection;

        aimController.HideArrow();
        powerController.HidePowerBar();

        activeThrownBag =
            bagThrower.ThrowBag(
                selectedDirection,
                selectedPower
            );

        if (activeThrownBag == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "Çanta fırlatılamadı.",
                this
            );

            bagSpawner.CancelPreparedBag();
            PrepareNextBag();
            return;
        }

        bagSpawner.MarkPreparedBagAsUsed();

        gameUIController.SetRemainingBagCount(
            bagSpawner.RemainingBagCount
        );

        flightTimer = 0f;
        stoppedTimer = 0f;
        throwResultHandled = false;

        currentState = GameState.BagFlying;

        gameUIController.SetInstruction(
            "Çanta uçuyor..."
        );
    }

    private void UpdateFlyingBag()
    {
        if (throwResultHandled)
        {
            return;
        }

        flightTimer += Time.deltaTime;

        if (activeThrownBag == null)
        {
            HandleMissedThrow();
            return;
        }

        if (BagIsOutsideWorld())
        {
            HandleMissedThrow();
            return;
        }

        if (flightTimer >= maximumFlightTime)
        {
            HandleMissedThrow();
            return;
        }

        if (flightTimer < minimumFlightTime)
        {
            return;
        }

        CheckIfBagStopped();
    }

    private void CheckIfBagStopped()
    {
        Rigidbody2D bagBody =
            activeThrownBag.Body;

        if (bagBody == null)
        {
            return;
        }

        if (!bagBody.simulated)
        {
            return;
        }

        bool isSleeping =
            bagBody.IsSleeping();

        bool isMovingSlowly =
            bagBody.linearVelocity.sqrMagnitude <=
            stoppedSpeedThreshold *
            stoppedSpeedThreshold;

        if (isSleeping || isMovingSlowly)
        {
            stoppedTimer += Time.deltaTime;
        }
        else
        {
            stoppedTimer = 0f;
        }

        if (
            stoppedTimer >=
            stoppedDurationRequired)
        {
            HandleMissedThrow();
        }
    }

    private bool BagIsOutsideWorld()
    {
        Vector2 bagPosition =
            activeThrownBag.transform.position;

        return
            bagPosition.x < minimumWorldX ||
            bagPosition.x > maximumWorldX ||
            bagPosition.y < minimumWorldY ||
            bagPosition.y > maximumWorldY;
    }

    private void HandleBagDelivered(
        BagProjectile deliveredBag)
    {
        if (
            currentState !=
            GameState.BagFlying)
        {
            return;
        }

        if (throwResultHandled)
        {
            return;
        }

        if (
            deliveredBag !=
            activeThrownBag)
        {
            return;
        }

        HandleSuccessfulThrow();
    }

    private void HandleSuccessfulThrow()
    {
        throwResultHandled = true;

        gameUIController.RegisterSuccessfulBag(
            bagSpawner.RemainingBagCount
        );

        StartWaitingForNextThrow();
    }

    private void HandleMissedThrow()
    {
        if (throwResultHandled)
        {
            return;
        }

        throwResultHandled = true;

        gameUIController.RegisterMissedBag(
            bagSpawner.RemainingBagCount
        );

        if (activeThrownBag != null)
        {
            Destroy(
                activeThrownBag.gameObject,
                failedBagLifetime
            );
        }

        StartWaitingForNextThrow();
    }

    private void StartWaitingForNextThrow()
    {
        currentState =
            GameState.WaitingForNextThrow;

        nextThrowTimer =
            nextThrowDelay;
    }

    private void UpdateNextThrowTimer()
    {
        nextThrowTimer -= Time.deltaTime;

        if (nextThrowTimer > 0f)
        {
            return;
        }

        PrepareNextBag();
    }

    private void FinishGame()
    {
        currentState = GameState.GameOver;

        aimController.HideArrow();
        powerController.HidePowerBar();
        bagThrower.RemoveHeldBag();

        gameUIController.ShowGameOver(
            bagSpawner.TotalBagCount
        );

        Debug.Log(
            "Oyun bitti. Skor: " +
            gameUIController.CurrentScore +
            ", başarılı: " +
            gameUIController.SuccessfulBagCount +
            ", kaçan: " +
            gameUIController.MissedBagCount,
            this
        );
    }

    private bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool keyboardPressed =
            Keyboard.current != null &&
            (
                Keyboard.current
                    .spaceKey
                    .wasPressedThisFrame ||
                Keyboard.current
                    .enterKey
                    .wasPressedThisFrame
            );

        bool mousePressed =
            Mouse.current != null &&
            Mouse.current
                .leftButton
                .wasPressedThisFrame;

        return
            keyboardPressed ||
            mousePressed;
#else
        return
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetMouseButtonDown(0);
#endif
    }
}