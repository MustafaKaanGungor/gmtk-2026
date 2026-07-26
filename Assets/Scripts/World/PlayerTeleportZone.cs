using UnityEngine;

/// <summary>
/// Oyuncu bu trigger alanina girince onu belirlenen hedef noktaya isinlar.
/// Alanin ustune koyulan ayri bir collider ile oyuncuyu istenmeyen bolgeden
/// uzak tutmak icin kullanilir.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class PlayerTeleportZone : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Oyuncunun isinlanacagi hedef nokta.")]
    [SerializeField] private Transform teleportTarget;

    [Tooltip("Isinlarken oyuncunun hizini sifirla.")]
    [SerializeField] private bool resetVelocity = true;

    [Header("Debug")]
    [SerializeField] private bool printMessages = false;

    private void Reset()
    {
        Collider2D colliderComponent = GetComponent<Collider2D>();

        if (colliderComponent != null)
        {
            colliderComponent.isTrigger = true;
        }
    }

    private void Awake()
    {
        Collider2D colliderComponent = GetComponent<Collider2D>();

        if (!colliderComponent.isTrigger)
        {
            colliderComponent.isTrigger = true;

            Debug.LogWarning(
                "PlayerTeleportZone: Collider'in Is Trigger ayari " +
                "otomatik acildi.",
                this
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement2D player =
            other.GetComponentInParent<PlayerMovement2D>();

        if (player == null)
        {
            return;
        }

        if (teleportTarget == null)
        {
            Debug.LogWarning(
                "PlayerTeleportZone: Teleport Target atanmamis!",
                this
            );

            return;
        }

        if (resetVelocity)
        {
            Rigidbody2D playerBody =
                player.GetComponent<Rigidbody2D>();

            if (playerBody != null)
            {
                playerBody.linearVelocity = Vector2.zero;
                playerBody.angularVelocity = 0f;
            }
        }

        player.transform.position = teleportTarget.position;

        if (printMessages)
        {
            Debug.Log(
                "PlayerTeleportZone: oyuncu isinlandi.",
                this
            );
        }
    }
}
