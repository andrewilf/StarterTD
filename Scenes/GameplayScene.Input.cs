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
            _towerManager.SelectedTower = null;
            _selectedEnemy = null;
            _towerMovePreviewPath = null;
            _wallPlacementMode = false;
        }

        if (_inputManager.IsLeftClick())
            HandleLeftClick();

        if (_inputManager.IsRightClick())
            HandleRightClick();
    }

    private void HandleLeftClick()
    {
        Point mousePos = _inputManager.MousePosition;

        // Check if the world-space wall placement button was clicked.
        // This takes priority over grid clicks so it is checked before panel/grid routing.
        var selectedWalling = _towerManager.SelectedTower;
        if (
            selectedWalling != null
            && (
                selectedWalling.TowerType == TowerType.ChampionWalling
                || selectedWalling.TowerType.IsWallingGeneric()
            )
            && GetWallPlacementButtonRect(selectedWalling).Contains(mousePos)
        )
        {
            _wallPlacementMode = !_wallPlacementMode;
        }
        // Check if click is on UI panel first
        else if (_uiPanel.ContainsPoint(mousePos))
        {
            _uiPanel.HandleClick(mousePos, _money);

            // Selecting a tower to place clears the inspected tower, enemy, and wall mode
            if (_uiPanel.SelectedTowerType.HasValue)
            {
                _towerManager.SelectedTower = null;
                _selectedEnemy = null;
                _wallPlacementMode = false;
            }

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

    private void HandleGridLeftClick(Point mousePos)
    {
        Point gridPos = Map.WorldToGrid(mousePos.ToVector2());

        var wallingAnchor = _towerManager.SelectedTower;
        if (_wallPlacementMode && wallingAnchor != null)
        {
            _towerManager.TryPlaceWall(gridPos, wallingAnchor);
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
            var stats = TowerData.GetStats(_uiPanel.SelectedTowerType.Value);
            if (_money >= stats.Cost)
            {
                int cost = _towerManager.TryPlaceTower(_uiPanel.SelectedTowerType.Value, gridPos);
                if (cost >= 0)
                {
                    _money -= cost;
                    Vector2 worldPos = Map.GridToWorld(gridPos);
                    if (cost > 0)
                        SpawnFloatingText(worldPos, $"-${cost}", Color.Red);
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
                _selectedEnemy = enemy;
                _towerManager.SelectedTower = null;
                _wallPlacementMode = false;
            }
            else
            {
                // Select existing tower; clear wall mode if selection changes
                var tower = _towerManager.GetTowerAt(gridPos);
                if (tower != _towerManager.SelectedTower)
                    _wallPlacementMode = false;
                _towerManager.SelectedTower = tower;
                _selectedEnemy = null;
            }
        }
    }

    private void HandleRightClick()
    {
        Point mousePos = _inputManager.MousePosition;

        if (_uiPanel.ContainsPoint(mousePos))
            return;

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
            }

            int refund = _towerManager.SellTower(tower);
            _money += refund;

            Vector2 worldPos = Map.GridToWorld(gridPos);
            SpawnFloatingText(worldPos, $"+${refund}", Color.LimeGreen);
        }
    }
}
