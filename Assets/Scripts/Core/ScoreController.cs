using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ScoreController : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Score Settings")]
    [Min(0)]
    [SerializeField] private int successBaseScore = 100;

    [Min(0)]
    [SerializeField] private int streakBonus = 25;

    public int TotalScore { get; private set; }
    public int SuccessfulBagCount { get; private set; }
    public int MissedBagCount { get; private set; }
    public int CurrentStreak { get; private set; }
    public int HighestStreak { get; private set; }
    public int RemainingBagCount { get; private set; }

    /// <summary>
    /// Yeni oyun başlarken bütün skor değerlerini sıfırlar.
    /// </summary>
    public void ResetScore(int startingBagCount)
    {
        TotalScore = 0;
        SuccessfulBagCount = 0;
        MissedBagCount = 0;
        CurrentStreak = 0;
        HighestStreak = 0;

        RemainingBagCount =
            Mathf.Max(0, startingBagCount);

        UpdateScoreText();
    }

    /// <summary>
    /// Başarılı atışı kaydeder ve kazanılan puanı döndürür.
    /// </summary>
    public int RegisterSuccess()
    {
        SuccessfulBagCount++;
        CurrentStreak++;

        if (CurrentStreak > HighestStreak)
        {
            HighestStreak = CurrentStreak;
        }

        int earnedScore =
            successBaseScore +
            ((CurrentStreak - 1) * streakBonus);

        TotalScore += earnedScore;

        UpdateScoreText();

        return earnedScore;
    }

    /// <summary>
    /// Başarısız atışı kaydeder ve seriyi sıfırlar.
    /// </summary>
    public void RegisterMiss()
    {
        MissedBagCount++;
        CurrentStreak = 0;

        UpdateScoreText();
    }

    /// <summary>
    /// UI üzerinde gösterilen kalan çanta sayısını değiştirir.
    /// </summary>
    public void SetRemainingBagCount(int remainingBagCount)
    {
        RemainingBagCount =
            Mathf.Max(0, remainingBagCount);

        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text =
            "Score: " + TotalScore +
            "\nSuccessful: " + SuccessfulBagCount +
            "\nMissed: " + MissedBagCount +
            "\nStreak: " + CurrentStreak +
            "\nBags Remaining: " + RemainingBagCount;
    }
}
