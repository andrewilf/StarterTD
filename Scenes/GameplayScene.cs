using System;
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
/// Manages the map, towers, enemies, timed spawn schedule, and UI.
/// </summary>
public partial class GameplayScene : IScene
{
    private readonly Game1 _game;
    private Map _map = null!;
    private ChampionManager _championManager = null!;
    private TowerManager _towerManager = null!;
    private SpawnScheduleManager _spawnScheduleManager = null!;
    private InputManager _inputManager = null!;
    private UIPanel _uiPanel = null!;
    private GameplayHudGumView? _gameplayHudView;

    /// <summary>
    /// Pixel offset to center the map on the fullscreen display.
    /// Applied as a SpriteBatch translation matrix for world-space rendering,
    /// so GridToWorld/WorldToGrid stay in map-local coordinates (no offset needed).
    /// </summary>
    private Vector2 _mapOffset;

    /// <summary>
    /// Cached translation matrix built from _mapOffset. Avoids per-frame allocation.
    /// </summary>
    private Matrix _worldMatrix;

    private readonly List<IEnemy> _enemies = new();
    private readonly List<AoEEffect> _aoeEffects = new();
    private readonly List<RailgunEffect> _railgunEffects = new();
    private readonly List<SpikeEffect> _spikeEffects = new();
    private readonly List<EntranceWarningLaneState> _entranceWarningLanes = new();
    private readonly Dictionary<string, EntranceWarningLaneState> _entranceWarningBySpawn = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<Point, string> _spawnNameByGrid = new();
    private string _defaultSpawnLaneName = string.Empty;
    private float _entranceWarningRuntimeSeconds;
    private float _entranceWarningPulseTime;
    private LaserEffect? _laserEffect;
    private bool _laserSelected;
    private readonly Dictionary<TowerType, float> _placementCooldowns = new()
    {
        [TowerType.ChampionGun] = 0f, // Shared champion pool — all champions key on ChampionGun
        [TowerType.Gun] = 0f,
        [TowerType.Cannon] = 0f,
        [TowerType.Walling] = 0f,
    };

    // Cached to avoid per-frame allocation when ticking cooldowns in Update()
    private static readonly TowerType[] CooldownPoolKeys =
    [
        TowerType.ChampionGun,
        TowerType.Gun,
        TowerType.Cannon,
        TowerType.Walling,
    ];
    private int _lives;
    private bool _gameOver;
    private const float TimeSlowScale = 0.5f;
    private const float TimeSlowMaxBank = 20f;
    private const float TimeSlowMinToActivate = 5f;
    private const float TimeSlowDrainRate = 1f; // seconds drained per real second active
    private const float TimeSlowRegenRate = 1f / 2f; // seconds regained per real second inactive
    private float _timeSlowBank = TimeSlowMaxBank;
    private bool _gameWon;
    private bool _allEnemiesCleared;
    private readonly string _selectedMapId;
    private Tower? _hoveredTower;
    private IEnemy? _selectedEnemy;
    private Point _mouseGrid;
    private List<Point>? _towerMovePreviewPath;
    private bool _isTowerMoveDragArmed;
    private Vector2 _towerMoveDragStartWorld;
    private bool _isTowerMoveDragActive;
    private Point _towerMoveDragStartGrid;
    private Point _towerMoveDragCurrentGrid;
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
    private bool _isLaserRedirectArmed;
    private bool _isLaserRedirectActive;
    private Vector2 _laserRedirectTargetWorld;
    private Vector2 _laserRedirectStartWorld;
    private int _layoutWidth;
    private int _layoutHeight;
    private const float TowerMoveDragStartThreshold = 6f;
    private const float EntranceWarningDistancePx = 1f * GameSettings.TileSize;
    private const float EntranceWarningMinSpeed = 1f;

    public GameplayScene(Game1 game, string mapId)
    {
        _game = game;
        _selectedMapId = mapId;
    }

