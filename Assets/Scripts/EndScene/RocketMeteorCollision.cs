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
                "RocketMeteorCollision: "
                + "RocketDeathController atanmamış.",
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

        RocketKiller killer =
            other.GetComponentInParent<RocketKiller>();

        if (killer == null)
        {
            return;
        }

        collisionHandled = true;

        FinalHazard finalHazard =
            other.GetComponentInParent<FinalHazard>();

        if (finalHazard != null)
        {
            // Diş yağmurundaki ölüm:
            // siyah ekran ve StoryboardScene.
            deathController.KillRocketAndLoadFinalScene();
        }
        else
        {
            // Normal meteor ölümü:
            // ölüm ekranı ve Tekrar Oyna.
            deathController.KillRocket();
        }
    }
}