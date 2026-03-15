using System;
using System.Linq;
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
            _towerMovePreviewPath = null;
            DeselectAll();
        }

        if (_inputManager.IsLeftClick())
            HandleLeftClick();

        if (_inputManager.IsLeftHeld())
        {
            UpdateWallDrag();
            UpdateTowerMoveDrag();
            UpdateLaserRedirectDrag();
        }

        if (_inputManager.IsLeftReleased())
        {
            if (_isTowerMoveDragActive)
                CommitTowerMoveDrag();
            else if (_isTowerMoveDragArmed)
                CancelTowerMoveDrag();

            CommitWallDrag();

            if (_isLaserRedirectActive)
                CommitLaserRedirectDrag();
            else if (_isLaserRedirectArmed)
                CancelLaserRedirectDrag();
        }

        if (_inputManager.IsRightClick())
            HandleRightClick();
    }

    private void HandleLeftClick()
    {
        Point screenPos = _inputManager.MousePosition;
        // World-space mouse position (map-local coords, offset removed)
        Vector2 worldMouse = ScreenToWorld(_inputManager.MousePositionVector);

        if (IsWorldGumButtonHit(screenPos))
            return;

        // Panel background still consumes clicks so world interactions do not fire through it.
        if (_uiPanel.ContainsPoint(screenPos))
            return;

        HandleGridLeftClick(worldMouse);
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
        CancelTowerMoveDrag();
        CancelLaserRedirectDrag();
    }

    private void HandleGridLeftClick(Vector2 worldMouse)
    {
        // Left-clicking the active laser beam selects it so right-click can redirect it.
        if (_laserEffect != null && _laserEffect.ContainsPoint(worldMouse))
        {
            DeselectAll();
            _laserSelected = true;
            _laserEffect.IsSelected = true;
            return;
        }

        if (_laserSelected && _laserEffect != null)
        {
            StartLaserRedirectDrag(worldMouse);
            return;
        }

        Point gridPos = Map.WorldToGrid(worldMouse);

        var selectedTower = _towerManager.SelectedTower;
        var wallingAnchor = selectedTower;
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
            var stats = TowerData.GetStats(towerType);
            Point placementTopLeft = Map.SnapWorldToTopLeft(worldMouse, stats.FootprintTiles);
            var poolKey = GetCooldownPoolKey(towerType);
            if (_placementCooldowns[poolKey] <= 0f)
            {
                int poolCount = _towerManager.Towers.Count(t =>
                    GetCooldownPoolKey(t.TowerType) == poolKey
                );
                bool placed = _towerManager.TryPlaceTower(towerType, placementTopLeft);
                if (placed)
                {
                    _placementCooldowns[poolKey] +=
                        stats.BaseCooldown + stats.CooldownPenalty * poolCount;
                    _uiPanel.SelectedTowerType = null;
                }
            }
        }
        else
        {
            var clickedTower = _towerManager.GetTowerAt(gridPos);
            if (clickedTower is { CanWalk: true, CurrentState: TowerState.Active })
            {
                ArmTowerMoveDrag(clickedTower, gridPos, worldMouse);
                if (_isTowerMoveDragActive)
                    return;
            }

            var enemy = GetEnemyAt(worldMouse);
            if (enemy != null)
            {
                DeselectAll();
                _selectedEnemy = enemy;
            }
            else
            {
                var tower = clickedTower;
                if (tower != _towerManager.SelectedTower)
                    DeselectAll();
                _towerManager.SelectedTower = tower;
            }
        }
    }

    private void HandleRightClick()
    {
        if (_uiPanel.ContainsPoint(_inputManager.MousePosition))
            return;

        if (_laserSelected && _laserEffect != null)
            DeselectAll();
    }

    private void StartLaserRedirectDrag(Vector2 worldMouse)
    {
        if (_uiPanel.ContainsPoint(_inputManager.MousePosition))
            return;

        if (_laserSelected && _laserEffect != null)
        {
            _isLaserRedirectArmed = true;
            _isLaserRedirectActive = false;
            _laserRedirectStartWorld = worldMouse;
            _laserRedirectTargetWorld = worldMouse;
        }
    }

    private void UpdateLaserRedirectDrag()
    {
        if (!_isLaserRedirectArmed && !_isLaserRedirectActive)
            return;

        var activeLaser = _laserEffect;
        if (!_laserSelected || activeLaser == null)
        {
            CancelLaserRedirectDrag();
            return;
        }

        Vector2 currentWorldMouse = ScreenToWorld(_inputManager.MousePositionVector);
        _laserRedirectTargetWorld = currentWorldMouse;
        if (!_isLaserRedirectActive)
        {
            float dragSqr = Vector2.DistanceSquared(currentWorldMouse, _laserRedirectStartWorld);
            if (dragSqr < TowerMoveDragStartThreshold * TowerMoveDragStartThreshold)
                return;

            _isLaserRedirectActive = true;
        }

        activeLaser.SetTarget(currentWorldMouse);
    }

    private void CommitLaserRedirectDrag()
    {
        if (!_isLaserRedirectActive)
            return;

        var activeLaser = _laserEffect;
        if (_laserSelected && activeLaser != null)
            activeLaser.SetTarget(_laserRedirectTargetWorld);

        CancelLaserRedirectDrag();
    }

    private void CancelLaserRedirectDrag()
    {
        _isLaserRedirectArmed = false;
        _isLaserRedirectActive = false;
        _laserRedirectTargetWorld = Vector2.Zero;
        _laserRedirectStartWorld = Vector2.Zero;
    }

    private void ArmTowerMoveDrag(Tower selectedTower, Point startGrid, Vector2 worldMouse)
    {
        if (
            selectedTower.CurrentState != TowerState.Active
            || !selectedTower.CanWalk
            || !selectedTower.OccupiesTile(startGrid)
        )
            return;

        _towerManager.SelectedTower = selectedTower;
        _isTowerMoveDragArmed = true;
        _isTowerMoveDragActive = false;
        _towerMoveDragStartWorld = worldMouse;
        _towerMoveDragStartGrid = startGrid;
        _towerMoveDragCurrentGrid = startGrid;
        _towerMovePreviewPath = null;
    }

    private void UpdateTowerMoveDrag()
    {
        if (!_isTowerMoveDragActive && !_isTowerMoveDragArmed)
            return;

        var selectedTower = _towerManager.SelectedTower;
        if (
            selectedTower == null
            || !selectedTower.CanWalk
            || selectedTower.CurrentState != TowerState.Active
            || !selectedTower.OccupiesTile(_towerMoveDragStartGrid)
        )
        {
            CancelTowerMoveDrag();
            return;
        }

        Vector2 currentWorldMouse = ScreenToWorld(_inputManager.MousePositionVector);
        if (!_isTowerMoveDragActive)
        {
            float dragSqr = Vector2.DistanceSquared(currentWorldMouse, _towerMoveDragStartWorld);
            if (dragSqr < TowerMoveDragStartThreshold * TowerMoveDragStartThreshold)
                return;

            _isTowerMoveDragActive = true;
        }

        _towerMoveDragCurrentGrid = Map.SnapWorldToTopLeft(
            currentWorldMouse,
            selectedTower.FootprintSize
        );
        _towerMovePreviewPath = _towerManager.GetPreviewPath(_towerMoveDragCurrentGrid);
    }

    private void CommitTowerMoveDrag()
    {
        if (!_isTowerMoveDragActive)
            return;

        var selectedTower = _towerManager.SelectedTower;
        if (
            selectedTower != null
            && selectedTower.CurrentState == TowerState.Active
            && selectedTower.CanWalk
            && _towerMovePreviewPath is { Count: > 1 }
        )
        {
            _towerManager.MoveTower(selectedTower, _towerMoveDragCurrentGrid);
            _towerManager.SelectedTower = null;
        }

        CancelTowerMoveDrag();
    }

    private void CancelTowerMoveDrag()
    {
        _isTowerMoveDragArmed = false;
        _isTowerMoveDragActive = false;
        _towerMoveDragCurrentGrid = default;
        _towerMoveDragStartGrid = default;
        _towerMoveDragStartWorld = Vector2.Zero;
        _towerMovePreviewPath = null;
    }
}
