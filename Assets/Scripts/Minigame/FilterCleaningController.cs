using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FilterCleaningController : MonoBehaviour
{
    private sealed class CleaningZone
    {
        public Image dirtyImage;
        public Color baseColor;
        public float progress;
    }

    [Header("UI References")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Filtrenin temizlenebilir gövdesini çevreleyen RectTransform.")]
    [SerializeField] private RectTransform cleaningArea;

    [Tooltip("ContentRoot altındaki mevcut DirtyFilter Image nesnesi.")]
    [SerializeField] private Image dirtyFilterTemplate;

    [Tooltip("BrushCursor üzerindeki BrushCursorController.")]
    [SerializeField] private BrushCursorController brushCursor;

    [SerializeField] private TMP_Text progressText;

    [Header("Cleaning Zones")]
    [Range(2, 12)]
    [SerializeField] private int zoneCount = 6;

    [Tooltip(
        "Bir bölümü tamamen temizlemek için gereken yaklaşık fırça hareketi."
    )]
    [Min(1f)]
    [SerializeField] private float movementNeededPerZone = 650f;

    [Tooltip(
        "Bundan küçük hareketler el titremesi kabul edilir ve sayılmaz."
    )]
    [Min(0f)]
    [SerializeField] private float minimumMovement = 1.5f;

    [Tooltip(
        "Tek karede sayılabilecek en yüksek hareket. " +
        "Fareyi ışınlar gibi hareket ettirerek temizlemeyi engeller."
    )]
    [Min(1f)]
    [SerializeField] private float maximumMovementPerFrame = 80f;

    [Tooltip(
        "Dilimlerin arasında ince çizgi oluşmasını engelleyen örtüşme."
    )]
    [Min(0f)]
    [SerializeField] private float maskOverlap = 2f;

    [Tooltip(
        "Açıksa minigame her açıldığında filtre yeniden kirlenir."
    )]
    [SerializeField] private bool resetWheneverOpened;

    [Header("Completion")]
    [SerializeField] private UnityEvent onCleaningCompleted;

    private CleaningZone[] zones;
    private RectTransform generatedSlicesRoot;
    private Camera uiCamera;

    private Vector2 previousLocalBrushPosition;
    private bool hasPreviousBrushPosition;

    private bool zonesBuilt;
    private bool cleaningCompleted;

    public bool IsCompleted => cleaningCompleted;

    public float OverallProgress =>
        CalculateOverallProgress();

    private void Awake()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        RefreshUICamera();
        BuildDirtySlices();
    }

    private void OnEnable()
    {
        hasPreviousBrushPosition = false;

        if (resetWheneverOpened && zonesBuilt)
        {
            ResetCleaning();
        }
    }

    private void Update()
    {
        if (!zonesBuilt ||
            cleaningCompleted ||
            brushCursor == null)
        {
            return;
        }

        if (!brushCursor.IsBrushing)
        {
            hasPreviousBrushPosition = false;
            return;
        }

        if (!TryGetBrushLocalPosition(
                out Vector2 currentLocalPosition))
        {
            hasPreviousBrushPosition = false;
            return;
        }

        if (!hasPreviousBrushPosition)
        {
            previousLocalBrushPosition =
                currentLocalPosition;

            hasPreviousBrushPosition = true;
            return;
        }

        float movementDistance = Vector2.Distance(
            currentLocalPosition,
            previousLocalBrushPosition
        );

        previousLocalBrushPosition =
            currentLocalPosition;

        if (movementDistance < minimumMovement)
        {
            return;
        }

        movementDistance = Mathf.Min(
            movementDistance,
            maximumMovementPerFrame
        );

        Rect areaRect = cleaningArea.rect;

        if (!areaRect.Contains(currentLocalPosition))
        {
            return;
        }

        int zoneIndex = GetZoneIndex(
            currentLocalPosition.x,
            areaRect
        );

        AddCleaningProgress(
            zoneIndex,
            movementDistance
        );
    }

    private void BuildDirtySlices()
    {
        if (cleaningArea == null)
        {
            Debug.LogError(
                "FilterCleaningController: " +
                "Cleaning Area atanmamış.",
                this
            );

            enabled = false;
            return;
        }

        if (dirtyFilterTemplate == null)
        {
            Debug.LogError(
                "FilterCleaningController: " +
                "Dirty Filter Template atanmamış.",
                this
            );

            enabled = false;
            return;
        }

        if (zoneCount < 1)
        {
            zoneCount = 1;
        }

        zones = new CleaningZone[zoneCount];

        RectTransform dirtyRect =
            dirtyFilterTemplate.rectTransform;

        Transform dirtyParent = dirtyRect.parent;

        GameObject rootObject = new GameObject(
            "GeneratedDirtySlices",
            typeof(RectTransform)
        );

        generatedSlicesRoot =
            rootObject.GetComponent<RectTransform>();

        generatedSlicesRoot.SetParent(
            dirtyParent,
            false
        );

        generatedSlicesRoot.anchorMin =
            Vector2.zero;

        generatedSlicesRoot.anchorMax =
            Vector2.one;

        generatedSlicesRoot.offsetMin =
            Vector2.zero;

        generatedSlicesRoot.offsetMax =
            Vector2.zero;

        generatedSlicesRoot.pivot =
            new Vector2(0.5f, 0.5f);

        generatedSlicesRoot.localScale =
            Vector3.one;

        generatedSlicesRoot.SetSiblingIndex(
            dirtyRect.GetSiblingIndex()
        );

        Vector3[] worldCorners =
            new Vector3[4];

        cleaningArea.GetWorldCorners(
            worldCorners
        );

        Vector3 localBottomLeft =
            generatedSlicesRoot.InverseTransformPoint(
                worldCorners[0]
            );

        Vector3 localTopRight =
            generatedSlicesRoot.InverseTransformPoint(
                worldCorners[2]
            );

        float areaWidth =
            localTopRight.x -
            localBottomLeft.x;

        float areaHeight =
            localTopRight.y -
            localBottomLeft.y;

        float sliceWidth =
            areaWidth / zoneCount;

        float centerY =
            (localBottomLeft.y +
             localTopRight.y) * 0.5f;

        for (int i = 0; i < zoneCount; i++)
        {
            GameObject maskObject =
                new GameObject(
                    $"DirtySliceMask_{i + 1:00}",
                    typeof(RectTransform),
                    typeof(RectMask2D)
                );

            RectTransform maskRect =
                maskObject.GetComponent<RectTransform>();

            maskRect.SetParent(
                generatedSlicesRoot,
                false
            );

            maskRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            maskRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            maskRect.pivot =
                new Vector2(0.5f, 0.5f);

            maskRect.sizeDelta =
                new Vector2(
                    sliceWidth +
                    maskOverlap * 2f,

                    areaHeight +
                    maskOverlap * 2f
                );

            float centerX =
                localBottomLeft.x +
                sliceWidth * (i + 0.5f);

            maskRect.localPosition =
                new Vector3(
                    centerX,
                    centerY,
                    0f
                );

            maskRect.localRotation =
                Quaternion.identity;

            maskRect.localScale =
                Vector3.one;

            // Kirli filtrenin bir kopyasını oluşturur.
            // World-space görünümünü koruyarak
            // ilgili maskenin altına taşır.
            Image dirtySlice = Instantiate(
                dirtyFilterTemplate,
                maskRect,
                true
            );

            dirtySlice.name =
                $"DirtyVisual_{i + 1:00}";

            dirtySlice.raycastTarget = false;
            dirtySlice.gameObject.SetActive(true);

            zones[i] = new CleaningZone
            {
                dirtyImage = dirtySlice,
                baseColor = dirtySlice.color,
                progress = 0f
            };
        }

        // Orijinal görsel artık yalnızca şablondu.
        // Ekranda otomatik oluşturulan dilimler gösterilecek.
        dirtyFilterTemplate.gameObject.SetActive(false);

        zonesBuilt = true;

        UpdateAllZoneVisuals();
        UpdateProgressText();
    }

    private void RefreshUICamera()
{
    if (canvas == null)
    {
        uiCamera = null;
        return;
    }

    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
    {
        // Screen Space - Overlay Canvas için kamera null olmalıdır.
        uiCamera = null;
    }
    else
    {
        // Screen Space - Camera veya World Space Canvas.
        uiCamera = canvas.worldCamera != null
            ? canvas.worldCamera
            : Camera.main;
    }
}

    private bool TryGetBrushLocalPosition(
        out Vector2 localPosition)
    {
        localPosition = Vector2.zero;

        if (cleaningArea == null ||
            brushCursor == null)
        {
            return false;
        }

        return RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                cleaningArea,
                brushCursor.ContactScreenPosition,
                uiCamera,
                out localPosition
            );
    }

    private int GetZoneIndex(
        float localX,
        Rect areaRect)
    {
        float normalizedX = Mathf.InverseLerp(
            areaRect.xMin,
            areaRect.xMax,
            localX
        );

        int index = Mathf.FloorToInt(
            normalizedX * zoneCount
        );

        return Mathf.Clamp(
            index,
            0,
            zoneCount - 1
        );
    }

    private void AddCleaningProgress(
        int zoneIndex,
        float movementDistance)
    {
        if (zoneIndex < 0 ||
            zoneIndex >= zones.Length)
        {
            return;
        }

        CleaningZone zone =
            zones[zoneIndex];

        if (zone.progress >= 1f)
        {
            return;
        }

        float progressAmount =
            movementDistance /
            movementNeededPerZone;

        zone.progress = Mathf.Clamp01(
            zone.progress +
            progressAmount
        );

        UpdateZoneVisual(zone);
        UpdateProgressText();
        CheckCompletion();
    }

    private void UpdateZoneVisual(
        CleaningZone zone)
    {
        if (zone == null ||
            zone.dirtyImage == null)
        {
            return;
        }

        float fadedProgress =
            Mathf.SmoothStep(
                0f,
                1f,
                zone.progress
            );

        Color color =
            zone.baseColor;

        color.a =
            zone.baseColor.a *
            (1f - fadedProgress);

        zone.dirtyImage.color = color;
    }

    private void UpdateAllZoneVisuals()
    {
        if (zones == null)
        {
            return;
        }

        foreach (CleaningZone zone in zones)
        {
            UpdateZoneVisual(zone);
        }
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
        {
            return;
        }

        int percentage = Mathf.RoundToInt(
            CalculateOverallProgress() * 100f
        );

        progressText.text =
            $"CLEANING: {percentage}%";
    }

    private float CalculateOverallProgress()
    {
        if (zones == null ||
            zones.Length == 0)
        {
            return 0f;
        }

        float totalProgress = 0f;

        foreach (CleaningZone zone in zones)
        {
            totalProgress += zone.progress;
        }

        return totalProgress / zones.Length;
    }

    private void CheckCompletion()
    {
        if (cleaningCompleted)
        {
            return;
        }

        foreach (CleaningZone zone in zones)
        {
            if (zone.progress < 1f)
            {
                return;
            }
        }

        cleaningCompleted = true;
        hasPreviousBrushPosition = false;

        if (progressText != null)
        {
            progressText.text =
                "CLEANING: 100%";
        }

        onCleaningCompleted?.Invoke();
    }

    public void ResetCleaning()
    {
        if (!zonesBuilt ||
            zones == null)
        {
            return;
        }

        cleaningCompleted = false;
        hasPreviousBrushPosition = false;

        foreach (CleaningZone zone in zones)
        {
            zone.progress = 0f;
        }

        UpdateAllZoneVisuals();
        UpdateProgressText();
    }
}