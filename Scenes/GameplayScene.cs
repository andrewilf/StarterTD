using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.Interfaces;
using StarterTD.Managers;
using StarterTD.UI;

namespace StarterTD.Scenes;

/// <summary>
/// The main gameplay scene. This is where the tower defense game loop runs.
/// Manages the map, towers, enemies, waves, and UI.
/// </summary>
public class GameplayScene : IScene
{
    private readonly Game1 _game;
    private Map _map = null!;
    private TowerManager _towerManager = null!;
    private WaveManager _waveManager = null!;
    private InputManager _inputManager = null!;
    private UIPanel _uiPanel = null!;

    private List<IEnemy> _enemies = new();
    private List<FloatingText> _floatingTexts = new();
    private int _money;
    private int _lives;
    private bool _gameOver;
    private bool _gameWon;
    private bool _allEnemiesCleared;
    private readonly string _selectedMapId;

    public GameplayScene(Game1 game, string mapId)
    {
        _game = game;
        _selectedMapId = mapId;
    }

    public void LoadContent()
    {
        _map = new Map(MapDataRepository.GetMap(_selectedMapId));
        _towerManager = new TowerManager(_map);
        _waveManager = new WaveManager(() => _map.ActivePath);
        _inputManager = new InputManager();
        _uiPanel = new UIPanel(GameSettings.ScreenWidth, GameSettings.ScreenHeight);

        _money = GameSettings.StartingMoney;
        _lives = GameSettings.StartingLives;
        _gameOver = false;
        _gameWon = false;
        _allEnemiesCleared = true;

        // Wire up enemy spawning
        _waveManager.OnEnemySpawned = enemy => _enemies.Add(enemy);

        // With movement costs, placement never blocks the path — always valid
        _towerManager.OnValidatePlacement = (gridPos) => true;

        // Recompute heat map and reroute all enemies after any tower placement
        _towerManager.OnTowerPlaced = (gridPos) =>
        {
            _map.RecomputeActivePath();

            foreach (var enemy in _enemies)
            {
                if (enemy is Enemy concreteEnemy)
                    concreteEnemy.UpdatePath(_map.ActivePath);
            }
        };

        // Try to load font if available
        try
        {
            var font = _game.Content.Load<SpriteFont>("DefaultFont");
            _uiPanel.SetFont(font);
        }
        catch
        {
            // Font not available — UI will use fallback rendering
        }
    }

    public void Update(GameTime gameTime)
    {
        _inputManager.Update();

        if ((_gameOver || _gameWon) && _inputManager.IsKeyPressed(Keys.R))
        {
            _game.SetScene(new MapSelectionScene(_game));
            return;
        }

        if (_gameOver || _gameWon)
            return;

        if (_inputManager.IsKeyPressed(Keys.Escape))
        {
            _uiPanel.SelectedTowerType = null;
            _towerManager.SelectedTower = null;
        }

        if (_inputManager.IsLeftClick())
        {
            Point mousePos = _inputManager.MousePosition;

            // Check if click is on UI panel first
            if (_uiPanel.ContainsPoint(mousePos))
            {
                _uiPanel.HandleClick(mousePos, _money);

                if (_uiPanel.StartWaveClicked && !_waveManager.WaveInProgress && _allEnemiesCleared)
                {
                    _waveManager.StartNextWave();
                }
            }
            else
            {
                // Click on the game grid
                Point gridPos = Map.WorldToGrid(mousePos.ToVector2());

                if (_uiPanel.SelectedTowerType.HasValue)
                {
                    // Try to place a tower
                    var stats = TowerData.GetStats(_uiPanel.SelectedTowerType.Value, 1);
                    if (_money >= stats.Cost)
                    {
                        int cost = _towerManager.TryPlaceTower(
                            _uiPanel.SelectedTowerType.Value,
                            gridPos
                        );
                        if (cost > 0)
                        {
                            _money -= cost;
                            Vector2 worldPos = Map.GridToWorld(gridPos);
                            SpawnFloatingText(worldPos, $"-${cost}", Color.Red);
                        }
                    }
                }
                else
                {
                    // Select existing tower
                    var tower = _towerManager.GetTowerAt(gridPos);
                    _towerManager.SelectedTower = tower;
                }
            }
        }

        // --- Handle right click (upgrade) ---
        if (_inputManager.IsRightClick())
        {
            Point mousePos = _inputManager.MousePosition;
            if (!_uiPanel.ContainsPoint(mousePos))
            {
                Point gridPos = Map.WorldToGrid(mousePos.ToVector2());
                var tower = _towerManager.GetTowerAt(gridPos);
                if (tower != null && tower.Level < 2 && _money >= tower.UpgradeCost)
                {
                    int cost = _towerManager.TryUpgradeTower(gridPos);
                    if (cost > 0)
                    {
                        _money -= cost;
                        var upgradedTower = _towerManager.GetTowerAt(gridPos);
                        if (upgradedTower != null)
                            SpawnFloatingText(
                                upgradedTower.WorldPosition,
                                $"-${cost}",
                                Color.Orange
                            );
                    }
                }
            }
        }

        // --- Update wave spawning ---
        _waveManager.Update(gameTime);

        // --- Update enemies ---
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            _enemies[i].Update(gameTime);

            if (_enemies[i].IsDead)
            {
                int bounty = _enemies[i].Bounty;
                _money += bounty;
                SpawnFloatingText(_enemies[i].Position, $"+${bounty}", Color.Gold);
                _enemies.RemoveAt(i);
            }
            else if (_enemies[i].ReachedEnd)
            {
                _lives--;
                _enemies.RemoveAt(i);

                if (_lives <= 0)
                {
                    _gameOver = true;
                    return;
                }
            }
        }

