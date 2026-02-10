using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StarterTD.Entities;
using StarterTD.Interfaces;

namespace StarterTD.Managers;

/// <summary>
/// Manages wave progression: spawning enemies in timed intervals.
/// Hardcoded for 10 waves with increasing difficulty.
/// </summary>
public class WaveManager
{
    /// <summary>Definition of a single wave.</summary>
    public record WaveDefinition(
        int EnemyCount,
        float EnemyHealth,
        float EnemySpeed,
        int EnemyBounty,
        float SpawnInterval
    );

    public int CurrentWave { get; private set; }
    public int TotalWaves => _waves.Count;
    public bool AllWavesComplete { get; private set; }
    public bool WaveInProgress { get; private set; }

    private readonly List<WaveDefinition> _waves;
    private readonly Func<List<Point>> _pathProvider;
    private float _spawnTimer;
    private int _enemiesSpawned;
    private WaveDefinition? _currentWaveDef;

    /// <summary>Callback invoked when an enemy is spawned.</summary>
    public Action<IEnemy>? OnEnemySpawned;

    /// <summary>
    /// Constructor takes a Func that returns the current path.
    /// This way each spawned enemy gets the latest ActivePath (including maze reroutes).
    ///
    /// TypeScript analogy: Like passing `() => map.activePath` instead of `map.activePath` directly.
    /// The function is called each time we need the path, so we always get the current one.
    /// </summary>
    public WaveManager(Func<List<Point>> pathProvider)
    {
        _pathProvider = pathProvider;
        CurrentWave = 0;

        // Define 10 waves with increasing difficulty
        _waves = new List<WaveDefinition>
        {
            new(
                EnemyCount: 5,
                EnemyHealth: 30,
                EnemySpeed: 90,
                EnemyBounty: 5,
                SpawnInterval: 1.0f
            ),
            new(
                EnemyCount: 8,
                EnemyHealth: 40,
                EnemySpeed: 95,
                EnemyBounty: 5,
                SpawnInterval: 0.9f
            ),
            new(
                EnemyCount: 10,
                EnemyHealth: 60,
                EnemySpeed: 100,
                EnemyBounty: 8,
                SpawnInterval: 0.8f
            ),
            new(
                EnemyCount: 12,
                EnemyHealth: 80,
                EnemySpeed: 110,
                EnemyBounty: 8,
                SpawnInterval: 0.8f
            ),
            new(
                EnemyCount: 15,
                EnemyHealth: 100,
                EnemySpeed: 120,
                EnemyBounty: 10,
                SpawnInterval: 0.7f
            ),
            new(
                EnemyCount: 18,
                EnemyHealth: 130,
                EnemySpeed: 130,
                EnemyBounty: 10,
                SpawnInterval: 0.7f
            ),
            new(
                EnemyCount: 20,
                EnemyHealth: 170,
                EnemySpeed: 135,
                EnemyBounty: 12,
                SpawnInterval: 0.6f
            ),
            new(
                EnemyCount: 22,
                EnemyHealth: 220,
                EnemySpeed: 140,
                EnemyBounty: 15,
                SpawnInterval: 0.6f
            ),
            new(
                EnemyCount: 25,
                EnemyHealth: 300,
                EnemySpeed: 145,
                EnemyBounty: 18,
                SpawnInterval: 0.5f
            ),
            new(
                EnemyCount: 30,
                EnemyHealth: 400,
                EnemySpeed: 145,
                EnemyBounty: 25,
                SpawnInterval: 0.4f
            ),
        };
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

        _currentWaveDef = _waves[CurrentWave];
        CurrentWave++;
        _enemiesSpawned = 0;
        _spawnTimer = 0;
        WaveInProgress = true;
        return true;
    }

    /// <summary>
    /// Update spawning logic. Call every frame during a wave.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (!WaveInProgress || _currentWaveDef == null)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _spawnTimer -= dt;

        if (_spawnTimer <= 0 && _enemiesSpawned < _currentWaveDef.EnemyCount)
        {
            // Spawn an enemy
            var enemy = new Enemy(
                $"Wave {CurrentWave} Enemy",
                _currentWaveDef.EnemyHealth,
                _currentWaveDef.EnemySpeed,
                _currentWaveDef.EnemyBounty,
                _pathProvider(),
                new Color(220, 50, 50)
            ); // Red-ish enemies

            OnEnemySpawned?.Invoke(enemy);
            _enemiesSpawned++;
            _spawnTimer = _currentWaveDef.SpawnInterval;
        }

        // Wave is done spawning when all enemies have been spawned
        if (_enemiesSpawned >= _currentWaveDef.EnemyCount)
        {
            WaveInProgress = false;
        }
    }
}
