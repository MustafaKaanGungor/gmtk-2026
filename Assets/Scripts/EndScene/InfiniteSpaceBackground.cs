using UnityEngine;

public class InfiniteSpaceBackground : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer backgroundA;
    [SerializeField] private SpriteRenderer backgroundB;

    [Header("Scrolling")]
    [Min(0f)]
    [SerializeField] private float scrollSpeed = 2f;

    [Header("Screen Fitting")]
    [Tooltip("Kenar çizgilerinin görünmesini engellemek için arka planı biraz büyütür.")]
    [Min(1f)]
    [SerializeField] private float extraScale = 1.02f;

    [Tooltip("Tekrarlanma hissini azaltmak için ikinci görseli dikey çevirir.")]
    [SerializeField] private bool flipSecondBackground = true;

    [Tooltip("İki görsel arasındaki çok küçük boşlukları kapatır.")]
    [Min(0f)]
    [SerializeField] private float overlap = 0.02f;

    private float backgroundHeight;

    private void Awake()
    {
        SetupBackgrounds();
    }

    private void Update()
    {
        float movement = scrollSpeed * Time.deltaTime;

        MoveBackground(backgroundA, movement);
        MoveBackground(backgroundB, movement);
    }

    [ContextMenu("Setup Backgrounds")]
    public void SetupBackgrounds()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError(
                "InfiniteSpaceBackground: Sahne içerisinde Main Camera bulunamadı.",
                this
            );

            enabled = false;
            return;
        }

        if (backgroundA == null || backgroundB == null)
        {
            Debug.LogError(
                "InfiniteSpaceBackground: Background A ve Background B atanmalı.",
                this
            );

            enabled = false;
            return;
        }

        if (backgroundA.sprite == null || backgroundB.sprite == null)
        {
            Debug.LogError(
                "InfiniteSpaceBackground: Arka plan nesnelerinde Sprite bulunamadı.",
                this
            );

            enabled = false;
            return;
        }

        FitBackgroundToCamera(backgroundA);
        FitBackgroundToCamera(backgroundB);

        backgroundHeight = backgroundA.bounds.size.y;

        float cameraX = targetCamera.transform.position.x;
        float cameraY = targetCamera.transform.position.y;

        Vector3 firstPosition = backgroundA.transform.position;
        firstPosition.x = cameraX;
        firstPosition.y = cameraY;
        backgroundA.transform.position = firstPosition;

        Vector3 secondPosition = backgroundB.transform.position;
        secondPosition.x = cameraX;
        secondPosition.y = cameraY + backgroundHeight - overlap;
        backgroundB.transform.position = secondPosition;

        backgroundB.flipY = flipSecondBackground;
    }

    private void FitBackgroundToCamera(SpriteRenderer background)
    {
        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;

        Vector2 originalSpriteSize = background.sprite.bounds.size;

        float horizontalScale = cameraWidth / originalSpriteSize.x;
        float verticalScale = cameraHeight / originalSpriteSize.y;

        // Büyük olan değeri seçerek ekranın tamamen kaplanmasını sağlıyoruz.
        // Böylece görsel esnetilmez; bazı kısımları ekran dışında kalabilir.
        float requiredScale = Mathf.Max(horizontalScale, verticalScale);
        requiredScale *= extraScale;

        background.transform.localScale = new Vector3(
            requiredScale,
            requiredScale,
            1f
        );
    }

    private void MoveBackground(
        SpriteRenderer background,
        float movement
    )
    {
        background.transform.position += Vector3.down * movement;

        float cameraY = targetCamera.transform.position.y;
        float recycleLimit = cameraY - backgroundHeight;

        if (background.transform.position.y <= recycleLimit)
        {
            float recycleDistance =
                (backgroundHeight * 2f) - (overlap * 2f);

            background.transform.position +=
                Vector3.up * recycleDistance;
        }
    }
}