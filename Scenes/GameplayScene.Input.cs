using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.UI;

namespace StarterTD.Scenes;

/// <summary>
/// Input handling for GameplayScene. Partial class split from GameplayScene.cs.
/// </summary>
public partial class GameplayScene
{
    private void HandleInput()
    {
        if ((_gameOver || _gameWon) && _inputManager.IsKeyPressed(Keys.R))
        {
            _game.SetScene(new MapSelectionScene(_game));
            return;
        }

        if (_gameOver || _gameWon)
            return;

        if (_inputManager.IsKeyPressed(Keys.P))
        {
            _game.PushScene(new PauseScene(_game));
            return;
        }

        if (_inputManager.IsKeyPressed(Keys.Escape))
        {
            _uiPanel.ClearSelection();
            _selectedTowerRange = 0f;
            _towerMovePreviewPath = null;
            DeselectAll();
        }

        if (_inputManager.IsLeftClick())
            HandleLeftClick();

        if (_inputManager.IsLeftHeld())
            UpdateWallDrag();

        if (_inputManager.IsLeftReleased())
            CommitWallDrag();

        if (_inputManager.IsRightClick())
            HandleRightClick();
    }

    private void HandleLeftClick()
    {
        Point mousePos = _inputManager.MousePosition;

        // Check if the world-space wall placement button was clicked.
        // This takes priority over grid clicks so it is checked before panel/grid routing.
        var selectedWalling = GetSelectedWallingAnchor();
        if (
            selectedWalling != null
            && GetWallPlacementButtonRect(selectedWalling).Contains(mousePos)
        )
        {
            _wallPlacementMode = !_wallPlacementMode;
            if (!_wallPlacementMode)
                CancelWallDrag();
        }
        // Check if click is on UI panel first
        else if (_uiPanel.ContainsPoint(mousePos))
        {
            _uiPanel.HandleClick(mousePos, _placementCooldowns);

            // Selecting a tower to place clears any existing selection
            if (_uiPanel.SelectedTowerType.HasValue)
                DeselectAll();

            // Cache selected tower range to avoid per-frame GetStats allocation in Draw
            _selectedTowerRange = _uiPanel.SelectedTowerType.HasValue
                ? TowerData.GetStats(_uiPanel.SelectedTowerType.Value).Range
                : 0f;

            if (_uiPanel.StartWaveClicked && !_waveManager.WaveInProgress && _allEnemiesCleared)
                _waveManager.StartNextWave();

            if (_uiPanel.SpawnEnemyClicked)
                SpawnDebugEnemy();
        }
        else
        {
            HandleGridLeftClick(mousePos);
        }
    }

    /// <summary>
    /// Clears all selections (laser, tower, enemy) and resets associated modes.
    /// Call before setting any new selection to enforce single-selection invariant.
    /// </summary>
    private void DeselectAll()
    {
        _laserSelected = false;
        if (_laserEffect != null)
            _laserEffect.IsSelected = false;

        _towerManager.SelectedTower = null;
        _selectedEnemy = null;
        _wallPlacementMode = false;
        CancelWallDrag();
    }

    private void HandleGridLeftClick(Point mousePos)
    {
        // Left-clicking the active laser beam selects it so right-click can redirect it.
        if (_laserEffect != null && _laserEffect.ContainsPoint(mousePos.ToVector2()))
        {
            DeselectAll();
            _laserSelected = true;
            _laserEffect.IsSelected = true;
            return;
        }

        Point gridPos = Map.WorldToGrid(mousePos.ToVector2());

        // Preserve wall drag if the selected walling tower is still selected
        var wallingAnchor = _towerManager.SelectedTower;
        if (_wallPlacementMode && wallingAnchor != null)
        {
            StartWallDrag(gridPos);
        }
        else if (_uiPanel.SelectionMode == UISelectionMode.PlaceHighGround)
        {
            if (
                gridPos.X >= 0
                && gridPos.X < _map.Columns
                && gridPos.Y >= 0
                && gridPos.Y < _map.Rows
            )
            {
                _map.Tiles[gridPos.X, gridPos.Y].Type = TileType.HighGround;
                RecomputePathAndReroute();
            }
        }
        else if (_uiPanel.SelectedTowerType.HasValue)
        {
            var towerType = _uiPanel.SelectedTowerType.Value;
            var poolKey = GetCooldownPoolKey(towerType);
            // UIPanel already blocks selection during cooldown, but guard here as well
            if (_placementCooldowns[poolKey] <= 0f)
            {
                bool placed = _towerManager.TryPlaceTower(towerType, gridPos);
                if (placed)
                {
                    var stats = TowerData.GetStats(towerType);
                    _placementCooldowns[poolKey] += stats.BaseCooldown + stats.CooldownPenalty;
                    _uiPanel.SelectedTowerType = null;
                    _selectedTowerRange = 0f;
                }
            }
        }
        else
        {
            // Try to select an enemy first, then a tower
            var enemy = GetEnemyAt(mousePos.ToVector2());
            if (enemy != null)
            {
                DeselectAll();
                _selectedEnemy = enemy;
            }
            else
            {
                var tower = _towerManager.GetTowerAt(gridPos);
                // Selecting a different tower resets wall mode
                if (tower != _towerManager.SelectedTower)
                    DeselectAll();
                _towerManager.SelectedTower = tower;
            }
        }
    }

    private void HandleRightClick()
    {
        Point mousePos = _inputManager.MousePosition;

        if (_uiPanel.ContainsPoint(mousePos))
            return;

        // When the laser is selected, right-click redirects the beam instead of selling/moving
        if (_laserSelected && _laserEffect != null)
        {
            _laserEffect.SetTarget(mousePos.ToVector2());
            return;
        }

        Point gridPos = Map.WorldToGrid(mousePos.ToVector2());
        var tower = _towerManager.GetTowerAt(gridPos);

        // Move command: selected walkable tower + right-click on empty buildable tile.
        // Blocked when wall placement mode is active so the champion stays put.
        var selected = _towerManager.SelectedTower;
        if (
            selected != null
            && selected.CanWalk
            && selected.CurrentState == TowerState.Active
            && !_wallPlacementMode
            && tower == null
            && _map.CanBuild(gridPos)
        )
        {
            _towerManager.MoveTower(selected, gridPos);
            _towerManager.SelectedTower = null;
            _towerMovePreviewPath = null;
        }
        else if (tower != null)
        {
            if (_towerManager.SelectedTower == tower)
            {
                _towerManager.SelectedTower = null;
                _wallPlacementMode = false;
                CancelWallDrag();
            }

            float penalty = _towerManager.SellTower(tower);
            // Reduce that type's placement pool by CooldownPenalty (capped at 0)
            if (penalty > 0f)
            {
                var poolKey = GetCooldownPoolKey(tower.TowerType);
                _placementCooldowns[poolKey] = Math.Max(0f, _placementCooldowns[poolKey] - penalty);
            }
        }
    }
}
