using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.Interfaces;

namespace StarterTD.Managers;

/// <summary>
/// Manages all placed towers: placement, upgrading, updating, and drawing.
/// </summary>
public class TowerManager
{
    private readonly List<Tower> _towers = new();
    private readonly Map _map;

    /// <summary>The currently selected/hovered tower (for showing range, upgrade info).</summary>
    public Tower? SelectedTower { get; set; }

    public IReadOnlyList<Tower> Towers => _towers;

    /// <summary>
    /// Callback to validate placement. Set by GameplayScene (mediator).
    /// With movement costs, always returns true (path is never blocked).
    ///
    /// C# analogy to Python/TS: Like setting `tower_manager.on_validate = lambda pos: check_path(pos)`.
    /// Func&lt;Point, bool&gt; is a function that takes a Point and returns a bool.
    /// </summary>
    public Func<Point, bool>? OnValidatePlacement;

    /// <summary>
    /// Callback fired after any tower is successfully placed.
    /// GameplayScene uses this to recompute the heat map and reroute enemies.
    /// </summary>
    public Action<Point>? OnTowerPlaced;

    /// <summary>
    /// Callback fired when a tower is destroyed (health reaches 0).
    /// GameplayScene uses this to recompute the heat map and reroute enemies.
    /// </summary>
    public Action<Point>? OnTowerDestroyed;

    public TowerManager(Map map)
    {
        _map = map;
    }

    /// <summary>
    /// Try to place a tower at the given grid position.
    /// Returns the cost if successful, or -1 if placement failed.
    /// </summary>
    public int TryPlaceTower(TowerType type, Point gridPos)
    {
        if (!_map.CanBuild(gridPos))
            return -1;

        var tile = _map.Tiles[gridPos.X, gridPos.Y];

        if (OnValidatePlacement != null && !OnValidatePlacement(gridPos))
            return -1;

        var tower = new Tower(type, gridPos);
        _towers.Add(tower);
        tile.Type = TileType.Occupied;
        tile.OccupyingTowerType = type;

        // Every placement changes the heat map — notify mediator to recompute
        OnTowerPlaced?.Invoke(gridPos);

        return tower.Cost;
    }

    /// <summary>
    /// Try to upgrade the tower at the given grid position.
    /// Returns the upgrade cost if successful, or -1 if failed.
    /// </summary>
    public int TryUpgradeTower(Point gridPos)
    {
        var tower = GetTowerAt(gridPos);
        if (tower == null || tower.Level >= 2)
            return -1;

        int cost = tower.UpgradeCost;
        tower.Upgrade();
        return cost;
    }

    /// <summary>
    /// Get the tower at a specific grid position, or null.
    /// </summary>
    public Tower? GetTowerAt(Point gridPos)
    {
        return _towers.FirstOrDefault(t => t.GridPosition == gridPos);
    }

    /// <summary>
    /// Remove a tower from the grid: restore tile to its original type, clear tower reference, notify scene.
    /// </summary>
    private void RemoveTower(Tower tower)
    {
        var tile = _map.Tiles[tower.GridPosition.X, tower.GridPosition.Y];
        tile.Type = tile.OriginalType;
        tile.OccupyingTowerType = null;
        _towers.Remove(tower);

        if (SelectedTower == tower)
            SelectedTower = null;

        OnTowerDestroyed?.Invoke(tower.GridPosition);
    }

    /// <summary>
    /// Debug: instantly remove all towers. Each triggers OnTowerDestroyed individually.
    /// </summary>
    public void KillAllTowers()
    {
        var towersToKill = new List<Tower>(_towers);
        foreach (var tower in towersToKill)
        {
            RemoveTower(tower);
        }
    }

    /// <summary>
    /// Update all towers (targeting, firing, projectiles).
    /// </summary>
    public void Update(GameTime gameTime, List<IEnemy> enemies)
    {
        foreach (var tower in _towers)
        {
            tower.Update(gameTime, enemies);
        }

        // Remove destroyed towers (iterate backwards to safely remove during iteration)
        for (int i = _towers.Count - 1; i >= 0; i--)
        {
            if (_towers[i].IsDead)
            {
                RemoveTower(_towers[i]);
            }
        }
    }

    /// <summary>
    /// Draw all towers and their projectiles.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont? font = null)
    {
        foreach (var tower in _towers)
        {
            tower.Draw(spriteBatch, font);
        }

        // Draw range indicator for selected tower
        SelectedTower?.DrawRangeIndicator(spriteBatch);
    }
}