        // Check if all enemies are cleared (wave done spawning + no enemies alive)
        _allEnemiesCleared = !_waveManager.WaveInProgress && _enemies.Count == 0;

        // Check win condition
        if (_waveManager.CurrentWave >= _waveManager.TotalWaves && _allEnemiesCleared)
        {
            _gameWon = true;
        }

        // --- Update towers ---
        _towerManager.Update(gameTime, _enemies);

        // --- Update floating texts ---
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            _floatingTexts[i].Update(gameTime);
            if (!_floatingTexts[i].IsActive)
                _floatingTexts.RemoveAt(i);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw map
        _map.Draw(spriteBatch);

        // Draw towers
        _towerManager.Draw(spriteBatch, _uiPanel.GetFont());

        // Draw enemies
        foreach (var enemy in _enemies)
        {
            enemy.Draw(spriteBatch);
        }

        // Draw floating texts
        foreach (var floatingText in _floatingTexts)
        {
            floatingText.Draw(spriteBatch, _uiPanel.GetFont());
        }

        // Draw UI panel
        bool waveActive = _waveManager.WaveInProgress || !_allEnemiesCleared;
        _uiPanel.Draw(
            spriteBatch,
            _money,
            _lives,
            _waveManager.CurrentWave,
            _waveManager.TotalWaves,
            waveActive
        );

        // Draw hover indicator on grid
        Point mouseGrid = Map.WorldToGrid(_inputManager.MousePositionVector);
        if (
            mouseGrid.X >= 0
            && mouseGrid.X < _map.Columns
            && mouseGrid.Y >= 0
            && mouseGrid.Y < _map.Rows
            && !_uiPanel.ContainsPoint(_inputManager.MousePosition)
        )
        {
            var hoverRect = new Rectangle(
                mouseGrid.X * GameSettings.TileSize,
                mouseGrid.Y * GameSettings.TileSize,
                GameSettings.TileSize,
                GameSettings.TileSize
            );

            bool canPlace = _map.CanBuild(mouseGrid) && _uiPanel.SelectedTowerType.HasValue;
            bool wouldBlock = canPlace && _map.WouldBlockPath(mouseGrid);

            Color hoverColor =
                canPlace && !wouldBlock
                    ? new Color(255, 255, 255, 60) // White — valid placement
                    : new Color(255, 0, 0, 40); // Red — invalid or would block path

            TextureManager.DrawRect(spriteBatch, hoverRect, hoverColor);
            TextureManager.DrawRectOutline(spriteBatch, hoverRect, Color.White, 1);
        }

        // Draw game over / win overlay
        if (_gameOver || _gameWon)
        {
            DrawGameOverOverlay(spriteBatch, _uiPanel.GetFont());
        }
    }

    private void DrawGameOverOverlay(SpriteBatch spriteBatch, SpriteFont? font)
    {
        // Semi-transparent dark overlay
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(0, 0, GameSettings.ScreenWidth, GameSettings.ScreenHeight),
            new Color(0, 0, 0, 180)
        );

        int centerX = GameSettings.ScreenWidth / 2;
        int centerY = GameSettings.ScreenHeight / 2;

        if (font != null)
        {
            // Title text
            string title = _gameWon ? "VICTORY!" : "DEFEAT";
            Color titleColor = _gameWon ? Color.Gold : Color.Red;
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(centerX - titleSize.X / 2, centerY - 100);

            // Draw title with shadow for readability
            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, titleColor);

            // Statistics
            int statsY = centerY - 40;
            string[] stats;

            if (_gameWon)
            {
                stats = new[]
                {
                    $"All {_waveManager.TotalWaves} waves completed!",
                    $"Final Money: ${_money}",
                    $"Lives Remaining: {_lives}",
                    "",
                    "Press R to return to map selection",
                };
            }
            else
            {
                stats = new[]
                {
                    $"Wave Reached: {_waveManager.CurrentWave}/{_waveManager.TotalWaves}",
                    $"Money: ${_money}",
                    $"Lives: {_lives}",
                    "",
                    "Press R to return to map selection",
                };
            }

            for (int i = 0; i < stats.Length; i++)
            {
                Vector2 size = font.MeasureString(stats[i]);
                Vector2 pos = new Vector2(centerX - size.X / 2, statsY + i * 30);

                // Shadow
                spriteBatch.DrawString(font, stats[i], pos + new Vector2(1, 1), Color.Black);
                // Main text
                spriteBatch.DrawString(font, stats[i], pos, Color.White);
            }
        }
        else
        {
            // Fallback: colored indicator (existing code)
            Color indicatorColor = _gameWon ? Color.Gold : Color.Red;
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(centerX - 100, centerY - 30, 200, 60),
                indicatorColor
            );
        }
    }

    /// <summary>
    /// Spawns a floating text at the specified world position.
    /// </summary>
    private void SpawnFloatingText(Vector2 worldPos, string text, Color color)
    {
        _floatingTexts.Add(new FloatingText(worldPos, text, color));
    }
}