    // Maps any champion type to ChampionGun (shared pool key); generics map to themselves.
    private static TowerType GetCooldownPoolKey(TowerType type)
    {
        if (type.IsChampion())
            return TowerType.ChampionGun;

        if (type == TowerType.WallSegment)
            return TowerType.Walling;

        return type;
    }

    public void LoadContent()
    {
        _map = new Map(MapDataRepository.GetMap(_selectedMapId));
        InitializeSpawnLaneLookup();

        _championManager = new ChampionManager();
        _towerManager = new TowerManager(_map, _championManager);
        List<SpawnEntry> spawnEntries =
            SpawnScheduleLoader.TryLoad(_selectedMapId)?.Spawns ?? FallbackSpawnSchedule();
        _spawnScheduleManager = new SpawnScheduleManager(
            spawnName => _map.ActivePaths.GetValueOrDefault(spawnName) ?? _map.ActivePath,
            spawnEntries
        );
        _inputManager = new InputManager();
        var (viewportWidth, viewportHeight) = GetViewportSize();
        _uiPanel = new UIPanel(viewportWidth, viewportHeight, _championManager);
        RebuildLayout(viewportWidth, viewportHeight);

        _lives = GameSettings.StartingLives;
        _gameOver = false;
        _gameWon = false;
        _allEnemiesCleared = _spawnScheduleManager.TotalSpawnCount == 0;
        ClearEntranceWarnings();

        // Wire up enemy spawning
        _spawnScheduleManager.OnEnemySpawned = enemy =>
        {
            _enemies.Add(enemy);
            TrackEntranceWarningEnemy(enemy);
        };

        // With movement costs, placement never blocks the path — always valid
        _towerManager.OnValidatePlacement = (gridPos) => true;

        // Recompute heat map and reroute all enemies after any tower placement or destruction
        _towerManager.OnTowerPlaced = (gridPos) => RecomputePathAndReroute();
        _towerManager.OnTowerDestroyed = (gridPos) => RecomputePathAndReroute();

        // Subscribe to AoE impact events to spawn visual effects
        _towerManager.OnAOEImpact = (pos, radius) => _aoeEffects.Add(new AoEEffect(pos, radius));

        // Subscribe to healing champion railgun shots for beam visuals
        _towerManager.OnRailgunShot = (start, end) =>
            _railgunEffects.Add(new RailgunEffect(start, end));

        // Subscribe to wall spike attacks to spawn visual effects
        _towerManager.OnWallAttack = pos => _spikeEffects.Add(new SpikeEffect(pos));

        // Subscribe to cannon champion laser activation to spawn the laser effect
        _towerManager.OnLaserActivated = pos =>
        {
            _laserEffect = new LaserEffect(pos);
            _laserSelected = false;
        };

        // Cancel the laser immediately if the champion tower moves or is destroyed
        _towerManager.OnLaserCancelled = () =>
        {
            _laserEffect?.Cancel();
            _laserEffect = null;
            _laserSelected = false;
        };

        // Champion super ability: start cooldown and apply buff to relevant towers
        _uiPanel.OnAbilityTriggered = championType =>
        {
            _championManager.StartAbilityCooldown(championType);
            _towerManager.TriggerChampionAbility(championType, _enemies);
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

        InitializeGameplayHudGum();
    }

    public void UnloadContent()
    {
        if (_uiPanel != null)
            _uiPanel.OnAbilityTriggered = null;

        _gameplayHudView?.Dispose();
        _gameplayHudView = null;

        if (_towerManager != null)
        {
            _towerManager.OnValidatePlacement = null;
            _towerManager.OnTowerPlaced = null;
            _towerManager.OnTowerDestroyed = null;
            _towerManager.OnAOEImpact = null;
            _towerManager.OnRailgunShot = null;
            _towerManager.OnWallAttack = null;
            _towerManager.OnLaserActivated = null;
            _towerManager.OnLaserCancelled = null;
        }
    }

    public void Update(GameTime gameTime)
    {
        _inputManager.Update();
        HandleViewportResize();
        HandleInput();
        UpdateGameplayHudGum();

        if (_gameOver || _gameWon)
            return;

        // Update bank against real (unscaled) time before activeTime is computed.
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _uiPanel.CanActivateTimeSlow = _timeSlowBank >= TimeSlowMinToActivate;
        if (_uiPanel.IsTimeSlowed)
        {
            _timeSlowBank -= TimeSlowDrainRate * dt;
            if (_timeSlowBank <= 0f)
            {
                _timeSlowBank = 0f;
                _uiPanel.ForceDeactivateTimeSlow();
            }
        }
        else
        {
            _timeSlowBank = Math.Min(_timeSlowBank + TimeSlowRegenRate * dt, TimeSlowMaxBank);
        }

        // Scale elapsed time uniformly when time-slow is active. All downstream systems
        // (CountdownTimers, float dt, timed spawning) propagate this automatically through
        // their GameTime parameter, requiring no changes inside Enemy, Tower, or SpawnScheduleManager.
        GameTime activeTime = _uiPanel.IsTimeSlowed
            ? new GameTime(
                gameTime.TotalGameTime,
                TimeSpan.FromSeconds(gameTime.ElapsedGameTime.TotalSeconds * TimeSlowScale)
            )
            : gameTime;

        // Tick placement cooldown pools using scaled time — time-slow extends the wait.
        float scaledDt = (float)activeTime.ElapsedGameTime.TotalSeconds;
        foreach (var key in CooldownPoolKeys)
            _placementCooldowns[key] = Math.Max(0f, _placementCooldowns[key] - scaledDt);

        // --- Update timed enemy spawning ---
        _spawnScheduleManager.Update(activeTime);

        // --- Update enemies ---
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            _enemies[i].Update(activeTime, _map);

            if (_enemies[i].IsDead)
            {
                if (_selectedEnemy == _enemies[i])
                    _selectedEnemy = null;

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

        // Check if the schedule has completed and the field is empty.
        _allEnemiesCleared = _spawnScheduleManager.IsScheduleComplete && _enemies.Count == 0;
        UpdateEntranceWarnings(activeTime);

        // Check win condition
        if (_allEnemiesCleared)
        {
            _gameWon = true;
        }

        // --- Update towers ---
        _towerManager.Update(activeTime, _enemies);

        // --- Detect hovered tower for range indicator ---
        _mouseGrid = Map.WorldToGrid(ScreenToWorld(_inputManager.MousePositionVector));
        _hoveredTower = _towerManager.GetTowerAt(_mouseGrid);

        // --- Compute tower movement path preview only while tower movement drag is active.
        // This keeps selection from showing an eager path; path appears only after drag starts.
        if (_isTowerMoveDragActive)
            _towerMovePreviewPath = _towerManager.GetPreviewPath(_towerMoveDragCurrentGrid);
        else
            _towerMovePreviewPath = null;

        // --- Update laser effect ---
        if (_laserEffect != null)
        {
            _laserEffect.Update(activeTime, _enemies);
            if (!_laserEffect.IsActive)
            {
                _laserEffect = null;
                _laserSelected = false;
            }
        }

        // --- Update AoE effects ---
        for (int i = _aoeEffects.Count - 1; i >= 0; i--)
        {
            _aoeEffects[i].Update(activeTime);
            if (!_aoeEffects[i].IsActive)
                _aoeEffects.RemoveAt(i);
        }

        // --- Update spike effects ---
        for (int i = _spikeEffects.Count - 1; i >= 0; i--)
        {
            _spikeEffects[i].Update(activeTime);
            if (!_spikeEffects[i].IsActive)
                _spikeEffects.RemoveAt(i);
        }

        // --- Update railgun effects ---
        for (int i = _railgunEffects.Count - 1; i >= 0; i--)
        {
            _railgunEffects[i].Update(activeTime);
            if (!_railgunEffects[i].IsActive)
                _railgunEffects.RemoveAt(i);
        }
    }

    private void HandleViewportResize()
    {
        var (viewportWidth, viewportHeight) = GetViewportSize();
        if (viewportWidth == _layoutWidth && viewportHeight == _layoutHeight)
            return;

        RebuildLayout(viewportWidth, viewportHeight);
    }

    private (int width, int height) GetViewportSize() =>
        (
            Math.Max(1, _game.GraphicsDevice.Viewport.Width),
            Math.Max(1, _game.GraphicsDevice.Viewport.Height)
        );

    private void RebuildLayout(int viewportWidth, int viewportHeight)
    {
        _layoutWidth = viewportWidth;
        _layoutHeight = viewportHeight;
        GameSettings.SetScreenSize(viewportWidth, viewportHeight);
        _uiPanel.Resize(viewportWidth, viewportHeight);
        UpdateWorldLayout(viewportWidth, viewportHeight);
        _gameplayHudView?.ResizeToViewport(viewportWidth, viewportHeight);
    }

    private void UpdateWorldLayout(int viewportWidth, int viewportHeight)
    {
        int mapPixelWidth = _map.Columns * GameSettings.TileSize;
        int mapPixelHeight = _map.Rows * GameSettings.TileSize;
        float offsetX = (viewportWidth - GameSettings.UIPanelWidth - mapPixelWidth) / 2f;
        float offsetY = (viewportHeight - mapPixelHeight) / 2f;
        _mapOffset = new Vector2(MathF.Floor(offsetX), MathF.Floor(offsetY));
        _worldMatrix = Matrix.CreateTranslation(_mapOffset.X, _mapOffset.Y, 0f);
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
    /// Convert a screen-space position (e.g. mouse) to map-local world coordinates
    /// by subtracting the map centering offset.
    /// </summary>
    private Vector2 ScreenToWorld(Vector2 screenPos) => screenPos - _mapOffset;

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
            path,
            spawnName: "",
            Color.Purple,
            attackDamage: 5
        );

        _enemies.Add(enemy);
        TrackEntranceWarningEnemy(enemy);
    }

    /// <summary>
    /// Hardcoded spawn schedule used when no Content/SpawnSchedules/{mapId}.json exists.
    /// Mirrors the legacy 5-burst progression on a single match timeline.
    /// </summary>
    private static List<SpawnEntry> FallbackSpawnSchedule()
    {
        const float firstBurstStartSeconds = 10f;
        const float interBurstGapSeconds = 10f;

        static List<SpawnEntry> MakeBurst(
            int count,
            float health,
            float speed,
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
                        AttackDamage: damage,
                        Color: "Purple"
                    )
                );
            }
            return entries;
        }

        static void AppendBurst(
            List<SpawnEntry> schedule,
            List<SpawnEntry> burst,
            ref float nextBurstStartSeconds
        )
        {
            float burstStartSeconds = nextBurstStartSeconds;
            float burstFirstAt = burst.Count > 0 ? burst[0].At : 0f;

            for (int i = 0; i < burst.Count; i++)
            {
                SpawnEntry spawn = burst[i];
                schedule.Add(spawn with { At = burstStartSeconds + (spawn.At - burstFirstAt) });
            }

            if (burst.Count == 0)
                return;

            float burstLastAt = schedule[^1].At;
            nextBurstStartSeconds = burstLastAt + interBurstGapSeconds;
        }

        var schedule = new List<SpawnEntry>();
        float nextBurstStartSeconds = firstBurstStartSeconds;

        AppendBurst(schedule, MakeBurst(5, 300, 90, 1.0f, 5), ref nextBurstStartSeconds);
        AppendBurst(schedule, MakeBurst(8, 400, 95, 0.9f, 5), ref nextBurstStartSeconds);
        AppendBurst(schedule, MakeBurst(10, 600, 100, 0.8f, 8), ref nextBurstStartSeconds);
        AppendBurst(schedule, MakeBurst(12, 800, 110, 0.8f, 8), ref nextBurstStartSeconds);
        AppendBurst(schedule, MakeBurst(15, 1000, 120, 0.7f, 12), ref nextBurstStartSeconds);

        return schedule;
    }
}
