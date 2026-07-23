using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PowerController : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image fillImage;

    [Header("Power Settings")]
    [SerializeField] private float minimumPower = 7f;
    [SerializeField] private float maximumPower = 18f;

    [Tooltip("Barin bir saniyede ne kadar ilerleyecegi.")]
    [SerializeField] private float fillSpeed = 1.25f;

    [Header("Temporary Test")]
    [Tooltip("Simdilik test etmek icin acik. GameController geldiginde kapatacagiz.")]
    [SerializeField] private bool playOnStart = true;

    private float normalizedPower;
    private int movementDirection = 1;

    public bool IsSelecting { get; private set; }

    // UI barinin 0 ile 1 arasindaki degeri.
    public float NormalizedPower => normalizedPower;

    // Fizikte kullanacagimiz gercek firlatma kuvveti.
    public float CurrentPower =>
        Mathf.Lerp(
            minimumPower,
            maximumPower,
            normalizedPower
        );

    private void Awake()
    {
        if (fillImage == null)
        {
            Debug.LogError(
                "PowerController: Fill Image atanmamis!",
                this
            );

            enabled = false;
            return;
        }

        ResetPower();
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartSelectingPower();
        }
    }

    private void Update()
    {
        if (!IsSelecting)
        {
            return;
        }

        MovePowerBar();
    }

    private void MovePowerBar()
    {
        normalizedPower +=
            movementDirection *
            fillSpeed *
            Time.deltaTime;

        if (normalizedPower >= 1f)
        {
            normalizedPower = 1f;
            movementDirection = -1;
        }
        else if (normalizedPower <= 0f)
        {
            normalizedPower = 0f;
            movementDirection = 1;
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        fillImage.fillAmount = normalizedPower;
    }

    public void StartSelectingPower()
    {
        IsSelecting = true;
    }

    public float LockPower()
    {
        IsSelecting = false;

        return CurrentPower;
    }

    public void StopSelectingPower()
    {
        IsSelecting = false;
    }

    public void ResetPower()
    {
        normalizedPower = 0f;
        movementDirection = 1;
        IsSelecting = false;

        if (fillImage != null)
        {
            UpdateVisual();
        }
    }

    public void ShowPowerBar()
    {
        gameObject.SetActive(true);
    }

    public void HidePowerBar()
    {
        gameObject.SetActive(false);
    }
}