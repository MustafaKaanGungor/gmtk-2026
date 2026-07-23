using UnityEngine;

public enum GameArtType
{
    Background,
    Ground,
    Player,
    Rocket,
    Bag,
    AimArrowBody,
    AimArrowHead,
    PowerBarBackground,
    PowerBarFill
}

[CreateAssetMenu(
    fileName = "DefaultGameArt",
    menuName = "GMTK Jam/Game Art Config"
)]
public class GameArtConfig : ScriptableObject
{
    [Header("Environment")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite groundSprite;

    [Header("Gameplay")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite rocketSprite;
    [SerializeField] private Sprite bagSprite;

    [Header("Aim Arrow")]
    [SerializeField] private Sprite aimArrowBodySprite;
    [SerializeField] private Sprite aimArrowHeadSprite;

    [Header("UI")]
    [SerializeField] private Sprite powerBarBackgroundSprite;
    [SerializeField] private Sprite powerBarFillSprite;

    public Sprite GetSprite(GameArtType artType)
    {
        switch (artType)
        {
            case GameArtType.Background:
                return backgroundSprite;

            case GameArtType.Ground:
                return groundSprite;

            case GameArtType.Player:
                return playerSprite;

            case GameArtType.Rocket:
                return rocketSprite;

            case GameArtType.Bag:
                return bagSprite;

            case GameArtType.AimArrowBody:
                return aimArrowBodySprite;

            case GameArtType.AimArrowHead:
                return aimArrowHeadSprite;

            case GameArtType.PowerBarBackground:
                return powerBarBackgroundSprite;

            case GameArtType.PowerBarFill:
                return powerBarFillSprite;

            default:
                return null;
        }
    }
}