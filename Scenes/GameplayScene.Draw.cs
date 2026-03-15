using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.UI;

namespace StarterTD.Scenes;

/// <summary>
/// Rendering methods for GameplayScene. Partial class split from GameplayScene.cs.
///
/// Uses two SpriteBatch passes:
///   1. World-space — translated by _mapOffset so the map renders centered on screen.
///   2. Screen-space — no transform, used for the UI panel and fullscreen overlays.
///
/// SceneManager opens a screen-space batch before calling this, so we End() it first,
/// then re-open a screen-space batch at the end so SceneManager can close it consistently.
/// </summary>
public partial class GameplayScene
{
    public void Draw(SpriteBatch spriteBatch)
    {
        // --- Close SceneManager's default batch so we can open our own with a matrix ---
        spriteBatch.End();

        // === World-space pass (map-local coords translated to screen center) ===
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            _worldMatrix
        );

        _map.Draw(spriteBatch);
        DrawEntranceWarnings(spriteBatch);

        if (_towerMovePreviewPath != null)
        {
            Point footprintSize = _towerManager.SelectedTower?.FootprintSize ?? new Point(1, 1);
            DrawTowerMovePreview(spriteBatch, _towerMovePreviewPath, footprintSize);
        }

        _towerManager.Draw(spriteBatch, _uiPanel.GetFont(), _hoveredTower);

        DrawWallDragPreview(spriteBatch);

        foreach (var enemy in _enemies)
            enemy.Draw(spriteBatch);

        DrawSelectionIndicators(spriteBatch);

        foreach (var effect in _aoeEffects)
            effect.Draw(spriteBatch);

        foreach (var spike in _spikeEffects)
            spike.Draw(spriteBatch);

        foreach (var railgun in _railgunEffects)
            railgun.Draw(spriteBatch);

        _laserEffect?.Draw(spriteBatch);
        DrawLaserRedirectPreview(spriteBatch);

        // Hover indicator on grid (world-space so it aligns with tiles)
        if (
            _mouseGrid.X >= 0
            && _mouseGrid.X < _map.Columns
            && _mouseGrid.Y >= 0
            && _mouseGrid.Y < _map.Rows
            && !_uiPanel.ContainsPoint(_inputManager.MousePosition)
            && !(_wallPlacementMode && _isWallDragActive)
            && !_isTowerMoveDragActive
        )
        {
            DrawHoverIndicator(spriteBatch);
        }

        spriteBatch.End();

