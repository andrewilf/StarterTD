using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace StarterTD.Managers;

/// <summary>
/// Tracks mouse and keyboard state each frame.
/// Provides helper methods for detecting single clicks vs held buttons.
/// Similar to how you'd track "previous state" in a React useRef.
/// </summary>
public class InputManager
{
    private MouseState _currentMouse;
    private MouseState _previousMouse;
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;

    /// <summary>Current mouse position as a Point (grid-friendly).</summary>
    public Point MousePosition => _currentMouse.Position;

    /// <summary>Current mouse position as a Vector2 (world-friendly).</summary>
    public Vector2 MousePositionVector => _currentMouse.Position.ToVector2();

    /// <summary>Update input state. Call once per frame at the start of Update.</summary>
    public void Update()
    {
        _previousMouse = _currentMouse;
        _currentMouse = Mouse.GetState();
        _previousKeyboard = _currentKeyboard;
        _currentKeyboard = Keyboard.GetState();
    }

    /// <summary>True on the single frame the left mouse button is pressed.</summary>
    public bool IsLeftClick()
    {
        return _currentMouse.LeftButton == ButtonState.Pressed
            && _previousMouse.LeftButton == ButtonState.Released;
    }

    /// <summary>True while the left mouse button is held down.</summary>
    public bool IsLeftHeld()
    {
        return _currentMouse.LeftButton == ButtonState.Pressed;
    }

    /// <summary>True on the single frame the left mouse button is released.</summary>
    public bool IsLeftReleased()
    {
        return _currentMouse.LeftButton == ButtonState.Released
            && _previousMouse.LeftButton == ButtonState.Pressed;
    }

    /// <summary>True on the single frame the right mouse button is pressed.</summary>
    public bool IsRightClick()
    {
        return _currentMouse.RightButton == ButtonState.Pressed
            && _previousMouse.RightButton == ButtonState.Released;
    }

    /// <summary>True on the single frame a key is pressed.</summary>
    public bool IsKeyPressed(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }

    /// <summary>True while a key is held down.</summary>
    public bool IsKeyDown(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key);
    }
}
