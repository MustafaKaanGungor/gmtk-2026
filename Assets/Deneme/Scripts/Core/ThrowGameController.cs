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

    private readonly HashSet<GroundBagPickup>
    activeThrownBags =
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

        scoreController.ResetScore(
            totalBagCount
        );

        currentState = GameState.Playing;

        gameUIController.SetInstruction(
            "Bavula yaklaş. E veya sol tık ile al, " +
            "tekrar basarak rokete fırlat."
        );
    }

    private bool ReferencesAreValid()
    {
        bool isValid = true;

        if (playerBagThrower == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "PlayerGroundBagThrower atanmamış.",
                this
            );

            isValid = false;
        }

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

        if (gameUIController == null)
        {
            Debug.LogError(
                "ThrowGameController: " +
                "GameUIController atanmamış.",
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
            "Bavul uçuyor..."
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
            !activeThrownBags.Contains(
                deliveredBag))
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
            "Başarılı! +" +
            earnedScore +
            " puan."
        );

        if (deliveredBagCount >= totalBagCount)
        {
            FinishGame();
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

        gameUIController.SetInstruction(
            "Atış kaçtı. Bavulu tekrar alabilirsin."
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

        gameUIController.SetInstruction(
            "Oyun bitti! Toplam skor: " +
            scoreController.TotalScore +
            ". En yüksek seri: " +
            scoreController.HighestStreak
        );

        Debug.Log(
            "Oyun bitti. Skor: " +
            scoreController.TotalScore,
            this
        );
    }
}
