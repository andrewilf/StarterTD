using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.Interfaces;

namespace StarterTD.Managers;

/// <summary>
/// Manages wave progression: spawning enemies according to timed SpawnEntry events.
///
/// Each wave is a list of SpawnEntry records sorted by the 'At' field (seconds from wave start).
/// On each Update(), elapsed time is accumulated and any entries whose 'At' time has passed
/// are dequeued and spawned immediately — allowing multiple enemies to spawn in the same frame
/// if a wave is unpaused or starts with simultaneous entries (At == 0).
/// </summary>
public class WaveManager
{
    public int CurrentWave { get; private set; }
    public int TotalWaves => _waves.Count;
    public bool AllWavesComplete { get; private set; }
    public bool WaveInProgress { get; private set; }

    // Key = spawn point name (matches Map.ActivePaths key), Value = path provider for that spawn
    private readonly Func<string, List<Point>?> _pathProvider;
    private readonly List<WaveData> _waves;

    // Spawns remaining in the current wave, ordered ascending by At time
    private List<SpawnEntry> _pendingSpawns = new();
    private float _waveElapsed;

    /// <summary>Callback invoked each time an enemy is spawned.</summary>
    public Action<IEnemy>? OnEnemySpawned;

    /// <param name="pathProvider">
    /// Given a spawn-point name (e.g. "spawn", "spawn_a"), returns the current path for that
    /// lane. Returns null if the name is unknown — enemy will not be spawned.
    /// </param>
    /// <param name="waves">Wave definitions loaded from JSON or a hardcoded fallback.</param>
    public WaveManager(Func<string, List<Point>?> pathProvider, List<WaveData> waves)
    {
        _pathProvider = pathProvider;
        _waves = waves;
        CurrentWave = 0;
    }

    /// <summary>
    /// Start the next wave. Returns false if all waves are complete.
    /// </summary>
    public bool StartNextWave()
    {
        if (CurrentWave >= TotalWaves)
        {
            AllWavesComplete = true;
            return false;
        }

        // Sort ascending by At so we can dequeue front-to-back in Update()
        _pendingSpawns = _waves[CurrentWave].Spawns.OrderBy(e => e.At).ToList();

        CurrentWave++;
        _waveElapsed = 0;
        WaveInProgress = true;
        return true;
    }

    /// <summary>
    /// Update spawning logic. Call every frame during a wave.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (!WaveInProgress)
            return;

        _waveElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Dequeue all entries whose scheduled time has arrived
        // Iterate from front; list is sorted ascending so we stop at first future entry
        while (_pendingSpawns.Count > 0 && _pendingSpawns[0].At <= _waveElapsed)
        {
            SpawnEnemy(_pendingSpawns[0]);
            _pendingSpawns.RemoveAt(0);
        }

        if (_pendingSpawns.Count == 0)
            WaveInProgress = false;
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
            entry.Bounty,
            path,
            entry.SpawnPoint,
            WaveLoader.ParseColor(entry.Color),
            entry.AttackDamage
        );

        OnEnemySpawned?.Invoke(enemy);
    }
}
