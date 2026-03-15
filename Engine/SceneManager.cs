using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Interfaces;

namespace StarterTD.Engine;

/// <summary>
/// Manages scene transitions and modal overlays using a stack.
/// SetScene replaces the entire stack (for full scene transitions like MainMenu → Gameplay).
/// PushScene adds an overlay (for modal dialogs like pause menu, dialogs).
/// PopScene removes the top overlay (to resume underlying scene).
/// </summary>
public class SceneManager
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Stack<IScene> _sceneStack = new();
    private SceneTransitionState? _activeTransition;
    private RenderTarget2D? _outgoingSceneTarget;
    private RenderTarget2D? _incomingSceneTarget;
    private int _renderTargetWidth;
    private int _renderTargetHeight;

    public SceneManager(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    /// <summary>The currently active (top) scene.</summary>
    public IScene? CurrentScene => _sceneStack.Count > 0 ? _sceneStack.Peek() : null;

    /// <summary>
    /// Replace the entire stack with a new scene. Used for main scene transitions.
    /// Calls LoadContent on the new scene.
    /// </summary>
    public void SetScene(IScene scene)
    {
        _activeTransition = null;
        ReplaceStack(scene, loadContent: true);
    }

    /// <summary>
    /// Replace the current scene using a timed transition.
    /// Preloads the incoming scene immediately so rendering is ready on the next frame.
    /// </summary>
    public void TransitionToScene(
        IScene scene,
        SceneTransitionPreset preset = SceneTransitionPreset.MenuForwardSlideFade
    )
    {
        if (CurrentScene == null)
        {
            SetScene(scene);
            return;
        }

        scene.LoadContent();
        _activeTransition = new SceneTransitionState(CurrentScene, scene, preset);
    }

    /// <summary>
    /// Push a scene on top of the stack (e.g., pause menu overlay).
    /// Underlying scenes are preserved and can be resumed.
    /// Calls LoadContent on the new scene.
    /// </summary>
    public void PushScene(IScene scene)
    {
        _activeTransition = null;
        _sceneStack.Push(scene);
        scene.LoadContent();
    }

    /// <summary>
    /// Pop the top scene from the stack (e.g., close pause menu).
    /// If stack becomes empty, nothing happens.
    /// </summary>
    public void PopScene()
    {
        _activeTransition = null;

        if (_sceneStack.Count > 0)
            _sceneStack.Pop();
    }

    /// <summary>Update the top scene.</summary>
    public void Update(GameTime gameTime)
    {
        if (_activeTransition != null)
        {
            UpdateTransition(gameTime);
            return;
        }

        CurrentScene?.Update(gameTime);
    }

    /// <summary>Draw the top scene.</summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        if (_activeTransition != null)
        {
            DrawTransition(spriteBatch, _activeTransition);
            return;
        }

        if (CurrentScene != null)
            DrawScene(CurrentScene, spriteBatch);
    }

    private void UpdateTransition(GameTime gameTime)
    {
        if (_activeTransition == null)
            return;

        _activeTransition.Advance((float)gameTime.ElapsedGameTime.TotalSeconds);
        if (!_activeTransition.IsComplete)
            return;

        ReplaceStack(_activeTransition.IncomingScene, loadContent: false);
        _activeTransition = null;
    }

    private void DrawTransition(SpriteBatch spriteBatch, SceneTransitionState transition)
    {
        EnsureRenderTargets();

        DrawSceneToRenderTarget(transition.OutgoingScene, _outgoingSceneTarget!, spriteBatch);
        DrawSceneToRenderTarget(transition.IncomingScene, _incomingSceneTarget!, spriteBatch);

        _graphicsDevice.SetRenderTarget(null);

        float progress = EaseOutCubic(transition.Progress);
        float inverseProgress = 1f - transition.Progress;
        float incomingAlpha = 1f - MathF.Pow(inverseProgress, transition.FadeExponent);
        float outgoingAlpha = MathF.Pow(inverseProgress, transition.FadeExponent);
        float overlayBlend = EaseOutCubic(1f - MathF.Abs((transition.Progress * 2f) - 1f));
        float overlayAlpha = transition.OverlayPeakAlpha * overlayBlend;

        float width = _renderTargetWidth;
        float incomingSlidePixels = width * transition.IncomingSlideFraction;
        float outgoingDriftPixels = width * transition.OutgoingDriftFraction;
        int directionSign = transition.Direction == SceneTransitionDirection.Forward ? 1 : -1;

        Vector2 outgoingPosition = new(
            MathF.Round(-directionSign * outgoingDriftPixels * progress),
            0f
        );
        Vector2 incomingPosition = new(
            MathF.Round(directionSign * incomingSlidePixels * (1f - progress)),
            0f
        );

        BeginSceneBatch(spriteBatch);
        spriteBatch.Draw(_outgoingSceneTarget!, outgoingPosition, Color.White * outgoingAlpha);
        spriteBatch.Draw(_incomingSceneTarget!, incomingPosition, Color.White * incomingAlpha);
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(0, 0, _renderTargetWidth, _renderTargetHeight),
            transition.OverlayTint * overlayAlpha
        );
        spriteBatch.End();
    }

    private void DrawSceneToRenderTarget(
        IScene scene,
        RenderTarget2D renderTarget,
        SpriteBatch spriteBatch
    )
    {
        _graphicsDevice.SetRenderTarget(renderTarget);
        // Match the normal backbuffer clear so scenes that do not draw a full background
        // still look consistent during transitions.
        _graphicsDevice.Clear(Color.Black);
        DrawScene(scene, spriteBatch);
    }

    private static void DrawScene(IScene scene, SpriteBatch spriteBatch)
    {
        BeginSceneBatch(spriteBatch);
        scene.Draw(spriteBatch);
        spriteBatch.End();
    }

    private static void BeginSceneBatch(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            null
        );
    }

    private void ReplaceStack(IScene scene, bool loadContent)
    {
        _sceneStack.Clear();
        _sceneStack.Push(scene);

        if (loadContent)
            scene.LoadContent();
    }

    private void EnsureRenderTargets()
    {
        int viewportWidth = Math.Max(1, _graphicsDevice.Viewport.Width);
        int viewportHeight = Math.Max(1, _graphicsDevice.Viewport.Height);
        if (
            _outgoingSceneTarget != null
            && _incomingSceneTarget != null
            && viewportWidth == _renderTargetWidth
            && viewportHeight == _renderTargetHeight
        )
        {
            return;
        }

        _outgoingSceneTarget?.Dispose();
        _incomingSceneTarget?.Dispose();

        _renderTargetWidth = viewportWidth;
        _renderTargetHeight = viewportHeight;
        _outgoingSceneTarget = CreateSceneRenderTarget(viewportWidth, viewportHeight);
        _incomingSceneTarget = CreateSceneRenderTarget(viewportWidth, viewportHeight);
    }

    private RenderTarget2D CreateSceneRenderTarget(int width, int height)
    {
        return new RenderTarget2D(
            _graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None
        );
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - value;
        return 1f - (inverse * inverse * inverse);
    }
}
