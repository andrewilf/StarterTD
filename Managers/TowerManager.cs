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
    /// Callback to validate placement in a maze zone. Set by GameplayScene (mediator).
    /// Returns true if the path is still valid after placing here, false if it would block.
    ///
    /// C# analogy to Python/TS: Like setting `tower_manager.on_validate = lambda pos: check_path(pos)`.
    /// Func&lt;Point, bool&gt; is a function that takes a Point and returns a bool.
    /// </summary>
    public Func<Point, bool>? OnValidateMazeZonePlacement;

    /// <summary>
    /// Callback fired after a tower is successfully placed in a maze zone.
    /// GameplayScene uses this to trigger path recomputation and enemy rerouting.
    /// </summary>
    public Action<Point>? OnTowerPlacedInMazeZone;

    public TowerManager(Map map)
    {
        _map = map;
    }

    /// <summary>
    /// Try to place a tower at the given grid position.
    /// Returns the cost if successful, or -1 if placement failed.
    /// For maze zone tiles, validates that the path isn't fully blocked before allowing.
    /// </summary>
    public int TryPlaceTower(TowerType type, Point gridPos)
    {
        if (!_map.CanBuild(gridPos))
            return -1;

        var tile = _map.Tiles[gridPos.X, gridPos.Y];

        // Maze zone validation: check that placing here won't fully block the path
        if (tile.MazeZone != null && OnValidateMazeZonePlacement != null)
        {
            if (!OnValidateMazeZonePlacement(gridPos))
                return -1; // Would block the path — placement denied
        }

        var tower = new Tower(type, gridPos);
        _towers.Add(tower);
        tile.Type = TileType.Occupied;

        // Notify mediator if placed in a maze zone (triggers path recomputation)
        if (tile.MazeZone != null)
            OnTowerPlacedInMazeZone?.Invoke(gridPos);

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
    /// Update all towers (targeting, firing, projectiles).
    /// </summary>
    public void Update(GameTime gameTime, List<IEnemy> enemies)
    {
        foreach (var tower in _towers)
        {
            tower.Update(gameTime, enemies);
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
