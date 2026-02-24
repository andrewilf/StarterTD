using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.Interfaces;

namespace StarterTD.Managers;

/// <summary>
/// Wall-network placement, targeting, decay, and rendering for the ChampionWalling tower.
/// Partial class split from TowerManager.cs to keep wall logic isolated.
/// </summary>
public partial class TowerManager
{
    private sealed class WallGrowthChain(Tower anchor, List<Point> pendingPath)
    {
        public Tower Anchor { get; } = anchor;
        public List<Point> PendingPath { get; } = pendingPath;
        public int NextIndex { get; set; }
        public Tower? ActiveSegment { get; set; }
        public bool Cancelled { get; set; }
    }

    private readonly List<WallGrowthChain> _wallGrowthChains = [];

    private const int WallGrowthStartMaxHealth = 1;
    private const float WallGrowthPerSecond = 20f;

    /// <summary>
    /// Attempts to place wall segments in order and stops at the first invalid tile.
    /// Returns the number of segments successfully placed.
    /// </summary>
    public int TryPlaceWallPath(IReadOnlyList<Point> orderedPath, Tower wallingTower)
    {
        var connected = GetConnectedSetForTower(wallingTower);
        List<Point> pendingPath = [];

        foreach (var point in orderedPath)
        {
            if (!_map.CanBuild(point))
                break;

            if (!IsAdjacentToConnectedSet(point, connected))
                break;

            connected.Add(point);
            pendingPath.Add(point);
        }

        if (pendingPath.Count == 0)
            return 0;

        ReservePendingWallTiles(pendingPath, wallingTower);

        var chain = new WallGrowthChain(wallingTower, pendingPath);
        _wallGrowthChains.Add(chain);
        SpawnNextSegmentIfPossible(chain);

        _wallConnectedSets[wallingTower] = connected;
        return pendingPath.Count;
    }

    /// <summary>
    /// Returns how many tiles from the start of orderedPath are valid wall placements,
    /// simulating sequential placement without mutating map state.
    /// </summary>
    public int GetWallPathValidPrefixLength(IReadOnlyList<Point> orderedPath, Tower wallingTower)
    {
        var connected = GetConnectedSetForTower(wallingTower);
        int validCount = 0;

        foreach (var point in orderedPath)
        {
            if (!_map.CanBuild(point))
                break;

            if (!IsAdjacentToConnectedSet(point, connected))
                break;

            connected.Add(point);
            validCount++;
        }

        return validCount;
    }

    private HashSet<Point> GetConnectedSetForTower(Tower wallingTower)
    {
        return _wallConnectedSets.TryGetValue(wallingTower, out var cached)
            ? new HashSet<Point>(cached)
            : BuildConnectedWallSet([wallingTower.GridPosition]);
    }

    private static bool IsAdjacentToConnectedSet(Point point, HashSet<Point> connected)
    {
        Point[] dirs = [new(0, -1), new(0, 1), new(-1, 0), new(1, 0)];
        foreach (var dir in dirs)
        {
            var neighbor = new Point(point.X + dir.X, point.Y + dir.Y);
            if (connected.Contains(neighbor))
                return true;
        }
        return false;
    }

    private Tower PlaceWallSegment(Point gridPos)
    {
        var wall = new Tower(TowerType.WallSegment, gridPos);
        wall.InitializeWallGrowth(
            startMaxHealth: WallGrowthStartMaxHealth,
            targetMaxHealth: wall.MaxHealth,
            growthPerSecond: WallGrowthPerSecond,
            syncCurrentWhileUndamaged: true
        );
        _towers.Add(wall);
        _map.Tiles[gridPos.X, gridPos.Y].OccupyingTower = wall;
        OnTowerPlaced?.Invoke(gridPos);
        return wall;
    }

    private void ReservePendingWallTiles(List<Point> pendingPath, Tower anchor)
    {
        foreach (var point in pendingPath)
            _map.Tiles[point.X, point.Y].ReservedForPendingWallBy = anchor;
    }

