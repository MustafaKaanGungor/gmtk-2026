using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float maximumSpeed = 6f;

    [Min(0f)]
    [SerializeField] private float groundAcceleration = 45f;

    [Min(0f)]
    [SerializeField] private float airAcceleration = 25f;

    [Min(0f)]
    [SerializeField] private float jumpImpulse = 9f;

    [Min(0f)]
    [SerializeField] private float gravityScale = 3f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayers = ~0;

    [Min(0.01f)]
    [SerializeField] private float groundCheckDistance = 0.12f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer characterVisual;

    [Tooltip("-1 sola, 1 sağa bakarak başlatır.")]
    [SerializeField] private int initialFacingDirection = -1;

    private readonly RaycastHit2D[] groundHits =
    new RaycastHit2D[8];

    private Rigidbody2D body;
    private Collider2D characterCollider;
    private ContactFilter2D groundFilter;

    private float horizontalInput;
    private bool jumpRequested;

    public int FacingDirection { get; private set; }
    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        characterCollider = GetComponent<Collider2D>();

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = gravityScale;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayers);
        groundFilter.useTriggers = false;

        FacingDirection =
        initialFacingDirection < 0 ? -1 : 1;

        if (characterVisual == null)
        {
            characterVisual =
            GetComponentInChildren<SpriteRenderer>();
        }

        ApplyFacingVisual();
    }

    private void Update()
    {
        horizontalInput = ReadHorizontalInput();

        if (!Mathf.Approximately(horizontalInput, 0f))
        {
            FacingDirection =
            horizontalInput < 0f ? -1 : 1;

            ApplyFacingVisual();
        }

        if (JumpPressed())
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        IsGrounded = CheckGrounded();

        float acceleration =
        IsGrounded
        ? groundAcceleration
        : airAcceleration;

        float targetHorizontalSpeed =
        horizontalInput * maximumSpeed;

        Vector2 velocity = body.linearVelocity;

        velocity.x = Mathf.MoveTowards(
            velocity.x,
            targetHorizontalSpeed,
            acceleration * Time.fixedDeltaTime
        );

        body.linearVelocity = velocity;

        if (jumpRequested && IsGrounded)
        {
            velocity = body.linearVelocity;
            velocity.y = 0f;
            body.linearVelocity = velocity;

            body.AddForce(
                Vector2.up * jumpImpulse,
                ForceMode2D.Impulse
            );
        }

        jumpRequested = false;
    }

    private bool CheckGrounded()
    {
        if (characterCollider == null)
        {
            return false;
        }

        int hitCount = characterCollider.Cast(
            Vector2.down,
            groundFilter,
            groundHits,
            groundCheckDistance
        );

        for (int index = 0; index < hitCount; index++)
        {
            Collider2D hitCollider =
            groundHits[index].collider;

            if (
                hitCollider == null ||
                hitCollider.isTrigger)
            {
                continue;
            }

            Transform hitTransform =
            hitCollider.transform;

            bool belongsToPlayer =
            hitTransform == transform ||
            hitTransform.IsChildOf(transform);

            if (belongsToPlayer)
            {
                continue;
            }

            GroundBagPickup groundBag =
            hitCollider.GetComponentInParent<
            GroundBagPickup>();

            if (
                groundBag != null &&
                groundBag.IsHeld)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void ApplyFacingVisual()
    {
        if (characterVisual == null)
        {
            return;
        }

        characterVisual.flipX =
        FacingDirection > 0;
    }

    private float ReadHorizontalInput()
    {
        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return 0f;
        }

        float input = 0f;

        if (
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            input -= 1f;
        }

        if (
            Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            input += 1f;
        }

        return Mathf.Clamp(input, -1f, 1f);
        #else
        return Input.GetAxisRaw("Horizontal");
        #endif
    }

    private bool JumpPressed()
    {
        #if ENABLE_INPUT_SYSTEM
        return
        Keyboard.current != null &&
        (
            Keyboard.current.spaceKey
            .wasPressedThisFrame ||
            Keyboard.current.wKey
            .wasPressedThisFrame ||
            Keyboard.current.upArrowKey
            .wasPressedThisFrame
        );
        #else
        return
        Input.GetKeyDown(KeyCode.Space) ||
        Input.GetKeyDown(KeyCode.W) ||
        Input.GetKeyDown(KeyCode.UpArrow);
        #endif
    }
}
