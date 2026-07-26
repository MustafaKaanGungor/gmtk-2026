using System.Collections;
using UnityEngine;

public class ToothRainSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] toothPrefabs;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform toothParent;

    [Header("Full Screen Layout")]
    [Tooltip("Ekranın kaç sütuna ayrılacağı.")]
    [Min(2)]
    [SerializeField] private int columnCount = 14;

    [Tooltip("Ekranın sağ ve sol kenarındaki küçük boşluk.")]
    [Min(0f)]
    [SerializeField] private float horizontalPadding = 0.1f;

    [Tooltip("Dişlerin ekranın ne kadar üstünde üretileceği.")]
    [Min(0f)]
    [SerializeField] private float spawnHeightPadding = 1.5f;

    [Header("Rain Settings")]
    [Min(0.05f)]
    [SerializeField] private float waveInterval = 0.3f;

    [Min(0f)]
    [SerializeField] private float toothFallSpeed = 10f;

    [Tooltip(
        "İlk sıradaki boşlukların hizasını kapatmak için "
        + "ikinci bir kaydırılmış sıra üretir."
    )]
    [SerializeField] private bool spawnStaggeredSecondRow = true;

    [Min(0f)]
    [SerializeField] private float secondRowVerticalOffset = 1.25f;

    private Coroutine rainCoroutine;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnDisable()
    {
        StopRain();
    }

    public void StartRain()
    {
        if (rainCoroutine != null)
        {
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        rainCoroutine = StartCoroutine(RainLoop());
    }

    public void StopRain()
    {
        if (rainCoroutine == null)
        {
            return;
        }

        StopCoroutine(rainCoroutine);
        rainCoroutine = null;
    }

    private IEnumerator RainLoop()
    {
        while (true)
        {
            SpawnFullScreenWave();

            yield return new WaitForSeconds(waveInterval);
        }
    }

    private void SpawnFullScreenWave()
    {
        CalculateSpawnArea(
            out float leftX,
            out float rightX,
            out float spawnY
        );

        float availableWidth = rightX - leftX;
        float columnWidth = availableWidth / columnCount;

        SpawnRow(
            leftX,
            rightX,
            spawnY,
            columnWidth,
            0f
        );

        if (spawnStaggeredSecondRow)
        {
            SpawnRow(
                leftX,
                rightX,
                spawnY + secondRowVerticalOffset,
                columnWidth,
                columnWidth * 0.5f
            );
        }
    }

    private void SpawnRow(
        float leftX,
        float rightX,
        float spawnY,
        float columnWidth,
        float horizontalOffset
    )
    {
        // -1 ve columnCount değerleri sayesinde ekranın
        // dış kenarlarında da diş bulunur.
        for (int columnIndex = -1;
             columnIndex <= columnCount;
             columnIndex++)
        {
            float spawnX =
                leftX
                + (columnIndex + 0.5f) * columnWidth
                + horizontalOffset;

            if (spawnX < leftX - columnWidth
                || spawnX > rightX + columnWidth)
            {
                continue;
            }

            SpawnTooth(
                new Vector3(spawnX, spawnY, 0f)
            );
        }
    }

    private void SpawnTooth(Vector3 spawnPosition)
    {
        int prefabIndex = Random.Range(
            0,
            toothPrefabs.Length
        );

        GameObject spawnedTooth = Instantiate(
            toothPrefabs[prefabIndex],
            spawnPosition,
            Quaternion.identity
        );

        if (toothParent != null)
        {
            spawnedTooth.transform.SetParent(
                toothParent,
                true
            );
        }

        ToothMover toothMover =
            spawnedTooth.GetComponent<ToothMover>();

        if (toothMover == null)
        {
            Debug.LogError(
                $"{spawnedTooth.name} üzerinde ToothMover bulunamadı.",
                spawnedTooth
            );

            Destroy(spawnedTooth);
            return;
        }

        toothMover.SetFallSpeed(toothFallSpeed);
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

    private bool ValidateReferences()
    {
        if (targetCamera == null)
        {
            Debug.LogError(
                "ToothRainSpawner: Main Camera bulunamadı.",
                this
            );

            return false;
        }

        if (toothPrefabs == null || toothPrefabs.Length == 0)
        {
            Debug.LogError(
                "ToothRainSpawner: En az bir diş prefabı atanmalı.",
                this
            );

            return false;
        }

        for (int i = 0; i < toothPrefabs.Length; i++)
        {
            if (toothPrefabs[i] == null)
            {
                Debug.LogError(
                    $"Tooth Prefabs listesindeki {i}. eleman boş.",
                    this
                );

                return false;
            }
        }

        return true;
    }
}