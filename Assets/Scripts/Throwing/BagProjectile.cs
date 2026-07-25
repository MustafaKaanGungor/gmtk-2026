using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class BagProjectile : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float thrownGravityScale = 1.5f;

    private Rigidbody2D body;

    public Rigidbody2D Body => body;

    public bool IsThrown { get; private set; }

    /// <summary>Bavulun en son firlatildigi (elden birakildigi) konum.</summary>
    public Vector3 ThrowOrigin { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Çantayı oyuncunun eline veya ThrowPoint'e yerleştirir.
    /// </summary>
    public void HoldAt(Transform holdPoint)
    {
        if (holdPoint == null)
        {
            Debug.LogError(
                "BagProjectile: HoldPoint atanmamış.",
                this
            );

            return;
        }

        IsThrown = false;

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        ClearMotion();

        transform.SetParent(holdPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Çantayı elden bırakıp fiziğe hazır hale getirir.
    /// Henüz kuvvet uygulamaz.
    /// </summary>
    public Rigidbody2D ReleaseForThrow()
    {
        transform.SetParent(null, true);

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = thrownGravityScale;

        ClearMotion();

        // Firlatma anindaki konumu kaydet; yanlis kapiya girerse buraya doner.
        ThrowOrigin = transform.position;

        IsThrown = true;

        return body;
    }

    /// <summary>
    /// Bavulu en son firlatildigi konuma isinlar ve hareketini durdurur.
    /// </summary>
    public void ReturnToThrowOrigin()
    {
        ClearMotion();

        transform.position = ThrowOrigin;
    }

    /// <summary>
    /// Bavulun mevcut hiz vektorunu tersine cevirir (geldigi yone geri firlatir).
    /// multiplier ile hizi olceklendirebilirsin (1 = ayni hizla geri).
    /// </summary>
    public void ReverseVelocity(float multiplier = 1f)
    {
        body.linearVelocity = -body.linearVelocity * multiplier;
        body.angularVelocity = -body.angularVelocity;
    }

    /// <summary>
    /// Çantanın bütün hareketini durdurur.
    /// </summary>
    public void StopMotion()
    {
        ClearMotion();
    }

    /// <summary>
    /// Çantayı bir hedef noktasına sabitler.
    /// Rokete başarıyla girince kullanılabilir.
    /// </summary>
    public void AttachTo(Transform target)
    {
        if (target == null)
        {
            return;
        }

        IsThrown = false;

        ClearMotion();

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        transform.SetParent(target);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void ClearMotion()
    {
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }
}