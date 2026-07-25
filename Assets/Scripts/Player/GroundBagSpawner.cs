using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GroundBagSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip(
        "Spawnlanabilecek bütün çanta prefablarını buraya ekle."
    )]
    [SerializeField]
    private BagProjectile[] bagPrefabs;

    [SerializeField]
    private Transform[] spawnPoints;

    [Header("Spawn")]
    [Min(1)]
    [SerializeField]
    private int bagCount = 10;

    [Min(0f)]
    [SerializeField]
    private float horizontalSpacing = 1.1f;

    [SerializeField]
    private bool spawnOnStart = true;

    private readonly List<GroundBagPickup> spawnedBags =
        new List<GroundBagPickup>();

    public IReadOnlyList<GroundBagPickup> SpawnedBags =>
        spawnedBags;

    public int SpawnedBagCount =>
        spawnedBags.Count;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnBags();
        }
    }

    [ContextMenu("Spawn Bags")]
    public void SpawnBags()
    {
        if (!HasValidBagPrefab())
        {
            Debug.LogError(
                "GroundBagSpawner: En az bir geçerli " +
                "Bag Prefab atanmalı.",
                this
            );

            return;
        }

        RemoveMissingReferences();

        int amountToSpawn =
            spawnPoints != null &&
            spawnPoints.Length > 0
                ? Mathf.Min(
                    bagCount,
                    spawnPoints.Length
                )
                : bagCount;

        for (
            int index = 0;
            index < amountToSpawn;
            index++)
        {
            GetSpawnPose(
                index,
                amountToSpawn,
                out Vector3 position,
                out Quaternion rotation
            );

            BagProjectile selectedPrefab =
                GetRandomBagPrefab();

            if (selectedPrefab == null)
            {
                Debug.LogWarning(
                    "GroundBagSpawner: Rastgele seçilecek " +
                    "geçerli prefab bulunamadı.",
                    this
                );

                continue;
            }

            BagProjectile spawnedBag =
                Instantiate(
                    selectedPrefab,
                    position,
                    rotation
                );

            GroundBagPickup pickup =
                spawnedBag.GetComponent<GroundBagPickup>();

            if (pickup == null)
            {
                pickup =
                    spawnedBag.gameObject.AddComponent<
                        GroundBagPickup
                    >();
            }
            else
            {
                pickup.PrepareAsGroundBag();
            }

            spawnedBags.Add(pickup);
        }
    }

    private BagProjectile GetRandomBagPrefab()
    {
        if (
            bagPrefabs == null ||
            bagPrefabs.Length == 0)
        {
            return null;
        }

        // Rastgele bir başlangıç noktası seçilir.
        int randomStartIndex =
            Random.Range(0, bagPrefabs.Length);

        // Seçilen alan boşsa dizideki diğer prefablar aranır.
        for (
            int offset = 0;
            offset < bagPrefabs.Length;
            offset++)
        {
            int prefabIndex =
                (randomStartIndex + offset) %
                bagPrefabs.Length;

            BagProjectile candidate =
                bagPrefabs[prefabIndex];

            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    private bool HasValidBagPrefab()
    {
        if (
            bagPrefabs == null ||
            bagPrefabs.Length == 0)
        {
            return false;
        }

        foreach (BagProjectile prefab in bagPrefabs)
        {
            if (prefab != null)
            {
                return true;
            }
        }

        return false;
    }

    private void GetSpawnPose(
        int index,
        int totalAmount,
        out Vector3 position,
        out Quaternion rotation)
    {
        if (
            spawnPoints != null &&
            index < spawnPoints.Length &&
            spawnPoints[index] != null)
        {
            Transform spawnPoint =
                spawnPoints[index];

            position = spawnPoint.position;
            rotation = spawnPoint.rotation;

            return;
        }

        float centeredIndex =
            index - ((totalAmount - 1) * 0.5f);

        position =
            transform.position +
            Vector3.right *
            centeredIndex *
            horizontalSpacing;

        rotation = transform.rotation;
    }

    private void RemoveMissingReferences()
    {
        for (
            int index = spawnedBags.Count - 1;
            index >= 0;
            index--)
        {
            if (spawnedBags[index] == null)
            {
                spawnedBags.RemoveAt(index);
            }
        }
    }
}