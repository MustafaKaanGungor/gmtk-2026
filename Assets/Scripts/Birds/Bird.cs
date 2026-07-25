using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bird : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Hazırlanan kuş görseli normal hâlinde sağa bakıyorsa açık bırak.")]
    [SerializeField] private bool spriteFacesRight = true;

    [SerializeField] private SpriteRenderer spriteRenderer;

    private BirdSpawner ownerSpawner;
    private Rigidbody2D rb;
    private Camera targetCamera;

    private Vector2 moveDirection;
    private float moveSpeed;
    private float destroyMargin;

    private bool ownerNotified;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // Kuş yerçekiminden etkilenmeyecek.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void Initialize(
        BirdSpawner spawner,
        Camera cameraToUse,
        Vector2 direction,
        float speed,
        float screenDestroyMargin)
    {
        ownerSpawner = spawner;
        targetCamera = cameraToUse;
        moveDirection = direction.normalized;
        moveSpeed = speed;
        destroyMargin = screenDestroyMargin;

        UpdateSpriteDirection();
    }

    private void FixedUpdate()
    {
        Vector2 newPosition =
            rb.position +
            moveDirection * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(newPosition);
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        float cameraDistance =
            Mathf.Abs(targetCamera.transform.position.z - transform.position.z);

        float leftEdge = targetCamera.ViewportToWorldPoint(
            new Vector3(0f, 0.5f, cameraDistance)
        ).x;

        float rightEdge = targetCamera.ViewportToWorldPoint(
            new Vector3(1f, 0.5f, cameraDistance)
        ).x;

        bool leftScreenFromRight =
            moveDirection.x > 0f &&
            transform.position.x > rightEdge + destroyMargin;

        bool leftScreenFromLeft =
            moveDirection.x < 0f &&
            transform.position.x < leftEdge - destroyMargin;

        if (leftScreenFromRight || leftScreenFromLeft)
        {
            RemoveBird();
        }
    }

    private void UpdateSpriteDirection()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        bool movingRight = moveDirection.x > 0f;

        if (spriteFacesRight)
        {
            spriteRenderer.flipX = !movingRight;
        }
        else
        {
            spriteRenderer.flipX = movingRight;
        }
    }

    private void RemoveBird()
    {
        NotifySpawner();

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Kuş başka bir nedenle silinirse sayaç yine güncellensin.
        NotifySpawner();
    }

    private void NotifySpawner()
    {
        if (ownerNotified)
        {
            return;
        }

        ownerNotified = true;

        if (ownerSpawner != null)
        {
            ownerSpawner.NotifyBirdDestroyed();
        }
    }
}