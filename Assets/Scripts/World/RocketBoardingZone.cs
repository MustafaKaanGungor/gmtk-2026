using System;
using UnityEngine;

/// <summary>
/// Roketin yanindaki bekleme alani. Baslangicta kapalidir; gorevler bitince
/// aktif edilir. Oyuncu (PlayerMovement2D) alana girdiginde event tetikler.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class RocketBoardingZone : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Acik olursa oyun basinda alan zaten aktiftir.")]
    [SerializeField] private bool startActive = false;

    [Header("Debug")]
    [SerializeField] private bool printMessages = true;

    /// <summary>Oyuncu, alan aktifken iceri girdiginde tetiklenir.</summary>
    public event Action PlayerEntered;

    public bool IsActive { get; private set; }

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
                "RocketBoardingZone: Collider'in Is Trigger ayari " +
                "otomatik acildi.",
                this
            );
        }

        IsActive = startActive;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsActive)
        {
            return;
        }

        PlayerMovement2D player =
            other.GetComponentInParent<PlayerMovement2D>();

        if (player == null)
        {
            return;
        }

        if (printMessages)
        {
            Debug.Log(
                "RocketBoardingZone: Oyuncu alana girdi.",
                this
            );
        }

        PlayerEntered?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D colliderComponent = GetComponent<Collider2D>();

        if (colliderComponent == null)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        Gizmos.DrawWireCube(
            colliderComponent.bounds.center,
            colliderComponent.bounds.size
        );
    }
}
