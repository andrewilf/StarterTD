using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarterTD.Engine;
using StarterTD.Interfaces;
using StarterTD.Managers;

namespace StarterTD.Scenes;

/// <summary>
/// Pause menu overlay scene. Displays pause UI and handles resume/menu navigation.
/// Sits on top of GameplayScene using the scene stack.
/// </summary>
public class PauseScene : IScene
{
    private readonly Game1 _game;
    private InputManager _inputManager = null!;
    private SpriteFont? _font;

    private readonly Rectangle _resumeButton;
    private readonly Rectangle _mapSelectionButton;

    private const int ButtonWidth = 200;
    private const int ButtonHeight = 60;
    private const int Gap = 20;

    public PauseScene(Game1 game)
    {
        _game = game;

        int centerX = GameSettings.ScreenWidth / 2 - ButtonWidth / 2;
        int startY = GameSettings.ScreenHeight / 2 - 80;

        _resumeButton = new Rectangle(centerX, startY, ButtonWidth, ButtonHeight);
        _mapSelectionButton = new Rectangle(
            centerX,
            startY + ButtonHeight + Gap,
            ButtonWidth,
            ButtonHeight
        );
    }

    public void LoadContent()
    {
        _inputManager = new InputManager();

        try
        {
            _font = _game.Content.Load<SpriteFont>("DefaultFont");
        }
        catch
        {
            // Font not available — UI will use fallback rendering
        }
    }

    public void UnloadContent() { }

    public void Update(GameTime gameTime)
    {
        _inputManager.Update();

        if (_inputManager.IsKeyPressed(Keys.Escape) || _inputManager.IsKeyPressed(Keys.P))
        {
            _game.PopScene();
            return;
        }

        if (_inputManager.IsLeftClick())
        {
            Point mousePos = _inputManager.MousePosition;

            if (_resumeButton.Contains(mousePos))
            {
                _game.PopScene();
                return;
            }

            if (_mapSelectionButton.Contains(mousePos))
            {
                _game.SetScene(new MapSelectionScene(_game));
            }
        }
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
            DrawButton(spriteBatch, _mapSelectionButton, "Map Selection");
        }
        else
        {
            TextureManager.DrawRect(spriteBatch, _resumeButton, Color.DarkSlateGray);
            TextureManager.DrawRectOutline(spriteBatch, _resumeButton, Color.White, 2);

            TextureManager.DrawRect(spriteBatch, _mapSelectionButton, Color.DarkSlateGray);
            TextureManager.DrawRectOutline(spriteBatch, _mapSelectionButton, Color.White, 2);
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
