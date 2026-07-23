using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class SpriteArtSlot : MonoBehaviour
{
    [Header("Art Settings")]
    [SerializeField] private GameArtConfig artConfig;
    [SerializeField] private GameArtType artType;

    [Tooltip(
        "Açık olduğunda sprite bozulmadan, " +
        "placeholder alanının içine sığdırılır."
    )]
    [SerializeField] private bool preserveAspectRatio = true;

    [Tooltip(
        "Sadece placeholder kullanılırken görünmesi gereken nesneler. " +
        "Örneğin geçici çanta tutacağı."
    )]
    [SerializeField] private GameObject[] placeholderOnlyObjects;

    [Header("Saved Placeholder Data")]
    [SerializeField] private Sprite placeholderSprite;
    [SerializeField] private Color placeholderColor = Color.white;
    [SerializeField] private Vector3 placeholderLocalScale =
        Vector3.one;

    [SerializeField] private Vector2 placeholderVisualSize =
        Vector2.one;

    private SpriteRenderer targetRenderer;

    private void Reset()
    {
        targetRenderer = GetComponent<SpriteRenderer>();
        CaptureCurrentAsPlaceholder();
    }

    private void Awake()
    {
        targetRenderer = GetComponent<SpriteRenderer>();

        EnsurePlaceholderIsCaptured();
        ApplyArt();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }
    }
#endif

    [ContextMenu("Capture Current As Placeholder")]
    public void CaptureCurrentAsPlaceholder()
    {
        targetRenderer = GetComponent<SpriteRenderer>();

        if (targetRenderer == null)
        {
            return;
        }

        placeholderSprite = targetRenderer.sprite;
        placeholderColor = targetRenderer.color;
        placeholderLocalScale = transform.localScale;

        if (placeholderSprite != null)
        {
            Vector2 spriteSize =
                placeholderSprite.bounds.size;

            placeholderVisualSize = new Vector2(
                Mathf.Abs(
                    spriteSize.x *
                    placeholderLocalScale.x
                ),
                Mathf.Abs(
                    spriteSize.y *
                    placeholderLocalScale.y
                )
            );
        }
        else
        {
            placeholderVisualSize = new Vector2(
                Mathf.Abs(placeholderLocalScale.x),
                Mathf.Abs(placeholderLocalScale.y)
            );
        }
    }

    [ContextMenu("Apply Art")]
    public void ApplyArt()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetRenderer == null)
        {
            return;
        }

        EnsurePlaceholderIsCaptured();

        Sprite selectedSprite = null;

        if (artConfig != null)
        {
            selectedSprite =
                artConfig.GetSprite(artType);
        }

        if (selectedSprite == null)
        {
            RestorePlaceholder();
            return;
        }

        targetRenderer.sprite = selectedSprite;
        targetRenderer.color = Color.white;

        FitSpriteToPlaceholderSize(
            selectedSprite
        );

        SetPlaceholderObjectsActive(false);
    }

    private void EnsurePlaceholderIsCaptured()
    {
        bool hasValidSize =
            placeholderVisualSize.x > 0f &&
            placeholderVisualSize.y > 0f;

        if (
            placeholderSprite == null ||
            !hasValidSize)
        {
            CaptureCurrentAsPlaceholder();
        }
    }

    private void RestorePlaceholder()
    {
        if (placeholderSprite != null)
        {
            targetRenderer.sprite =
                placeholderSprite;
        }

        targetRenderer.color =
            placeholderColor;

        transform.localScale =
            placeholderLocalScale;

        SetPlaceholderObjectsActive(true);
    }

    private void FitSpriteToPlaceholderSize(
        Sprite selectedSprite)
    {
        Vector2 spriteSize =
            selectedSprite.bounds.size;

        if (
            spriteSize.x <= Mathf.Epsilon ||
            spriteSize.y <= Mathf.Epsilon)
        {
            return;
        }

        float scaleX =
            placeholderVisualSize.x /
            spriteSize.x;

        float scaleY =
            placeholderVisualSize.y /
            spriteSize.y;

        float signX =
            Mathf.Sign(placeholderLocalScale.x);

        float signY =
            Mathf.Sign(placeholderLocalScale.y);

        if (Mathf.Approximately(signX, 0f))
        {
            signX = 1f;
        }

        if (Mathf.Approximately(signY, 0f))
        {
            signY = 1f;
        }

        if (preserveAspectRatio)
        {
            float uniformScale =
                Mathf.Min(scaleX, scaleY);

            transform.localScale =
                new Vector3(
                    uniformScale * signX,
                    uniformScale * signY,
                    placeholderLocalScale.z
                );
        }
        else
        {
            transform.localScale =
                new Vector3(
                    scaleX * signX,
                    scaleY * signY,
                    placeholderLocalScale.z
                );
        }
    }

    private void SetPlaceholderObjectsActive(
        bool isActive)
    {
        if (placeholderOnlyObjects == null)
        {
            return;
        }

        foreach (
            GameObject placeholderObject
            in placeholderOnlyObjects)
        {
            if (placeholderObject != null)
            {
                placeholderObject.SetActive(
                    isActive
                );
            }
        }
    }
}