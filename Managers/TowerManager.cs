using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.Interfaces;

namespace StarterTD.Managers;

/// <summary>
/// Manages all placed towers: placement, updating, and drawing.
/// </summary>
public class TowerManager
{
    private readonly List<Tower> _towers = new();
    private readonly Map _map;
    private readonly ChampionManager _championManager;

    /// <summary>The currently selected tower (for future info display).</summary>
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

    /// <summary>
    /// Callback fired when an AoE projectile impacts (for visual effects).
    /// Passes the impact position and AoE radius. GameplayScene uses this to spawn AoEEffect.
    /// </summary>
    public Action<Vector2, float>? OnAOEImpact;

    /// <summary>
    /// Callback fired when the walling champion's spike attack lands on an enemy.
    /// Passes the enemy's world position. GameplayScene uses this to spawn SpikeEffect.
    /// </summary>
    public Action<Vector2>? OnWallAttack;

    public TowerManager(Map map, ChampionManager championManager)
    {
        _map = map;
        _championManager = championManager;
    }

    /// <summary>
    /// Try to place a tower at the given grid position.
    /// Returns the cost if successful, or -1 if placement failed.
    /// Champions can only have one per type placed at a time.
    /// </summary>
    public int TryPlaceTower(TowerType type, Point gridPos)
    {
        if (!_map.CanBuild(gridPos))
            return -1;

        var tile = _map.Tiles[gridPos.X, gridPos.Y];

        bool isChampion = type.IsChampion();
        bool canPlace = isChampion
            ? _championManager.CanPlaceChampion(type)
            : _championManager.CanPlaceGeneric(type);

        if (!canPlace)
            return -1;

        if (OnValidatePlacement != null && !OnValidatePlacement(gridPos))
            return -1;

        var tower = new Tower(type, gridPos);
        // Wire AoE callback once at placement (not per-frame)
        tower.OnAOEImpact = (pos, radius) => OnAOEImpact?.Invoke(pos, radius);
        _towers.Add(tower);
        tile.OccupyingTower = tower;

        if (type.IsChampion())
            _championManager.OnChampionPlaced(type);

        OnTowerPlaced?.Invoke(gridPos);

        return tower.Cost;
    }

    /// <summary>
    /// Get the tower at a specific grid position, or null.
    /// </summary>
    public Tower? GetTowerAt(Point gridPos)
    {
        foreach (var tower in _towers)
        {
            if (tower.GridPosition == gridPos)
                return tower;
        }
        return null;
    }

    /// <summary>
    /// Returns the planned movement path from the selected tower to destination,
    /// or null if no valid path exists. Used by GameplayScene for the hover preview.
    /// </summary>
    public List<Point>? GetPreviewPath(Point destination)
    {
        var tower = SelectedTower;
        if (tower == null || !tower.CanWalk || tower.CurrentState != TowerState.Active)
            return null;

        if (!_map.CanBuild(destination))
            return null;

        var queue = TowerPathfinder.FindPath(tower.GridPosition, destination, _map);
        if (queue == null || queue.Count <= 1)
            return null;

        return new List<Point>(queue);
    }

    /// <summary>
    /// Sell a tower, returning its refund value.
    /// Refund is 60% of cost, scaled by remaining health percentage.
    /// </summary>
    public int SellTower(Tower tower)
    {
        float healthPercent = (float)tower.CurrentHealth / tower.MaxHealth;
        int refund = (int)(tower.Cost * 0.6f * healthPercent);
        RemoveTower(tower);
        return refund;
    }

    /// <summary>
    /// Remove a tower from the grid: clear the tile's tower reference and notify scene.
    /// </summary>
    private void RemoveTower(Tower tower)
    {
        var tile = _map.Tiles[tower.GridPosition.X, tower.GridPosition.Y];
        if (tile.OccupyingTower == tower)
            tile.OccupyingTower = null;

        // If the tower died while moving, its reserved destination tile must be freed
        if (tower.CurrentState == TowerState.Moving)
            ClearReservationFor(tower);

        _towers.Remove(tower);

        if (tower.TowerType.IsChampion())
            _championManager.OnChampionDeath(tower.TowerType, _towers);

        if (SelectedTower == tower)
            SelectedTower = null;

        OnTowerDestroyed?.Invoke(tower.GridPosition);
    }

