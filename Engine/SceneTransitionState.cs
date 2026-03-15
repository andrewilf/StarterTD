using Microsoft.Xna.Framework;
using StarterTD.Interfaces;

namespace StarterTD.Engine;

public enum SceneTransitionPreset
{
    MenuForwardSlideFade,
    MenuBackwardSlideFade,
}

internal enum SceneTransitionDirection
{
    Forward = 1,
    Backward = -1,
}

internal sealed class SceneTransitionState
{
    public SceneTransitionState(
        IScene outgoingScene,
        IScene incomingScene,
        SceneTransitionPreset preset
    )
    {
        OutgoingScene = outgoingScene;
        IncomingScene = incomingScene;

        switch (preset)
        {
            case SceneTransitionPreset.MenuBackwardSlideFade:
                Direction = SceneTransitionDirection.Backward;
                break;
            default:
                Direction = SceneTransitionDirection.Forward;
                break;
        }
    }

    public IScene OutgoingScene { get; }
    public IScene IncomingScene { get; }
    public SceneTransitionDirection Direction { get; }
    public float DurationSeconds { get; } = 0.36f;
    public float IncomingSlideFraction { get; } = 0.12f;
    public float OutgoingDriftFraction { get; } = 0.035f;
    public float FadeExponent { get; } = 1.75f;
    public float OverlayPeakAlpha { get; } = 0.24f;
    public Color OverlayTint { get; } = new(8, 12, 20);
    public float ElapsedSeconds { get; private set; }

    public float Progress =>
        DurationSeconds <= 0f ? 1f : MathHelper.Clamp(ElapsedSeconds / DurationSeconds, 0f, 1f);

    public bool IsComplete => ElapsedSeconds >= DurationSeconds;

    public void Advance(float elapsedSeconds)
    {
        ElapsedSeconds += elapsedSeconds;
    }
}
