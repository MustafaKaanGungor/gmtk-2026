using System;
using UnityEngine;

[RequireComponent(typeof(BagProjectile))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class GroundBagPickup : MonoBehaviour
{
    [Header("Bag Identity")]
    [SerializeField] private BagType bagType = BagType.Brown;

    public BagType Type => bagType;

    [Header("Ground Physics")]
    [Min(0f)]
    [SerializeField] private float groundGravityScale = 1.5f;

    [Header("Pickup")]
    [Tooltip(
        "Fırlatılan bavulun yanlışlıkla anında geri alınmasını önler. " +
        "Bu süre dolunca bavul hareket ediyor veya havada olsa da alınabilir."
    )]
    [Min(0f)]
    [SerializeField] private float pickupCooldownAfterThrow = 0.2f;

    [Header("Return To Ground")]
    [Tooltip(
        "Fırlatıldıktan sonra bu süre dolmadan durma kontrolü yapılmaz."
    )]
    [Min(0f)]
    [SerializeField] private float minimumFlightTime = 0.35f;

    [Tooltip(
        "Bavulun yeniden alınabilir sayılması için gereken hız sınırı."
    )]
    [Min(0f)]
    [SerializeField] private float stoppedSpeedThreshold = 0.35f;

    [Tooltip(
        "Bavul bu süre boyunca yavaş kalırsa yeniden alınabilir olur."
    )]
    [Min(0f)]
    [SerializeField] private float stoppedDurationRequired = 0.5f;

    private BagProjectile projectile;
    private Rigidbody2D body;
    private Collider2D[] bagColliders;
    private Collider2D[] holderColliders;

    private bool waitingToBecomeAvailable;
    private float flightTimer;
    private float stoppedTimer;
    private float pickupCooldownTimer;

    public bool IsAvailable { get; private set; }
    public bool IsHeld { get; private set; }
    public bool IsDelivered { get; private set; }
    public bool IsInFlight => waitingToBecomeAvailable;

    public bool CanBePickedUp =>
        !IsHeld &&
        !IsDelivered &&
        pickupCooldownTimer <= 0f &&
        body != null &&
        body.simulated &&
        body.bodyType == RigidbodyType2D.Dynamic;

    public BagProjectile Projectile => projectile;

    /// <summary>
    /// Fırlatılan bavul yerde durup yeniden alınabilir olduğunda çalışır.
    /// </summary>
    public event Action<GroundBagPickup> ReturnedToGround;
    public event Action<GroundBagPickup> PickedUp;
    public event Action<GroundBagPickup> Delivered;

    private void Awake()
    {
        projectile = GetComponent<BagProjectile>();
        body = GetComponent<Rigidbody2D>();
        bagColliders = GetComponentsInChildren<Collider2D>();

        PrepareAsGroundBag();
    }

    private void FixedUpdate()
    {
        UpdatePickupCooldown();
        UpdateReturnToGroundState();
    }

    /// <summary>
    /// Bavulu alınabilir ve yerde fizik gören duruma getirir.
    /// </summary>
    public void PrepareAsGroundBag()
    {
        if (projectile == null || body == null)
        {
            return;
        }

        body.simulated = true;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = groundGravityScale;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.interpolation =
        RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode =
        CollisionDetectionMode2D.Continuous;

        IsAvailable = true;
        IsHeld = false;
        IsDelivered = false;

        waitingToBecomeAvailable = false;
        flightTimer = 0f;
        stoppedTimer = 0f;
        pickupCooldownTimer = 0f;
    }

    public bool TryHold(
        Transform holdPoint,
        Collider2D[] newHolderColliders)
    {
        if (
            !CanBePickedUp ||
            projectile == null ||
            holdPoint == null)
        {
            return false;
        }

        IsAvailable = false;
        IsHeld = true;
        waitingToBecomeAvailable = false;

        flightTimer = 0f;
        stoppedTimer = 0f;
        pickupCooldownTimer = 0f;

        holderColliders = newHolderColliders;

        SetHolderCollisionIgnored(true);

        body.simulated = true;
        projectile.HoldAt(holdPoint);

        PickedUp?.Invoke(this);

        return true;
    }

    /// <summary>
    /// Bavulu teslim edilmiş duruma getirir ve varsa taşıyan
    /// oyuncunun çantayı bırakabilmesi için haber verir.
    /// </summary>
    public void MarkAsDelivered()
    {
        if (IsDelivered)
        {
            return;
        }

        SetHolderCollisionIgnored(false);

        IsAvailable = false;
        IsHeld = false;
        IsDelivered = true;

        waitingToBecomeAvailable = false;
        flightTimer = 0f;
        stoppedTimer = 0f;
        pickupCooldownTimer = 0f;

        holderColliders = null;

        Delivered?.Invoke(this);
    }

    public bool TryThrow(
        Vector2 throwDirection,
        float throwImpulse,
        float spinImpulse)
    {
        if (
            !IsHeld ||
            projectile == null ||
            throwDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        throwDirection.Normalize();

        Rigidbody2D thrownBody =
        projectile.ReleaseForThrow();

        SetHolderCollisionIgnored(false);

        thrownBody.AddForce(
            throwDirection * throwImpulse,
            ForceMode2D.Impulse
        );

        thrownBody.AddTorque(
            spinImpulse,
            ForceMode2D.Impulse
        );

        IsHeld = false;
        IsAvailable = false;

        waitingToBecomeAvailable = true;
        flightTimer = 0f;
        stoppedTimer = 0f;
        pickupCooldownTimer = pickupCooldownAfterThrow;

        holderColliders = null;

        return true;
    }

    private void UpdatePickupCooldown()
    {
        if (pickupCooldownTimer <= 0f)
        {
            return;
        }

        pickupCooldownTimer = Mathf.Max(
            0f,
            pickupCooldownTimer - Time.fixedDeltaTime
        );
    }

    private void UpdateReturnToGroundState()
    {
        if (!waitingToBecomeAvailable)
        {
            return;
        }

        // Rokete ulaşan bavulun fiziği RocketTarget tarafından
        // kapatılır. Böyle bir bavul tekrar alınabilir yapılmaz.
        if (
            body == null ||
            !body.simulated ||
            body.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        flightTimer += Time.fixedDeltaTime;

        if (flightTimer < minimumFlightTime)
        {
            return;
        }

        float thresholdSquared =
        stoppedSpeedThreshold *
        stoppedSpeedThreshold;

        bool isStopped =
        body.IsSleeping() ||
        body.linearVelocity.sqrMagnitude <=
        thresholdSquared;

        if (isStopped)
        {
            stoppedTimer += Time.fixedDeltaTime;
        }
        else
        {
            stoppedTimer = 0f;
        }

        if (
            stoppedTimer <
            stoppedDurationRequired)
        {
            return;
        }

        waitingToBecomeAvailable = false;
        IsAvailable = true;

        flightTimer = 0f;
        stoppedTimer = 0f;

        ReturnedToGround?.Invoke(this);
    }

    private void SetHolderCollisionIgnored(
        bool ignoreCollision)
    {
        if (
            bagColliders == null ||
            holderColliders == null)
        {
            return;
        }

        foreach (Collider2D bagCollider in bagColliders)
        {
            if (bagCollider == null)
            {
                continue;
            }

            foreach (
                Collider2D holderCollider
                in holderColliders)
            {
                if (holderCollider == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(
                    bagCollider,
                    holderCollider,
                    ignoreCollision
                );
            }
        }
    }

    private void OnDestroy()
    {
        SetHolderCollisionIgnored(false);
    }
}