    internal void UpdateWallGrowthChains()
    {
        for (int i = _wallGrowthChains.Count - 1; i >= 0; i--)
        {
            var chain = _wallGrowthChains[i];
            if (chain.Cancelled)
            {
                _wallGrowthChains.RemoveAt(i);
                continue;
            }

            bool anchorMissing = !_towers.Contains(chain.Anchor) || chain.Anchor.IsDead;
            if (anchorMissing)
            {
                CancelChain(chain, stopActiveGrowth: true);
                _wallGrowthChains.RemoveAt(i);
                continue;
            }

            var active = chain.ActiveSegment;
            if (active != null)
            {
                bool activeNoLongerManaged = !_towers.Contains(active);
                if (activeNoLongerManaged || active.IsDead)
                {
                    if (!active.IsWallGrowthComplete)
                    {
                        // If the current growing segment is destroyed before completion,
                        // cancel remaining pending reservations in this path.
                        CancelChain(chain, stopActiveGrowth: false);
                        _wallGrowthChains.RemoveAt(i);
                        continue;
                    }

                    chain.ActiveSegment = null;
                }
                else if (active.IsWallGrowthComplete)
                {
                    chain.ActiveSegment = null;
                }
            }

            if (chain.ActiveSegment == null)
            {
                bool spawned = SpawnNextSegmentIfPossible(chain);
                if (chain.Cancelled)
                {
                    _wallGrowthChains.RemoveAt(i);
                    continue;
                }

                if (!spawned && chain.NextIndex >= chain.PendingPath.Count)
                    _wallGrowthChains.RemoveAt(i);
            }
        }
    }

    private bool SpawnNextSegmentIfPossible(WallGrowthChain chain)
    {
        if (chain.Cancelled || chain.NextIndex >= chain.PendingPath.Count)
            return false;

        var point = chain.PendingPath[chain.NextIndex];
        var tile = _map.Tiles[point.X, point.Y];

        if (tile.ReservedForPendingWallBy != chain.Anchor || tile.OccupyingTower != null)
        {
            CancelChain(chain, stopActiveGrowth: true);
            return false;
        }

        tile.ReservedForPendingWallBy = null;

        var wall = PlaceWallSegment(point);
        chain.ActiveSegment = wall;
        chain.NextIndex++;
        wall.StartWallGrowth();
        return true;
    }

    private void ClearPendingWallReservations(WallGrowthChain chain)
    {
        for (int i = chain.NextIndex; i < chain.PendingPath.Count; i++)
        {
            var point = chain.PendingPath[i];
            var tile = _map.Tiles[point.X, point.Y];
            if (tile.ReservedForPendingWallBy == chain.Anchor)
                tile.ReservedForPendingWallBy = null;
        }
    }

    private void CancelChain(WallGrowthChain chain, bool stopActiveGrowth)
    {
        if (chain.Cancelled)
            return;

        if (stopActiveGrowth)
            StopGrowthForAnchor(chain.Anchor);

        ClearPendingWallReservations(chain);
        chain.Cancelled = true;
    }

    private void StopGrowthForAnchor(Tower anchor)
    {
        foreach (var chain in _wallGrowthChains)
        {
            if (chain.Cancelled || chain.Anchor != anchor)
                continue;

            if (chain.ActiveSegment == null || !_towers.Contains(chain.ActiveSegment))
                continue;

            chain.ActiveSegment.StopWallGrowth();
        }
    }

    internal void DrawPendingWallReservations(SpriteBatch spriteBatch)
    {
        int tileSize = GameSettings.TileSize;

        foreach (var chain in _wallGrowthChains)
        {
            if (chain.Cancelled)
                continue;

            for (int i = chain.NextIndex; i < chain.PendingPath.Count; i++)
            {
                var point = chain.PendingPath[i];
                if (point.X < 0 || point.X >= _map.Columns || point.Y < 0 || point.Y >= _map.Rows)
                    continue;

                var tile = _map.Tiles[point.X, point.Y];
                if (tile.ReservedForPendingWallBy != chain.Anchor)
                    continue;

                var rect = new Rectangle(
                    point.X * tileSize,
                    point.Y * tileSize,
                    tileSize,
                    tileSize
                );
                TextureManager.DrawRect(spriteBatch, rect, Color.DarkGreen * 0.25f);
                TextureManager.DrawRectOutline(spriteBatch, rect, Color.LimeGreen * 0.5f, 1);
            }
        }
    }

