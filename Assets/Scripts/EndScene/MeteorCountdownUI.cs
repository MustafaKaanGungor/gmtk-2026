using TMPro;
using UnityEngine;

public class MeteorCountdownUI : MonoBehaviour
{
    [Header("Text Reference")]
    [SerializeField] private TMP_Text countText;

    [Header("Number Formatting")]
    [Tooltip("2 seçilirse sayılar 09, 08, 07 şeklinde görünür.")]
    [Range(1, 4)]
    [SerializeField] private int minimumDigits = 2;

    private void Awake()
    {
        if (countText == null)
        {
            Debug.LogError(
                "MeteorCountdownUI: Count Text atanmamış.",
                this
            );
        }
    }

    public void SetCount(int newCount)
    {
        if (countText == null)
        {
            return;
        }

        int safeCount = Mathf.Max(0, newCount);

        string numberFormat = "D" + minimumDigits;

        countText.text = safeCount.ToString(numberFormat);
    }
}