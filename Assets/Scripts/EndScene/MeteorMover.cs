using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MeteorMover : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float fallSpeed = 5f;

    [Header("Cleanup")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Meteor tamamen ekranın altına çıktıktan sonra bırakılacak ek mesafe.")]
    [Min(0f)]
    [SerializeField] private float destroyPadding = 1f;

    private Rigidbody2D meteorRigidbody;
    private Collider2D meteorCollider;

    private bool movementEnabled = true;

    private void Awake()
    {
        meteorRigidbody = GetComponent<Rigidbody2D>();
        meteorCollider = GetComponent<Collider2D>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError(
                "MeteorMover: Main Camera bulunamadı.",
                this
            );

            enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (!movementEnabled)
        {
            return;
        }

        Vector2 nextPosition =
            meteorRigidbody.position
            + Vector2.down * fallSpeed * Time.fixedDeltaTime;

        meteorRigidbody.MovePosition(nextPosition);
    }

    private void LateUpdate()
    {
        CheckDestroyPosition();
    }

    private void CheckDestroyPosition()
    {
        if (targetCamera == null)
        {
            return;
        }

        float cameraDistance = Mathf.Abs(
            targetCamera.transform.position.z
            - transform.position.z
        );

        Vector3 cameraBottomPosition =
            targetCamera.ViewportToWorldPoint(
                new Vector3(0f, 0f, cameraDistance)
            );

        float destroyY =
            cameraBottomPosition.y - destroyPadding;

        // Collider'ın üst kısmı bile ekranın altına çıktıysa
        // meteor artık görünmüyor demektir.
        if (meteorCollider.bounds.max.y < destroyY)
        {
            Destroy(gameObject);
        }
    }

    public void SetFallSpeed(float newSpeed)
    {
        fallSpeed = Mathf.Max(0f, newSpeed);
    }

    public void SetMovementEnabled(bool isEnabled)
    {
        movementEnabled = isEnabled;
    }

    public float GetFallSpeed()
    {
        return fallSpeed;
    }
}