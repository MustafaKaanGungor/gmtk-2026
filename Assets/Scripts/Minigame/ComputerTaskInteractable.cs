using UnityEngine;
using UnityEngine.InputSystem;

public class ComputerTaskInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ArrowSequenceMinigame minigame;
    [SerializeField] private Collider2D interactionArea;
    [SerializeField] private Camera worldCamera;

    [Header("Visuals")]
    [SerializeField] private GameObject taskHighlight;
    [SerializeField] private GameObject interactionPrompt;

    [Header("Interaction Settings")]
    [SerializeField] private bool clickRequiresPlayerInRange = true;
    [SerializeField] private bool disableAfterCompletion = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private int playerColliderCount;
    private bool completed;

    private bool PlayerInRange => playerColliderCount > 0;

    private void Awake()
    {
        if (interactionArea == null)
        {
            interactionArea = GetComponent<Collider2D>();
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        RefreshVisuals();
    }

    private void Update()
    {
        RefreshVisuals();

        if (completed && disableAfterCompletion)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    $"E basıldı. PlayerInRange: {PlayerInRange}, " +
                    $"Minigame atandı: {minigame != null}",
                    gameObject
                );
            }

            if (!PlayerInRange)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning(
                        "E basıldı fakat oyuncu etkileşim alanında değil.",
                        gameObject
                    );
                }

                return;
            }

            OpenMinigame();
            return;
        }

        Mouse mouse = Mouse.current;

        if (mouse != null &&
            mouse.leftButton.wasPressedThisFrame)
        {
            TryOpenWithMouse(mouse.position.ReadValue());
        }
    }

    private void OpenMinigame()
    {
        if (minigame == null)
        {
            Debug.LogError(
                "ComputerTaskInteractable üzerindeki Minigame alanı boş!",
                gameObject
            );

            return;
        }

        if (minigame.IsOpen)
        {
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log("Minigame açılıyor.", gameObject);
        }

        minigame.OpenMinigame();
    }

    private void TryOpenWithMouse(Vector2 screenPosition)
    {
        if (clickRequiresPlayerInRange && !PlayerInRange)
        {
            return;
        }

        if (interactionArea == null)
        {
            Debug.LogError(
                "Interaction Area alanı boş.",
                gameObject
            );

            return;
        }

        if (worldCamera == null)
        {
            Debug.LogError(
                "World Camera alanı boş.",
                gameObject
            );

            return;
        }

        Vector3 screenPoint = new Vector3(
            screenPosition.x,
            screenPosition.y,
            Mathf.Abs(worldCamera.transform.position.z)
        );

        Vector3 worldPoint =
            worldCamera.ScreenToWorldPoint(screenPoint);

        if (interactionArea.OverlapPoint(worldPoint))
        {
            OpenMinigame();
        }
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            return true;
        }

        if (other.attachedRigidbody != null &&
            other.attachedRigidbody.CompareTag("Player"))
        {
            return true;
        }

        Transform root = other.transform.root;

        return root != null && root.CompareTag("Player");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (showDebugLogs)
        {
            Debug.Log(
                $"Computer trigger giriş: {other.name}, " +
                $"Tag: {other.tag}",
                gameObject
            );
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerColliderCount++;

        if (showDebugLogs)
        {
            Debug.Log(
                $"Oyuncu bilgisayar alanına girdi. " +
                $"Collider sayısı: {playerColliderCount}",
                gameObject
            );
        }

        RefreshVisuals();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerColliderCount--;

        if (playerColliderCount < 0)
        {
            playerColliderCount = 0;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"Oyuncu bilgisayar alanından çıktı. " +
                $"Collider sayısı: {playerColliderCount}",
                gameObject
            );
        }

        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        bool taskAvailable =
            !completed || !disableAfterCompletion;

        if (taskHighlight != null)
        {
            taskHighlight.SetActive(
                taskAvailable &&
                (minigame == null || !minigame.IsOpen)
            );
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(
                taskAvailable &&
                PlayerInRange &&
                minigame != null &&
                !minigame.IsOpen
            );
        }
    }

    public void MarkCompleted()
    {
        completed = true;
        RefreshVisuals();
    }

    public void ResetTask()
    {
        completed = false;
        RefreshVisuals();
    }
}