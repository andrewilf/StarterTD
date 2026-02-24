using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.Interfaces;
using StarterTD.Managers;
using StarterTD.UI;

namespace StarterTD.Scenes;

/// <summary>
/// The main gameplay scene. This is where the tower defense game loop runs.
/// Manages the map, towers, enemies, waves, and UI.
/// </summary>
public partial class GameplayScene : IScene
{
    private readonly Game1 _game;
    private Map _map = null!;
    private ChampionManager _championManager = null!;
    private TowerManager _towerManager = null!;
    private WaveManager _waveManager = null!;
    private InputManager _inputManager = null!;
    private UIPanel _uiPanel = null!;

    private readonly List<IEnemy> _enemies = new();
    private readonly List<FloatingText> _floatingTexts = new();
    private readonly List<AoEEffect> _aoeEffects = new();
    private readonly List<SpikeEffect> _spikeEffects = new();
    private int _money;
    private int _lives;
    private bool _gameOver;
    private bool _gameWon;
    private bool _allEnemiesCleared;
    private readonly string _selectedMapId;
    private Tower? _hoveredTower;
    private IEnemy? _selectedEnemy;
    private Point _mouseGrid;
    private float _selectedTowerRange;
    private List<Point>? _towerMovePreviewPath;
    private bool _isWallDragActive;
    private Point _wallDragStartGrid;
    private Point _wallDragCurrentGrid;
    private List<Point>? _wallDragPreviewPath;
    private int _wallDragValidPrefixLength;

    // null = not yet locked (still on a straight line from start).
    // true = horizontal-first locked, false = vertical-first locked.
    private bool? _wallDragLockedHorizontalFirst;

    /// <summary>
    /// True when the player has activated wall-placement mode by clicking the world-space "+"
    /// button on a selected walling tower. Press-drag-release on the grid places wall segments.
    /// </summary>
    private bool _wallPlacementMode;

    public GameplayScene(Game1 game, string mapId)
    {
        _game = game;
        _selectedMapId = mapId;
    }

    public void LoadContent()
    {
        _map = new Map(MapDataRepository.GetMap(_selectedMapId));
        _championManager = new ChampionManager();
        _towerManager = new TowerManager(_map, _championManager);
        _waveManager = new WaveManager(
            spawnName => _map.ActivePaths.GetValueOrDefault(spawnName) ?? _map.ActivePath,
            WaveLoader.TryLoad(_selectedMapId) ?? FallbackWaves()
        );
        _inputManager = new InputManager();
        _uiPanel = new UIPanel(
            GameSettings.ScreenWidth,
            GameSettings.ScreenHeight,
            _championManager
        );

        _money = GameSettings.StartingMoney;
        _lives = GameSettings.StartingLives;
        _gameOver = false;
        _gameWon = false;
        _allEnemiesCleared = true;

        // Wire up enemy spawning
        _waveManager.OnEnemySpawned = enemy => _enemies.Add(enemy);

        // With movement costs, placement never blocks the path — always valid
        _towerManager.OnValidatePlacement = (gridPos) => true;

        // Recompute heat map and reroute all enemies after any tower placement or destruction
        _towerManager.OnTowerPlaced = (gridPos) => RecomputePathAndReroute();
        _towerManager.OnTowerDestroyed = (gridPos) => RecomputePathAndReroute();

        // Subscribe to AoE impact events to spawn visual effects
        _towerManager.OnAOEImpact = (pos, radius) => _aoeEffects.Add(new AoEEffect(pos, radius));

        // Subscribe to wall spike attacks to spawn visual effects
        _towerManager.OnWallAttack = pos => _spikeEffects.Add(new SpikeEffect(pos));

        // Champion super ability: start cooldown and apply buff to relevant towers
        _uiPanel.OnAbilityTriggered = championType =>
        {
            _championManager.StartAbilityCooldown(championType);
            _towerManager.TriggerChampionAbility(championType);
        };

        // Try to load font if available
        try
        {
            var font = _game.Content.Load<SpriteFont>("DefaultFont");
            _uiPanel.SetFont(font);
        }
        catch
        {
            // Font not available — UI will use fallback rendering
        }
    }

    public void Update(GameTime gameTime)
    {
        _inputManager.Update();
        HandleInput();

        if (_gameOver || _gameWon)
            return;

        // --- Update wave spawning ---
        _waveManager.Update(gameTime);

        // --- Update enemies ---
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            _enemies[i].Update(gameTime, _map);

            if (_enemies[i].IsDead)
            {
                if (_selectedEnemy == _enemies[i])
                    _selectedEnemy = null;

                int bounty = _enemies[i].Bounty;
                _money += bounty;
                SpawnFloatingText(_enemies[i].Position, $"+${bounty}", Color.Gold);
                _enemies[i].OnDestroy(); // Release tower engagement before removal
                _enemies.RemoveAt(i);
            }
            else if (_enemies[i].ReachedEnd)
            {
                if (_selectedEnemy == _enemies[i])
                    _selectedEnemy = null;

                _lives--;
                _enemies[i].OnDestroy(); // Release tower engagement before removal
                _enemies.RemoveAt(i);

                if (_lives <= 0)
                {
                    _gameOver = true;
                    return;
                }
            }
        }

