using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// Describes the layout of a sprite sheet for a single entity's animations.
/// This is the only thing that needs updating when swapping in new art.
/// Python equivalent: a dataclass / named tuple.
/// </summary>
public readonly record struct AnimationConfig
{
    // TODO: Set FrameWidth and FrameHeight to match your sprite sheet's per-frame pixel dimensions.
    public int FrameWidth { get; init; }
    public int FrameHeight { get; init; }
    public int IdleFrameCount { get; init; }
    public int IdleRow { get; init; }
    public int IdleStartFrame { get; init; }
    public int WalkFrameCount { get; init; }
    public int WalkRow { get; init; }
    public int WalkStartFrame { get; init; }
    public float FramesPerSecond { get; init; }
}

public enum AnimationState
{
    Idle,
    Walk,
}

/// <summary>
/// Frame-based sprite sheet animator. All source Rectangles are pre-computed in the
/// constructor so no allocations happen during Update or Draw.
/// Python equivalent: a state machine class with a float accumulator as the frame counter.
/// </summary>
public sealed class SpriteAnimator
{
    public AnimationConfig Config { get; }

    /// <summary>Source rectangle into the sheet for the current animation frame.</summary>
    public Rectangle CurrentSourceRect => _currentFrames[_frameIndex];

    /// <summary>False when FrameWidth or FrameHeight is 0 (config not yet filled in).</summary>
    public bool IsConfigured => Config.FrameWidth > 0 && Config.FrameHeight > 0;

    private readonly Rectangle[] _idleFrames;
    private readonly Rectangle[] _walkFrames;
    private Rectangle[] _currentFrames;
    private int _frameIndex;
    private float _elapsed;
    private AnimationState _currentState;

    public SpriteAnimator(AnimationConfig config)
    {
        Config = config;
        _idleFrames = PrecomputeFrames(config, config.IdleRow, config.IdleStartFrame, config.IdleFrameCount);
        _walkFrames = PrecomputeFrames(config, config.WalkRow, config.WalkStartFrame, config.WalkFrameCount);
        _currentFrames = _idleFrames;
    }

    /// <summary>
    /// Switch the active animation. Resets the frame counter when the state changes.
    /// Safe to call every frame — is a no-op if the state hasn't changed.
    /// </summary>
    public void SetAnimation(AnimationState state)
    {
        if (state == _currentState)
            return;

        _currentState = state;
        _currentFrames = state == AnimationState.Walk ? _walkFrames : _idleFrames;
        _frameIndex = 0;
        _elapsed = 0f;
    }

    /// <summary>Advance the frame accumulator. Call once per Update tick.</summary>
    public void Update(float dt)
    {
        if (_currentFrames.Length == 0)
            return;

        _elapsed += dt;
        float frameDuration = 1f / Config.FramesPerSecond;

        // Consume elapsed time in frame-sized chunks so fast dt values don't skip frames.
        while (_elapsed >= frameDuration)
        {
            _elapsed -= frameDuration;
            _frameIndex = (_frameIndex + 1) % _currentFrames.Length;
        }
    }

    private static Rectangle[] PrecomputeFrames(
        AnimationConfig config,
        int row,
        int startFrame,
        int frameCount
    )
    {
        if (frameCount <= 0 || config.FrameWidth <= 0 || config.FrameHeight <= 0)
            return [];

        var frames = new Rectangle[frameCount];
        for (int i = 0; i < frameCount; i++)
            frames[i] = new Rectangle(
                (startFrame + i) * config.FrameWidth,
                row * config.FrameHeight,
                config.FrameWidth,
                config.FrameHeight
            );

        return frames;
    }
}
