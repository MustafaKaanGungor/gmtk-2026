using UnityEngine;
using UnityEngine.Rendering;

public class LadderForegroundSorting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerLadderClimber ladderClimber;

    [Tooltip("UpperFloorForeground objesinin SpriteRenderer bileşeni.")]
    [SerializeField] private SpriteRenderer upperFloorForeground;

    [Header("Player Sorting")]
    [Tooltip("Player objesinde SortingGroup varsa buraya sürükle.")]
    [SerializeField] private SortingGroup playerSortingGroup;

    [Tooltip("SortingGroup kullanmıyorsan Player SpriteRenderer'ını buraya sürükle.")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    private int originalForegroundLayerId;
    private int originalForegroundOrder;

    private bool wasClimbing;

    private void Awake()
    {
        if (ladderClimber == null)
        {
            ladderClimber = GetComponent<PlayerLadderClimber>();
        }

        if (playerSortingGroup == null)
        {
            playerSortingGroup = GetComponent<SortingGroup>();
        }

        if (playerSpriteRenderer == null)
        {
            playerSpriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (upperFloorForeground == null)
        {
            Debug.LogError(
                "Upper Floor Foreground SpriteRenderer atanmamış.",
                this
            );

            enabled = false;
            return;
        }

        // Foreground'un normal değerlerini sakla.
        originalForegroundLayerId =
            upperFloorForeground.sortingLayerID;

        originalForegroundOrder =
            upperFloorForeground.sortingOrder;
    }

    private void LateUpdate()
    {
        if (ladderClimber == null)
            return;

        if (ladderClimber.IsClimbing)
        {
            PutForegroundInFrontOfPlayer();
        }
        else if (wasClimbing)
        {
            RestoreForegroundSorting();
        }

        wasClimbing = ladderClimber.IsClimbing;
    }

    private void PutForegroundInFrontOfPlayer()
    {
        int playerLayerId;
        int playerOrder;

        // SortingGroup varsa onu kullan.
        if (playerSortingGroup != null)
        {
            playerLayerId =
                playerSortingGroup.sortingLayerID;

            playerOrder =
                playerSortingGroup.sortingOrder;
        }
        else if (playerSpriteRenderer != null)
        {
            playerLayerId =
                playerSpriteRenderer.sortingLayerID;

            playerOrder =
                playerSpriteRenderer.sortingOrder;
        }
        else
        {
            return;
        }

        // Foreground'u Player ile aynı Sorting Layer'a al.
        upperFloorForeground.sortingLayerID =
            playerLayerId;

        // Player'ın bir sıra önünde çiz.
        upperFloorForeground.sortingOrder =
            playerOrder + 1;
    }

    private void RestoreForegroundSorting()
    {
        if (upperFloorForeground == null)
            return;

        upperFloorForeground.sortingLayerID =
            originalForegroundLayerId;

        upperFloorForeground.sortingOrder =
            originalForegroundOrder;
    }

    private void OnDisable()
    {
        RestoreForegroundSorting();
    }
}