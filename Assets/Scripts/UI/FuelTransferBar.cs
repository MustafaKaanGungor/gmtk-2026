using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Basinc pompasinin ustunde duran, aktarilan yakit miktarini gosteren bar.
/// Bar yalnizca basinc yeterli (yesil) bolgede kaldikca dolar; bunu
/// PressurePumpMinigame.HoldProgress uzerinden okur.
/// </summary>
[DisallowMultipleComponent]
public class FuelTransferBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PressurePumpMinigame minigame;

    [Tooltip("Image Type = Filled olan yakit dolum gorseli.")]
    [SerializeField] private Image fuelFillImage;

    [Tooltip("Opsiyonel. Yuzde yazisi (ornegin '%42').")]
    [SerializeField] private TMP_Text percentText;

    [Header("Colors (opsiyonel)")]
    [Tooltip("Acik ise bar dolarken renk degisir.")]
    [SerializeField] private bool useColorGradient = false;
    [SerializeField] private Color emptyColor = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private Color fullColor = new Color(0.3f, 1f, 0.4f);

    private void Awake()
    {
        if (minigame == null)
        {
            Debug.LogError(
                "FuelTransferBar: PressurePumpMinigame atanmamis!",
                this
            );

            enabled = false;
            return;
        }

        if (fuelFillImage == null)
        {
            Debug.LogError(
                "FuelTransferBar: Fuel Fill Image atanmamis!",
                this
            );

            enabled = false;
            return;
        }

        UpdateBar();
    }

    private void Update()
    {
        UpdateBar();
    }

    private void UpdateBar()
    {
        float fuel = minigame.HoldProgress;

        fuelFillImage.fillAmount = fuel;

        if (useColorGradient)
        {
            fuelFillImage.color =
                Color.Lerp(emptyColor, fullColor, fuel);
        }

        if (percentText != null)
        {
            percentText.text =
                "%" + Mathf.RoundToInt(fuel * 100f);
        }
    }
}
