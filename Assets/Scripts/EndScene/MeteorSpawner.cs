using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class MeteorSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Rastgele seçilecek meteor prefabları.")]
    [SerializeField] private GameObject[] meteorPrefabs;

    [SerializeField] private Camera targetCamera;

    [Tooltip("Üretilen meteorların Hierarchy içerisinde yerleştirileceği nesne.")]
    [SerializeField] private Transform meteorParent;

    [Header("Wave Layout")]
    [Tooltip("Ekranın yatay olarak kaç görünmez sütuna ayrılacağı.")]
    [Min(2)]
    [SerializeField] private int columnCount = 8;

    [Tooltip("Her dalgada yan yana kaç sütunun boş bırakılacağı.")]
    [Min(1)]
    [SerializeField] private int emptyColumnCount = 2;

    [Tooltip(
        "Güvenli boşluğun bir sonraki dalgada en fazla kaç sütun "
        + "sağa veya sola hareket edebileceği."
    )]
    [Min(0)]
    [SerializeField] private int maximumGapShiftPerWave = 1;

    [Tooltip(
        "Güvenli boşluk dışındaki her sütunda meteor oluşma ihtimali."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float meteorSpawnChance = 0.9f;

    [Header("Spawn Area")]
    [Tooltip("Meteorlarla ekranın sağ ve sol kenarı arasında bırakılacak mesafe.")]
    [Min(0f)]
    [SerializeField] private float horizontalPadding = 0.75f;

    [Tooltip("Meteorların ekranın üstünden ne kadar yukarıda oluşturulacağı.")]
    [Min(0f)]
    [SerializeField] private float spawnHeightPadding = 1.5f;

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float firstWaveDelay = 1f;

    [Min(0.05f)]
    [SerializeField] private float spawnInterval = 1.35f;

    [Header("Meteor Settings")]
    [Min(0f)]
    [SerializeField] private float meteorFallSpeed = 5f;

    [Header("State")]
    [SerializeField] private bool startAutomatically = true;

    [Header("Meteor Countdown")]
    [Tooltip("Başlangıçta gösterilecek toplam meteor dalgası sayısı.")]
    [Min(2)]
    [SerializeField] private int startingWaveCount = 12;

    [Tooltip("Sayaç bu değere geldiğinde final sekansı tetiklenir.")]
    [Min(0)]
    [SerializeField] private int finalTriggerCount = 1;

    [Tooltip(
        "Son dalga üretildikten sonra final sekansının başlaması "
        + "için beklenecek süre."
    )]
    [Min(0f)]
    [SerializeField] private float finalTriggerDelay = 2f;

    [SerializeField] private MeteorCountdownUI countdownUI;

    [Header("Final Event")]
    [SerializeField] private UnityEvent onFinalCountdownReached;

    private int remainingWaveCount;
    private bool finalTriggered;

    private Coroutine spawnCoroutine;

    // -1 değeri henüz herhangi bir boşluk seçilmediğini belirtir.
    private int previousGapStartColumn = -1;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        remainingWaveCount = Mathf.Max(
            startingWaveCount,
            finalTriggerCount + 1
        );

        finalTriggered = false;

        if (countdownUI != null)
        {
            countdownUI.SetCount(remainingWaveCount);
        }

        ValidateReferences();
    }   

    private void Start()
    {
        if (startAutomatically)
        {
            StartSpawning();
        }
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private void OnValidate()
    {
        columnCount = Mathf.Max(2, columnCount);

        // En az bir meteor sütunu kalması gerekiyor.
        emptyColumnCount = Mathf.Clamp(
            emptyColumnCount,
            1,
            columnCount - 1
        );

        maximumGapShiftPerWave = Mathf.Max(
            0,
            maximumGapShiftPerWave
        );

        spawnInterval = Mathf.Max(0.05f, spawnInterval);
        meteorFallSpeed = Mathf.Max(0f, meteorFallSpeed);
        horizontalPadding = Mathf.Max(0f, horizontalPadding);
        spawnHeightPadding = Mathf.Max(0f, spawnHeightPadding);

        startingWaveCount = Mathf.Max(
            2,
            startingWaveCount
        );

        finalTriggerCount = Mathf.Clamp(
            finalTriggerCount,
            0,
            startingWaveCount - 1
        );

        finalTriggerDelay = Mathf.Max(
            0f,
            finalTriggerDelay
        );
            }

    private bool ValidateReferences()
    {
        if (targetCamera == null)
        {
            Debug.LogError(
                "MeteorSpawner: Main Camera bulunamadı.",
                this
            );

            enabled = false;
            return false;
        }

        if (meteorPrefabs == null || meteorPrefabs.Length == 0)
        {
            Debug.LogError(
                "MeteorSpawner: Meteor Prefabs listesine "
                + "en az bir prefab eklenmeli.",
                this
            );

            enabled = false;
            return false;
        }

        for (int i = 0; i < meteorPrefabs.Length; i++)
        {
            if (meteorPrefabs[i] != null)
            {
                continue;
            }

            Debug.LogError(
                $"MeteorSpawner: Meteor Prefabs listesindeki "
                + $"{i}. eleman boş.",
                this
            );

            enabled = false;
            return false;
        }

        return true;
    }

    private IEnumerator SpawnLoop()
    {
        if (firstWaveDelay > 0f)
        {
            yield return new WaitForSeconds(firstWaveDelay);
        }
    
        while (!finalTriggered)
        {
            SpawnWave();
    
            remainingWaveCount = Mathf.Max(
                0,
                remainingWaveCount - 1
            );
    
            if (countdownUI != null)
            {
                countdownUI.SetCount(remainingWaveCount);
            }
    
            if (remainingWaveCount <= finalTriggerCount)
            {
                if (finalTriggerDelay > 0f)
                {
                    yield return new WaitForSeconds(
                        finalTriggerDelay
                    );
                }
    
                TriggerFinalCountdown();
                yield break;
            }
    
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnWave()
    {
        CalculateSpawnArea(
            out float leftX,
            out float rightX,
            out float spawnY
        );

        float availableWidth = rightX - leftX;
        float columnWidth = availableWidth / columnCount;

        int gapStartColumn = ChooseGapStartColumn();

        for (int columnIndex = 0;
             columnIndex < columnCount;
             columnIndex++)
        {
            bool isInsideSafeGap =
                columnIndex >= gapStartColumn
                && columnIndex <
                gapStartColumn + emptyColumnCount;

            if (isInsideSafeGap)
            {
                continue;
            }

            // Boşluk dışında da bazı sütunların rastgele boş
            // kalmasını sağlayarak dalgaları daha doğal gösterir.
            if (Random.value > meteorSpawnChance)
            {
                continue;
            }

            float columnCenterX =
                leftX
                + columnWidth * columnIndex
                + columnWidth * 0.5f;

            Vector3 spawnPosition = new Vector3(
                columnCenterX,
                spawnY,
                0f
            );

            SpawnMeteor(spawnPosition);
        }

        previousGapStartColumn = gapStartColumn;
    }

    private int ChooseGapStartColumn()
    {
        int maximumGapStart =
            columnCount - emptyColumnCount;

        // İlk dalgada boşluk ekranın herhangi bir yerinde olabilir.
        if (previousGapStartColumn < 0)
        {
            return Random.Range(
                0,
                maximumGapStart + 1
            );
        }

        // Sonraki dalgalarda güvenli boşluğun bir anda
        // çok uzak bir noktaya geçmesini engeller.
        int minimumAllowedStart = Mathf.Max(
            0,
            previousGapStartColumn - maximumGapShiftPerWave
        );

        int maximumAllowedStart = Mathf.Min(
            maximumGapStart,
            previousGapStartColumn + maximumGapShiftPerWave
        );

        return Random.Range(
            minimumAllowedStart,
            maximumAllowedStart + 1
        );
    }

    private void SpawnMeteor(Vector3 spawnPosition)
    {
        int randomPrefabIndex = Random.Range(
            0,
            meteorPrefabs.Length
        );

        GameObject selectedPrefab =
            meteorPrefabs[randomPrefabIndex];

        GameObject spawnedMeteor = Instantiate(
            selectedPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (meteorParent != null)
        {
            spawnedMeteor.transform.SetParent(
                meteorParent,
                true
            );
        }

        MeteorMover meteorMover =
            spawnedMeteor.GetComponent<MeteorMover>();

        if (meteorMover == null)
        {
            Debug.LogError(
                $"MeteorSpawner: {selectedPrefab.name} prefabında "
                + "MeteorMover scripti bulunamadı.",
                selectedPrefab
            );

            Destroy(spawnedMeteor);
            return;
        }

        meteorMover.SetFallSpeed(meteorFallSpeed);
    }

    private void CalculateSpawnArea(
        out float leftX,
        out float rightX,
        out float spawnY
    )
    {
        float cameraDistance = Mathf.Abs(
            targetCamera.transform.position.z
            - transform.position.z
        );

        Vector3 bottomLeft =
            targetCamera.ViewportToWorldPoint(
                new Vector3(0f, 0f, cameraDistance)
            );

        Vector3 topRight =
            targetCamera.ViewportToWorldPoint(
                new Vector3(1f, 1f, cameraDistance)
            );

        leftX = bottomLeft.x + horizontalPadding;
        rightX = topRight.x - horizontalPadding;
        spawnY = topRight.y + spawnHeightPadding;
    }

    public void StartSpawning()
    {
        if (spawnCoroutine != null)
        {
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine == null)
        {
            return;
        }

        StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;
    }

    public void SetMeteorFallSpeed(float newSpeed)
    {
        meteorFallSpeed = Mathf.Max(0f, newSpeed);
    }

    public void SetSpawnInterval(float newInterval)
    {
        spawnInterval = Mathf.Max(0.05f, newInterval);
    }

    public void ResetGapPosition()
    {
        previousGapStartColumn = -1;
    }

    private void TriggerFinalCountdown()
    {
        if (finalTriggered)
        {
            return;
        }
    
        finalTriggered = true;
        spawnCoroutine = null;
    
        Debug.Log(
            "Meteor countdown tamamlandı. Final sekansı tetiklendi.",
            this
        );
    
        onFinalCountdownReached?.Invoke();
    }
}