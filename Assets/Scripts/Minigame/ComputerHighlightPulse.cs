using UnityEngine;

public class ComputerHighlightPulse : MonoBehaviour
{
    [Header("Sprite References")]
    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private SpriteRenderer glowRenderer;

    [Header("Pulse Settings")]
    [SerializeField] private Color glowColor =
        new Color(1f, 0.25f, 0.05f, 0.65f);

    [SerializeField] private float minimumScale = 1.05f;
    [SerializeField] private float maximumScale = 1.12f;
    [SerializeField] private float pulseSpeed = 3f;

    [SerializeField] private float minimumAlphaMultiplier = 0.45f;
    [SerializeField] private float maximumAlphaMultiplier = 1f;

    [Header("Completed Settings")]
    [SerializeField] private Color completedGlowColor =
        new Color(0.25f, 1f, 0.35f, 0.8f);

    private Vector3 originalLocalScale;
    private bool completed;

    private void Awake()
    {
        if (glowRenderer == null)
        {
            glowRenderer = GetComponent<SpriteRenderer>();
        }

        if (glowRenderer != null)
        {
            originalLocalScale =
                glowRenderer.transform.localScale;
        }
        else
        {
            originalLocalScale = transform.localScale;
        }
    }

    private void LateUpdate()
    {
        if (sourceRenderer == null || glowRenderer == null)
        {
            return;
        }

        SyncSprite();

        if (completed)
        {
            ApplyCompletedVisual();
            return;
        }

        float pulseValue =
            (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;

        float scaleMultiplier = Mathf.Lerp(
            minimumScale,
            maximumScale,
            pulseValue
        );

        glowRenderer.transform.localScale =
            originalLocalScale * scaleMultiplier;

        float alphaMultiplier = Mathf.Lerp(
            minimumAlphaMultiplier,
            maximumAlphaMultiplier,
            pulseValue
        );

        Color currentColor = glowColor;
        currentColor.a *= alphaMultiplier;

        glowRenderer.color = currentColor;
    }

    public void SetCompleted(bool isCompleted)
    {
        completed = isCompleted;

        if (completed && glowRenderer != null)
        {
            ApplyCompletedVisual();
        }
    }

    private void ApplyCompletedVisual()
    {
        glowRenderer.transform.localScale =
            originalLocalScale * maximumScale;

        glowRenderer.color = completedGlowColor;
    }

    private void SyncSprite()
    {
        glowRenderer.sprite = sourceRenderer.sprite;
        glowRenderer.flipX = sourceRenderer.flipX;
        glowRenderer.flipY = sourceRenderer.flipY;

        glowRenderer.sortingLayerID =
            sourceRenderer.sortingLayerID;

        glowRenderer.sortingOrder =
            sourceRenderer.sortingOrder - 1;

        glowRenderer.drawMode =
            sourceRenderer.drawMode;

        glowRenderer.size =
            sourceRenderer.size;
    }
}
