using UnityEngine;
using UnityEngine.EventSystems;

public class VentMinigameInteractable : MonoBehaviour
{
    [Header("World Highlight")]
    [Tooltip("VentGlow nesnesini buraya sürükle.")]
    [SerializeField] private GameObject highlightObject;
    [SerializeField] private ComputerHighlightPulse highlightPulse;

    [Header("Minigame UI")]
    [SerializeField] private GameObject minigamePanel;

    [Header("Click Detection")]
    [Tooltip("VentClickArea üzerindeki Collider2D bileşenini buraya sürükle.")]
    [SerializeField] private Collider2D clickCollider;

    [Tooltip("Oyuncuyu gösteren ana kamerayı buraya sürükle.")]
    [SerializeField] private Camera worldCamera;

    [Header("Player")]
    [Tooltip("Player üzerindeki hareket component'ini buraya sürükle.")]
    [SerializeField] private MonoBehaviour playerMovement;

    [Tooltip("Player üzerindeki Rigidbody2D bileşenini buraya sürükle.")]
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Header("Task Integration")]
    [Tooltip("Opsiyonel. Atanirsa gorev tamamlaninca TaskManager'a bildirilir.")]
    [SerializeField] private TaskManager taskManager;

    [Tooltip("TaskManager'da tamamlanacak gorevin kimligi.")]
    [SerializeField] private string taskId = "vent_task";

    [Header("Input")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    [Tooltip("Açık olduğunda oyuncu uzaktaysa fareyle minigame açılamaz.")]
    [SerializeField] private bool clickRequiresPlayerInRange = true;

    private bool playerInRange;
    private bool minigameOpen;
    private bool taskCompleted;

    private bool movementWasEnabled;
    private RigidbodyConstraints2D previousConstraints;

    public bool IsMinigameOpen => minigameOpen;
    public bool IsTaskCompleted => taskCompleted;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        ResolveHighlightPulse();

        if (highlightPulse != null)
        {
            highlightPulse.SetCompleted(false);
        }

        SetHighlight(false);

        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (minigameOpen)
        {
            if (Input.GetKeyDown(closeKey))
            {
                CloseMinigame();
            }

            return;
        }

        if (taskCompleted)
        {
            return;
        }

        bool keyboardInteraction =
            playerInRange &&
            Input.GetKeyDown(interactionKey);

        bool mouseInteraction =
            Input.GetMouseButtonDown(0) &&
            IsMouseOverVent();

        if (clickRequiresPlayerInRange)
        {
            mouseInteraction =
                mouseInteraction && playerInRange;
        }

        if (keyboardInteraction || mouseInteraction)
        {
            OpenMinigame();
        }
    }

    private bool IsMouseOverVent()
    {
        if (clickCollider == null)
        {
            return false;
        }

        if (worldCamera == null)
        {
            return false;
        }

        // Bir UI elemanına tıklanıyorsa dünyadaki
        // havalandırmaya tıklanmış sayma.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;

        Vector3 mouseWorldPosition =
            worldCamera.ScreenToWorldPoint(mouseScreenPosition);

        return clickCollider.OverlapPoint(mouseWorldPosition);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInRange = true;

        if (playerRigidbody == null)
        {
            playerRigidbody = other.attachedRigidbody;
        }

        if (!minigameOpen && !taskCompleted)
        {
            SetHighlight(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInRange = false;

        if (!taskCompleted)
        {
            SetHighlight(false);
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            return true;
        }

        Rigidbody2D attachedBody = other.attachedRigidbody;

        return attachedBody != null &&
               attachedBody.CompareTag("Player");
    }

    public void OpenMinigame()
    {
        if (minigameOpen || taskCompleted)
        {
            return;
        }

        if (minigamePanel == null)
        {
            Debug.LogError(
                "VentMinigameInteractable: Minigame Panel atanmamış.",
                this
            );

            return;
        }

        minigameOpen = true;

        SetHighlight(false);
        minigamePanel.SetActive(true);

        LockPlayer();
    }

    public void CloseMinigame()
    {
        if (!minigameOpen)
        {
            return;
        }

        minigameOpen = false;

        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }

        UnlockPlayer();

        SetHighlight(
            taskCompleted ||
            playerInRange
        );
    }

    public void CompleteTask()
    {
        if (taskCompleted)
        {
            return;
        }

        taskCompleted = true;

        ResolveHighlightPulse();

        if (highlightPulse != null)
        {
            highlightPulse.SetCompleted(true);
        }

        SetHighlight(!minigameOpen);

        if (taskManager != null)
        {
            // CompleteTask zaten tamamlanmis gorevi tekrar tetiklemez.
            taskManager.CompleteTask(taskId);
        }
    }

    private void ResolveHighlightPulse()
    {
        if (highlightPulse != null)
        {
            return;
        }

        if (highlightObject != null)
        {
            highlightPulse =
                highlightObject.GetComponentInParent<ComputerHighlightPulse>();

            if (highlightPulse == null)
            {
                highlightPulse =
                    highlightObject.GetComponentInChildren<ComputerHighlightPulse>(
                        true
                    );
            }
        }

        if (highlightPulse != null)
        {
            return;
        }

        Transform searchRoot =
            transform.parent != null
                ? transform.parent
                : transform;

        ComputerHighlightPulse[] candidates =
            searchRoot.GetComponentsInChildren<ComputerHighlightPulse>(true);

        Vector3 referencePosition =
            clickCollider != null
                ? clickCollider.bounds.center
                : transform.position;

        float closestDistance = float.PositiveInfinity;

        foreach (ComputerHighlightPulse candidate in candidates)
        {
            float distance =
                (candidate.transform.position - referencePosition).sqrMagnitude;

            if (distance >= closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            highlightPulse = candidate;
        }
    }

    private void SetHighlight(bool visible)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(visible);
        }
    }

    private void LockPlayer()
    {
        if (playerMovement != null)
        {
            movementWasEnabled = playerMovement.enabled;
            playerMovement.enabled = false;
        }

        if (playerRigidbody != null)
        {
            previousConstraints = playerRigidbody.constraints;

            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;

            playerRigidbody.constraints =
                RigidbodyConstraints2D.FreezeAll;
        }
    }

    private void UnlockPlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = movementWasEnabled;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.constraints = previousConstraints;
        }
    }

    private void OnDisable()
    {
        if (!minigameOpen)
        {
            return;
        }

        minigameOpen = false;
        UnlockPlayer();
    }
}
