using UnityEngine;

[DisallowMultipleComponent]
public class BagThrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BagProjectile bagPrefab;
    [SerializeField] private Transform throwPoint;

    [Header("Throw Settings")]
    [SerializeField] private float minimumSpinImpulse = -2f;
    [SerializeField] private float maximumSpinImpulse = 2f;

    private BagProjectile currentBag;

    public BagProjectile CurrentBag => currentBag;
    public bool HasBag => currentBag != null;

    private void Awake()
    {
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (bagPrefab == null)
        {
            Debug.LogError("BagThrower: Bag Prefab atanmamış.", this);
        }

        if (throwPoint == null)
        {
            Debug.LogError("BagThrower: Throw Point atanmamış.", this);
        }
    }

    /// <summary>
    /// ThrowPoint üzerinde yeni bir çanta oluşturur.
    /// </summary>
    public BagProjectile SpawnBag()
    {
        if (bagPrefab == null || throwPoint == null)
        {
            Debug.LogError("BagThrower: Çanta oluşturmak için gereken referanslar eksik.", this);
            return null;
        }

        if (currentBag != null)
        {
            Debug.LogWarning("BagThrower: Zaten elde bir çanta var.", this);
            return currentBag;
        }

        currentBag = Instantiate(bagPrefab, throwPoint.position, throwPoint.rotation);
        currentBag.HoldAt(throwPoint);
        
        return currentBag;
    }

    /// <summary>
    /// Elde bulunan çantayı verilen yön ve kuvvetle fırlatır.
    /// </summary>
    public BagProjectile ThrowBag(Vector2 throwDirection, float throwPower)
    {
        if (currentBag == null)
        {
            Debug.LogWarning("BagThrower: Fırlatılacak çanta bulunamadı.", this);
            return null;
        }

        if (throwDirection.sqrMagnitude <= 0.001f)
        {
            Debug.LogWarning("BagThrower: Atış yönü geçersiz.", this);
            return null;
        }

        throwDirection.Normalize();

        BagProjectile thrownBag = currentBag;
        currentBag = null;

        Rigidbody2D bagBody = thrownBag.ReleaseForThrow();
        bagBody.AddForce(throwDirection * throwPower, ForceMode2D.Impulse);

        float spinImpulse = Random.Range(minimumSpinImpulse, maximumSpinImpulse);
        bagBody.AddTorque(spinImpulse, ForceMode2D.Impulse);

        return thrownBag;
    }

    /// <summary>
    /// Elde duran çantayı yok eder.
    /// Test veya bölüm sıfırlamasında kullanılabilir.
    /// </summary>
    public void RemoveHeldBag()
    {
        if (currentBag == null)
        {
            return;
        }

        Destroy(currentBag.gameObject);
        currentBag = null;
    }
}