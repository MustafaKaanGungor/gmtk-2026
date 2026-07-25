using System;
using UnityEngine;

/// <summary>
/// Tek bir bavul teslim alani. Kapisi yoktur. Kabul ettigi renk (BagType) bir
/// sayac (zamanlayici) ile surekli degisir; her fazin suresi belirlenen bir
/// araliktan RASTGELE secilir, boylece ritim dinamiktir.
///
/// Aktif renkte firlatilmis bir bavul girerse kabul edilir (sayilir). Yanlis
/// renkte bir bavul girerse, hiz vektoru tersine cevrilerek geldigi yone
/// geri firlatilir.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class ColorZone : MonoBehaviour
{
    [Serializable]
    public class ColorPhase
    {
        [Tooltip("Bu fazda kabul edilen bavul tipi (rengi).")]
        public BagType bagType = BagType.Brown;

        [Tooltip("Gorsel tint (zoneVisual atanmissa uygulanir).")]
        public Color displayColor = Color.white;
    }

    [Header("Color Phases")]
    [Tooltip("Alanin dongu halinde gececegi renkler/tipler.")]
    [SerializeField] private ColorPhase[] phases;

    [Tooltip("Renk sirasi rastgele mi secilsin (kapali ise sirayla).")]
    [SerializeField] private bool randomOrder = true;

    [Header("Timing (saniye) - X = min, Y = max")]
    [Tooltip("Her rengin aktif kalma suresi. Her seferinde bu aralikta rastgele.")]
    [SerializeField] private Vector2 phaseDurationRange = new Vector2(2f, 4f);

    [Header("Visual (opsiyonel)")]
    [Tooltip("Aktif renge gore boyanacak gorsel.")]
    [SerializeField] private SpriteRenderer zoneVisual;

    [Header("Bag Detection")]
    [Tooltip("Sadece firlatilmis (IsThrown) bavullar dikkate alinsin mi.")]
    [SerializeField] private bool onlyThrownBags = true;

    [Header("Correct Bag")]
    [Min(1)]
    [Tooltip("Gorevin tamamlanmasi icin toplam kac dogru bavul gerekli.")]
    [SerializeField] private int requiredCount = 5;

    [Tooltip("Dogru bavul yok edilsin mi (teslim edilmis sayilir). " +
        "Kapali ise sadece fizigi durdurulur.")]
    [SerializeField] private bool destroyBagOnAccept = true;

    [Header("Wrong Bag")]
    [Tooltip("Yanlis renkte bavul girince hiz vektoru ters cevrilip geri firlatilsin mi.")]
    [SerializeField] private bool bounceWrongBags = true;

    [Min(0f)]
    [Tooltip("Geri firlatma hiz carpani. 1 = ayni hizla geri, >1 daha sert.")]
    [SerializeField] private float bounceMultiplier = 1f;

    [Header("Debug")]
    [SerializeField] private bool printMessages = false;

    /// <summary>Dogru tipte bir bavul kabul edildiginde tetiklenir.</summary>
    public event Action<ColorZone> BagAccepted;

    /// <summary>Gereken sayida dogru bavul alindiginda (bir kez) tetiklenir.</summary>
    public event Action<ColorZone> Satisfied;

    /// <summary>Aktif kabul edilen renk degistiginde tetiklenir.</summary>
    public event Action<BagType> ColorChanged;

    public BagType CurrentAcceptedType { get; private set; }
    public int RequiredCount => requiredCount;
    public int ReceivedCount { get; private set; }
    public bool IsSatisfied => ReceivedCount >= requiredCount;

    private int currentPhaseIndex = -1;
    private float phaseTimer;

    private void Awake()
    {
        Collider2D colliderComponent = GetComponent<Collider2D>();

        if (!colliderComponent.isTrigger)
        {
            colliderComponent.isTrigger = true;

            Debug.LogWarning(
                "ColorZone: Collider'in Is Trigger ayari otomatik acildi.",
                this
            );
        }
    }

    private void Start()
    {
        if (phases == null || phases.Length == 0)
        {
            Debug.LogError(
                "ColorZone: En az bir renk fazi (phases) tanimlanmali.",
                this
            );

            enabled = false;
            return;
        }

        int startIndex =
            randomOrder
                ? UnityEngine.Random.Range(0, phases.Length)
                : 0;

        GoToPhase(startIndex);
    }

    private void Update()
    {
        phaseTimer -= Time.deltaTime;

        if (phaseTimer <= 0f)
        {
            AdvancePhase();
        }
    }

    private void AdvancePhase()
    {
        int nextIndex;

        if (randomOrder && phases.Length > 1)
        {
            // Ayni rengi ust uste secmemek icin farklisini bul.
            do
            {
                nextIndex = UnityEngine.Random.Range(0, phases.Length);
            }
            while (nextIndex == currentPhaseIndex);
        }
        else
        {
            nextIndex = (currentPhaseIndex + 1) % phases.Length;
        }

        GoToPhase(nextIndex);
    }

    private void GoToPhase(int index)
    {
        currentPhaseIndex = index;

        ColorPhase phase = phases[index];

        CurrentAcceptedType = phase.bagType;

        if (zoneVisual != null)
        {
            zoneVisual.color = phase.displayColor;
        }

        phaseTimer = PickDuration();

        ColorChanged?.Invoke(CurrentAcceptedType);

        if (printMessages)
        {
            Debug.Log(
                "ColorZone: aktif renk -> " + CurrentAcceptedType,
                this
            );
        }
    }

    private float PickDuration()
    {
        float min = Mathf.Min(phaseDurationRange.x, phaseDurationRange.y);
        float max = Mathf.Max(phaseDurationRange.x, phaseDurationRange.y);

        return UnityEngine.Random.Range(min, max);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        BagProjectile bag = FindBagProjectile(other);

        if (bag == null)
        {
            return;
        }

        if (onlyThrownBags && !bag.IsThrown)
        {
            return;
        }

        GroundBagPickup pickup =
            bag.GetComponent<GroundBagPickup>();

        bool isCorrect =
            pickup != null && pickup.Type == CurrentAcceptedType;

        if (isCorrect)
        {
            if (!IsSatisfied)
            {
                AcceptBag(bag);
            }

            return;
        }

        // Yanlis renk: geldigi yone geri firlat.
        if (bounceWrongBags)
        {
            bag.ReverseVelocity(bounceMultiplier);

            if (printMessages)
            {
                Debug.Log(
                    "ColorZone: yanlis bavul geri firlatildi.",
                    this
                );
            }
        }
    }

    private void AcceptBag(BagProjectile bag)
    {
        ReceivedCount++;

        if (printMessages)
        {
            Debug.Log(
                "ColorZone: " + CurrentAcceptedType +
                " bavul alindi (" + ReceivedCount + "/" + requiredCount + ").",
                this
            );
        }

        BagAccepted?.Invoke(this);

        if (destroyBagOnAccept)
        {
            Destroy(bag.gameObject);
        }
        else if (bag.Body != null)
        {
            bag.Body.simulated = false;
        }

        if (IsSatisfied)
        {
            Satisfied?.Invoke(this);
        }
    }

    private BagProjectile FindBagProjectile(Collider2D incomingCollider)
    {
        Rigidbody2D incomingBody = incomingCollider.attachedRigidbody;

        if (incomingBody == null)
        {
            return null;
        }

        return incomingBody.GetComponent<BagProjectile>();
    }

    private void OnValidate()
    {
        phaseDurationRange.x = Mathf.Max(0f, phaseDurationRange.x);
        phaseDurationRange.y = Mathf.Max(0f, phaseDurationRange.y);
    }
}
