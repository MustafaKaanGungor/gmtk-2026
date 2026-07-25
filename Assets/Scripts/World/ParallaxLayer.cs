using UnityEngine;

[DisallowMultipleComponent]
public sealed class ParallaxLayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax")]
    [Tooltip("0 = ekranda sabit, 1 = dünya içerisinde tamamen sabit")]
    [Range(0f, 1f)]
    [SerializeField] private float horizontalStrength = 0.1f;

    [Tooltip("0 = ekranda sabit, 1 = dünya içerisinde tamamen sabit")]
    [Range(0f, 1f)]
    [SerializeField] private float verticalStrength = 0f;

    [Header("Optional cloud movement")]
    [Tooltip("Kamera dururken bile katmanın kendi kendine hareket etme hızı.")]
    [SerializeField] private Vector2 driftSpeed;

    private Vector3 startingLayerPosition;
    private Vector3 startingCameraPosition;
    private Vector3 driftOffset;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        if (cameraTransform == null)
        {
            Debug.LogError(
                $"{name}: Parallax için kamera bulunamadı. " +
                "Main Camera etiketini veya Camera Transform alanını kontrol et.",
                this
            );

            enabled = false;
            return;
        }

        startingLayerPosition = transform.position;
        startingCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 cameraMovement =
            cameraTransform.position - startingCameraPosition;

        driftOffset += new Vector3(
            driftSpeed.x,
            driftSpeed.y,
            0f
        ) * Time.deltaTime;

        float cameraFollowX = 1f - horizontalStrength;
        float cameraFollowY = 1f - verticalStrength;

        transform.position = new Vector3(
            startingLayerPosition.x +
            cameraMovement.x * cameraFollowX +
            driftOffset.x,

            startingLayerPosition.y +
            cameraMovement.y * cameraFollowY +
            driftOffset.y,

            startingLayerPosition.z
        );
    }
}