    /// <summary>
    /// Scans all tiles to clear a reservation left by a tower that died mid-movement.
    /// O(W*H) but only runs on the rare event of a moving tower dying.
    /// </summary>
    private void ClearReservationFor(Tower tower)
    {
        for (int x = 0; x < _map.Columns; x++)
        {
            for (int y = 0; y < _map.Rows; y++)
            {
                if (_map.Tiles[x, y].ReservedByTower == tower)
                {
                    _map.Tiles[x, y].ReservedByTower = null;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Initiate tower movement: clears the origin tile (ghost), triggers enemy reroute,
    /// and starts the tower's smooth movement along the path.
    /// </summary>
    public void MoveTower(Tower tower, Point destination)
    {
        var path = TowerPathfinder.FindPath(tower.GridPosition, destination, _map);
        if (path == null || path.Count <= 1)
            return;

        // Ghost: remove tower from origin tile so enemies treat it as open
        var originTile = _map.Tiles[tower.GridPosition.X, tower.GridPosition.Y];
        originTile.OccupyingTower = null;

        // Reserve destination immediately — blocks placement and other movement commands mid-transit
        var destTile = _map.Tiles[destination.X, destination.Y];
        destTile.ReservedByTower = tower;

        // Wire completion callback before starting (handles single-frame edge case)
        tower.OnMovementComplete = () => HandleMovementComplete(tower, destination);

        // Reroute enemies through the now-vacated tile
        OnTowerPlaced?.Invoke(tower.GridPosition);

        tower.StartMoving(path);
    }

    /// <summary>
    /// Called when a tower finishes its movement path. Re-occupies the destination tile
    /// and triggers enemy reroute around the newly placed tower.
    /// </summary>
    private void HandleMovementComplete(Tower tower, Point destination)
    {
        var destTile = _map.Tiles[destination.X, destination.Y];
        destTile.ReservedByTower = null;
        destTile.OccupyingTower = tower;

        OnTowerPlaced?.Invoke(destination);
    }

    /// <summary>
    /// Dispatches the champion super ability to all relevant towers.
    /// Each tower type's AbilityEffect delegate (defined in its own stats file) handles what the buff does.
    /// </summary>
    public void TriggerChampionAbility(TowerType championType)
    {
        // ChampionWalling has no generic variant and no ability effect.
        if (championType.IsWallingChampion())
            return;

        var genericType = championType.GetGenericVariant();

        foreach (var tower in _towers)
        {
            if (tower.TowerType == championType || tower.TowerType == genericType)
                TowerData.GetStats(tower.TowerType).AbilityEffect?.Invoke(tower);
        }
    }

    /// <summary>
    /// Place a wall segment adjacent to the walling champion's network.
    /// Bypasses the terrain buildability check so walls can go on any non-tower tile.
    /// Returns true if the wall was placed successfully.
    /// </summary>
    public bool TryPlaceWall(Point gridPos, Tower wallingTower)
    {
        if (!_map.CanBuild(gridPos))
            return false;

        if (!IsAdjacentToWallingNetwork(gridPos, wallingTower))
            return false;

        var wall = new Tower(TowerType.WallSegment, gridPos);
        _towers.Add(wall);
        _map.Tiles[gridPos.X, gridPos.Y].OccupyingTower = wall;
        OnTowerPlaced?.Invoke(gridPos);
        return true;
    }

    /// <summary>
    /// Returns true if gridPos is adjacent (4-directional) to a tile occupied by
    /// the walling champion or any wall segment that is chain-connected back to the champion.
    /// Uses BFS from the champion's position across wall segment tiles.
    /// Public so GameplayScene can use it for hover feedback during wall placement mode.
    /// </summary>
    public bool IsAdjacentToWallingNetwork(Point gridPos, Tower wallingTower)
    {
        var connected = BuildConnectedWallSet(wallingTower);

        Point[] dirs = [new(0, -1), new(0, 1), new(-1, 0), new(1, 0)];
        foreach (var dir in dirs)
        {
            var neighbor = new Point(gridPos.X + dir.X, gridPos.Y + dir.Y);
            if (connected.Contains(neighbor))
                return true;
        }
        return false;
    }

    /// <summary>
    /// BFS from the walling champion's position across adjacent wall segment tiles.
    /// Returns the set of grid positions that are connected to the champion (including the champion's own position).
    /// If wallingChampion is null (champion is dead), returns an empty set.
    /// </summary>
    private HashSet<Point> BuildConnectedWallSet(Tower? wallingChampion)
    {
        var connected = new HashSet<Point>();
        if (wallingChampion == null)
            return connected;

        Point[] dirs = [new(0, -1), new(0, 1), new(-1, 0), new(1, 0)];
        var queue = new Queue<Point>();

        connected.Add(wallingChampion.GridPosition);
        queue.Enqueue(wallingChampion.GridPosition);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var dir in dirs)
            {
                var neighbor = new Point(current.X + dir.X, current.Y + dir.Y);
                if (connected.Contains(neighbor))
                    continue;
                if (
                    neighbor.X < 0
                    || neighbor.X >= _map.Columns
                    || neighbor.Y < 0
                    || neighbor.Y >= _map.Rows
                )
                    continue;

                var occupant = _map.Tiles[neighbor.X, neighbor.Y].OccupyingTower;
                if (occupant != null && occupant.TowerType.IsWallSegment())
                {
                    connected.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return connected;
    }

    /// <summary>
    /// Apply decay damage to wall segments when the walling champion is not connected to any wall.
    /// If the champion is alive and adjacent to at least one wall segment, no decay occurs.
    /// Once disconnected (champion dead or moved away), each wall segment decays at 1 HP/sec
    /// per exposed cardinal side (sides not sheltered by another wall segment).
    /// </summary>
    private void UpdateWallDecay(GameTime gameTime, Tower? wallingChampion)
    {
        // Champion touching any wall suppresses all decay
        if (ChampionIsConnectedToWalls(wallingChampion))
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Point[] dirs = [new(0, -1), new(0, 1), new(-1, 0), new(1, 0)];

        foreach (var tower in _towers)
        {
            if (!tower.TowerType.IsWallSegment())
                continue;

            int exposedSides = 0;
            foreach (var dir in dirs)
            {
                var neighbor = new Point(
                    tower.GridPosition.X + dir.X,
                    tower.GridPosition.Y + dir.Y
                );

                if (
                    neighbor.X < 0
                    || neighbor.X >= _map.Columns
                    || neighbor.Y < 0
                    || neighbor.Y >= _map.Rows
                )
                {
                    exposedSides++;
                    continue;
                }

                var occupant = _map.Tiles[neighbor.X, neighbor.Y].OccupyingTower;
                bool sheltered = occupant != null && occupant.TowerType.IsWallSegment();

                if (!sheltered)
                    exposedSides++;
            }

            if (exposedSides > 0)
                tower.ApplyDecayDamage(dt * exposedSides);
        }
    }

    /// <summary>
    /// Returns true if the walling champion is alive and at least one of its 4 cardinal
    /// neighbors is occupied by a wall segment (champion is "touching" the wall group).
    /// </summary>
    private bool ChampionIsConnectedToWalls(Tower? wallingChampion)
    {
        if (wallingChampion == null)
            return false;

        Point[] dirs = [new(0, -1), new(0, 1), new(-1, 0), new(1, 0)];
        foreach (var dir in dirs)
        {
            var neighbor = new Point(
                wallingChampion.GridPosition.X + dir.X,
                wallingChampion.GridPosition.Y + dir.Y
            );

            if (
                neighbor.X < 0
                || neighbor.X >= _map.Columns
                || neighbor.Y < 0
                || neighbor.Y >= _map.Rows
            )
                continue;

            var occupant = _map.Tiles[neighbor.X, neighbor.Y].OccupyingTower;
            if (occupant != null && occupant.TowerType.IsWallSegment())
                return true;
        }

        return false;
    }

    public void Update(GameTime gameTime, List<IEnemy> enemies)
    {
        _championManager.Update(gameTime);

        // Assign wall-network targeting delegate each frame (network topology can change)
        var wallingChampion = _towers.Find(t => t.TowerType == TowerType.ChampionWalling);
        if (wallingChampion != null)
        {
            wallingChampion.WallNetworkTargetFinder = e =>
                FindWallNetworkTarget(e, wallingChampion);
            wallingChampion.OnWallAttack = pos => OnWallAttack?.Invoke(pos);
        }

        foreach (var tower in _towers)
        {
            tower.Update(gameTime, enemies);
        }

        // Decay disconnected wall segments before the dead-tower sweep so they can die this frame
        UpdateWallDecay(gameTime, wallingChampion);

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
    /// Draws range indicator for the hovered tower.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont? font = null, Tower? hoveredTower = null)
    {
        foreach (var tower in _towers)
        {
            tower.Draw(spriteBatch, font);
        }

        // Draw range indicator for hovered tower
        if (hoveredTower != null)
        {
            if (hoveredTower.TowerType == TowerType.ChampionWalling)
                DrawWallRangeIndicator(spriteBatch, hoveredTower);
            else
                hoveredTower.DrawRangeIndicator(spriteBatch);
        }
    }

    /// <summary>
    /// Highlights all tiles in the wall attack zone (1 tile outside the connected wall network).
    /// </summary>
    private void DrawWallRangeIndicator(SpriteBatch spriteBatch, Tower wallingChampion)
    {
        var wallSet = BuildConnectedWallSet(wallingChampion);
        var attackZone = BuildAttackZone(wallSet);
        int tileSize = GameSettings.TileSize;

        foreach (var tile in attackZone)
        {
            Vector2 center = Map.GridToWorld(tile);
            var rect = new Rectangle(
                (int)(center.X - tileSize / 2f),
                (int)(center.Y - tileSize / 2f),
                tileSize,
                tileSize
            );
            TextureManager.DrawRect(spriteBatch, rect, Color.LimeGreen * 0.3f);
        }
    }

    /// <summary>
    /// Finds the best enemy to attack in the wall-network attack zone.
    /// Prefers non-slowed enemies; falls back to the closest slowed enemy.
    /// </summary>
    private IEnemy? FindWallNetworkTarget(List<IEnemy> enemies, Tower wallingChampion)
    {
        var wallSet = BuildConnectedWallSet(wallingChampion);
        var attackZone = BuildAttackZone(wallSet);

        // Prefer non-slowed enemies so the slow debuff isn't wasted on already-slowed targets.
        // Fall back to closest slowed enemy if no unslowed targets are in range.
        IEnemy? bestUnslowed = null;
        float closestUnslow = float.MaxValue;
        IEnemy? bestSlowed = null;
        float closestSlow = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead || enemy.ReachedEnd)
                continue;

            Point enemyGrid = Map.WorldToGrid(enemy.Position);
            if (!attackZone.Contains(enemyGrid))
                continue;

            float dist = Vector2.Distance(wallingChampion.WorldPosition, enemy.Position);
            if (!enemy.IsSlowed && dist < closestUnslow)
            {
                closestUnslow = dist;
                bestUnslowed = enemy;
            }
            else if (enemy.IsSlowed && dist < closestSlow)
            {
                closestSlow = dist;
                bestSlowed = enemy;
            }
        }

        return bestUnslowed ?? bestSlowed;
    }

    /// <summary>
    /// Returns the set of in-bounds tiles that are 1 step outside the given wall set —
    /// i.e. all cardinal neighbours of wall tiles that are not themselves in the wall set.
    /// Used by both DrawWallRangeIndicator and FindWallNetworkTarget.
    /// </summary>
    private HashSet<Point> BuildAttackZone(HashSet<Point> wallSet)
    {
        Point[] dirs = [new(0, -1), new(0, 1), new(-1, 0), new(1, 0)];
        var attackZone = new HashSet<Point>();

        foreach (var wallPos in wallSet)
        {
            foreach (var dir in dirs)
            {
                var neighbor = new Point(wallPos.X + dir.X, wallPos.Y + dir.Y);

                if (
                    neighbor.X < 0
                    || neighbor.X >= _map.Columns
                    || neighbor.Y < 0
                    || neighbor.Y >= _map.Rows
                )
                    continue;

                if (!wallSet.Contains(neighbor))
                    attackZone.Add(neighbor);
            }
        }

        return attackZone;
    }
}
