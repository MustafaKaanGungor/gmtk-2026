using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class ThrowTestController : MonoBehaviour
{
    private enum TestState
    {
        Aiming,
        SelectingPower,
        BagFlying
    }

    [Header("References")]
    [SerializeField] private AimController aimController;
    [SerializeField] private PowerController powerController;
    [SerializeField] private BagThrower bagThrower;

    private TestState currentState;

    private void Start()
    {
        StartNewTestThrow();
    }

    private void Update()
    {
        if (ConfirmPressed())
        {
            HandleConfirmInput();
        }

        if (RestartPressed())
        {
            StartNewTestThrow();
        }
    }

    private void HandleConfirmInput()
    {
        switch (currentState)
        {
            case TestState.Aiming:
                LockAimAndStartPower();
                break;

            case TestState.SelectingPower:
                LockPowerAndThrow();
                break;

            case TestState.BagFlying:
                Debug.Log("Yeni atış için R tuşuna bas.");
                break;
        }
    }

    private void LockAimAndStartPower()
    {
        aimController.StopAiming();

        powerController.ResetPower();
        powerController.ShowPowerBar();
        powerController.StartSelectingPower();

        currentState = TestState.SelectingPower;

        Debug.Log("Açı kilitlendi. Şimdi gücü seç.");
    }

    private void LockPowerAndThrow()
    {
        float selectedPower = powerController.LockPower();
        Vector2 selectedDirection = aimController.AimDirection;

        powerController.HidePowerBar();
        aimController.HideArrow();

        bagThrower.ThrowBag(selectedDirection, selectedPower);

        currentState = TestState.BagFlying;

        Debug.Log("Çanta fırlatıldı. Yeni atış için R tuşuna bas.");
    }

    private void StartNewTestThrow()
    {
        bagThrower.RemoveHeldBag();

        aimController.gameObject.SetActive(true);
        aimController.ResetAim();
        aimController.StartAiming();

        powerController.gameObject.SetActive(true);
        powerController.ResetPower();
        powerController.HidePowerBar();

        bagThrower.SpawnBag();

        currentState = TestState.Aiming;

        Debug.Log("Açıyı kilitlemek için Space, Enter veya sol tık kullan.");
    }

    private bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool keyboardPressed = Keyboard.current != null && 
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);

        bool mousePressed = Mouse.current != null && 
            Mouse.current.leftButton.wasPressedThisFrame;

        return keyboardPressed || mousePressed;
#else
        return Input.GetKeyDown(KeyCode.Space) || 
               Input.GetKeyDown(KeyCode.Return) || 
               Input.GetMouseButtonDown(0);
#endif
    }

    private bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }
}