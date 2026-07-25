using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class RocketTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bagAttachPoint;

    [Header("Delivery Settings")]
    [SerializeField] private bool attachBagToRocket = true;

    [Tooltip("Başarılı çantayı fizik sisteminden tamamen çıkarır. Böylece sonraki çantalar ona çarpmaz.")]
    [SerializeField] private bool disablePhysicsAfterDelivery = true;

    [Header("Debug")]
    [SerializeField] private bool printSuccessMessage = true;

    private Collider2D targetCollider;
    private readonly HashSet<BagProjectile> acceptedBags =
        new HashSet<BagProjectile>();

    /// <summary>
    /// Bir çanta başarıyla hedefe ulaştığında çalışır.
    /// Daha sonra skor ve oyun akışı bu olayı dinleyecek.
    /// </summary>
    public event Action<BagProjectile> BagDelivered;

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
        targetCollider = GetComponent<Collider2D>();

        PrepareTargetCollider();
        PrepareAttachPoint();
    }

    private void PrepareTargetCollider()
    {
        if (targetCollider.isTrigger)
        {
            return;
        }

        targetCollider.isTrigger = true;

        Debug.LogWarning("RocketTarget: TargetZone collider'ının Is Trigger ayarı otomatik olarak açıldı.", this);
    }

    private void PrepareAttachPoint()
    {
        if (bagAttachPoint != null)
        {
            return;
        }

        bagAttachPoint = transform;

        Debug.LogWarning("RocketTarget: Bag Attach Point atanmamış. TargetZone konumu kullanılacak.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryAcceptBag(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryAcceptBag(other);
    }

    private void TryAcceptBag(Collider2D other)
    {
        BagProjectile incomingBag = FindBagProjectile(other);

        if (
            incomingBag == null ||
            acceptedBags.Contains(incomingBag))
        {
            return;
        }

        GroundBagPickup pickup =
            incomingBag.GetComponent<GroundBagPickup>();

        bool canBeDelivered =
            incomingBag.IsThrown ||
            (pickup != null && pickup.IsHeld);

        if (!canBeDelivered)
        {
            return;
        }

        acceptedBags.Add(incomingBag);
        AcceptBag(incomingBag);
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

    private void AcceptBag(BagProjectile bag)
    {
        GroundBagPickup pickup =
            bag.GetComponent<GroundBagPickup>();

        if (pickup != null)
        {
            pickup.MarkAsDelivered();
        }

        if (attachBagToRocket)
        {
            bag.AttachTo(bagAttachPoint);
        }
        else
        {
            bag.StopMotion();
        }

        if (disablePhysicsAfterDelivery)
        {
            bag.Body.simulated = false;
        }

        if (printSuccessMessage)
        {
            Debug.Log("Başarılı atış! Çanta rokete ulaştı.", bag);
        }

        BagDelivered?.Invoke(bag);
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D colliderComponent = GetComponent<Collider2D>();

        if (colliderComponent == null)
        {
            return;
        }

        Gizmos.color = new Color(0f, 1f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(colliderComponent.bounds.center, colliderComponent.bounds.size);
    }
}
