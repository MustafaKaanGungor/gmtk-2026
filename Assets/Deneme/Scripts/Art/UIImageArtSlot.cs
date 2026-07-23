using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class UIImageArtSlot : MonoBehaviour
{
    [Header("Art Settings")]
    [SerializeField] private GameArtConfig artConfig;
    [SerializeField] private GameArtType artType;

    [Header("Saved Placeholder Data")]
    [SerializeField] private Sprite placeholderSprite;
    [SerializeField] private Color placeholderColor =
        Color.white;

    private Image targetImage;

    private void Reset()
    {
        targetImage = GetComponent<Image>();
        CaptureCurrentAsPlaceholder();
    }

    private void Awake()
    {
        targetImage = GetComponent<Image>();

        EnsurePlaceholderIsCaptured();
        ApplyArt();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }
#endif

    [ContextMenu("Capture Current As Placeholder")]
    public void CaptureCurrentAsPlaceholder()
    {
        targetImage = GetComponent<Image>();

        if (targetImage == null)
        {
            return;
        }

        placeholderSprite = targetImage.sprite;
        placeholderColor = targetImage.color;
    }

    [ContextMenu("Apply Art")]
    public void ApplyArt()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetImage == null)
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

        targetImage.sprite = selectedSprite;
        targetImage.color = Color.white;
    }

    private void EnsurePlaceholderIsCaptured()
    {
        if (placeholderSprite == null)
        {
            CaptureCurrentAsPlaceholder();
        }
    }

    private void RestorePlaceholder()
    {
        if (placeholderSprite != null)
        {
            targetImage.sprite =
                placeholderSprite;
        }

        targetImage.color =
            placeholderColor;
    }
}