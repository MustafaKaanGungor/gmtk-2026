using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RocketMeteorCollision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RocketDeathController deathController;

    private bool collisionHandled;

    private void Awake()
    {
        collisionHandled = false;

        if (deathController == null)
        {
            Debug.LogError(
                "RocketMeteorCollision: RocketDeathController atanmamış.",
                this
            );

            enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collisionHandled)
        {
            return;
        }

        // Çarpılan nesnenin kendisinde veya parent nesnesinde
        // MeteorMover olup olmadığını kontrol eder.
        MeteorMover meteor =
            other.GetComponentInParent<MeteorMover>();

        if (meteor == null)
        {
            return;
        }

        collisionHandled = true;

        // Çarpılan meteoru hemen durdur.
        meteor.SetMovementEnabled(false);

        deathController.KillRocket();
    }
}