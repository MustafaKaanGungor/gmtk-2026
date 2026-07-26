using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class GameUIController : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Score Settings")]
    [Min(0)]
    [SerializeField] private int pointsPerSuccessfulBag = 100;

    private int currentScore;
    private int successfulBagCount;
    private int missedBagCount;
    private int remainingBagCount;

    public int CurrentScore => currentScore;
    public int SuccessfulBagCount => successfulBagCount;
    public int MissedBagCount => missedBagCount;

    private void Awake()
    {
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (instructionText == null)
        {
            Debug.LogWarning(
                "GameUIController: InstructionText atanmamış.",
                this
            );
        }

        if (scoreText == null)
        {
            Debug.LogWarning(
                "GameUIController: ScoreText atanmamış.",
                this
            );
        }
    }

    /// <summary>
    /// Skor değerlerini yeni oyun için sıfırlar.
    /// </summary>
    public void ResetGame(int startingBagCount)
    {
        currentScore = 0;
        successfulBagCount = 0;
        missedBagCount = 0;
        remainingBagCount = Mathf.Max(
            0,
            startingBagCount
        );

        SetInstruction(
            "Preparing a new game..."
        );

        RefreshScoreText();
    }

    /// <summary>
    /// Ekranın üstündeki yönlendirme yazısını değiştirir.
    /// </summary>
    public void SetInstruction(string message)
    {
        if (instructionText == null)
        {
            return;
        }

        instructionText.text = message;
    }

    /// <summary>
    /// Yalnızca kalan çanta sayısını günceller.
    /// </summary>
    public void SetRemainingBagCount(
        int newRemainingBagCount)
    {
        remainingBagCount = Mathf.Max(
            0,
            newRemainingBagCount
        );

        RefreshScoreText();
    }

    /// <summary>
    /// Başarılı atışı skora ekler.
    /// </summary>
    public void RegisterSuccessfulBag(
        int newRemainingBagCount)
    {
        successfulBagCount++;
        currentScore += pointsPerSuccessfulBag;

        remainingBagCount = Mathf.Max(
            0,
            newRemainingBagCount
        );

        SetInstruction(
            "Success! The bag reached the rocket."
        );

        RefreshScoreText();
    }

    /// <summary>
    /// Başarısız atışı kaydeder.
    /// </summary>
    public void RegisterMissedBag(
        int newRemainingBagCount)
    {
        missedBagCount++;

        remainingBagCount = Mathf.Max(
            0,
            newRemainingBagCount
        );

        SetInstruction("Throw missed!");

        RefreshScoreText();
    }

    /// <summary>
    /// Oyun sonu mesajını gösterir.
    /// </summary>
    public void ShowGameOver(int totalBagCount)
    {
        SetInstruction(
            "Game over! " +
            successfulBagCount +
            " / " +
            totalBagCount +
            " bags reached the rocket. " +
            "Total score: " +
            currentScore
        );

        RefreshScoreText();
    }

    private void RefreshScoreText()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text =
            "Score: " +
            currentScore +
            "\nSuccessful: " +
            successfulBagCount +
            "\nMissed: " +
            missedBagCount +
            "\nBags Remaining: " +
            remainingBagCount;
    }
}
