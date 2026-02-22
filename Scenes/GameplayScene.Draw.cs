using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.UI;

namespace StarterTD.Scenes;

/// <summary>
/// Rendering methods for GameplayScene. Partial class split from GameplayScene.cs.
/// </summary>
public partial class GameplayScene
{
    public void Draw(SpriteBatch spriteBatch)
    {
        _map.Draw(spriteBatch);

        // Draw tower movement path preview (before towers so towers render on top)
        if (_towerMovePreviewPath != null)
            DrawTowerMovePreview(spriteBatch, _towerMovePreviewPath);

        _towerManager.Draw(spriteBatch, _uiPanel.GetFont(), _hoveredTower);

        foreach (var enemy in _enemies)
            enemy.Draw(spriteBatch);

        DrawSelectionIndicators(spriteBatch);

        foreach (var effect in _aoeEffects)
            effect.Draw(spriteBatch);

        foreach (var spike in _spikeEffects)
            spike.Draw(spriteBatch);

        foreach (var floatingText in _floatingTexts)
            floatingText.Draw(spriteBatch, _uiPanel.GetFont());

        bool waveActive = _waveManager.WaveInProgress || !_allEnemiesCleared;
        _uiPanel.Draw(
            spriteBatch,
            _money,
            _lives,
            _waveManager.CurrentWave,
            _waveManager.TotalWaves,
            waveActive,
            _towerManager.SelectedTower,
            _selectedEnemy
        );

        // Hover indicator on grid
        if (
            _mouseGrid.X >= 0
            && _mouseGrid.X < _map.Columns
            && _mouseGrid.Y >= 0
            && _mouseGrid.Y < _map.Rows
            && !_uiPanel.ContainsPoint(_inputManager.MousePosition)
        )
        {
            DrawHoverIndicator(spriteBatch);
        }

        if (_gameOver || _gameWon)
            DrawGameOverOverlay(spriteBatch, _uiPanel.GetFont());
    }

    private void DrawHoverIndicator(SpriteBatch spriteBatch)
    {
        var hoverRect = new Rectangle(
            _mouseGrid.X * GameSettings.TileSize,
            _mouseGrid.Y * GameSettings.TileSize,
            GameSettings.TileSize,
            GameSettings.TileSize
        );

        bool canPlaceTower = _map.CanBuild(_mouseGrid) && _uiPanel.SelectedTowerType.HasValue;
        bool isHighGroundMode = _uiPanel.SelectionMode == UISelectionMode.PlaceHighGround;
        var wallingAnchor = _towerManager.SelectedTower;
        bool isWallMode =
            _wallPlacementMode
            && wallingAnchor != null
            && (
                wallingAnchor.TowerType == TowerType.ChampionWalling
                || wallingAnchor.TowerType.IsWallingGeneric()
            );

        Color hoverColor;
        if (isWallMode)
        {
            // Green if the tile is a valid wall placement, red otherwise
            bool canPlaceWall =
                _map.CanBuild(_mouseGrid)
                && _towerManager.IsAdjacentToWallingNetwork(_mouseGrid, wallingAnchor!);
            hoverColor = canPlaceWall ? Color.DarkGreen * 0.5f : Color.Red * 0.3f;
        }
        else if (canPlaceTower)
            hoverColor = Color.White * 0.24f;
        else if (isHighGroundMode)
            hoverColor = Color.ForestGreen * 0.3f;
        else
            hoverColor = Color.Red * 0.16f;

        // Show range preview when placing a tower
        if (_selectedTowerRange > 0f)
        {
            Vector2 hoverCenter = Map.GridToWorld(_mouseGrid);
            TextureManager.DrawFilledCircle(
                spriteBatch,
                hoverCenter,
                _selectedTowerRange,
                Color.White * 0.15f
            );
        }

        TextureManager.DrawRect(spriteBatch, hoverRect, hoverColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            hoverRect,
            isWallMode || isHighGroundMode ? Color.DarkGreen : Color.White,
            1
        );
    }

    private void DrawGameOverOverlay(SpriteBatch spriteBatch, SpriteFont? font)
    {
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(0, 0, GameSettings.ScreenWidth, GameSettings.ScreenHeight),
            Color.Black * 0.71f
        );

        int centerX = GameSettings.ScreenWidth / 2;
        int centerY = GameSettings.ScreenHeight / 2;

        if (font != null)
        {
            string title = _gameWon ? "VICTORY!" : "DEFEAT";
            Color titleColor = _gameWon ? Color.Gold : Color.Red;
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(centerX - titleSize.X / 2, centerY - 100);

            // Shadow + main text for readability
            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, titleColor);

            int statsY = centerY - 40;
            string[] stats = _gameWon
                ? new[]
                {
                    $"All {_waveManager.TotalWaves} waves completed!",
                    $"Final Money: ${_money}",
                    $"Lives Remaining: {_lives}",
                    "",
                    "Press R to return to map selection",
                }
                : new[]
                {
                    $"Wave Reached: {_waveManager.CurrentWave}/{_waveManager.TotalWaves}",
                    $"Money: ${_money}",
                    $"Lives: {_lives}",
                    "",
                    "Press R to return to map selection",
                };

