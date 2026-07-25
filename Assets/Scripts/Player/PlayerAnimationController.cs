using System;
using UnityEngine;

/// <summary>
/// Oyuncunun animasyon state'lerini yonetir. Hareket/tutma/ziplama durumlarini
/// otomatik algilar; pump ve climb disaridan set edilir. Her state icin bir
/// Animator state adi ve ses sinyali (enter/exit) baglanabilir. Ses sinyalleri
/// GameSignals ile yayinlanir; SoundEventBinder bunlari seslere baglar.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAnimationController : MonoBehaviour
{
    public enum AnimationState
    {
        Idle,
        WalkLeft,
        WalkRight,
        Jump,
        HoldLeft,
        HoldRight,
        Climb,
        Pump
    }

    [Serializable]
    public class StateBinding
    {
        [Tooltip("Hangi state.")]
        public AnimationState state;

        [Tooltip("Animator'da oynatilacak state adi. Bos ise enum adi kullanilir.")]
        public string animatorStateName;

        [Tooltip("Bu state'e GIRINCE yayinlanacak ses sinyali. Bos ise ses yok.")]
        public string enterSoundSignal;

        [Tooltip("Bu state'ten CIKINCA yayinlanacak ses sinyali (loop durdurmak " +
            "icin). Bos ise ses yok.")]
        public string exitSoundSignal;
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement2D movement;
    [SerializeField] private PlayerGroundBagThrower bagThrower;
    [SerializeField] private Rigidbody2D body;

    [Header("Detection")]
    [Min(0f)]
    [Tooltip("Bu hizin uzerinde yatay hareket 'yurume' sayilir.")]
    [SerializeField] private float moveSpeedThreshold = 0.1f;

    [Header("State Bindings (state -> animasyon + ses)")]
    [SerializeField] private StateBinding[] stateBindings;

    public AnimationState CurrentState { get; private set; }
    public bool IsPumping { get; private set; }
    public bool IsClimbing { get; private set; }

    private void Reset()
    {
        movement = GetComponent<PlayerMovement2D>();
        bagThrower = GetComponent<PlayerGroundBagThrower>();
        body = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        BuildDefaultBindings();
    }

    private void Awake()
    {
        if (movement == null)
        {
            movement = GetComponent<PlayerMovement2D>();
        }

        if (bagThrower == null)
        {
            bagThrower = GetComponent<PlayerGroundBagThrower>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        // Ilk state'i zorla uygula (cikis sinyali yayinlamadan).
        ApplyState(DetermineState(), true);
    }

    private void Update()
    {
        AnimationState next = DetermineState();

        if (next != CurrentState)
        {
            ApplyState(next, false);
        }
    }

    /// <summary>Pump animasyonunu disaridan ac/kapat (ornegin pompa alani).</summary>
    public void SetPumping(bool value)
    {
        IsPumping = value;
    }

    /// <summary>Climb animasyonunu disaridan ac/kapat.</summary>
    public void SetClimbing(bool value)
    {
        IsClimbing = value;
    }

    private AnimationState DetermineState()
    {
        bool facingRight =
            movement == null || movement.FacingDirection > 0;

        // Oncelik: pump > climb > zipla > tut > yuru > idle
        if (IsPumping)
        {
            return AnimationState.Pump;
        }

        if (IsClimbing)
        {
            return AnimationState.Climb;
        }

        bool grounded = movement != null && movement.IsGrounded;

        if (!grounded)
        {
            return AnimationState.Jump;
        }

        bool holding = bagThrower != null && bagThrower.HasBag;

        if (holding)
        {
            return facingRight
                ? AnimationState.HoldRight
                : AnimationState.HoldLeft;
        }

        float speedX =
            body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;

        if (speedX > moveSpeedThreshold)
        {
            return facingRight
                ? AnimationState.WalkRight
                : AnimationState.WalkLeft;
        }

        return AnimationState.Idle;
    }

    private void ApplyState(AnimationState newState, bool force)
    {
        if (!force && newState == CurrentState)
        {
            return;
        }

        // Eski state'in cikis sesini yayinla (ilk zorlamada atla).
        if (!force)
        {
            StateBinding oldBinding = GetBinding(CurrentState);

            if (oldBinding != null &&
                !string.IsNullOrEmpty(oldBinding.exitSoundSignal))
            {
                GameSignals.Raise(oldBinding.exitSoundSignal);
            }
        }

        CurrentState = newState;

        StateBinding newBinding = GetBinding(newState);

        string animStateName =
            newBinding != null &&
            !string.IsNullOrEmpty(newBinding.animatorStateName)
                ? newBinding.animatorStateName
                : newState.ToString();

        if (animator != null)
        {
            animator.Play(animStateName);
        }

        if (newBinding != null &&
            !string.IsNullOrEmpty(newBinding.enterSoundSignal))
        {
            GameSignals.Raise(newBinding.enterSoundSignal);
        }
    }

    private StateBinding GetBinding(AnimationState state)
    {
        if (stateBindings == null)
        {
            return null;
        }

        for (int index = 0; index < stateBindings.Length; index++)
        {
            if (stateBindings[index] != null &&
                stateBindings[index].state == state)
            {
                return stateBindings[index];
            }
        }

        return null;
    }

    private void BuildDefaultBindings()
    {
        AnimationState[] states =
            (AnimationState[])Enum.GetValues(typeof(AnimationState));

        stateBindings = new StateBinding[states.Length];

        for (int index = 0; index < states.Length; index++)
        {
            stateBindings[index] = new StateBinding
            {
                state = states[index],
                animatorStateName = states[index].ToString()
            };
        }
    }
}
