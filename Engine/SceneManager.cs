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
    private readonly Stack<IScene> _sceneStack = new();

    /// <summary>The currently active (top) scene.</summary>
    public IScene? CurrentScene => _sceneStack.Count > 0 ? _sceneStack.Peek() : null;

    /// <summary>
    /// Replace the entire stack with a new scene. Used for main scene transitions.
    /// Calls LoadContent on the new scene.
    /// </summary>
    public void SetScene(IScene scene)
    {
        _sceneStack.Clear();
        _sceneStack.Push(scene);
        scene.LoadContent();
    }

    /// <summary>
    /// Push a scene on top of the stack (e.g., pause menu overlay).
    /// Underlying scenes are preserved and can be resumed.
    /// Calls LoadContent on the new scene.
    /// </summary>
    public void PushScene(IScene scene)
    {
        _sceneStack.Push(scene);
        scene.LoadContent();
    }

    /// <summary>
    /// Pop the top scene from the stack (e.g., close pause menu).
    /// If stack becomes empty, nothing happens.
    /// </summary>
    public void PopScene()
    {
        if (_sceneStack.Count > 0)
            _sceneStack.Pop();
    }

    /// <summary>Update the top scene.</summary>
    public void Update(GameTime gameTime)
    {
        CurrentScene?.Update(gameTime);
    }

    /// <summary>Draw the top scene.</summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        CurrentScene?.Draw(spriteBatch);
    }
}
