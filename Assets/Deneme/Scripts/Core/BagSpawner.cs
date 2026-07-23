using UnityEngine;

[DisallowMultipleComponent]
public class BagSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BagThrower bagThrower;

    [Header("Bag Stock")]
    [Min(1)]
    [SerializeField] private int totalBagCount = 10;

    private int usedBagCount;
    private bool hasPreparedBag;

    public int TotalBagCount => totalBagCount;
    public int UsedBagCount => usedBagCount;

    public int RemainingBagCount =>
        Mathf.Max(0, totalBagCount - usedBagCount);

    public bool HasRemainingBags =>
        usedBagCount < totalBagCount;

    public bool HasPreparedBag =>
        hasPreparedBag;

    private void Awake()
    {
        if (bagThrower == null)
        {
            Debug.LogError(
                "BagSpawner: BagThrower atanmamış.",
                this
            );
        }
    }

    /// <summary>
    /// Çanta stok sistemini başlangıç durumuna döndürür.
    /// </summary>
    public void ResetSpawner()
    {
        if (bagThrower != null)
        {
            bagThrower.RemoveHeldBag();
        }

        usedBagCount = 0;
        hasPreparedBag = false;
    }

    /// <summary>
    /// Oyuncunun elinde yeni bir çanta hazırlar.
    /// </summary>
    public bool TryPrepareNextBag(
        out BagProjectile preparedBag)
    {
        preparedBag = null;

        if (bagThrower == null)
        {
            Debug.LogError(
                "BagSpawner: BagThrower referansı eksik.",
                this
            );

            return false;
        }

        if (!HasRemainingBags)
        {
            Debug.Log(
                "BagSpawner: Kullanılabilecek çanta kalmadı.",
                this
            );

            return false;
        }

        if (hasPreparedBag)
        {
            preparedBag = bagThrower.CurrentBag;

            return preparedBag != null;
        }

        preparedBag = bagThrower.SpawnBag();

        if (preparedBag == null)
        {
            Debug.LogError(
                "BagSpawner: Yeni çanta hazırlanamadı.",
                this
            );

            return false;
        }

        hasPreparedBag = true;

        return true;
    }

    /// <summary>
    /// Hazırlanan çanta fırlatıldığında çağrılır.
    /// Kalan çanta sayısını bir azaltır.
    /// </summary>
    public void MarkPreparedBagAsUsed()
    {
        if (!hasPreparedBag)
        {
            Debug.LogWarning(
                "BagSpawner: Kullanılmış sayılacak " +
                "hazır bir çanta bulunamadı.",
                this
            );

            return;
        }

        usedBagCount++;
        hasPreparedBag = false;
    }

    /// <summary>
    /// Henüz fırlatılmamış çantayı iptal eder.
    /// </summary>
    public void CancelPreparedBag()
    {
        if (!hasPreparedBag)
        {
            return;
        }

        if (bagThrower != null)
        {
            bagThrower.RemoveHeldBag();
        }

        hasPreparedBag = false;
    }
}