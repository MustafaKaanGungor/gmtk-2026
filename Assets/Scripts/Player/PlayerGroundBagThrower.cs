using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(10)]
[RequireComponent(typeof(PlayerMovement2D))]
[DisallowMultipleComponent]
public class PlayerGroundBagThrower : MonoBehaviour
{
    [Header("Hold Points")]
    [Tooltip("Karakter sola bakarken bavulun duracağı nokta.")]
    [SerializeField] private Transform leftHoldPoint;

    [Tooltip("Karakter sağa bakarken bavulun duracağı nokta.")]
    [SerializeField] private Transform rightHoldPoint;

    [Header("Alien Power Visual")]
    [Tooltip("Animasyonu taşıyan ana nesne. Animator tercihen bunun çocuğunda olmalı.")]
    [SerializeField] private GameObject alienPowerVisualRoot;

    [Header("Alien Power Flip")]
    [Tooltip("Karakter sola bakarken animasyonu X ekseninde ters çevirir.")]
    [SerializeField] private bool flipPowerOnLeft = false;

    [Tooltip("Karakter sağa bakarken animasyonu X ekseninde ters çevirir.")]
    [SerializeField] private bool flipPowerOnRight = true;

    [Tooltip("Animasyonun normal ölçeği.")]
    [SerializeField] private Vector3 powerVisualScale = Vector3.one;

    [Tooltip("Karakter sola bakarken animasyonun duracağı nokta.")]
    [SerializeField] private Transform leftPowerPoint;

    [Tooltip("Karakter sağa bakarken animasyonun duracağı nokta.")]
    [SerializeField] private Transform rightPowerPoint;

    [Header("Other References")]
    [SerializeField] private Camera gameplayCamera;

    [Header("Pickup")]
    [Min(0.1f)]
    [SerializeField] private float pickupRadius = 1.5f;

    [Tooltip(
        "Mouse'un bir bavulu hedeflemesi için collider'a en fazla " +
        "ne kadar yakın olması gerektiği."
    )]
    [Min(0.05f)]
    [SerializeField] private float mouseSelectionRadius = 0.75f;

    [Tooltip(
        "Oyuncu alma tuşuna bavul menzile girmeden hemen önce basarsa " +
        "girdiyi bu süre boyunca saklar."
    )]
    [Min(0f)]
    [SerializeField] private float catchInputBufferDuration = 0.12f;

    [SerializeField] private LayerMask bagLayers = ~0;

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
    private float catchInputBufferTimer;
    private Vector2 bufferedPointerWorldPosition;
    private bool hasBufferedPointerPosition;

    public GroundBagPickup NearbyBag => nearbyBag;
    public GroundBagPickup HeldBag => heldBag;
    public bool HasBag => heldBag != null;

    public event Action<GroundBagPickup> BagPickedUp;
    public event Action<GroundBagPickup> BagThrown;

    private Transform CurrentHoldPoint
    {
        get
        {
            if (movement != null && movement.FacingDirection > 0)
            {
                return rightHoldPoint;
            }

            return leftHoldPoint;
        }
    }

    private Transform CurrentPowerPoint
    {
        get
        {
            if (movement != null && movement.FacingDirection > 0)
            {
                return rightPowerPoint;
            }

            return leftPowerPoint;
        }
    }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement2D>();

        playerColliders =
            GetComponentsInChildren<Collider2D>();

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        bagFilter = new ContactFilter2D();
        bagFilter.SetLayerMask(bagLayers);
        bagFilter.useTriggers = true;

        if (alienPowerVisualRoot != null)
        {
            alienPowerVisualRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (heldBag == null)
        {
            return;
        }

        if (heldBag.IsDelivered)
        {
            heldBag = null;
            RefreshDirectionObjects();
            return;
        }

        heldBag.Delivered -= HandleHeldBagDelivered;
        heldBag.Delivered += HandleHeldBagDelivered;
    }

    private void OnDisable()
    {
        if (heldBag != null)
        {
            heldBag.Delivered -= HandleHeldBagDelivered;
        }
    }

