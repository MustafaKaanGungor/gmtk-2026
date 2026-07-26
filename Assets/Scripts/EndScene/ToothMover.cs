using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ToothMover : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float fallSpeed = 10f;

    [Header("Cleanup")]
    [SerializeField] private Camera targetCamera;

    [Min(0f)]
    [SerializeField] private float destroyPadding = 1f;

    private Rigidbody2D toothRigidbody;
    private Collider2D toothCollider;

    private void Awake()
    {
        toothRigidbody = GetComponent<Rigidbody2D>();
        toothCollider = GetComponent<Collider2D>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError(
                "ToothMover: Main Camera bulunamadı.",
                this
            );

            enabled = false;
        }
    }

    private void FixedUpdate()
    {
        Vector2 nextPosition =
            toothRigidbody.position
            + Vector2.down
            * fallSpeed
            * Time.fixedDeltaTime;

        toothRigidbody.MovePosition(nextPosition);
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        float cameraDistance = Mathf.Abs(
            targetCamera.transform.position.z
            - transform.position.z
        );

        Vector3 cameraBottom =
            targetCamera.ViewportToWorldPoint(
                new Vector3(0f, 0f, cameraDistance)
            );

        float destroyY =
            cameraBottom.y - destroyPadding;

        if (toothCollider.bounds.max.y < destroyY)
        {
            Destroy(gameObject);
        }
    }

    public void SetFallSpeed(float newSpeed)
    {
        fallSpeed = Mathf.Max(0f, newSpeed);
    }
}