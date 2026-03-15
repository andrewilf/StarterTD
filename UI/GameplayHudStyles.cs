using Microsoft.Xna.Framework;

namespace StarterTD.UI;

internal static class GameplayHudStyles
{
    public static readonly GumMenuButtonStyle SelectedButton = new(
        BackgroundColor: new Color(84, 86, 112),
        FocusedIndicatorColor: new Color(250, 230, 118),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle DisabledButton = new(
        BackgroundColor: new Color(52, 52, 52),
        FocusedIndicatorColor: new Color(102, 102, 102),
        ForegroundColor: new Color(150, 150, 150)
    );

    public static readonly GumMenuButtonStyle AbilityCooldown = new(
        BackgroundColor: new Color(64, 64, 64),
        FocusedIndicatorColor: new Color(140, 140, 140),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle AbilityUnavailable = new(
        BackgroundColor: new Color(48, 48, 48),
        FocusedIndicatorColor: new Color(110, 110, 110),
        ForegroundColor: new Color(150, 150, 150)
    );

    public static readonly GumMenuButtonStyle DebugButton = new(
        BackgroundColor: new Color(78, 58, 34),
        FocusedIndicatorColor: new Color(230, 198, 153),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle DebugSelected = new(
        BackgroundColor: new Color(112, 84, 44),
        FocusedIndicatorColor: new Color(250, 224, 160),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle TimeSlowBase = new(
        BackgroundColor: new Color(20, 60, 80),
        FocusedIndicatorColor: new Color(120, 220, 255),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle TimeSlowActive = new(
        BackgroundColor: new Color(0, 92, 128),
        FocusedIndicatorColor: new Color(140, 240, 255),
        ForegroundColor: new Color(232, 250, 255)
    );

    public static readonly GumMenuButtonStyle SellButton = new(
        BackgroundColor: new Color(130, 0, 0),
        FocusedIndicatorColor: new Color(214, 110, 110),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle HealingMode = new(
        BackgroundColor: new Color(18, 55, 88),
        FocusedIndicatorColor: new Color(90, 235, 255),
        ForegroundColor: new Color(195, 255, 215)
    );

    public static readonly GumMenuButtonStyle HealingModeAttack = new(
        BackgroundColor: new Color(85, 18, 18),
        FocusedIndicatorColor: new Color(255, 110, 70),
        ForegroundColor: new Color(255, 215, 120)
    );

    public static readonly GumMenuButtonStyle WallMode = new(
        BackgroundColor: new Color(20, 60, 20),
        FocusedIndicatorColor: new Color(140, 220, 140),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle WallModeActive = new(
        BackgroundColor: new Color(8, 96, 24),
        FocusedIndicatorColor: new Color(180, 255, 180),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle TowerPlacement = new(
        BackgroundColor: new Color(50, 70, 92),
        FocusedIndicatorColor: new Color(182, 212, 236),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle GunAbility = new(
        BackgroundColor: new Color(40, 64, 84),
        FocusedIndicatorColor: new Color(160, 205, 240),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle CannonAbility = new(
        BackgroundColor: new Color(56, 52, 40),
        FocusedIndicatorColor: new Color(212, 194, 147),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle WallAbility = new(
        BackgroundColor: new Color(34, 70, 40),
        FocusedIndicatorColor: new Color(156, 218, 164),
        ForegroundColor: Color.White
    );

    public static readonly GumMenuButtonStyle HealingAbility = new(
        BackgroundColor: new Color(38, 74, 56),
        FocusedIndicatorColor: new Color(176, 232, 200),
        ForegroundColor: Color.White
    );
}