    private void Update()
    {
        RefreshDirectionObjects();

        bool actionPressed = ActionPressed();

        if (heldBag != null)
        {
            nearbyBag = null;
            ClearCatchInputBuffer();

            if (actionPressed)
            {
                TryThrowHeldBag();
            }

            return;
        }

        if (actionPressed)
        {
            hasBufferedPointerPosition =
                TryGetPointerWorldPosition(
                    out bufferedPointerWorldPosition
                );

            catchInputBufferTimer =
                catchInputBufferDuration;

            FindBestBagForPointer(
                bufferedPointerWorldPosition,
                hasBufferedPointerPosition
            );

            if (TryPickUpNearbyBag())
            {
                ClearCatchInputBuffer();
                return;
            }
        }

        if (catchInputBufferTimer > 0f)
        {
            FindBestBagForPointer(
                bufferedPointerWorldPosition,
                hasBufferedPointerPosition
            );

            catchInputBufferTimer = Mathf.Max(
                0f,
                catchInputBufferTimer - Time.deltaTime
            );

            if (TryPickUpNearbyBag())
            {
                ClearCatchInputBuffer();
            }

            return;
        }

        bool hasLivePointerPosition =
            TryGetPointerWorldPosition(
                out Vector2 livePointerWorldPosition
            );

        FindBestBagForPointer(
            livePointerWorldPosition,
            hasLivePointerPosition
        );
    }

    private void LateUpdate()
    {
        // PlayerMovement2D yönü değiştirdikten sonra
        // bavul ve efektin doğru tarafta olduğundan emin olur.
        RefreshDirectionObjects();
    }

    private void FindBestBagForPointer(
        Vector2 pointerWorldPosition,
        bool hasPointerPosition)
    {
        Transform holdPoint = CurrentHoldPoint;

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
        float bestPointerDistanceSquared =
            float.PositiveInfinity;
        float bestPlayerDistanceSquared =
            float.PositiveInfinity;

        float maximumPointerDistanceSquared =
            mouseSelectionRadius *
            mouseSelectionRadius;

        for (int index = 0; index < resultCount; index++)
        {
            Collider2D result = nearbyResults[index];

            if (result == null)
            {
                continue;
            }

            GroundBagPickup candidate =
                result.GetComponentInParent<GroundBagPickup>();

            if (candidate == null || !candidate.CanBePickedUp)
            {
                continue;
            }

            Vector2 closestPointToPlayer =
                result.ClosestPoint(searchPosition);

            float playerDistanceSquared =
                (
                    closestPointToPlayer -
                    searchPosition
                ).sqrMagnitude;

            float pointerDistanceSquared = 0f;

            if (hasPointerPosition)
            {
                Vector2 closestPointToPointer =
                    result.ClosestPoint(
                        pointerWorldPosition
                    );

                pointerDistanceSquared =
                    (
                        closestPointToPointer -
                        pointerWorldPosition
                    ).sqrMagnitude;

                if (
                    pointerDistanceSquared >
                    maximumPointerDistanceSquared)
                {
                    continue;
                }
            }

            bool isBetterPointerMatch =
                pointerDistanceSquared <
                bestPointerDistanceSquared;

            bool isSamePointerDistance =
                Mathf.Approximately(
                    pointerDistanceSquared,
                    bestPointerDistanceSquared
                );

            bool isBetterPlayerMatch =
                playerDistanceSquared <
                bestPlayerDistanceSquared;

            if (
                !isBetterPointerMatch &&
                !(isSamePointerDistance &&
                    isBetterPlayerMatch))
            {
                continue;
            }

            closestBag = candidate;
            bestPointerDistanceSquared =
                pointerDistanceSquared;
            bestPlayerDistanceSquared =
                playerDistanceSquared;
        }

        nearbyBag = closestBag;
    }

    private void ClearCatchInputBuffer()
    {
        catchInputBufferTimer = 0f;
        hasBufferedPointerPosition = false;
        bufferedPointerWorldPosition = Vector2.zero;
    }

    private bool TryPickUpNearbyBag()
    {
        if (nearbyBag == null)
        {
            return false;
        }

        Transform holdPoint = CurrentHoldPoint;

        if (holdPoint == null)
        {
            Debug.LogWarning(
                "Aktif HoldPoint atanmadığı için bavul alınamadı.",
                this
            );

            return false;
        }

        GroundBagPickup selectedBag = nearbyBag;

        if (!selectedBag.TryHold(
                holdPoint,
                playerColliders))
        {
            nearbyBag = null;
            return false;
        }

        heldBag = selectedBag;
        heldBag.Delivered += HandleHeldBagDelivered;
        nearbyBag = null;

        RefreshDirectionObjects();

        BagPickedUp?.Invoke(heldBag);

        return true;
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

        if (!thrownBag.TryThrow(
                throwDirection,
                throwImpulse,
                spinImpulse))
        {
            return;
        }

        thrownBag.Delivered -= HandleHeldBagDelivered;
        heldBag = null;

        RefreshDirectionObjects();

        BagThrown?.Invoke(thrownBag);
    }

