# Architecture

## Scene Stack
- `SceneManager`: `SetScene()` replaces, `PushScene()` overlays, `PopScene()` removes top. Only top scene updates/draws
- Scenes: `MapSelectionScene`, `GameplayScene`, `PauseScene`

## Data Flow
- Down: Scene → managers via `Update()` args
- Up: Managers → Scene via `Action<T>` callbacks. Managers never reference each other
- Rendering: `SpriteBatch`/fonts passed via `Draw()`

## GameplayScene Owns
`Map`, `WaveManager`, `ChampionManager`, `TowerManager`, `InputManager`, `UIPanel`, `FloatingTexts`, `AoEEffects`, `SpikeEffects`

## Tower System
- `TowerType` enum: Generic (Gun, Cannon, Walling) + Champion (ChampionGun, ChampionCannon, ChampionWalling) + WallSegment
- Stats: `Entities/Towers/<Type>Tower.cs`; registry: `TowerData.GetStats()`
- Variant mapping: `TowerType.GetChampionVariant()` / `GetGenericVariant()`
- `TowerTypeExtensions`: `IsChampion()`, `IsWallingChampion()`, `IsWallingGeneric()`, `IsWallSegment()`
- Tiles: `OccupyingTower` (present) + `ReservedByTower` (movement destination) + `ReservedForPendingWallBy` (deferred wall spawn reservation). `Map.CanBuild()` rejects if any reservation/occupant is non-null. `TileType` immutable after init
- `DrawScale`: Generics {1,1}, Champions {1,1.5}. Champions: bottom-center origin (0.5,1.0), Y offset grows upward. Bars use `SpriteSize * DrawScale.Y`
- `Tower.Draw()`: origin conditional on `DrawScale.Y > 1.0f`; champions offset Y by `SpriteSize / 2f`
- `Tower.UpdateChampionStatus(bool)`: virtual hook for debuffs on champion death
- AoE chain: `Projectile.OnAOEImpact` → `Tower` → `TowerManager` → `GameplayScene` spawns visual
- Wall-attack chain: `Tower.WallNetworkTargetFinder` (set per-frame by `TowerManager`) → on fire: instant `TakeDamage` + `ApplySlow(_abilityDuration)` → `Tower.OnWallAttack` → `TowerManager.OnWallAttack` → `GameplayScene` spawns `SpikeEffect`. `_abilityDuration` = `TowerStats.AbilityDuration` (ChampionWalling: 5s, Walling: 3s)
- `CanWalk`: Champions `true`, Generics `false`. `MoveSpeed`/`CooldownDuration` default `0f`, only set when `CanWalk: true`
- `TowerState` (Active/Moving/Cooldown). Moving: `_drawPosition` interpolates, `GridPosition` updates on cell arrival, origin cleared (ghost). Cooldown: timer only — tower still fires. Active+Cooldown both run `UpdateActive()`. `OnMovementComplete` → `TowerManager` re-occupies dest + triggers reroute
- `TowerManager.GetPreviewPath(dest)`: `List<Point>?` for move hover — checks `CanWalk`, `Active`, `CanBuild`
- `Tower.UpdateActive()`: guarded by `Range <= 0f && WallNetworkTargetFinder == null` — skips fire loop entirely (avoids `CountdownTimer` overflow). Wall targeting uses `WallNetworkTargetFinder` delegate, deals instant damage (no projectile)
- `TargetingStrategy` enum (`Closest`, `LowestHP`, `MostGrouped`) on `TowerStats.Targeting`/`Tower._targeting`. Gun+ChampionGun: `LowestHP`. Cannon+ChampionCannon: `MostGrouped` (count alive enemies within `AOERadius`, tie-break lowest HP). Wall bypasses `SelectTarget`
- `TowerManager.BuildAttackZone(wallSet)`: all in-bounds tiles 1 step outside `wallSet` in all 8 directions. Used by `DrawWallRangeIndicatorForSet`, `FindWallNetworkTarget`, `UpdateWallFrenzy`. Target preference: closest non-slowed → closest slowed
- `Tower.ApplyDecayDamage(float dt)`: accumulates fractional HP; used by wall segment decay
- Wall segments: `TowerType.WallSegment`, 30 HP cap, 10k movement cost, deferred spawn pipeline. `TryPlaceWallPath()` reserves valid path tiles first (`ReservedForPendingWallBy`), then spawns segments sequentially as each prior segment reaches cap. Spawned segment starts at `1` max/current HP and grows at `20 HP/sec` via `Tower.InitializeWallGrowth()` + `StartWallGrowth()`. On anchor loss: pending reservations canceled and active segment growth stopped. If active segment dies pre-cap: remaining pending reservations in that chain are canceled. `_wallConnectedSets: Dictionary<Tower, HashSet<Point>>` — rebuilt each `Update` as single-root BFS per walling tower; disconnected towers don't share zones. `UpdateWallDecay()` unions all sets; orphaned segments decay 1 HP/sec per exposed cardinal side. Runs before dead-tower sweep
- `TowerManager.DrawPendingWallReservations()`: renders ghost tiles for reserved-but-not-yet-spawned wall segments
- `TowerManager.IsAdjacentToWallingNetwork(Point, Tower)`: public; uses `_wallConnectedSets[tower]` cache, falls back to fresh BFS if called before first `Update`
- `TowerManager.TryPlaceWallPath(path, tower)`: validates contiguous prefix, reserves pending path, and starts deferred sequential spawning. `GetWallPathValidPrefixLength(path, tower)` simulates contiguous prefix validity without mutating map state (preview/corner choice only)
- `TowerStats.AbilityEffect: Action<Tower>?`: called by `TriggerChampionAbility()` on champion + generics. ChampionWalling always buffs all Walling generics even if champion is dead
- `Tower.ActivateAbilityBuff(damageMult, fireRateSpeedMult)`: saves originals via `_hasStoredAbilityStats` guard, sets `IsAbilityBuffActive = true`, runs for `AbilityDuration`. Re-trigger resets timer, no stacking. `DeactivateAbilityBuff()` only restores if guard set. Gold aura while active
- `Tower.ActivateFrenzy(float duration)`: sets `IsAbilityBuffActive = true` + `_abilityTimer`, no stat change. `UpdateWallFrenzy(tower, wallSet)` multi-hits all enemies in attack zone at `tower.FireRate`; `WallNetworkTargetFinder` nulled during frenzy to prevent double-hits
- `TowerStats.AbilityCooldown`: per-champion CD; generics default 0
- Ability flow: `UIPanel.OnAbilityTriggered` → `GameplayScene` → `ChampionManager.StartAbilityCooldown()` + `TowerManager.TriggerChampionAbility()`
- `Tower.DrawPosition`: interpolated visual position. `WorldPosition`: grid-snapped
- `TowerManager.MoveTower(tower, dest)`: ghost origin → `destTile.ReservedByTower = tower` → reroute → `StartMoving()`. `HandleMovementComplete`: clear reservation → re-occupy → reroute. `RemoveTower()` calls `ClearReservationFor()` on mid-move death
- `TowerPathfinder`: Dijkstra via `Pathfinder.ComputeHeatMap()`. Costs: Path=1, HighGround=2, occupied=10, Rock=impassable. Ignores enemies

