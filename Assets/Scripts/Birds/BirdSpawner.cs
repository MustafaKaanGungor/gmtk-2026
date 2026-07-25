using System.Collections;
using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    public enum BirdDirectionMode
    {
        LeftToRight,
        RightToLeft,
        Random
    }

    [Header("References")]
    [SerializeField] private Bird birdPrefab;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform birdContainer;

    [Header("Spawn Density")]
    [Tooltip("Sahnede aynı anda bulunabilecek maksimum kuş sayısı.")]
    [SerializeField, Min(1)]
    private int maxBirdsAtOnce = 3;

    [Tooltip("İki kuş arasındaki minimum bekleme süresi.")]
    [SerializeField, Min(0.05f)]
    private float minSpawnDelay = 1.5f;

    [Tooltip("İki kuş arasındaki maksimum bekleme süresi.")]
    [SerializeField, Min(0.05f)]
    private float maxSpawnDelay = 3f;

    [Tooltip("Oyun başladığında beklemeden bir kuş oluşturur.")]
    [SerializeField]
    private bool spawnOneImmediately = true;

    [Header("Spawn Y Positions")]
    [Tooltip("Kuşların spawn olabileceği Y değerleri.")]
    [SerializeField]
    private float[] spawnYLevels = { -1f, 0.5f, 2f };

    [Tooltip("Seçilen Y değerine eklenecek küçük rastgele sapma.")]
    [SerializeField, Min(0f)]
    private float yJitter = 0.15f;

    [Tooltip(
        "Açıksa Y değerleri kameraya göre hesaplanır. " +
        "Kapalıysa doğrudan dünya koordinatı olarak kullanılır."
    )]
    [SerializeField]
    private bool yLevelsRelativeToCamera;

    [Header("Bird Movement")]
    [SerializeField]
    private BirdDirectionMode directionMode = BirdDirectionMode.Random;

    [SerializeField, Min(0.01f)]
    private float minBirdSpeed = 2.5f;

    [SerializeField, Min(0.01f)]
    private float maxBirdSpeed = 4f;

    [Header("Screen Margins")]
    [Tooltip("Kuş ekran kenarından ne kadar uzakta oluşturulsun?")]
    [SerializeField, Min(0f)]
    private float spawnMargin = 1.5f;

    [Tooltip("Ekrandan çıktıktan ne kadar sonra silinsin?")]
    [SerializeField, Min(0f)]
    private float destroyMargin = 2f;

    private int aliveBirdCount;
    private Coroutine spawnCoroutine;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (birdContainer == null)
        {
            birdContainer = transform;
        }
    }

    private void OnEnable()
    {
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        if (spawnOneImmediately)
        {
            TrySpawnBird();
        }

        while (true)
        {
            float spawnDelay =
                Random.Range(minSpawnDelay, maxSpawnDelay);

            yield return new WaitForSeconds(spawnDelay);

            TrySpawnBird();
        }
    }

    private void TrySpawnBird()
    {
        if (birdPrefab == null)
        {
            Debug.LogWarning(
                "BirdSpawner: Bird Prefab atanmamış.",
                this
            );

            return;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning(
                "BirdSpawner: Target Camera bulunamadı.",
                this
            );

            return;
        }

        if (spawnYLevels == null || spawnYLevels.Length == 0)
        {
            Debug.LogWarning(
                "BirdSpawner: En az bir Spawn Y Level eklenmeli.",
                this
            );

            return;
        }

        if (aliveBirdCount >= maxBirdsAtOnce)
        {
            return;
        }

        bool movingRight = ChooseMovingRight();

        float spawnX = GetSpawnX(movingRight);
        float spawnY = GetRandomSpawnY();
        float birdSpeed = Random.Range(minBirdSpeed, maxBirdSpeed);

        Vector3 spawnPosition = new Vector3(
            spawnX,
            spawnY,
            transform.position.z
        );

        Bird spawnedBird = Instantiate(
            birdPrefab,
            spawnPosition,
            Quaternion.identity,
            birdContainer
        );

        aliveBirdCount++;

        Vector2 direction = movingRight
            ? Vector2.right
            : Vector2.left;

        spawnedBird.Initialize(
            this,
            targetCamera,
            direction,
            birdSpeed,
            destroyMargin
        );
    }

    private bool ChooseMovingRight()
    {
        switch (directionMode)
        {
            case BirdDirectionMode.LeftToRight:
                return true;

            case BirdDirectionMode.RightToLeft:
                return false;

            case BirdDirectionMode.Random:
            default:
                return Random.value >= 0.5f;
        }
    }

    private float GetSpawnX(bool movingRight)
    {
        float cameraDistance =
            Mathf.Abs(targetCamera.transform.position.z - transform.position.z);

        float leftEdge = targetCamera.ViewportToWorldPoint(
            new Vector3(0f, 0.5f, cameraDistance)
        ).x;

        float rightEdge = targetCamera.ViewportToWorldPoint(
            new Vector3(1f, 0.5f, cameraDistance)
        ).x;

        if (movingRight)
        {
            return leftEdge - spawnMargin;
        }

        return rightEdge + spawnMargin;
    }

    private float GetRandomSpawnY()
    {
        int randomIndex = Random.Range(0, spawnYLevels.Length);

        float selectedY = spawnYLevels[randomIndex];

        if (yLevelsRelativeToCamera)
        {
            selectedY += targetCamera.transform.position.y;
        }

        selectedY += Random.Range(-yJitter, yJitter);

        return selectedY;
    }

    public void NotifyBirdDestroyed()
    {
        aliveBirdCount = Mathf.Max(0, aliveBirdCount - 1);
    }

    private void OnValidate()
    {
        if (maxSpawnDelay < minSpawnDelay)
        {
            maxSpawnDelay = minSpawnDelay;
        }

        if (maxBirdSpeed < minBirdSpeed)
        {
            maxBirdSpeed = minBirdSpeed;
        }
    }
}