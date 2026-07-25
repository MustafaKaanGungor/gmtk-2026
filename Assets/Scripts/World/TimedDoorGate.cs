using System;
using UnityEngine;

/// <summary>
/// Dinamik zamanlamali kapi gecidi. Kapi belirli araliklarla acilip kapanir;
/// her dongude acik/kapali suresi belirlenen bir araliktan RASTGELE secilir,
/// boylece ritim surekli degisir. Kapi kapaliyken girisi fiziksel olarak
/// engeller ve enter-trigger yalnizca kapi acikken FIRLATILMIS BAVULU algilar.
///
/// Kurulum: bu script'i, uzerinde enter-trigger Collider2D (Is Trigger) olan
/// bir "gecit" objesine koy. Kapiyi (sprite + bariyer collider) ayri bir cocuk
/// obje olarak 'door' alanina ata.
/// </summary>
[DisallowMultipleComponent]
public class TimedDoorGate : MonoBehaviour
{
    public enum GateState
    {
        Open,
        Closed
    }

    [Header("Door (bariyer)")]
    [Tooltip("Girisi kapatan kapi objesi (sprite + bariyer Collider2D).")]
    [SerializeField] private GameObject door;

    [Header("Door Movement")]
    [Tooltip("Acik ise kapi iki konum arasinda kayar. Kapali ise kapi acikken " +
        "obje tamamen kapatilir (SetActive).")]
    [SerializeField] private bool slideDoor = true;

    [Tooltip("Kapaliyken kapinin local konumu (girisi kapatir).")]
    [SerializeField] private Vector3 closedLocalPosition;

    [Tooltip("Acikken kapinin local konumu (girisi acar).")]
    [SerializeField] private Vector3 openLocalPosition;

    [Min(0f)]
    [Tooltip("Kayma hizi (birim/saniye). 0 ise aninda gecer.")]
    [SerializeField] private float slideSpeed = 8f;

    [Header("Entry Trigger")]
    [Tooltip("Gecis alani collider'i (Is Trigger). Yalnizca kapi acikken aktif olur.")]
    [SerializeField] private Collider2D entryTrigger;

    [Tooltip("Acik ise sadece FIRLATILMIS (IsThrown) bavullar algilanir; " +
        "elde tutulan bavul tetiklemez.")]
    [SerializeField] private bool onlyThrownBags = true;

    [Header("Bag Requirement")]
    [Tooltip("Bu kapinin kabul ettigi bavul tipi (rengi).")]
    [SerializeField] private BagType acceptedBagType = BagType.Brown;

    [Min(1)]
    [Tooltip("Gorevin tamamlanmasi icin bu kapiya girmesi gereken bavul sayisi.")]
    [SerializeField] private int requiredCount = 3;

    [Tooltip("Kabul edilen bavul yok edilsin mi (teslim edilmis sayilir). " +
        "Kapali ise sadece fizigi durdurulur.")]
    [SerializeField] private bool destroyBagOnAccept = true;

    [Tooltip("Yanlis renkte bavul girince firlatildigi yere geri isinlansin mi.")]
    [SerializeField] private bool teleportWrongBags = true;

    [Header("Timing (saniye) - X = min, Y = max")]
    [Tooltip("Kapi ACIK kalma suresi araligi. Her seferinde bu aralikta rastgele.")]
    [SerializeField] private Vector2 openDurationRange = new Vector2(2f, 4f);

    [Tooltip("Kapi KAPALI kalma suresi araligi. Her seferinde bu aralikta rastgele.")]
    [SerializeField] private Vector2 closedDurationRange = new Vector2(1.5f, 3f);

    [Header("Start")]
    [Tooltip("Baslangicta kapi acik mi olsun.")]
    [SerializeField] private bool startOpen = true;

    [Tooltip("Acik ise baslangic durumu ve zamani rastgele secilir " +
        "(3 kapiyi desenkronize etmek icin pratik).")]
    [SerializeField] private bool randomizeStart = true;

    [Min(0f)]
    [Tooltip("randomizeStart kapaliyken baslangicta beklenecek ek sure.")]
    [SerializeField] private float startDelay = 0f;

    [Header("Debug")]
    [SerializeField] private bool printMessages = false;

    /// <summary>Kapi acildiginda tetiklenir.</summary>
    public event Action Opened;

    /// <summary>Kapi kapandiginda tetiklenir.</summary>
    public event Action Closed;

    /// <summary>Kapi dogru tipte bir bavul kabul ettiginde tetiklenir.</summary>
    public event Action<TimedDoorGate> BagAccepted;

