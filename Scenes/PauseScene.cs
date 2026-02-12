using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarterTD.Engine;
using StarterTD.Interfaces;

namespace StarterTD.Scenes;

/// <summary>
/// Pause menu overlay scene. Displays pause UI and handles resume/menu navigation.
/// Sits on top of GameplayScene using the scene stack.
/// </summary>
public class PauseScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont? _font;
    private KeyboardState _previousKeyboard = new();

    private readonly Rectangle _resumeButton;
    private readonly Rectangle _mainMenuButton;

    private const int ButtonWidth = 200;
    private const int ButtonHeight = 60;
    private const int Gap = 20;

    public PauseScene(Game1 game)
    {
        _game = game;

        int centerX = GameSettings.ScreenWidth / 2 - ButtonWidth / 2;
        int startY = GameSettings.ScreenHeight / 2 - 80;

        _resumeButton = new Rectangle(centerX, startY, ButtonWidth, ButtonHeight);
        _mainMenuButton = new Rectangle(
            centerX,
            startY + ButtonHeight + Gap,
            ButtonWidth,
            ButtonHeight
        );
    }

    public void LoadContent()
    {
        try
        {
            _font = _game.Content.Load<SpriteFont>("DefaultFont");
        }
        catch
        {
            // Font not available — UI will use fallback rendering
        }

        _previousKeyboard = Keyboard.GetState();
    }

    public void Update(GameTime gameTime)
    {
        KeyboardState currentKeyboard = Keyboard.GetState();

        if (
            (currentKeyboard.IsKeyDown(Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape))
            || (currentKeyboard.IsKeyDown(Keys.P) && _previousKeyboard.IsKeyUp(Keys.P))
        )
        {
            _game.PopScene();
            _previousKeyboard = currentKeyboard;
            return;
        }

        MouseState mouse = Mouse.GetState();
        if (mouse.LeftButton == ButtonState.Pressed)
        {
            Point mousePos = mouse.Position;

            if (_resumeButton.Contains(mousePos))
            {
                _game.PopScene();
                _previousKeyboard = currentKeyboard;
                return;
            }

            if (_mainMenuButton.Contains(mousePos))
            {
                _game.SetScene(new MapSelectionScene(_game));
                _previousKeyboard = currentKeyboard;
                return;
            }
        }

        _previousKeyboard = currentKeyboard;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(0, 0, GameSettings.ScreenWidth, GameSettings.ScreenHeight),
            Color.Black * 0.7f
        );

        if (_font != null)
        {
            string title = "PAUSED";
            Vector2 titleSize = _font.MeasureString(title);
            spriteBatch.DrawString(
                _font,
                title,
                new Vector2(
                    GameSettings.ScreenWidth / 2 - titleSize.X / 2,
                    GameSettings.ScreenHeight / 2 - 150
                ),
                Color.White
            );

            DrawButton(spriteBatch, _resumeButton, "Resume (P/ESC)");
            DrawButton(spriteBatch, _mainMenuButton, "Main Menu");
        }
        else
        {
            TextureManager.DrawRect(spriteBatch, _resumeButton, Color.DarkSlateGray);
            TextureManager.DrawRectOutline(spriteBatch, _resumeButton, Color.White, 2);

            TextureManager.DrawRect(spriteBatch, _mainMenuButton, Color.DarkSlateGray);
            TextureManager.DrawRectOutline(spriteBatch, _mainMenuButton, Color.White, 2);
        }
    }

    private void DrawButton(SpriteBatch spriteBatch, Rectangle rect, string label)
    {
        TextureManager.DrawRect(spriteBatch, rect, Color.DarkSlateGray);
        TextureManager.DrawRectOutline(spriteBatch, rect, Color.White, 2);

        if (_font != null)
        {
            Vector2 textSize = _font.MeasureString(label);
            spriteBatch.DrawString(
                _font,
                label,
                new Vector2(
                    rect.X + (rect.Width - textSize.X) / 2,
                    rect.Y + (rect.Height - textSize.Y) / 2
                ),
                Color.White
            );
        }
    }
}
