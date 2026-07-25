using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class PressurePumpController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Hat uzerinde asagi yukari hareket eden gosterge.")]
    [SerializeField] private RectTransform pumpIndicator;

    [Tooltip("Yesil bolgeyi gosteren gorsel (opsiyonel). " +
        "greenZoneMin/Max degerlerine gore otomatik konumlanir.")]
    [SerializeField] private RectTransform greenZoneVisual;

    [Tooltip("Gostergenin rengini degistirmek icin (opsiyonel).")]
    [SerializeField] private Image indicatorImage;

    [Header("Track Settings")]
    [Tooltip("Gostergenin hat uzerinde gidebilecegi toplam yukseklik (piksel).")]
    [Min(1f)]
    [SerializeField] private float trackHeight = 400f;

    [Header("Pressure Settings")]
    [Tooltip("Basincin bir saniyede ne kadar dusecegi (0-1 arasi).")]
    [Min(0f)]
    [SerializeField] private float pressureDropSpeed = 0.35f;

    [Tooltip("Tusa her basildiginda basincin ne kadar artacagi (0-1 arasi).")]
    [Range(0f, 1f)]
    [SerializeField] private float pumpAmount = 0.15f;

    [Tooltip("Baslangic basinci (0-1 arasi).")]
    [Range(0f, 1f)]
    [SerializeField] private float startingPressure = 0.5f;

    [Header("Green Zone")]
    [Tooltip("Yesil bolgenin alt siniri (0-1 arasi).")]
    [Range(0f, 1f)]
    [SerializeField] private float greenZoneMin = 0.4f;

    [Tooltip("Yesil bolgenin ust siniri (0-1 arasi).")]
    [Range(0f, 1f)]
    [SerializeField] private float greenZoneMax = 0.6f;

    [Header("Visual Feedback")]
    [SerializeField] private Color inZoneColor = Color.green;
    [SerializeField] private Color outOfZoneColor = Color.red;

    [Header("Sound Events")]
    [Tooltip("Pompa calisirken donen loop sesinin adi. Bos ise calmaz. " +
        "SoundManager'da bu girisin Loop ayari acik olmali.")]
    [SerializeField] private string pumpLoopSound = "Pressure_Pump_Loop";

    [Header("Input")]
    [Tooltip("Eski Input Manager kullanildiginda pompalama tusu.")]
    [SerializeField] private KeyCode pumpKey = KeyCode.Space;

    [Header("Temporary Test")]
    [Tooltip("Simdilik test etmek icin acik. GameController geldiginde kapatacagiz.")]
    [SerializeField] private bool playOnStart = true;

    private float normalizedPressure;

    public bool IsRunning { get; private set; }

    // 0 ile 1 arasindaki basinc degeri.
    public float NormalizedPressure => normalizedPressure;

    // Basinc su an yesil bolgede mi?
    public bool IsInGreenZone =>
        normalizedPressure >= greenZoneMin &&
        normalizedPressure <= greenZoneMax;

    // Yesil bolgede toplam ne kadar sure kalindi.
    public float TimeInGreenZone { get; private set; }

    private void Awake()
    {
        if (pumpIndicator == null)
        {
            Debug.LogError(
                "PressurePumpController: Pump Indicator atanmamis!",
                this
            );

            enabled = false;
            return;
        }

        ResetPressure();
        LayoutGreenZone();
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartPumping();
        }
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        if (PumpPressed())
        {
            Pump();
        }

        DropPressure();

        if (IsInGreenZone)
        {
            TimeInGreenZone += Time.deltaTime;
        }

        UpdateVisual();
    }

    private void DropPressure()
    {
        normalizedPressure -=
            pressureDropSpeed * Time.deltaTime;

        normalizedPressure =
            Mathf.Clamp01(normalizedPressure);
    }

    private void Pump()
    {
        normalizedPressure =
            Mathf.Clamp01(normalizedPressure + pumpAmount);
    }

    private void UpdateVisual()
    {
        Vector2 anchoredPosition =
            pumpIndicator.anchoredPosition;

        anchoredPosition.y =
            normalizedPressure * trackHeight;

        pumpIndicator.anchoredPosition = anchoredPosition;

        if (indicatorImage != null)
        {
            indicatorImage.color =
                IsInGreenZone ? inZoneColor : outOfZoneColor;
        }
    }

    private void LayoutGreenZone()
    {
        if (greenZoneVisual == null)
        {
            return;
        }

        float clampedMin = Mathf.Clamp01(greenZoneMin);
        float clampedMax = Mathf.Clamp01(greenZoneMax);

        float zoneHeight =
            Mathf.Abs(clampedMax - clampedMin) * trackHeight;

        float zoneCenter =
            ((clampedMin + clampedMax) * 0.5f) * trackHeight;

        greenZoneVisual.sizeDelta = new Vector2(
            greenZoneVisual.sizeDelta.x,
            zoneHeight
        );

        Vector2 anchoredPosition =
            greenZoneVisual.anchoredPosition;

        anchoredPosition.y = zoneCenter;

        greenZoneVisual.anchoredPosition = anchoredPosition;
    }

    public void StartPumping()
    {
        // Zaten calisiyorsa loop'u tekrar baslatmiyoruz.
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;

        PlayPumpLoop();
    }

    public void StopPumping()
    {
        IsRunning = false;

        StopPumpLoop();
    }

    public void ResetPressure()
    {
        normalizedPressure = Mathf.Clamp01(startingPressure);
        TimeInGreenZone = 0f;
        IsRunning = false;

        StopPumpLoop();

        if (pumpIndicator != null)
        {
            UpdateVisual();
        }
    }

    private void OnDisable()
    {
        // Obje kapatilirsa loop sesi sonsuza kadar calmasin.
        StopPumpLoop();
    }

    private void PlayPumpLoop()
    {
        if (string.IsNullOrEmpty(pumpLoopSound))
        {
            return;
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogWarning(
                "PressurePumpController: SoundManager sahnede yok, " +
                "pompa sesi calinamiyor.",
                this
            );

            return;
        }

        SoundManager.Instance.Play(pumpLoopSound);
    }

    private void StopPumpLoop()
    {
        if (string.IsNullOrEmpty(pumpLoopSound))
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Stop(pumpLoopSound);
        }
    }

    private bool PumpPressed()
    {
        #if ENABLE_INPUT_SYSTEM
        return
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame;
        #else
        return Input.GetKeyDown(pumpKey);
        #endif
    }

    private void OnValidate()
    {
        // Ust sinir alt sinirin altina dusmesin.
        if (greenZoneMax < greenZoneMin)
        {
            greenZoneMax = greenZoneMin;
        }
    }
}