        // Check if all enemies are cleared (wave done spawning + no enemies alive)
        _allEnemiesCleared = !_waveManager.WaveInProgress && _enemies.Count == 0;

        // Check win condition
        if (_waveManager.CurrentWave >= _waveManager.TotalWaves && _allEnemiesCleared)
        {
            _gameWon = true;
        }

        // --- Update towers ---
        _towerManager.Update(gameTime, _enemies);

        // --- Detect hovered tower for range indicator ---
        _mouseGrid = Map.WorldToGrid(_inputManager.MousePositionVector);
        _hoveredTower = _towerManager.GetTowerAt(_mouseGrid);

        // --- Compute tower movement path preview when a walkable tower is selected ---
        // Suppressed in wall placement mode since the champion can't move while placing walls.
        _towerMovePreviewPath = _wallPlacementMode
            ? null
            : _towerManager.GetPreviewPath(_mouseGrid);

        // --- Update AoE effects ---
        for (int i = _aoeEffects.Count - 1; i >= 0; i--)
        {
            _aoeEffects[i].Update(gameTime);
            if (!_aoeEffects[i].IsActive)
                _aoeEffects.RemoveAt(i);
        }

        // --- Update spike effects ---
        for (int i = _spikeEffects.Count - 1; i >= 0; i--)
        {
            _spikeEffects[i].Update(gameTime);
            if (!_spikeEffects[i].IsActive)
                _spikeEffects.RemoveAt(i);
        }

        // --- Update floating texts ---
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            _floatingTexts[i].Update(gameTime);
            if (!_floatingTexts[i].IsActive)
                _floatingTexts.RemoveAt(i);
        }
    }

    /// <summary>
    /// Recomputes the Dijkstra heat map and reroutes all live enemies to the new path.
    /// Called after tower placement or destruction changes the grid costs.
    /// </summary>
    private void RecomputePathAndReroute()
    {
        _map.RecomputeActivePath();

        foreach (var enemy in _enemies)
        {
            if (enemy is Enemy concreteEnemy)
                concreteEnemy.UpdatePath(_map);
        }
    }

    /// <summary>
    /// Get the enemy at a specific world position, or null.
    /// Uses a click radius to make clicking on enemies easier.
    /// </summary>
    private IEnemy? GetEnemyAt(Vector2 worldPos)
    {
        const float clickRadius = 15f;

        foreach (var enemy in _enemies)
        {
            float distance = Vector2.Distance(enemy.Position, worldPos);
            if (distance <= clickRadius)
                return enemy;
        }
        return null;
    }

    private void SpawnFloatingText(Vector2 worldPos, string text, Color color)
    {
        _floatingTexts.Add(new FloatingText(worldPos, text, color));
    }

    /// <summary>
    /// Spawns a debug enemy at the first available spawn point using fixed stats.
    /// </summary>
    private void SpawnDebugEnemy()
    {
        var path = _map.ActivePath;
        if (path.Count == 0)
            return;

        var enemy = new Enemy(
            "Debug Enemy",
            health: 300,
            speed: 90,
            bounty: 5,
            path,
            spawnName: "",
            Color.Purple,
            attackDamage: 5
        );

        _enemies.Add(enemy);
    }

    /// <summary>
    /// Hardcoded wave definitions used when no Content/Waves/{mapId}.json exists.
    /// Mirrors the original 5-wave progression so existing maps work without a JSON file.
    /// </summary>
    private static List<WaveData> FallbackWaves()
    {
        static List<SpawnEntry> MakeWave(
            int count,
            float health,
            float speed,
            int bounty,
            float interval,
            int damage
        )
        {
            var entries = new List<SpawnEntry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(
                    new SpawnEntry(
                        At: i * interval,
                        SpawnPoint: "spawn",
                        Name: "Enemy",
                        Health: health,
                        Speed: speed,
                        Bounty: bounty,
                        AttackDamage: damage,
                        Color: "Purple"
                    )
                );
            }
            return entries;
        }

        return new List<WaveData>
        {
            new(1, MakeWave(5, 300, 90, 5, 1.0f, 5)),
            new(2, MakeWave(8, 400, 95, 5, 0.9f, 5)),
            new(3, MakeWave(10, 600, 100, 8, 0.8f, 8)),
            new(4, MakeWave(12, 800, 110, 8, 0.8f, 8)),
            new(5, MakeWave(15, 1000, 120, 10, 0.7f, 12)),
        };
    }
}
