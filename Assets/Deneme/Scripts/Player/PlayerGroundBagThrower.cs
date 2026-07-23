using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(PlayerMovement2D))]
[DisallowMultipleComponent]
public class PlayerGroundBagThrower : MonoBehaviour
{
    [Header("References")]
    [Tooltip(
        "Boş bırakılırsa ThrowPoint isimli çocuk nesne aranır."
    )]
    [SerializeField] private Transform holdPoint;

    [SerializeField] private Camera gameplayCamera;

    [Header("Pickup")]
    [Min(0.1f)]
    [SerializeField] private float pickupRadius = 1.5f;

    [SerializeField] private LayerMask bagLayers = ~0;

    [Tooltip(
        "Karakter yön değiştirince HoldPoint'in X konumunu aynalar."
    )]
    [SerializeField] private bool mirrorHoldPoint = true;

    [Header("Throw")]
    [Min(0f)]
    [SerializeField] private float throwImpulse = 12f;

    [Range(0f, 1f)]
    [SerializeField] private float keyboardUpwardBias = 0.35f;

    [SerializeField] private bool aimAtMouse = true;

    [SerializeField] private float minimumSpinImpulse = -2f;
    [SerializeField] private float maximumSpinImpulse = 2f;

    private readonly Collider2D[] nearbyResults =
        new Collider2D[24];

    private PlayerMovement2D movement;
    private Collider2D[] playerColliders;
    private ContactFilter2D bagFilter;

    private GroundBagPickup nearbyBag;
    private GroundBagPickup heldBag;
    private float holdPointAbsoluteX;

    public GroundBagPickup NearbyBag => nearbyBag;
    public GroundBagPickup HeldBag => heldBag;
    public bool HasBag => heldBag != null;

    public event Action<GroundBagPickup> BagPickedUp;
    public event Action<GroundBagPickup> BagThrown;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement2D>();
        playerColliders =
            GetComponentsInChildren<Collider2D>();

        if (holdPoint == null)
        {
            Transform foundPoint =
                transform.Find("ThrowPoint");

            holdPoint =
                foundPoint != null
                    ? foundPoint
                    : transform;
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        holdPointAbsoluteX =
            Mathf.Abs(holdPoint.localPosition.x);

        bagFilter = new ContactFilter2D();
        bagFilter.SetLayerMask(bagLayers);
        bagFilter.useTriggers = true;
    }

    private void Update()
    {
        UpdateHoldPointDirection();

        if (heldBag == null)
        {
            FindNearestBag();
        }
        else
        {
            nearbyBag = null;
        }

        if (!ActionPressed())
        {
            return;
        }

        if (heldBag == null)
        {
            TryPickUpNearbyBag();
        }
        else
        {
            TryThrowHeldBag();
        }
    }

    private void FindNearestBag()
    {
        Vector2 searchPosition =
            holdPoint != null
                ? holdPoint.position
                : transform.position;

        int resultCount = Physics2D.OverlapCircle(
            searchPosition,
            pickupRadius,
            bagFilter,
            nearbyResults
        );

        GroundBagPickup closestBag = null;
        float closestDistanceSquared =
            float.PositiveInfinity;

        for (int index = 0; index < resultCount; index++)
        {
            Collider2D result = nearbyResults[index];

            if (result == null)
            {
                continue;
            }

            GroundBagPickup candidate =
                result.GetComponentInParent<
                    GroundBagPickup>();

            if (
                candidate == null ||
                !candidate.IsAvailable)
            {
                continue;
            }

            float distanceSquared =
                (
                    (Vector2)candidate
                        .transform.position -
                    searchPosition
                ).sqrMagnitude;

            if (
                distanceSquared >=
                closestDistanceSquared)
            {
                continue;
            }

            closestBag = candidate;
            closestDistanceSquared =
                distanceSquared;
        }

        nearbyBag = closestBag;
    }

    private void TryPickUpNearbyBag()
    {
        if (nearbyBag == null)
        {
            return;
        }

        GroundBagPickup selectedBag = nearbyBag;

        if (
            !selectedBag.TryHold(
                holdPoint,
                playerColliders))
        {
            return;
        }

        heldBag = selectedBag;
        nearbyBag = null;

        BagPickedUp?.Invoke(heldBag);
    }

    private void TryThrowHeldBag()
    {
        if (heldBag == null)
        {
            return;
        }

        Vector2 throwDirection =
            CalculateThrowDirection();

        float spinImpulse = UnityEngine.Random.Range(
            minimumSpinImpulse,
            maximumSpinImpulse
        );

        GroundBagPickup thrownBag = heldBag;

        if (
            !thrownBag.TryThrow(
                throwDirection,
                throwImpulse,
                spinImpulse))
        {
            return;
        }

        heldBag = null;
        BagThrown?.Invoke(thrownBag);
    }

    private Vector2 CalculateThrowDirection()
    {
        if (
            aimAtMouse &&
            gameplayCamera != null &&
            TryGetPointerPosition(
                out Vector2 pointerScreenPosition))
        {
            Vector3 pointerWorldPosition =
                gameplayCamera.ScreenToWorldPoint(
                    pointerScreenPosition
                );

            Vector2 mouseDirection =
                (Vector2)pointerWorldPosition -
                (Vector2)holdPoint.position;

            if (
                mouseDirection.sqrMagnitude >
                0.001f)
            {
                return mouseDirection.normalized;
            }
        }

        return new Vector2(
            movement.FacingDirection,
            keyboardUpwardBias
        ).normalized;
    }

    private void UpdateHoldPointDirection()
    {
        if (
            !mirrorHoldPoint ||
            holdPoint == null ||
            holdPoint == transform)
        {
            return;
        }

        Vector3 localPosition =
            holdPoint.localPosition;

        localPosition.x =
            holdPointAbsoluteX *
            movement.FacingDirection;

        holdPoint.localPosition = localPosition;
    }

    private bool ActionPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool keyboardPressed =
            Keyboard.current != null &&
            Keyboard.current.eKey
                .wasPressedThisFrame;

        bool mousePressed =
            Mouse.current != null &&
            Mouse.current.leftButton
                .wasPressedThisFrame;

        return keyboardPressed || mousePressed;
#else
        return
            Input.GetKeyDown(KeyCode.E) ||
            Input.GetMouseButtonDown(0);
#endif
    }

    private bool TryGetPointerPosition(
        out Vector2 pointerPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            pointerPosition = Vector2.zero;
            return false;
        }

        pointerPosition =
            Mouse.current.position.ReadValue();

        return true;
#else
        pointerPosition = Input.mousePosition;
        return true;
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center =
            holdPoint != null
                ? holdPoint.position
                : transform.position;

        Gizmos.color =
            nearbyBag != null
                ? Color.green
                : Color.yellow;

        Gizmos.DrawWireSphere(
            center,
            pickupRadius
        );
    }
}