    /// <summary>
    /// Returns true if gridPos is adjacent (4-directional) to a tile occupied by
    /// the walling champion or any wall segment that is chain-connected back to the champion.
    /// Uses BFS from the champion's position across wall segment tiles.
    /// Public so GameplayScene can use it for hover feedback during wall placement mode.
    /// </summary>
    public bool IsAdjacentToWallingNetwork(Point gridPos, Tower wallingTower)
    {
        // Use the per-frame cache when available; fall back to a fresh BFS only if called
        // before the first Update (e.g. during initial placement validation).
        var connected = _wallConnectedSets.TryGetValue(wallingTower, out var cached)
            ? cached
            : BuildConnectedWallSet([wallingTower.GridPosition]);

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
    /// BFS from the given anchor positions across adjacent wall segment tiles.
    /// Returns all grid positions reachable from any anchor (anchors themselves included).
    /// </summary>
    private HashSet<Point> BuildConnectedWallSet(IEnumerable<Point> anchors)
    {
        var connected = anchors.ToHashSet();
        var queue = new Queue<Point>(connected);
        Point[] dirs = [new(0, -1), new(0, 1), new(-1, 0), new(1, 0)];

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
    /// Apply decay damage to wall segments not reachable from any walling tower.
    /// Segments reachable from any anchor are protected; truly orphaned segments
    /// decay at 1 HP/sec per exposed cardinal side (max 4 HP/sec).
    /// </summary>
    internal void UpdateWallDecay(GameTime gameTime)
    {
        // A wall segment is protected if it appears in any walling tower's connected set.
        var connectedSet = _wallConnectedSets.Values.Aggregate(
            new HashSet<Point>(),
            (acc, set) =>
            {
                acc.UnionWith(set);
                return acc;
            }
        );

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Point[] dirs = [new(0, -1), new(0, 1), new(-1, 0), new(1, 0)];

        foreach (var tower in _towers)
        {
            if (!tower.TowerType.IsWallSegment())
                continue;

            if (connectedSet.Contains(tower.GridPosition))
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

    private void DrawWallRangeIndicatorForSet(SpriteBatch spriteBatch, HashSet<Point> wallSet)
    {
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
            TextureManager.DrawRect(spriteBatch, rect, Color.White * 0.3f);
        }
    }

    /// <summary>
    /// Finds the best enemy to attack in the wall-network attack zone for a specific tower.
    /// Prefers non-slowed enemies; falls back to the closest slowed enemy.
    /// </summary>
    internal IEnemy? FindWallNetworkTarget(
        List<IEnemy> enemies,
        Vector2 towerWorldPos,
        HashSet<Point> wallSet
    )
    {
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

            float dist = Vector2.Distance(towerWorldPos, enemy.Position);
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
    /// When the walling champion's frenzy is active, hits every enemy in the attack zone
    /// at the champion's normal fire rate. Each hit deals spike damage and applies slow,
    /// same as the regular single-target attack — but targeting all enemies simultaneously.
    /// </summary>
    internal void UpdateWallFrenzy(
        GameTime gameTime,
        List<IEnemy> enemies,
        Tower tower,
        HashSet<Point> wallSet
    )
    {
        _frenzyFireTimers.TryGetValue(tower, out float timer);
        timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (timer < tower.FireRate)
        {
            _frenzyFireTimers[tower] = timer;
            return;
        }

        _frenzyFireTimers[tower] = 0f;

        var attackZone = BuildAttackZone(wallSet);
        float slowDuration = TowerData.GetStats(tower.TowerType).AbilityDuration;

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead || enemy.ReachedEnd)
                continue;

            if (!attackZone.Contains(Map.WorldToGrid(enemy.Position)))
                continue;

            enemy.TakeDamage((int)tower.Damage);
            enemy.ApplySlow(slowDuration);
            OnWallAttack?.Invoke(enemy.Position);
        }
    }

    /// <summary>
    /// Returns the set of in-bounds tiles that are 1 step outside the given wall set —
    /// i.e. all 8-directional neighbours of wall tiles that are not themselves in the wall set.
    /// Used by DrawWallRangeIndicatorForSet, FindWallNetworkTarget, and UpdateWallFrenzy.
    /// </summary>
    private HashSet<Point> BuildAttackZone(HashSet<Point> wallSet)
    {
        Point[] dirs =
        [
            new(0, -1),
            new(0, 1),
            new(-1, 0),
            new(1, 0),
            new(-1, -1),
            new(1, -1),
            new(-1, 1),
            new(1, 1),
        ];
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
