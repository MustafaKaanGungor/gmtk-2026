using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class RocketMovement2D : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Screen Limits")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Roket ile ekran kenarı arasında bırakılacak boşluk.")]
    [Min(0f)]
    [SerializeField] private float screenPadding = 0.15f;

    [Header("State")]
    [SerializeField] private bool canMove = true;

    private Rigidbody2D rocketRigidbody;
    private Collider2D rocketCollider;

    private Vector2 movementInput;

    private float minimumX;
    private float maximumX;
    private float minimumY;
    private float maximumY;

    private float leftExtent;
    private float rightExtent;
    private float bottomExtent;
    private float topExtent;

    private int cachedScreenWidth;
    private int cachedScreenHeight;

    private void Awake()
    {
        rocketRigidbody = GetComponent<Rigidbody2D>();
        rocketCollider = GetComponent<Collider2D>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError(
                "RocketMovement2D: Main Camera bulunamadı.",
                this
            );

            enabled = false;
            return;
        }

        CalculateScreenLimits();
    }

    private void Update()
    {
        CheckForScreenSizeChange();

        if (!canMove)
        {
            movementInput = Vector2.zero;
            return;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        movementInput = new Vector2(
            horizontalInput,
            verticalInput
        );

        // Çapraz giderken daha hızlı hareket etmesini engeller.
        if (movementInput.sqrMagnitude > 1f)
        {
            movementInput.Normalize();
        }
    }

    private void FixedUpdate()
    {
        if (!canMove)
        {
            return;
        }

        Vector2 nextPosition =
            rocketRigidbody.position
            + movementInput * moveSpeed * Time.fixedDeltaTime;

        nextPosition.x = Mathf.Clamp(
            nextPosition.x,
            minimumX,
            maximumX
        );

        nextPosition.y = Mathf.Clamp(
            nextPosition.y,
            minimumY,
            maximumY
        );

        rocketRigidbody.MovePosition(nextPosition);
    }

    private void CalculateScreenLimits()
    {
        Physics2D.SyncTransforms();

        Bounds colliderBounds = rocketCollider.bounds;
        Vector2 rocketPosition = rocketRigidbody.position;

        // Roketin merkezinden collider kenarlarına olan mesafeler.
        leftExtent =
            rocketPosition.x - colliderBounds.min.x;

        rightExtent =
            colliderBounds.max.x - rocketPosition.x;

        bottomExtent =
            rocketPosition.y - colliderBounds.min.y;

        topExtent =
            colliderBounds.max.y - rocketPosition.y;

        float cameraDistance = Mathf.Abs(
            targetCamera.transform.position.z
            - transform.position.z
        );

        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, cameraDistance)
        );

        Vector3 topRight = targetCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, cameraDistance)
        );

        minimumX =
            bottomLeft.x + leftExtent + screenPadding;

        maximumX =
            topRight.x - rightExtent - screenPadding;

        minimumY =
            bottomLeft.y + bottomExtent + screenPadding;

        maximumY =
            topRight.y - topExtent - screenPadding;

        cachedScreenWidth = Screen.width;
        cachedScreenHeight = Screen.height;

        ValidateScreenLimits();
    }

    private void CheckForScreenSizeChange()
    {
        if (Screen.width == cachedScreenWidth
            && Screen.height == cachedScreenHeight)
        {
            return;
        }

        CalculateScreenLimits();
    }

    private void ValidateScreenLimits()
    {
        if (minimumX > maximumX || minimumY > maximumY)
        {
            Debug.LogError(
                "RocketMovement2D: Roket collider'ı kamera alanından büyük "
                + "veya Screen Padding değeri çok yüksek.",
                this
            );
        }
    }

    public void SetMovementEnabled(bool isEnabled)
    {
        canMove = isEnabled;

        if (!canMove)
        {
            movementInput = Vector2.zero;
            rocketRigidbody.linearVelocity = Vector2.zero;
        }
    }

    [ContextMenu("Recalculate Screen Limits")]
    private void RecalculateScreenLimits()
    {
        CalculateScreenLimits();
    }
}