            for (int i = 0; i < stats.Length; i++)
            {
                Vector2 size = font.MeasureString(stats[i]);
                Vector2 pos = new Vector2(centerX - size.X / 2, statsY + i * 30);
                spriteBatch.DrawString(font, stats[i], pos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, stats[i], pos, Color.White);
            }
        }
        else
        {
            // Fallback: colored block indicator when font is unavailable
            Color indicatorColor = _gameWon ? Color.Gold : Color.Red;
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(centerX - 100, centerY - 30, 200, 60),
                indicatorColor
            );
        }
    }

    /// <summary>
    /// Draw a red rectangle outline around the selected tower or enemy.
    /// For the walling champion, also draws the world-space wall placement toggle button.
    /// </summary>
    private void DrawSelectionIndicators(SpriteBatch spriteBatch)
    {
        const int borderThickness = 2;
        const int padding = 4;
        const float towerSpriteSize = 30f;
        const float enemySpriteSize = 20f;

        if (_towerManager.SelectedTower != null)
        {
            var tower = _towerManager.SelectedTower;
            int w = (int)(towerSpriteSize * tower.DrawScale.X) + padding * 2;
            int h = (int)(towerSpriteSize * tower.DrawScale.Y) + padding * 2;
            var rect = new Rectangle(
                (int)(tower.WorldPosition.X - w / 2f),
                (int)(tower.WorldPosition.Y - h / 2f),
                w,
                h
            );
            TextureManager.DrawRectOutline(spriteBatch, rect, Color.Red, borderThickness);

            // World-space wall placement button: shown when any walling tower is selected.
            // Active (wall mode on) = dark green filled; inactive = dark outline only.
            if (tower.TowerType == TowerType.ChampionWalling || tower.TowerType.IsWallingGeneric())
            {
                var btnRect = GetWallPlacementButtonRect(tower);
                Color btnBg = _wallPlacementMode ? Color.DarkGreen : new Color(20, 60, 20);
                Color btnOutline = _wallPlacementMode ? Color.LimeGreen : Color.DarkGreen;
                TextureManager.DrawRect(spriteBatch, btnRect, btnBg);
                TextureManager.DrawRectOutline(spriteBatch, btnRect, btnOutline, 2);

                // "+" symbol: two thin rectangles (horizontal and vertical bars)
                int cx = btnRect.X + btnRect.Width / 2;
                int cy = btnRect.Y + btnRect.Height / 2;
                TextureManager.DrawRect(
                    spriteBatch,
                    new Rectangle(cx - 4, cy - 1, 8, 3),
                    Color.White
                );
                TextureManager.DrawRect(
                    spriteBatch,
                    new Rectangle(cx - 1, cy - 4, 3, 8),
                    Color.White
                );
            }
        }

        if (_selectedEnemy != null)
        {
            int size = (int)enemySpriteSize + padding * 2;
            var rect = new Rectangle(
                (int)(_selectedEnemy.Position.X - size / 2f),
                (int)(_selectedEnemy.Position.Y - size / 2f),
                size,
                size
            );
            TextureManager.DrawRectOutline(spriteBatch, rect, Color.Red, borderThickness);
        }
    }

    /// <summary>
    /// Visualizes a planned tower movement path as dots connected by line segments.
    /// Gold color distinguishes it from the enemy path (DeepSkyBlue).
    /// </summary>
    private static void DrawTowerMovePreview(SpriteBatch spriteBatch, List<Point> path)
    {
        if (path.Count == 0)
            return;

        const int dotSize = 8;
        int halfDot = dotSize / 2;
        var pathColor = Color.Gold * 0.75f;

        for (int i = 0; i < path.Count; i++)
        {
            var point = path[i];
            int centerX = point.X * GameSettings.TileSize + GameSettings.TileSize / 2;
            int centerY = point.Y * GameSettings.TileSize + GameSettings.TileSize / 2;

            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(centerX - halfDot, centerY - halfDot, dotSize, dotSize),
                pathColor
            );

            if (i < path.Count - 1)
            {
                var next = path[i + 1];
                int nextCenterX = next.X * GameSettings.TileSize + GameSettings.TileSize / 2;
                int nextCenterY = next.Y * GameSettings.TileSize + GameSettings.TileSize / 2;

                if (next.Y == point.Y)
                {
                    int minX = Math.Min(centerX, nextCenterX);
                    TextureManager.DrawRect(
                        spriteBatch,
                        new Rectangle(
                            minX,
                            centerY - halfDot / 2,
                            Math.Abs(nextCenterX - centerX),
                            halfDot
                        ),
                        pathColor
                    );
                }
                else if (next.X == point.X)
                {
                    int minY = Math.Min(centerY, nextCenterY);
                    TextureManager.DrawRect(
                        spriteBatch,
                        new Rectangle(
                            centerX - halfDot / 2,
                            minY,
                            halfDot,
                            Math.Abs(nextCenterY - centerY)
                        ),
                        pathColor
                    );
                }
            }
        }
    }

    /// <summary>
    /// Returns the screen-space rectangle for the world-space wall placement button
    /// shown top-right of the walling champion sprite when it is selected.
    /// </summary>
    private static Rectangle GetWallPlacementButtonRect(Tower wallingTower)
    {
        const int btnSize = 18;
        const float spriteHalfWidth = 15f; // SpriteSize(30) / 2
        Vector2 pos = wallingTower.DrawPosition;
        int bx = (int)(pos.X + spriteHalfWidth + 4);
        int by = (int)(pos.Y - spriteHalfWidth - btnSize - 2);
        return new Rectangle(bx, by, btnSize, btnSize);
    }
}