        // === Screen-space pass (UI panel, overlays — no matrix) ===
        // Left open so SceneManager can close the batch after the scene finishes drawing.
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            null
        );

        _uiPanel.Draw(
            spriteBatch,
            _placementCooldowns,
            _lives,
            GetSpawnStatusText(),
            _timeSlowBank / TimeSlowMaxBank,
            _towerManager.SelectedTower,
            _selectedEnemy
        );

        if (_gameOver || _gameWon)
            DrawGameOverOverlay(spriteBatch, _uiPanel.GetFont());

        if (!GumService.Default.IsInitialized)
            return;

        // SceneManager owns the SpriteBatch lifecycle around scene draws. Gum renders
        // through its own pipeline, so we end the current batch, draw Gum, and reopen
        // a matching batch so SceneManager can end it safely.
        spriteBatch.End();
        GumService.Default.Draw();
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

    private string GetSpawnStatusText()
    {
        if (
            _spawnScheduleManager.SpawnedCount == 0
            && _spawnScheduleManager.TryGetPendingSpawn(0, out SpawnEntry nextSpawn)
        )
        {
            float secondsUntilSpawn = Math.Max(
                0f,
                nextSpawn.At - _spawnScheduleManager.ElapsedSeconds
            );
            return $"First enemy in: {secondsUntilSpawn:F1}s";
        }

        return $"Enemies Spawned: {_spawnScheduleManager.SpawnedCount}/{_spawnScheduleManager.TotalSpawnCount}";
    }

    private void DrawHoverIndicator(SpriteBatch spriteBatch)
    {
        Vector2 worldMouse = ScreenToWorld(_inputManager.MousePositionVector);
        Point placementTopLeft = _mouseGrid;
        Point placementFootprint = new Point(1, 1);
        TowerStats? selectedTowerStats = null;

        if (_uiPanel.SelectedTowerType.HasValue)
        {
            selectedTowerStats = TowerData.GetStats(_uiPanel.SelectedTowerType.Value);
            placementFootprint = selectedTowerStats.FootprintTiles;
            placementTopLeft = Map.SnapWorldToTopLeft(worldMouse, placementFootprint);
        }

        bool hasTowerSelection = selectedTowerStats != null;

        var hoverRect = new Rectangle(
            _mouseGrid.X * GameSettings.TileSize,
            _mouseGrid.Y * GameSettings.TileSize,
            GameSettings.TileSize,
            GameSettings.TileSize
        );

        bool canPlaceTower =
            hasTowerSelection
            && _map.CanBuildFootprint(
                placementTopLeft,
                placementFootprint,
                requireUniformTileType: _uiPanel.SelectedTowerType?.IsChampion() ?? false
            );
        bool isHighGroundMode = _uiPanel.SelectionMode == UISelectionMode.PlaceHighGround;
        var wallingAnchor = GetSelectedWallingAnchor();
        bool isWallMode = _wallPlacementMode && wallingAnchor != null;

        Color hoverColor;
        if (isWallMode)
        {
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

        float selectedTowerRange = selectedTowerStats?.Range ?? 0f;
        if (selectedTowerRange > 0f)
        {
            Vector2 hoverCenter = hasTowerSelection
                ? Map.AnchorToWorld(Map.TopLeftToAnchor(placementTopLeft, placementFootprint))
                : Map.GridToWorld(_mouseGrid);
            TextureManager.DrawFilledCircle(
                spriteBatch,
                hoverCenter,
                selectedTowerRange,
                Color.White * 0.15f
            );
        }

        if (hasTowerSelection)
        {
            DrawFootprintHover(
                spriteBatch,
                placementTopLeft,
                placementFootprint,
                hoverColor,
                Color.White
            );
            return;
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

            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, titleColor);

            int statsY = centerY - 40;
            string[] stats = _gameWon
                ? new[]
                {
                    "Schedule Complete!",
                    $"Enemies Spawned: {_spawnScheduleManager.SpawnedCount}/{_spawnScheduleManager.TotalSpawnCount}",
                    $"Lives Remaining: {_lives}",
                    "Press R to return to map selection",
                }
                : new[]
                {
                    $"Enemies Spawned: {_spawnScheduleManager.SpawnedCount}/{_spawnScheduleManager.TotalSpawnCount}",
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
            Color indicatorColor = _gameWon ? Color.Gold : Color.Red;
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(centerX - 100, centerY - 30, 200, 60),
                indicatorColor
            );
        }
    }

    private void DrawLaserRedirectPreview(SpriteBatch spriteBatch)
    {
        if (!_isLaserRedirectArmed || !_laserSelected || _laserEffect == null)
            return;

        Vector2 laserTarget = _isLaserRedirectActive
            ? _laserRedirectTargetWorld
            : _laserRedirectStartWorld;

        if (Vector2.DistanceSquared(LaserEffect.BeamOriginWorld, laserTarget) < 1f)
            return;

        Vector2 start = LaserEffect.BeamOriginWorld;
        Vector2 end = laserTarget;
        Vector2 direction = end - start;
        float length = direction.Length();
        if (length <= 0f)
            return;

        float rotation = MathF.Atan2(direction.Y, direction.X);
        Vector2 center = (start + end) * 0.5f;
        TextureManager.DrawSprite(
            spriteBatch,
            center,
            new Vector2(length, 4f),
            Color.Red * 0.75f,
            rotation
        );
    }

    private void DrawWallDragPreview(SpriteBatch spriteBatch)
    {
        if (!_wallPlacementMode || !_isWallDragActive || _wallDragPreviewPath == null)
            return;

        for (int i = 0; i < _wallDragPreviewPath.Count; i++)
        {
            var point = _wallDragPreviewPath[i];
            if (point.X < 0 || point.X >= _map.Columns || point.Y < 0 || point.Y >= _map.Rows)
                continue;

            var rect = new Rectangle(
                point.X * GameSettings.TileSize,
                point.Y * GameSettings.TileSize,
                GameSettings.TileSize,
                GameSettings.TileSize
            );

            bool isValidPrefix = i < _wallDragValidPrefixLength;
            Color fill = isValidPrefix ? Color.DarkGreen * 0.5f : Color.Red * 0.3f;
            Color outline = isValidPrefix ? Color.LimeGreen : Color.Red;

            TextureManager.DrawRect(spriteBatch, rect, fill);
            TextureManager.DrawRectOutline(spriteBatch, rect, outline, 1);
        }
    }

    /// <summary>
    /// Draw a yellow rectangle outline around the selected tower or enemy.
    /// Also renders in-world controls for the selected tower.
    /// </summary>
    private void DrawSelectionIndicators(SpriteBatch spriteBatch)
    {
        const int borderThickness = 2;
        const int padding = 4;
        const float enemySpriteSize = 20f;

        if (_towerManager.SelectedTower != null)
        {
            var tower = _towerManager.SelectedTower;
            TowerDrawingHelper.GetVisualBounds(
                tower,
                out Vector2 drawPosition,
                out Vector2 drawSize
            );
            int w = (int)MathF.Round(drawSize.X) + padding * 2;
            int h = (int)MathF.Round(drawSize.Y) + padding * 2;
            var rect = new Rectangle(
                (int)(drawPosition.X - w / 2f),
                (int)(drawPosition.Y - h / 2f),
                w,
                h
            );
            TextureManager.DrawRectOutline(spriteBatch, rect, Color.Yellow, borderThickness);
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
            TextureManager.DrawRectOutline(spriteBatch, rect, Color.Yellow, borderThickness);
        }
    }

    /// <summary>
    /// Visualizes a planned tower movement path as dots connected by line segments.
    /// Gold color distinguishes it from the enemy path (DeepSkyBlue).
    /// </summary>
    private void DrawTowerMovePreview(
        SpriteBatch spriteBatch,
        List<Point> path,
        Point footprintSize
    )
    {
        if (path.Count == 0)
            return;

        const int dotSize = 8;
        int halfDot = dotSize / 2;
        var pathColor = Color.Gold * 0.75f;

        for (int i = 0; i < path.Count; i++)
        {
            var point = path[i];
            Vector2 center = Map.AnchorToWorld(Map.TopLeftToAnchor(point, footprintSize));
            int centerX = (int)center.X;
            int centerY = (int)center.Y;

            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(centerX - halfDot, centerY - halfDot, dotSize, dotSize),
                pathColor
            );

            if (i < path.Count - 1)
            {
                var next = path[i + 1];
                Vector2 nextCenter = Map.AnchorToWorld(Map.TopLeftToAnchor(next, footprintSize));
                int nextCenterX = (int)nextCenter.X;
                int nextCenterY = (int)nextCenter.Y;

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

        // Destination footprint indicator (reuses existing rectangle helpers).
        // Makes the final occupied tiles explicit for multi-tile towers.
        Point destinationTopLeft = path[^1];
        DrawFootprintHover(
            spriteBatch,
            destinationTopLeft,
            footprintSize,
            Color.White * 0.22f,
            Color.White * 0.7f
        );
    }

    private static Rectangle GetSellButtonRect(Tower tower)
    {
        const int btnSize = 18;
        var wallBtnRect = GetWallPlacementButtonRect(tower);
        int bx = wallBtnRect.X;
        int by =
            tower.TowerType.IsWallingChampion() || tower.TowerType.IsWallingGeneric()
                ? wallBtnRect.Y - btnSize - 2
                : wallBtnRect.Y;
        return new Rectangle(bx, by, btnSize, btnSize);
    }

    private static Rectangle GetHealingModeButtonRect(Tower tower)
    {
        const int btnSize = 18;
        var sellRect = GetSellButtonRect(tower);
        return new Rectangle(sellRect.X, sellRect.Bottom + 2, btnSize, btnSize);
    }

    /// <summary>
    /// Returns the world-space rectangle for the wall placement button
    /// shown top-right of the walling champion sprite when it is selected.
    /// </summary>
    private static Rectangle GetWallPlacementButtonRect(Tower wallingTower)
    {
        const int btnSize = 18;
        TowerDrawingHelper.GetVisualBounds(wallingTower, out Vector2 pos, out Vector2 drawSize);
        int bx = (int)(pos.X + drawSize.X / 2f + 4f);
        int by = (int)(pos.Y - drawSize.Y / 2f - btnSize - 2f);
        return new Rectangle(bx, by, btnSize, btnSize);
    }

    private void DrawFootprintHover(
        SpriteBatch spriteBatch,
        Point topLeft,
        Point footprintSize,
        Color fillColor,
        Color outlineColor
    )
    {
        int tileSize = GameSettings.TileSize;
        for (int y = 0; y < footprintSize.Y; y++)
        {
            for (int x = 0; x < footprintSize.X; x++)
            {
                int gx = topLeft.X + x;
                int gy = topLeft.Y + y;
                if (gx < 0 || gx >= _map.Columns || gy < 0 || gy >= _map.Rows)
                    continue;

                var rect = new Rectangle(gx * tileSize, gy * tileSize, tileSize, tileSize);
                TextureManager.DrawRect(spriteBatch, rect, fillColor);
                TextureManager.DrawRectOutline(spriteBatch, rect, outlineColor, 1);
            }
        }
    }
}
