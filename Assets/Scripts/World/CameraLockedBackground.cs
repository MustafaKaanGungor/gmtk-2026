using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraLockedBackground : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform targetCamera;

    [Header("Position")]
    [SerializeField] private Vector2 offset;

    private void Awake()
    {
        FindCameraIfMissing();
    }

    private void LateUpdate()
    {
        FindCameraIfMissing();

        if (targetCamera == null)
        {
            return;
        }

        Vector3 currentPosition = transform.position;

        transform.position = new Vector3(
            targetCamera.position.x + offset.x,
            targetCamera.position.y + offset.y,
            currentPosition.z
        );
    }

    private void FindCameraIfMissing()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
    }
}