using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.Interfaces;

namespace StarterTD.Managers;

/// <summary>
/// Manages match-long timed spawning from a single absolute spawn timeline.
/// </summary>
public class SpawnScheduleManager
{
    public float ElapsedSeconds { get; private set; }
    public int TotalSpawnCount => _totalSpawnCount;
    public int SpawnedCount { get; private set; }
    public int PendingSpawnCount => _scheduledSpawns.Count - _nextSpawnIndex;
    public bool IsScheduleComplete => _nextSpawnIndex >= _scheduledSpawns.Count;

    // Key = spawn point name (matches Map.ActivePaths key), Value = path provider for that spawn
    private readonly Func<string, List<Point>?> _pathProvider;
    private readonly List<SpawnEntry> _scheduledSpawns;
    private readonly int _totalSpawnCount;
    private int _nextSpawnIndex;

    /// <summary>Callback invoked each time an enemy is spawned.</summary>
    public Action<IEnemy>? OnEnemySpawned;

    /// <param name="pathProvider">
    /// Given a spawn-point name (e.g. "spawn", "spawn_a"), returns the current path for that
    /// lane. Returns null if the name is unknown — enemy will not be spawned.
    /// </param>
    /// <param name="spawns">Absolute match-timeline spawn definitions.</param>
    public SpawnScheduleManager(
        Func<string, List<Point>?> pathProvider,
        IEnumerable<SpawnEntry> spawns
    )
    {
        _pathProvider = pathProvider;
        _scheduledSpawns = spawns.OrderBy(e => e.At).ToList();
        _totalSpawnCount = _scheduledSpawns.Count;
    }

    /// <summary>
    /// Update spawning logic. Call every frame for the lifetime of the match.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (IsScheduleComplete)
            return;

        ElapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;

        while (
            _nextSpawnIndex < _scheduledSpawns.Count
            && _scheduledSpawns[_nextSpawnIndex].At <= ElapsedSeconds
        )
        {
            SpawnEnemy(_scheduledSpawns[_nextSpawnIndex]);
            _nextSpawnIndex++;
        }
    }

    /// <summary>
    /// Gets a pending spawn entry by index without allocating or copying the queue.
    /// </summary>
    public bool TryGetPendingSpawn(int index, out SpawnEntry spawn)
    {
        int scheduledIndex = _nextSpawnIndex + index;
        if (index < 0 || scheduledIndex >= _scheduledSpawns.Count)
        {
            spawn = default!;
            return false;
        }

        spawn = _scheduledSpawns[scheduledIndex];
        return true;
    }

    private void SpawnEnemy(SpawnEntry entry)
    {
        var path = _pathProvider(entry.SpawnPoint);
        if (path == null || path.Count == 0)
            return;

        var enemy = new Enemy(
            entry.Name,
            entry.Health,
            entry.Speed,
            path,
            entry.SpawnPoint,
            SpawnScheduleLoader.ParseColor(entry.Color),
            entry.AttackDamage
        );

        SpawnedCount++;
        OnEnemySpawned?.Invoke(enemy);
    }
}
