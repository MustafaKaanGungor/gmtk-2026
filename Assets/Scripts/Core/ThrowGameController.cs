using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ThrowGameController : MonoBehaviour
{
    private enum GameState
    {
        Preparing,
        Playing,
        GameOver
    }

    [Header("Gameplay References")]
    [SerializeField]
    private PlayerGroundBagThrower playerBagThrower;

    [SerializeField]
    private GroundBagSpawner groundBagSpawner;

    [SerializeField]
    private RocketTarget rocketTarget;

    [Header("Score And UI")]
    [SerializeField]
    private ScoreController scoreController;

    [SerializeField]
    private GameUIController gameUIController;

    [Header("Task Integration")]
    [Tooltip("Opsiyonel. Atanirsa yeterli sayida canta teslim edilince gorev tamamlanir.")]
    [SerializeField]
    private TaskManager taskManager;

    [Tooltip("Tamamlanacak canta gorevinin kimligi.")]
    [SerializeField]
    private string bagTaskId = "bag_delivery";

    [Tooltip("Gorevin tamamlanmasi icin gereken teslim edilmis canta sayisi. " +
        "0 veya daha az ise butun cantalar teslim edilince tamamlanir.")]
    [SerializeField]
    private int requiredDeliveredBags = 0;

    private readonly HashSet<GroundBagPickup>
    activeThrownBags =
    new HashSet<GroundBagPickup>();

    private readonly HashSet<GroundBagPickup>
    deliveredBags =
    new HashSet<GroundBagPickup>();

    private GameState currentState;
    private int totalBagCount;
    private int deliveredBagCount;

    private void OnEnable()
    {
        if (playerBagThrower != null)
        {
            playerBagThrower.BagThrown +=
            HandleBagThrown;
        }

        if (rocketTarget != null)
        {
            rocketTarget.BagDelivered +=
            HandleBagDelivered;
        }
    }

    private void OnDisable()
    {
        if (playerBagThrower != null)
        {
            playerBagThrower.BagThrown -=
            HandleBagThrown;
        }

        if (rocketTarget != null)
        {
            rocketTarget.BagDelivered -=
            HandleBagDelivered;
        }

        StopTrackingAllBags();
    }

    private void Start()
    {
        if (!ReferencesAreValid())
        {
            enabled = false;
            return;
        }

        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        currentState = GameState.Preparing;

        // GroundBagSpawner da Start içinde çalıştığı için bir kare
        // bekleyerek oluşturulan bavulların sayısını alıyoruz.
        yield return null;

        totalBagCount =
        groundBagSpawner.SpawnedBagCount;

        deliveredBagCount = 0;
        deliveredBags.Clear();

        scoreController.ResetScore(
            totalBagCount
        );

        currentState = GameState.Playing;

    }

    private bool ReferencesAreValid()
    {
        bool isValid = true;



        if (groundBagSpawner == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "GroundBagSpawner atanmamış.",
                this
            );

            isValid = false;
        }

        if (rocketTarget == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "RocketTarget atanmamış.",
                this
            );

            isValid = false;
        }

        if (scoreController == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "ScoreController atanmamış.",
                this
            );

            isValid = false;
        }

   

        return isValid;
    }

    private void HandleBagThrown(
        GroundBagPickup thrownBag)
    {
        if (
            currentState != GameState.Playing ||
            thrownBag == null)
        {
            return;
        }

        if (!activeThrownBags.Add(thrownBag))
        {
            return;
        }

        thrownBag.ReturnedToGround +=
        HandleBagReturnedToGround;

        gameUIController.SetInstruction(
            "Bag in flight..."
        );
    }

    private void HandleBagDelivered(
        BagProjectile deliveredProjectile)
    {
        if (
            currentState != GameState.Playing ||
            deliveredProjectile == null)
        {
            return;
        }

        GroundBagPickup deliveredBag =
        deliveredProjectile.GetComponent<
        GroundBagPickup>();

        if (
            deliveredBag == null ||
            !IsSpawnedBag(deliveredBag) ||
            !deliveredBags.Add(deliveredBag))
        {
            return;
        }

        StopTrackingBag(deliveredBag);

        deliveredBagCount++;

        int earnedScore =
        scoreController.RegisterSuccess();

        scoreController.SetRemainingBagCount(
            totalBagCount -
            deliveredBagCount
        );

        gameUIController.SetInstruction(
            "Success! +" +
            earnedScore +
            " points."
        );

        GameSignals.Raise(GameSignals.BagDelivered);

        TryCompleteBagTask();

        if (deliveredBagCount >= totalBagCount)
        {
            FinishGame();
        }
    }

    private bool IsSpawnedBag(GroundBagPickup bag)
    {
        IReadOnlyList<GroundBagPickup> spawnedBags =
            groundBagSpawner.SpawnedBags;

        for (int index = 0; index < spawnedBags.Count; index++)
        {
            if (spawnedBags[index] == bag)
            {
                return true;
            }
        }

        return false;
    }

    private void TryCompleteBagTask()
    {
        if (taskManager == null)
        {
            return;
        }

        // 0 veya daha az ise tum cantalar teslim edilince tamamlanir.
        int requiredCount =
            requiredDeliveredBags > 0
                ? requiredDeliveredBags
                : totalBagCount;

        if (deliveredBagCount >= requiredCount)
        {
            // CompleteTask zaten tamamlanmis gorevleri tekrar tetiklemez.
            taskManager.CompleteTask(bagTaskId);
        }
    }

    private void HandleBagReturnedToGround(
        GroundBagPickup returnedBag)
    {
        if (
            currentState != GameState.Playing ||
            returnedBag == null ||
            !activeThrownBags.Contains(
                returnedBag))
        {
            return;
        }

        StopTrackingBag(returnedBag);

        scoreController.RegisterMiss();

        GameSignals.Raise(GameSignals.BagMissed);

        gameUIController.SetInstruction(
            "Missed! You can pick up the bag again."
        );
    }

    private void StopTrackingBag(
        GroundBagPickup bag)
    {
        if (bag == null)
        {
            return;
        }

        bag.ReturnedToGround -=
        HandleBagReturnedToGround;

        activeThrownBags.Remove(bag);
    }

    private void StopTrackingAllBags()
    {
        foreach (
            GroundBagPickup bag
            in activeThrownBags)
        {
            if (bag != null)
            {
                bag.ReturnedToGround -=
                HandleBagReturnedToGround;
            }
        }

        activeThrownBags.Clear();
    }

    private void FinishGame()
    {
        currentState = GameState.GameOver;

        StopTrackingAllBags();

        GameSignals.Raise(GameSignals.GameOver);

        gameUIController.SetInstruction(
            "Game over! Total score: " +
            scoreController.TotalScore +
            ". Highest streak: " +
            scoreController.HighestStreak
        );

        Debug.Log(
            "Oyun bitti. Skor: " +
            scoreController.TotalScore,
            this
        );
    }
}
