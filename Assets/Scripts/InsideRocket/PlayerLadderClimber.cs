using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerLadderClimber : MonoBehaviour
{
    [Header("References")]

    [Tooltip("Oyuncunun normal hareket scriptini buraya sürükle.")]
    [SerializeField]
    private MonoBehaviour normalMovementScript;

    [Tooltip("Üst katın BoxCollider2D bileşenini buraya sürükle.")]
    [SerializeField]
    private Collider2D upperPlatformCollider;

    [Header("Ladder Movement")]

    [SerializeField]
    private float climbSpeed = 4f;

    [SerializeField]
    private float horizontalSpeedOnLadder = 2.5f;

    private Rigidbody2D rb;
    private Collider2D[] playerColliders;

    private float normalGravityScale;

    private int ladderContactCount;
    private bool isInsideLadder;

    public bool IsClimbing { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Player ve child objelerindeki colliderları alır.
        playerColliders = GetComponentsInChildren<Collider2D>();

        normalGravityScale = rb.gravityScale;
    }

    private void Update()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Oyuncu merdiven alanındayken yukarı veya aşağı basarsa
        // tırmanma moduna girer.
        if (isInsideLadder &&
            !IsClimbing &&
            Mathf.Abs(verticalInput) > 0.01f)
        {
            StartClimbing();
        }
    }

    private void FixedUpdate()
    {
        if (!IsClimbing)
            return;

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        rb.linearVelocity = new Vector2(
            horizontalInput * horizontalSpeedOnLadder,
            verticalInput * climbSpeed
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        LadderZone ladderZone =
            other.GetComponentInParent<LadderZone>();

        if (ladderZone == null)
            return;

        ladderContactCount++;
        isInsideLadder = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        LadderZone ladderZone =
            other.GetComponentInParent<LadderZone>();

        if (ladderZone == null)
            return;

        ladderContactCount = Mathf.Max(
            0,
            ladderContactCount - 1
        );

        isInsideLadder = ladderContactCount > 0;

        if (!isInsideLadder)
        {
            StopClimbing();
        }
    }

    private void StartClimbing()
    {
        if (IsClimbing)
            return;

        IsClimbing = true;

        // Yer çekimini kapat.
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        // Normal hareket scriptinin ladder hareketiyle
        // çakışmasını önle.
        if (normalMovementScript != null)
        {
            normalMovementScript.enabled = false;
        }

        // Tek yönlü üst platformu yalnızca bu oyuncu için
        // geçici olarak yok say.
        SetUpperPlatformCollisionIgnored(true);
    }

    private void StopClimbing()
    {
        if (!IsClimbing)
            return;

        IsClimbing = false;

        // Üst platform çarpışmasını geri aç.
        SetUpperPlatformCollisionIgnored(false);

        // Yer çekimini geri aç.
        rb.gravityScale = normalGravityScale;

        // Merdivenden çıkınca dikey hareketi durdur.
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            0f
        );

        if (normalMovementScript != null)
        {
            normalMovementScript.enabled = true;
        }
    }

    private void SetUpperPlatformCollisionIgnored(bool ignore)
    {
        if (upperPlatformCollider == null)
            return;

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider == null)
                continue;

            if (playerCollider.isTrigger)
                continue;

            Physics2D.IgnoreCollision(
                playerCollider,
                upperPlatformCollider,
                ignore
            );
        }
    }

    private void OnDisable()
    {
        // Sahne kapanırken veya script devre dışı kalırken
        // ayarların bozuk kalmasını engeller.
        SetUpperPlatformCollisionIgnored(false);

        if (rb != null)
        {
            rb.gravityScale = normalGravityScale;
        }

        if (normalMovementScript != null)
        {
            normalMovementScript.enabled = true;
        }

        IsClimbing = false;
    }
}