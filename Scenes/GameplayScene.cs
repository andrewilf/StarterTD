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
    private readonly List<SpikeEffect> _spikeEffects = new();
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
    private const float TowerMoveDragStartThreshold = 6f;
    private const float CrowdingMaxOffset = 10f;
    private const int CrowdingOverflowStartOccupancy = 5;
    private const float CrowdingOverflowExtraOffsetPerEnemy = 1.2f;
    private const float CrowdingOverflowMaxExtraOffset = 7f;
    private const float CrowdingTileSlotBaseRadius = 4f;
    private const float CrowdingTileSlotRadiusPerEnemy = 1.1f;
    private const float CrowdingTileSlotRingRadiusStep = 2.6f;
    private const float CrowdingTileSlotMaxRadius = 11f;
    private const float CrowdingOffsetSmoothTime = 0.44f;
    private const float CrowdingOffsetMaxSmoothingSpeed = 46f;
    private const float CrowdingOffsetHoldEpsilon = 1.35f;
    private const float CrowdingReturnToPathLerp = 0.14f;
    private const float CrowdingTinyOffsetSnap = 0.35f;
    private static readonly Point[] CardinalDirections =
    [
        new Point(0, -1),
        new Point(1, 0),
        new Point(0, 1),
        new Point(-1, 0),
    ];
    private static readonly float[] CrowdingSlotAngles =
    [
        -MathF.PI / 2f, // up
        MathF.PI / 2f, // down
        0f, // right
        MathF.PI, // left
        -MathF.PI / 4f,
        MathF.PI / 4f,
        3f * MathF.PI / 4f,
        -3f * MathF.PI / 4f,
    ];
    private int[] _enemyTileOccupancy = Array.Empty<int>();
    private Enemy[] _crowdingEnemiesBuffer = Array.Empty<Enemy>();
    private Point[] _crowdingEnemyTilesBuffer = Array.Empty<Point>();
    private int _crowdingEnemyCount;

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

        // Center the map in the screen area left of the UI panel
        int mapPixelW = _map.Columns * GameSettings.TileSize;
        int mapPixelH = _map.Rows * GameSettings.TileSize;
        float ox = (GameSettings.ScreenWidth - GameSettings.UIPanelWidth - mapPixelW) / 2f;
        float oy = (GameSettings.ScreenHeight - mapPixelH) / 2f;
        _mapOffset = new Vector2(MathF.Floor(ox), MathF.Floor(oy));
        _worldMatrix = Matrix.CreateTranslation(_mapOffset.X, _mapOffset.Y, 0);

        _lives = GameSettings.StartingLives;
        _gameOver = false;
        _gameWon = false;
        _allEnemiesCleared = true;
        EnsureCrowdingBuffersCapacity(0);

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
    }

    public void Update(GameTime gameTime)
    {
        _inputManager.Update();
        HandleInput();

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
        // (CountdownTimers, float dt, wave spawning) propagate this automatically through
        // their GameTime parameter, requiring no changes inside Enemy, Tower, or WaveManager.
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

        // --- Update wave spawning ---
        _waveManager.Update(activeTime);

        // --- Build crowd occupancy and assign blocked-enemy spread targets before enemy movement ---
        UpdateCrowdingOccupancyAndSpreadTargets();

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

        // --- Apply per-frame separation after movement so enemies spread out visually and physically ---
        ApplyEnemyCollisionAvoidance(scaledDt);

        // Check if all enemies are cleared (wave done spawning + no enemies alive)
        _allEnemiesCleared = !_waveManager.WaveInProgress && _enemies.Count == 0;

        // Check win condition
        if (_waveManager.CurrentWave >= _waveManager.TotalWaves && _allEnemiesCleared)
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
    }

    private void EnsureCrowdingBuffersCapacity(int enemyCount)
    {
        int mapCellCount = _map.Columns * _map.Rows;
        if (_enemyTileOccupancy.Length != mapCellCount)
            _enemyTileOccupancy = new int[mapCellCount];

        if (_crowdingEnemiesBuffer.Length < enemyCount)
            _crowdingEnemiesBuffer = new Enemy[enemyCount];

        if (_crowdingEnemyTilesBuffer.Length < enemyCount)
            _crowdingEnemyTilesBuffer = new Point[enemyCount];
    }

    private void UpdateCrowdingOccupancyAndSpreadTargets()
    {
        EnsureCrowdingBuffersCapacity(_enemies.Count);
        Array.Clear(_enemyTileOccupancy);
        _crowdingEnemyCount = 0;

        for (int i = 0; i < _enemies.Count; i++)
        {
            if (
                _enemies[i] is not Enemy concreteEnemy
                || concreteEnemy.IsDead
                || concreteEnemy.ReachedEnd
            )
                continue;

            _crowdingEnemiesBuffer[_crowdingEnemyCount] = concreteEnemy;
            _crowdingEnemyCount++;

            Point enemyGrid = Map.WorldToGrid(concreteEnemy.BasePosition);
            if (
                enemyGrid.X < 0
                || enemyGrid.X >= _map.Columns
                || enemyGrid.Y < 0
                || enemyGrid.Y >= _map.Rows
            )
            {
                continue;
            }

            int mapIndex = enemyGrid.Y * _map.Columns + enemyGrid.X;
            _enemyTileOccupancy[mapIndex]++;
        }

        for (int i = 0; i < _crowdingEnemyCount; i++)
        {
            Enemy enemy = _crowdingEnemiesBuffer[i];
            Tower? spreadTower = GetCrowdingSpreadTower(enemy);
            if (spreadTower == null)
            {
                enemy.ClearBlockedSpreadTarget();
                continue;
            }

            if (
                enemy.BlockedSpreadTarget is Vector2 currentSpreadTarget
                && IsValidBlockedSpreadTarget(enemy, spreadTower, currentSpreadTarget)
            )
            {
                continue;
            }

            enemy.ClearBlockedSpreadTarget();
            if (TryGetBlockedSpreadTarget(enemy, spreadTower, out Vector2 blockedSpreadTarget))
                enemy.SetBlockedSpreadTarget(blockedSpreadTarget);
        }
    }

    private static Tower? GetCrowdingSpreadTower(Enemy enemy)
    {
        if (enemy.BlockingTower != null && !enemy.BlockingTower.IsDead)
            return enemy.BlockingTower;

        if (enemy.IsAttacking && enemy.TargetTower != null && !enemy.TargetTower.IsDead)
            return enemy.TargetTower;

        return null;
    }

    private bool TryGetBlockedSpreadTarget(Enemy enemy, Tower targetTower, out Vector2 targetWorld)
    {
        targetWorld = default;
        Point topLeft = targetTower.GridPosition;
        Point footprint = targetTower.FootprintSize;
        Point currentTile = Map.WorldToGrid(enemy.BasePosition);
        if (!IsValidCrowdingTile(currentTile))
            return false;

        Span<Point> candidates = stackalloc Point[24];
        int candidateCount = 0;
        int minX = topLeft.X - 1;
        int maxX = topLeft.X + footprint.X;
        int minY = topLeft.Y - 1;
        int maxY = topLeft.Y + footprint.Y;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                bool isPerimeter =
                    x < topLeft.X
                    || x >= topLeft.X + footprint.X
                    || y < topLeft.Y
                    || y >= topLeft.Y + footprint.Y;
                if (!isPerimeter)
                    continue;

                TryAddUniqueSpreadCandidate(new Point(x, y), candidates, ref candidateCount);
            }
        }

        Span<Point> stepOptions = stackalloc Point[5];
        int stepOptionCount = 0;
        int currentMapIndex = currentTile.Y * _map.Columns + currentTile.X;
        int currentTileOccupancy = _enemyTileOccupancy[currentMapIndex];
        float towerCenterX = topLeft.X + ((footprint.X - 1) * 0.5f);
        float towerCenterY = topLeft.Y + ((footprint.Y - 1) * 0.5f);
        bool approachMostlyHorizontal =
            MathF.Abs(currentTile.X - towerCenterX) >= MathF.Abs(currentTile.Y - towerCenterY);
        Point towerApproachStep = approachMostlyHorizontal
            ? new Point(Math.Sign(towerCenterX - currentTile.X), 0)
            : new Point(0, Math.Sign(towerCenterY - currentTile.Y));
        int rearTileOccupancy = 0;
        int rearDiagonalOccupancy = 0;

        if (towerApproachStep != Point.Zero)
        {
            int rearTileX = currentTile.X - towerApproachStep.X;
            int rearTileY = currentTile.Y - towerApproachStep.Y;
            Point rearTile = new Point(rearTileX, rearTileY);
            if (IsValidCrowdingTile(rearTile))
            {
                int rearMapIndex = rearTile.Y * _map.Columns + rearTile.X;
                rearTileOccupancy = _enemyTileOccupancy[rearMapIndex];
            }

            if (approachMostlyHorizontal)
            {
                Point rearUpper = new Point(rearTileX, currentTile.Y - 1);
                Point rearLower = new Point(rearTileX, currentTile.Y + 1);

                if (IsValidCrowdingTile(rearUpper))
                {
                    int rearUpperIndex = rearUpper.Y * _map.Columns + rearUpper.X;
                    rearDiagonalOccupancy = Math.Max(
                        rearDiagonalOccupancy,
                        _enemyTileOccupancy[rearUpperIndex]
                    );
                }

                if (IsValidCrowdingTile(rearLower))
                {
                    int rearLowerIndex = rearLower.Y * _map.Columns + rearLower.X;
                    rearDiagonalOccupancy = Math.Max(
                        rearDiagonalOccupancy,
                        _enemyTileOccupancy[rearLowerIndex]
                    );
                }
            }
            else
            {
                Point rearLeft = new Point(currentTile.X - 1, rearTileY);
                Point rearRight = new Point(currentTile.X + 1, rearTileY);

                if (IsValidCrowdingTile(rearLeft))
                {
                    int rearLeftIndex = rearLeft.Y * _map.Columns + rearLeft.X;
                    rearDiagonalOccupancy = Math.Max(
                        rearDiagonalOccupancy,
                        _enemyTileOccupancy[rearLeftIndex]
                    );
                }

                if (IsValidCrowdingTile(rearRight))
                {
                    int rearRightIndex = rearRight.Y * _map.Columns + rearRight.X;
                    rearDiagonalOccupancy = Math.Max(
                        rearDiagonalOccupancy,
                        _enemyTileOccupancy[rearRightIndex]
                    );
                }
            }
        }

        // Only push a front enemy off its slot when there is actual backline pressure.
        bool isCrowdPressured =
            currentTileOccupancy > 1 || rearTileOccupancy > 0 || rearDiagonalOccupancy > 0;
        if (!isCrowdPressured)
            return false;

        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Point neighbor = new Point(
                currentTile.X + CardinalDirections[i].X,
                currentTile.Y + CardinalDirections[i].Y
            );
            if (IsValidCrowdingTile(neighbor))
                stepOptions[stepOptionCount++] = neighbor;
        }

        int bestStepIndex = -1;
        float bestScore = float.MaxValue;
        int bestFrontDistance = int.MaxValue;
        int bestOccupancy = int.MaxValue;
        float bestDistanceSquared = float.MaxValue;
        const float frontDistanceWeight = 4f;
        const float occupancyWeight = 1.5f;
        const float lateralSlipBonus = 3.5f;
        float rearPressureBonus = (rearTileOccupancy * 2f) + rearDiagonalOccupancy;

        for (int i = 0; i < stepOptionCount; i++)
        {
            Point tile = stepOptions[i];
            int nearestFrontDistance = GetClosestPerimeterDistance(
                tile,
                candidates,
                candidateCount
            );
            if (nearestFrontDistance == int.MaxValue)
                continue;

            int mapIndex = tile.Y * _map.Columns + tile.X;
            int occupancy = _enemyTileOccupancy[mapIndex];
            Vector2 stepWorld = Map.GridToWorld(tile);
            float distanceSquared = Vector2.DistanceSquared(enemy.BasePosition, stepWorld);
            float score =
                (nearestFrontDistance * frontDistanceWeight) + (occupancy * occupancyWeight);
            if (isCrowdPressured)
            {
                int stepDx = tile.X - currentTile.X;
                int stepDy = tile.Y - currentTile.Y;
                bool isLateralStep = approachMostlyHorizontal ? stepDy != 0 : stepDx != 0;
                if (isLateralStep)
                    score -= lateralSlipBonus + rearPressureBonus;
            }

            bool isScoreTie = MathF.Abs(score - bestScore) <= 0.0001f;

            if (
                score < bestScore
                || (
                    isScoreTie
                    && (
                        nearestFrontDistance < bestFrontDistance
                        || (
                            nearestFrontDistance == bestFrontDistance
                            && (
                                occupancy < bestOccupancy
                                || (
                                    occupancy == bestOccupancy
                                    && distanceSquared < bestDistanceSquared
                                )
                            )
                        )
                    )
                )
            )
            {
                bestScore = score;
                bestFrontDistance = nearestFrontDistance;
                bestOccupancy = occupancy;
                bestDistanceSquared = distanceSquared;
                bestStepIndex = i;
            }
        }

        if (bestStepIndex < 0)
            return false;

        Point bestStepTile = stepOptions[bestStepIndex];
        if (bestStepTile == currentTile)
            return false;

        targetWorld = Map.GridToWorld(bestStepTile);
        return true;
    }

    private bool IsValidBlockedSpreadTarget(Enemy enemy, Tower targetTower, Vector2 targetWorld)
    {
        Point targetTile = Map.WorldToGrid(targetWorld);
        Point topLeft = targetTower.GridPosition;
        Point footprint = targetTower.FootprintSize;

        bool inSearchBounds =
            targetTile.X >= topLeft.X - 1
            && targetTile.X <= topLeft.X + footprint.X
            && targetTile.Y >= topLeft.Y - 1
            && targetTile.Y <= topLeft.Y + footprint.Y;
        if (!inSearchBounds)
            return false;

        bool outsideTowerFootprint =
            targetTile.X < topLeft.X
            || targetTile.X >= topLeft.X + footprint.X
            || targetTile.Y < topLeft.Y
            || targetTile.Y >= topLeft.Y + footprint.Y;
        if (!outsideTowerFootprint)
            return false;

        Point currentTile = Map.WorldToGrid(enemy.BasePosition);
        int targetDistance =
            Math.Abs(targetTile.X - currentTile.X) + Math.Abs(targetTile.Y - currentTile.Y);
        if (targetDistance > 1)
            return false;

        return IsValidCrowdingTile(targetTile);
    }

    private bool IsValidCrowdingTile(Point tile)
    {
        if (tile.X < 0 || tile.X >= _map.Columns || tile.Y < 0 || tile.Y >= _map.Rows)
            return false;

        Tile mapTile = _map.Tiles[tile.X, tile.Y];
        if (mapTile.Type != TileType.Path)
            return false;

        return mapTile.OccupyingTower == null
            && mapTile.ReservedByTower == null
            && mapTile.ReservedForPendingWallBy == null;
    }

    private bool IsValidCrowdingWorldPosition(Vector2 worldPosition)
    {
        Point tile = Map.WorldToGrid(worldPosition);
        return IsValidCrowdingTile(tile);
    }

    private int GetClosestPerimeterDistance(Point from, Span<Point> perimeter, int count)
    {
        int best = int.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Point tile = perimeter[i];
            if (!IsValidCrowdingTile(tile))
                continue;

            int distance = Math.Abs(from.X - tile.X) + Math.Abs(from.Y - tile.Y);
            if (distance < best)
                best = distance;
        }

        return best;
    }

    private static void TryAddUniqueSpreadCandidate(
        Point candidate,
        Span<Point> candidates,
        ref int count
    )
    {
        for (int i = 0; i < count; i++)
        {
            if (candidates[i] == candidate)
                return;
        }

        if (count >= candidates.Length)
            return;

        candidates[count] = candidate;
        count++;
    }

    private void ApplyEnemyCollisionAvoidance(float dt)
    {
        EnsureCrowdingBuffersCapacity(_enemies.Count);
        _crowdingEnemyCount = 0;

        for (int i = 0; i < _enemies.Count; i++)
        {
            if (
                _enemies[i] is not Enemy concreteEnemy
                || concreteEnemy.IsDead
                || concreteEnemy.ReachedEnd
            )
                continue;

            _crowdingEnemiesBuffer[_crowdingEnemyCount] = concreteEnemy;
            _crowdingEnemyCount++;
        }

        if (_crowdingEnemyCount == 0)
            return;

        // Rebuild occupancy from current enemy base positions.
        Array.Clear(_enemyTileOccupancy);
        for (int i = 0; i < _crowdingEnemyCount; i++)
        {
            Point enemyGrid = Map.WorldToGrid(_crowdingEnemiesBuffer[i].BasePosition);
            _crowdingEnemyTilesBuffer[i] = enemyGrid;
            if (
                enemyGrid.X < 0
                || enemyGrid.X >= _map.Columns
                || enemyGrid.Y < 0
                || enemyGrid.Y >= _map.Rows
            )
            {
                continue;
            }

            int mapIndex = enemyGrid.Y * _map.Columns + enemyGrid.X;
            _enemyTileOccupancy[mapIndex]++;
        }

        for (int i = 0; i < _crowdingEnemyCount; i++)
        {
            Enemy enemy = _crowdingEnemiesBuffer[i];
            Point enemyTile = _crowdingEnemyTilesBuffer[i];
            Vector2 targetOffset = ComputeDeterministicCrowdingTargetOffset(i, enemy, enemyTile);
            targetOffset = ClampOffsetToValidCrowdingTile(enemy, targetOffset);
            bool hasCrowdingTarget = targetOffset.LengthSquared() > 0.0001f;

            Vector2 previousOffset = enemy.CrowdingOffset;
            Vector2 offset = previousOffset;
            Vector2 currentVelocity = enemy.CrowdingVelocity;
            if (!hasCrowdingTarget)
            {
                offset = Vector2.Lerp(previousOffset, Vector2.Zero, CrowdingReturnToPathLerp);
                currentVelocity = Vector2.Lerp(currentVelocity, Vector2.Zero, 0.9f);
            }

            int tileOccupancy = GetEnemyTileOccupancy(enemyTile);
            float dynamicMaxOffset = CrowdingMaxOffset;
            if (tileOccupancy >= CrowdingOverflowStartOccupancy)
            {
                float extraOffset = MathF.Min(
                    (tileOccupancy - (CrowdingOverflowStartOccupancy - 1))
                        * CrowdingOverflowExtraOffsetPerEnemy,
                    CrowdingOverflowMaxExtraOffset
                );
                dynamicMaxOffset += extraOffset;
            }

            if (
                hasCrowdingTarget
                && (
                    Vector2.DistanceSquared(previousOffset, targetOffset)
                    <= CrowdingOffsetHoldEpsilon * CrowdingOffsetHoldEpsilon
                )
            )
            {
                currentVelocity = Vector2.Lerp(currentVelocity, Vector2.Zero, 0.9f);
                offset = targetOffset;
            }
            else if (hasCrowdingTarget)
            {
                offset = SmoothDampVector2(
                    previousOffset,
                    targetOffset,
                    ref currentVelocity,
                    CrowdingOffsetSmoothTime,
                    CrowdingOffsetMaxSmoothingSpeed,
                    dt
                );
            }

            float maxOffsetSquared = dynamicMaxOffset * dynamicMaxOffset;
            float lengthSquared = offset.LengthSquared();
            if (lengthSquared > maxOffsetSquared)
            {
                float length = MathF.Sqrt(lengthSquared);
                if (length > 0f)
                    offset *= dynamicMaxOffset / length;
            }
            else if (lengthSquared <= CrowdingTinyOffsetSnap * CrowdingTinyOffsetSnap)
            {
                offset = Vector2.Zero;
            }

            Vector2 clampedOffset = ClampOffsetToValidCrowdingTile(enemy, offset);
            if (Vector2.DistanceSquared(clampedOffset, offset) > 0.0001f)
                currentVelocity = Vector2.Zero;

            offset = clampedOffset;
            enemy.SetCrowdingOffset(offset);
            enemy.SetCrowdingVelocity(currentVelocity);
        }
    }

    private Vector2 ComputeDeterministicCrowdingTargetOffset(
        int enemyIndex,
        Enemy enemy,
        Point tile
    )
    {
        if (tile.X < 0 || tile.X >= _map.Columns || tile.Y < 0 || tile.Y >= _map.Rows)
            return Vector2.Zero;

        int mapIndex = tile.Y * _map.Columns + tile.X;
        int tileOccupancy = _enemyTileOccupancy[mapIndex];
        if (tileOccupancy <= 1)
            return Vector2.Zero;

        int slotIndex = 0;
        for (int i = 0; i < _crowdingEnemyCount; i++)
        {
            if (i == enemyIndex || _crowdingEnemyTilesBuffer[i] != tile)
                continue;

            if (_crowdingEnemiesBuffer[i].StableId < enemy.StableId)
                slotIndex++;
        }

        if (slotIndex >= tileOccupancy)
            slotIndex = tileOccupancy - 1;

        int angleIndex = slotIndex % CrowdingSlotAngles.Length;
        int ringIndex = slotIndex / CrowdingSlotAngles.Length;
        float angle = CrowdingSlotAngles[angleIndex];
        float slotRadius =
            CrowdingTileSlotBaseRadius + (ringIndex * CrowdingTileSlotRingRadiusStep);
        slotRadius += (tileOccupancy - 2) * CrowdingTileSlotRadiusPerEnemy;
        slotRadius = MathF.Min(slotRadius, CrowdingTileSlotMaxRadius);

        float horizontalBias = tileOccupancy >= CrowdingOverflowStartOccupancy ? 0.86f : 1f;
        float verticalBias = tileOccupancy >= CrowdingOverflowStartOccupancy ? 1.22f : 1f;
        return new Vector2(
            MathF.Cos(angle) * slotRadius * horizontalBias,
            MathF.Sin(angle) * slotRadius * verticalBias
        );
    }

    private Vector2 ClampOffsetToValidCrowdingTile(Enemy enemy, Vector2 offset)
    {
        if (IsValidCrowdingWorldPosition(enemy.BasePosition + offset))
            return offset;

        for (int step = 5; step >= 1; step--)
        {
            float scale = step / 6f;
            Vector2 candidateOffset = offset * scale;
            if (IsValidCrowdingWorldPosition(enemy.BasePosition + candidateOffset))
                return candidateOffset;
        }

        return Vector2.Zero;
    }

    private int GetEnemyTileOccupancy(Point enemyGrid)
    {
        if (
            enemyGrid.X < 0
            || enemyGrid.X >= _map.Columns
            || enemyGrid.Y < 0
            || enemyGrid.Y >= _map.Rows
        )
        {
            return 1;
        }

        int mapIndex = enemyGrid.Y * _map.Columns + enemyGrid.X;
        return _enemyTileOccupancy[mapIndex];
    }

    private static Vector2 SmoothDampVector2(
        Vector2 current,
        Vector2 target,
        ref Vector2 currentVelocity,
        float smoothTime,
        float maxSpeed,
        float deltaTime
    )
    {
        if (deltaTime <= 0f)
            return current;

        smoothTime = MathF.Max(0.0001f, smoothTime);
        float omega = 2f / smoothTime;
        float x = omega * deltaTime;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

        Vector2 change = current - target;
        Vector2 originalTarget = target;

        float maxChange = maxSpeed * smoothTime;
        float maxChangeSquared = maxChange * maxChange;
        if (change.LengthSquared() > maxChangeSquared)
        {
            change.Normalize();
            change *= maxChange;
        }

        target = current - change;
        Vector2 temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exp;

        Vector2 output = target + (change + temp) * exp;
        Vector2 originalToCurrent = originalTarget - current;
        Vector2 outputToOriginal = output - originalTarget;
        if (Vector2.Dot(originalToCurrent, outputToOriginal) > 0f)
        {
            output = originalTarget;
            currentVelocity = Vector2.Zero;
        }

        return output;
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
