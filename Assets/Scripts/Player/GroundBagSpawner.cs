using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GroundBagSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BagProjectile bagPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn")]
    [Min(1)]
    [SerializeField] private int bagCount = 10;

    [Min(0f)]
    [SerializeField] private float horizontalSpacing = 1.1f;

    [SerializeField] private bool spawnOnStart = true;

    private readonly List<GroundBagPickup> spawnedBags =
        new List<GroundBagPickup>();

    public IReadOnlyList<GroundBagPickup> SpawnedBags =>
        spawnedBags;

    public int SpawnedBagCount => spawnedBags.Count;

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
        if (bagPrefab == null)
        {
            Debug.LogError(
                "GroundBagSpawner: Bag Prefab atanmamış.",
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
                    spawnPoints.Length)
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

            BagProjectile bag = Instantiate(
                bagPrefab,
                position,
                rotation
            );

            GroundBagPickup pickup =
                bag.GetComponent<GroundBagPickup>();

            if (pickup == null)
            {
                pickup =
                    bag.gameObject.AddComponent<
                        GroundBagPickup>();
            }
            else
            {
                pickup.PrepareAsGroundBag();
            }

            spawnedBags.Add(pickup);
        }
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