## Map Loading
- Tiled `.tmx` in `Content/Maps/`. `TmxLoader.TryLoad(id)` → `MapData.TileGrid` (column-major `[col,row]`)
- `MapDataRepository.GetAvailableMaps()` scans `Content/Maps/*.tmx` at runtime
- `TextureManager.DrawTile()`: `terrain.png` spritesheet (col = `(int)TileType`); fallback colored rect
- Multi-spawn/exit: `MapData.SpawnPoints/ExitPoints: Dictionary<string, Point>`. Lane pairing: `spawn_a` → `exit_a` by suffix; fallback first exit. `Map.ActivePaths`: spawn name → path. `Enemy._spawnName` preserved on reroute via `Map.ComputePathFromPosition()`

## Tile System
- `TileType`: HighGround, Path, Rock. Stats: `Engine/TileTypes/<Type>Tile.cs`; registry: `TileData.GetStats()`

## ChampionManager
- One alive champion per type. Global 10s CD after any placement. Individual 15s respawn CD per type
- Generics placeable only if champion variant alive
- Champion death → `UpdateChampionStatus(false)` on all matching generics
- API: `GlobalCooldownRemaining`, `GetRespawnCooldown(type)`, `IsChampionAlive(type)`, `StartAbilityCooldown(type)`, `GetAbilityCooldownRemaining(type)`, `IsAbilityReady(type)`

## UIPanel
- `UISelectionMode`: `None`, `PlaceTower`, `PlaceHighGround`, `SpawnEnemy`
- One consolidated button per tower type: champion mode (free, shows CD) when dead; generic mode (costs gold) when alive. `HandleConsolidatedTowerClick()` dispatches. Sub-labels: "Place Champion" (green), "Global/Respawn: X.Xs" (yellow), "Can't Afford" (red)
- Ability button per type: disabled (no champion) / CD with timer / ready (green). Fires `OnAbilityTriggered` only when `IsAbilityReady()`
- Debug: Place High Ground (grid click mode), Spawn Enemy (instant)

## Wall Placement Mode
- `_wallPlacementMode`: toggled by 18×18 "+" button top-right of selected walling tower (champion or generic). In wall mode, left press/drag/release builds a single-corner Manhattan L path. L direction locks based on which axis the user drags first from the start tile (`_wallDragLockedHorizontalFirst`); returning to a straight line resets the lock. Falls back to the other candidate only if it has a strictly longer valid prefix (blocked tiles). On release, `TryPlaceWallPath()` commits, reserves valid tiles, and starts deferred growth from the first segment. Blocks right-click move
- `GetWallPlacementButtonRect(tower)`: rect from `tower.DrawPosition` (tracks during cooldown movement)
- Hover: `DarkGreen * 0.5f` if buildable + adjacent; `Red * 0.3f` otherwise. During drag, preview draws valid prefix in dark green and blocked remainder in red
- Clears on: ESC, tower/enemy selection change, UI `SelectedTowerType` set, tower sold

## Enemy Selection
- `GameplayScene._selectedEnemy` via `GetEnemyAt()` (15px radius). Mutually exclusive with tower. Auto-clears on death/end

## Wave System
- `Content/Waves/{mapId}.json` via `WaveLoader.TryLoad()`. Fallback: `FallbackWaves()` in `GameplayScene`
- Schema: `{ waves: [ { wave, spawns: [ { at, spawnPoint, name, health, speed, bounty, attackDamage, color } ] } ] }`. `at` = seconds from wave start
- `WaveManager(Func<string, List<Point>?>, List<WaveData>)`: dequeues by elapsed time; wave ends when list empty
- `WaveLoader.ParseColor(string)`: XNA `Color` by name via reflection

## TextureManager
- `DrawSprite()`: optional `origin` param (default 0.5,0.5 centered)
- `DrawRect()`: top-left origin. `DrawSprite()`: centered origin. Do not confuse
