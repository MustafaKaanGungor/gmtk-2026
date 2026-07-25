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

    private void Update()
    {
        RefreshDirectionObjects();

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

    private void LateUpdate()
    {
        // PlayerMovement2D yönü değiştirdikten sonra
        // bavul ve efektin doğru tarafta olduğundan emin olur.
        RefreshDirectionObjects();
    }

    private void FindNearestBag()
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
                result.GetComponentInParent<GroundBagPickup>();

            if (candidate == null || !candidate.IsAvailable)
            {
                continue;
            }

            float distanceSquared =
                (
                    (Vector2)candidate.transform.position -
                    searchPosition
                ).sqrMagnitude;

            if (distanceSquared >= closestDistanceSquared)
            {
                continue;
            }

            closestBag = candidate;
            closestDistanceSquared = distanceSquared;
        }

        nearbyBag = closestBag;
    }

    private void TryPickUpNearbyBag()
    {
        if (nearbyBag == null)
        {
            return;
        }

        Transform holdPoint = CurrentHoldPoint;

        if (holdPoint == null)
        {
            Debug.LogWarning(
                "Aktif HoldPoint atanmadığı için bavul alınamadı.",
                this
            );

            return;
        }

        GroundBagPickup selectedBag = nearbyBag;

        if (!selectedBag.TryHold(
                holdPoint,
                playerColliders))
        {
            return;
        }

        heldBag = selectedBag;
        nearbyBag = null;

        RefreshDirectionObjects();

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

        if (!thrownBag.TryThrow(
                throwDirection,
                throwImpulse,
                spinImpulse))
        {
            return;
        }

        heldBag = null;

        RefreshDirectionObjects();

        BagThrown?.Invoke(thrownBag);
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
            gameplayCamera != null &&
            TryGetPointerPosition(
                out Vector2 pointerScreenPosition)
        )
        {
            Vector3 pointerWorldPosition =
                gameplayCamera.ScreenToWorldPoint(
                    pointerScreenPosition
                );

            Vector2 mouseDirection =
                (Vector2)pointerWorldPosition -
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