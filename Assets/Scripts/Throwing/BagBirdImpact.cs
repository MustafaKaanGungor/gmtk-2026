using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(GroundBagPickup))]
[DisallowMultipleComponent]
public class BagBirdImpact : MonoBehaviour
{
    [Header("Bird Impact")]

    [Tooltip(
        "Kuşa çarptığında çantanın aşağı yönlü başlangıç hızı. " +
        "Tam serbest düşme için 0 bırak."
    )]
    [Min(0f)]
    [SerializeField]
    private float initialDownwardSpeed = 0f;

    [Tooltip(
        "Kuşa çarptıktan sonra çantaya uygulanacak yerçekimi."
    )]
    [Min(0.01f)]
    [SerializeField]
    private float impactGravityScale = 2.5f;

    [Tooltip(
        "Açıksa çanta kuşa çarptığı X konumundan " +
        "dümdüz aşağı düşer."
    )]
    [SerializeField]
    private bool lockHorizontalPosition = true;

    [Tooltip(
        "Açıksa kuşa çarpınca çantanın dönüşü durur."
    )]
    [SerializeField]
    private bool freezeRotation = true;

    private Rigidbody2D body;
    private GroundBagPickup pickup;

    private RigidbodyConstraints2D normalConstraints;
    private float normalGravityScale;

    private bool wasHitByBird;

    public bool WasHitByBird => wasHitByBird;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        pickup = GetComponent<GroundBagPickup>();

        normalConstraints = body.constraints;
        normalGravityScale = body.gravityScale;
    }

    private void OnEnable()
    {
        if (pickup != null)
        {
            pickup.ReturnedToGround += HandleReturnedToGround;
        }

        ResetImpactState();
    }

    private void OnDisable()
    {
        if (pickup != null)
        {
            pickup.ReturnedToGround -= HandleReturnedToGround;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!CanBeHitByBird())
        {
            return;
        }

        Bird bird = other.GetComponentInParent<Bird>();

        if (bird == null)
        {
            return;
        }

        ApplyBirdImpact();
    }

    private bool CanBeHitByBird()
    {
        if (
            wasHitByBird ||
            body == null ||
            pickup == null)
        {
            return false;
        }

        // Elde tutulan çanta etkilenmesin.
        if (pickup.IsHeld)
        {
            return false;
        }

        // Yerde duran ve alınabilir olan çanta etkilenmesin.
        if (pickup.IsAvailable)
        {
            return false;
        }

        // Yalnızca fizik simülasyonu devam eden
        // Dynamic çantalar etkilenebilir.
        return
            body.simulated &&
            body.bodyType == RigidbodyType2D.Dynamic;
    }

    private void ApplyBirdImpact()
    {   
    wasHitByBird = true;

    // Yatay ve dikey fırlatma hızını söndür.
    body.linearVelocity = new Vector2(
        0f,
        -initialDownwardSpeed
    );

    body.gravityScale = impactGravityScale;

    RigidbodyConstraints2D impactConstraints =
        normalConstraints;

    if (lockHorizontalPosition)
    {
        impactConstraints |=
            RigidbodyConstraints2D.FreezePositionX;
    }

    if (freezeRotation)
    {
        // Tik açıksa dönüşü durdur ve kilitle.
        body.angularVelocity = 0f;

        impactConstraints |=
            RigidbodyConstraints2D.FreezeRotation;
    }
    else
    {
        // Tik kapalıysa dönüş kilidini kesin olarak kaldır.
        // Mevcut angularVelocity korunur.
        impactConstraints &=
            ~RigidbodyConstraints2D.FreezeRotation;
    }

    body.constraints = impactConstraints;
    body.WakeUp();
    }

    private void HandleReturnedToGround(
        GroundBagPickup returnedBag)
    {
        if (returnedBag != pickup)
        {
            return;
        }

        ResetImpactState();
    }

    private void ResetImpactState()
    {
        wasHitByBird = false;

        if (body == null)
        {
            return;
        }

        // Çanta yere indiğinde eski fizik ayarlarını geri getir.
        body.constraints = normalConstraints;
        body.gravityScale = normalGravityScale;
    }
}