    /// <summary>Kapi gereken sayida bavul aldiginda (bir kez) tetiklenir.</summary>
    public event Action<TimedDoorGate> Satisfied;

    public GateState State { get; private set; }
    public bool IsOpen => State == GateState.Open;

    public BagType AcceptedBagType => acceptedBagType;
    public int RequiredCount => requiredCount;
    public int ReceivedCount { get; private set; }
    public bool IsSatisfied => ReceivedCount >= requiredCount;

    private float stateTimer;

    private void Awake()
    {
        if (entryTrigger != null)
        {
            entryTrigger.isTrigger = true;
        }
    }

    private void Start()
    {
        GateState initialState;

        if (randomizeStart)
        {
            initialState =
                UnityEngine.Random.value < 0.5f
                    ? GateState.Open
                    : GateState.Closed;
        }
        else
        {
            initialState = startOpen ? GateState.Open : GateState.Closed;
        }

        SetState(initialState, true);

        stateTimer = startDelay + PickDuration(initialState);

        if (randomizeStart)
        {
            // Baslangic suresini kisaltarak kapilari birbirinden ayir.
            stateTimer *= UnityEngine.Random.Range(0.1f, 1f);
        }
    }

    private void Update()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            Toggle();
        }

        if (slideDoor && door != null)
        {
            Vector3 target =
                IsOpen ? openLocalPosition : closedLocalPosition;

            if (slideSpeed <= 0f)
            {
                door.transform.localPosition = target;
            }
            else
            {
                door.transform.localPosition = Vector3.MoveTowards(
                    door.transform.localPosition,
                    target,
                    slideSpeed * Time.deltaTime
                );
            }
        }
    }

    private void Toggle()
    {
        GateState next = IsOpen ? GateState.Closed : GateState.Open;

        SetState(next, false);

        stateTimer = PickDuration(next);
    }

    private void SetState(GateState newState, bool instant)
    {
        State = newState;

        // Enter-trigger yalnizca kapi acikken aktif.
        if (entryTrigger != null)
        {
            entryTrigger.enabled = newState == GateState.Open;
        }

        if (door != null)
        {
            if (slideDoor)
            {
                // Kayma modunda kapi hep aktif kalir, sadece konumu degisir.
                door.SetActive(true);

                if (instant)
                {
                    door.transform.localPosition =
                        newState == GateState.Open
                            ? openLocalPosition
                            : closedLocalPosition;
                }
            }
            else
            {
                // Kayma yoksa kapiyi acikken tamamen gizle/kapat.
                door.SetActive(newState == GateState.Closed);
            }
        }

        if (newState == GateState.Open)
        {
            Opened?.Invoke();
        }
        else
        {
            Closed?.Invoke();
        }

        if (printMessages)
        {
            Debug.Log(
                "TimedDoorGate: " + name + " -> " + newState,
                this
            );
        }
    }

    private float PickDuration(GateState state)
    {
        Vector2 range =
            state == GateState.Open
                ? openDurationRange
                : closedDurationRange;

        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);

        return UnityEngine.Random.Range(min, max);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOpen || IsSatisfied)
        {
            return;
        }

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

        bool isCorrectType =
            pickup != null && pickup.Type == acceptedBagType;

        // Yanlis renk: firlatildigi yere geri isinla, sayma.
        if (!isCorrectType)
        {
            if (teleportWrongBags && bag.IsThrown)
            {
                bag.ReturnToThrowOrigin();

                if (printMessages)
                {
                    Debug.Log(
                        "TimedDoorGate: " + name +
                        " yanlis bavul geri isinlandi.",
                        this
                    );
                }
            }

            return;
        }

        AcceptBag(bag);
    }

    private void AcceptBag(BagProjectile bag)
    {
        ReceivedCount++;

        if (printMessages)
        {
            Debug.Log(
                "TimedDoorGate: " + name + " " + acceptedBagType +
                " bavul aldi (" + ReceivedCount + "/" + requiredCount + ").",
                this
            );
        }

        BagAccepted?.Invoke(this);

        // Kabul edilen bavulu teslim edilmis say.
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
        openDurationRange.x = Mathf.Max(0f, openDurationRange.x);
        openDurationRange.y = Mathf.Max(0f, openDurationRange.y);
        closedDurationRange.x = Mathf.Max(0f, closedDurationRange.x);
        closedDurationRange.y = Mathf.Max(0f, closedDurationRange.y);
    }
}