    private void HandleHeldBagDelivered(
        GroundBagPickup deliveredBag)
    {
        if (deliveredBag == null || heldBag != deliveredBag)
        {
            return;
        }

        deliveredBag.Delivered -= HandleHeldBagDelivered;

        heldBag = null;
        nearbyBag = null;

        RefreshDirectionObjects();
    }

    private Vector2 CalculateThrowDirection()
    {
        Transform holdPoint = CurrentHoldPoint;

        Vector2 throwOrigin =
            holdPoint != null
                ? holdPoint.position
                : transform.position;

        if (
            aimAtMouse &&
            TryGetPointerWorldPosition(
                out Vector2 pointerWorldPosition)
        )
        {
            Vector2 mouseDirection =
                pointerWorldPosition -
                throwOrigin;

            if (mouseDirection.sqrMagnitude > 0.001f)
            {
                return mouseDirection.normalized;
            }
        }

        return new Vector2(
            movement.FacingDirection,
            keyboardUpwardBias
        ).normalized;
    }

    private void RefreshDirectionObjects()
    {
        Transform targetHoldPoint = CurrentHoldPoint;

        /*
         * Karakter yön değiştirdiyse eldeki bavulu
         * diğer HoldPoint'in altına taşır.
         */
        if (
            heldBag != null &&
            targetHoldPoint != null &&
            heldBag.transform.parent != targetHoldPoint
        )
        {
            heldBag.transform.SetParent(
                targetHoldPoint,
                false
            );

            heldBag.transform.localPosition =
                Vector3.zero;

            heldBag.transform.localRotation =
                Quaternion.identity;
        }

        if (alienPowerVisualRoot == null)
        {
            return;
        }

        Transform targetPowerPoint =
            CurrentPowerPoint;

        bool shouldShowPower =
            heldBag != null &&
            targetPowerPoint != null;

        if (!shouldShowPower)
        {
            if (alienPowerVisualRoot.activeSelf)
            {
                alienPowerVisualRoot.SetActive(false);
            }

            return;
        }

        Transform visualTransform =
            alienPowerVisualRoot.transform;

        if (visualTransform.parent != targetPowerPoint)
        {
            visualTransform.SetParent(
                targetPowerPoint,
                false
            );
        
            visualTransform.localPosition =
                Vector3.zero;
        
            visualTransform.localRotation =
                Quaternion.identity;
        }
        
        UpdatePowerVisualScale();
        
        if (!alienPowerVisualRoot.activeSelf)
        {
            alienPowerVisualRoot.SetActive(true);
        }
    }

    private void UpdatePowerVisualScale()
    {
        if (alienPowerVisualRoot == null)
        {
            return;
        }

        bool facingRight =
            movement != null &&
            movement.FacingDirection > 0;

        bool shouldFlip =
            facingRight
                ? flipPowerOnRight
                : flipPowerOnLeft;

        Vector3 finalScale = powerVisualScale;

        finalScale.x =
            Mathf.Abs(finalScale.x) *
            (shouldFlip ? -1f : 1f);

        alienPowerVisualRoot.transform.localScale =
            finalScale;
    }

    private bool ActionPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool keyboardPressed =
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;

        bool mousePressed =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

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

    private bool TryGetPointerWorldPosition(
        out Vector2 pointerWorldPosition)
    {
        if (
            gameplayCamera == null ||
            !TryGetPointerPosition(
                out Vector2 pointerScreenPosition))
        {
            pointerWorldPosition = Vector2.zero;
            return false;
        }

        pointerWorldPosition =
            gameplayCamera.ScreenToWorldPoint(
                pointerScreenPosition
            );

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (leftHoldPoint != null)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawWireSphere(
                leftHoldPoint.position,
                pickupRadius
            );
        }

        if (rightHoldPoint != null)
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawWireSphere(
                rightHoldPoint.position,
                pickupRadius
            );
        }
    }
}
