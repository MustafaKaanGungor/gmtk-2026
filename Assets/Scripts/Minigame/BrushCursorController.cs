using UnityEngine;

public class BrushCursorController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas canvas;

    [Tooltip("VentMinigamePanel RectTransform nesnesini buraya sürükle.")]
    [SerializeField] private RectTransform cursorArea;

    [Tooltip("BrushRotationRoot nesnesini buraya sürükle.")]
    [SerializeField] private RectTransform rotationRoot;

    [Tooltip("BrushVisual nesnesini buraya sürükle.")]
    [SerializeField] private RectTransform brushVisual;

    [Tooltip("BrushContactPoint nesnesini buraya sürükle.")]
    [SerializeField] private RectTransform contactPoint;

    [Header("Follow Settings")]
    [Tooltip("Yüksek değer fırçanın fareyi daha hızlı takip etmesini sağlar.")]
    [SerializeField] private float followSharpness = 40f;

    [Tooltip("Fırçanın minigame panelinin dışına çıkmasını engeller.")]
    [SerializeField] private bool clampInsideArea = true;

    [SerializeField] private Vector2 edgePadding = Vector2.zero;

    [Header("Tilt Settings")]
    [SerializeField] private bool tiltWithMovement = true;

    [SerializeField] private float maximumTiltAngle = 12f;

    [Tooltip("Farenin yatay hareketinin dönüşe etkisi.")]
    [SerializeField] private float tiltSensitivity = 0.12f;

    [SerializeField] private float tiltSharpness = 18f;

    [Header("Click Feedback")]
    [Tooltip("Sol tık basılıyken uygulanacak ölçek.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float pressedScale = 0.97f;

    [SerializeField] private float scaleSharpness = 18f;

    private RectTransform brushRoot;
    private Camera uiCamera;

    private Vector2 targetLocalPosition;
    private Vector2 previousMousePosition;

    private bool interactionEnabled = true;

    private Vector3 originalVisualScale;

    private bool previousCursorVisibility;
    private CursorLockMode previousCursorLockMode;

    /// <summary>
    /// Sol fare tuşunun basılı olup olmadığını bildirir.
    /// Temizleme sisteminde bunu kullanacağız.
    /// </summary>
    public bool IsBrushing =>
    interactionEnabled &&
    Input.GetMouseButton(0);

    /// <summary>
    /// Fırçanın filtreye temas eden UI noktasını döndürür.
    /// </summary>
    public RectTransform ContactPoint => contactPoint;

    /// <summary>
    /// Temas noktasının ekran koordinatını döndürür.
    /// </summary>
    public Vector2 ContactScreenPosition
    {
        get
        {
            Transform pointTransform =
                contactPoint != null
                    ? contactPoint
                    : brushRoot;

            return RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                pointTransform.position
            );
        }
    }

    private void Awake()
    {
        brushRoot = GetComponent<RectTransform>();

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (cursorArea == null &&
            brushRoot != null)
        {
            cursorArea =
                brushRoot.parent as RectTransform;
        }

        if (brushVisual != null)
        {
            originalVisualScale =
                brushVisual.localScale;
        }
        else
        {
            originalVisualScale = Vector3.one;
        }

        RefreshUICamera();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        interactionEnabled = true;
        SetBrushVisible(true);

        RefreshUICamera();

        // Minigame açılmadan önceki cursor durumunu sakla.
        previousCursorVisibility = Cursor.visible;
        previousCursorLockMode = Cursor.lockState;

        // Normal imleci gizle ve ekran kilidini kaldır.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        previousMousePosition = Input.mousePosition;

        // Minigame açıldığı anda fırçayı doğrudan
        // fare konumuna yerleştir.
        if (TryGetMouseLocalPosition(
                out Vector2 initialPosition))
        {
            targetLocalPosition = initialPosition;

            if (brushRoot != null)
            {
                brushRoot.anchoredPosition =
                    initialPosition;
            }
        }

        if (rotationRoot != null)
        {
            rotationRoot.localRotation =
                Quaternion.identity;
        }

        if (brushVisual != null)
        {
            brushVisual.localScale =
                originalVisualScale;
        }
    }

    private void Update()
    {
        if (brushRoot == null ||
            cursorArea == null)
        {
            return;
        }

        UpdateTargetPosition();
        UpdateBrushPosition();
        UpdateBrushTilt();
        UpdateClickFeedback();

        previousMousePosition = Input.mousePosition;
    }

    private void UpdateTargetPosition()
    {
        if (TryGetMouseLocalPosition(
                out Vector2 localPosition))
        {
            targetLocalPosition = localPosition;
        }
    }

    private void UpdateBrushPosition()
    {
        float interpolation =
            ExponentialInterpolation(
                followSharpness
            );

        brushRoot.anchoredPosition =
            Vector2.Lerp(
                brushRoot.anchoredPosition,
                targetLocalPosition,
                interpolation
            );
    }

    private void UpdateBrushTilt()
    {
        if (rotationRoot == null)
        {
            return;
        }

        float targetAngle = 0f;

        if (tiltWithMovement)
        {
            Vector2 currentMousePosition =
                Input.mousePosition;

            float horizontalMovement =
                currentMousePosition.x -
                previousMousePosition.x;

            targetAngle = Mathf.Clamp(
                -horizontalMovement *
                tiltSensitivity,
                -maximumTiltAngle,
                maximumTiltAngle
            );
        }

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                0f,
                targetAngle
            );

        float interpolation =
            ExponentialInterpolation(
                tiltSharpness
            );

        rotationRoot.localRotation =
            Quaternion.Lerp(
                rotationRoot.localRotation,
                targetRotation,
                interpolation
            );
    }

    private void UpdateClickFeedback()
    {
        if (brushVisual == null)
        {
            return;
        }

        float scaleMultiplier =
            IsBrushing
                ? pressedScale
                : 1f;

        Vector3 targetScale =
            originalVisualScale *
            scaleMultiplier;

        float interpolation =
            ExponentialInterpolation(
                scaleSharpness
            );

        brushVisual.localScale =
            Vector3.Lerp(
                brushVisual.localScale,
                targetScale,
                interpolation
            );
    }

    private bool TryGetMouseLocalPosition(
        out Vector2 localPosition)
    {
        localPosition = Vector2.zero;

        if (cursorArea == null)
        {
            return false;
        }

        bool converted =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    cursorArea,
                    Input.mousePosition,
                    uiCamera,
                    out localPosition
                );

        if (!converted)
        {
            return false;
        }

        if (clampInsideArea)
        {
            Rect areaRect = cursorArea.rect;

            float minimumX =
                areaRect.xMin +
                edgePadding.x;

            float maximumX =
                areaRect.xMax -
                edgePadding.x;

            float minimumY =
                areaRect.yMin +
                edgePadding.y;

            float maximumY =
                areaRect.yMax -
                edgePadding.y;

            localPosition.x = Mathf.Clamp(
                localPosition.x,
                minimumX,
                maximumX
            );

            localPosition.y = Mathf.Clamp(
                localPosition.y,
                minimumY,
                maximumY
            );
        }

        return true;
    }

    private void RefreshUICamera()
    {
        if (canvas == null)
        {
            uiCamera = null;
            return;
        }

        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = null;
        }
        else
        {
            uiCamera =
                canvas.worldCamera != null
                    ? canvas.worldCamera
                    : Camera.main;
        }
    }

    private float ExponentialInterpolation(
        float sharpness)
    {
        return 1f -
               Mathf.Exp(
                   -sharpness *
                   Time.unscaledDeltaTime
               );
    }

    public void SetInteractionEnabled(bool enabled)
{
    interactionEnabled = enabled;

    if (enabled)
    {
        return;
    }

    if (rotationRoot != null)
    {
        rotationRoot.localRotation =
            Quaternion.identity;
    }

    if (brushVisual != null)
    {
        brushVisual.localScale =
            originalVisualScale;
    }
}

public void SetBrushVisible(bool visible)
{
    if (brushVisual != null)
    {
        brushVisual.gameObject.SetActive(visible);
    }

    if (contactPoint != null)
    {
        contactPoint.gameObject.SetActive(visible);
    }
}
    
    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // Minigame kapanınca önceki cursor durumuna dön.
        Cursor.lockState =
            previousCursorLockMode;

        Cursor.visible =
            previousCursorVisibility;
    }
}