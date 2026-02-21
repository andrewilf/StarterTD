# Architecture

## Scene Stack
- `SceneManager` stack: `SetScene()` replaces, `PushScene()` overlays, `PopScene()` removes top
- Only top scene updates/draws
- Scenes: `MapSelectionScene`, `GameplayScene`, `PauseScene`

## Data Flow
- **Down**: Scene → managers via `Update()` args
- **Up**: Managers → Scene via `Action<T>` callbacks
- **Isolation**: Managers never reference each other
- **Rendering**: `SpriteBatch`/fonts passed via `Draw()`

## GameplayScene Owns
`Map`, `WaveManager`, `ChampionManager`, `TowerManager`, `InputManager`, `UIPanel`, `FloatingTexts`, `AoEEffects`, `SpikeEffects`

## Tower System
- `TowerType` enum: Generic (Gun, Cannon) + Champion (ChampionGun, ChampionCannon, ChampionWalling) + WallSegment
- Stats per type in `Entities/Towers/<Type>Tower.cs`; registry: `TowerData.GetStats()`
- Variant mapping: `TowerType.GetChampionVariant()` / `GetGenericVariant()`. **ChampionWalling has no generic variant** — guard all call sites with `IsWallingChampion()` before calling these
- `TowerTypeExtensions`: `IsChampion()`, `IsWallingChampion()`, `IsWallSegment()`
- Tiles track two tower references: `OccupyingTower` (physically present) and `ReservedByTower` (committed destination, not yet arrived). `Map.CanBuild()` rejects tiles where either is non-null. `TileType` immutable after init
- `DrawScale`: Generics {1,1}, Champions {1,1.5}. Champions use bottom-center origin (0.5,1.0), Y offset grows upward. Bars use `SpriteSize * DrawScale.Y`
- `Tower.Draw()`: origin conditional on `DrawScale.Y > 1.0f`; champions offset Y by `SpriteSize / 2f`
- `Tower.UpdateChampionStatus(bool)`: virtual hook for debuffs on champion death
- AoE chain: `Projectile.OnAOEImpact` → `Tower` → `TowerManager` → `GameplayScene` spawns visual
- Wall-attack chain: `Tower.WallNetworkTargetFinder` (set by `TowerManager` each frame) selects target; on fire: instant `TakeDamage` + `ApplySlow(_abilityDuration)`, then `Tower.OnWallAttack` → `TowerManager.OnWallAttack` → `GameplayScene` spawns `SpikeEffect`. `_abilityDuration` sourced from `TowerStats.AbilityDuration` (5s for `ChampionWalling`)
- `CanWalk` flag on `TowerStats` (and mirrored on `Tower`) gates movement. Champions: `true`; Generics: `false`. `MoveSpeed`/`CooldownDuration` are optional stats (default `0f`) — only specified when `CanWalk: true`
- State machine: `TowerState` (Active/Moving/Cooldown). Moving: `_drawPosition` interpolates tile-to-tile, `GridPosition` updates on cell arrival, origin tile cleared (ghost). Cooldown: move-ready timer only — tower still fires. Both Active+Cooldown run `UpdateActive()`. `OnMovementComplete` callback lets `TowerManager` re-occupy destination tile and trigger reroute
- `TowerManager.GetPreviewPath(dest)`: returns `List<Point>?` for hover preview — checks `CanWalk`, `Active` state, `CanBuild`. `GameplayScene` calls per-frame, draws gold dot+line path overlay between map and tower layers
- `Tower.UpdateActive()`: guarded by `Range <= 0f && WallNetworkTargetFinder == null` — towers with no range and no wall delegate skip the fire loop entirely (prevents `CountdownTimer` overflow from `float.MaxValue` FireRate). Wall targeting bypasses circular range: uses `WallNetworkTargetFinder` delegate (set per-frame by `TowerManager`) and deals instant damage instead of spawning a projectile
- **Targeting strategies**: `TargetingStrategy` enum (`Closest`, `LowestHP`, `MostGrouped`) stored on `TowerStats.Targeting` and cached in `Tower._targeting`. `Tower.SelectTarget()` dispatches to `FindClosest` / `FindLowestHP` / `FindMostGrouped`. Gun+ChampionGun use `LowestHP`; Cannon+ChampionCannon use `MostGrouped` (counts alive enemies within `AOERadius` of each candidate, tie-break lowest HP). Wall targeting bypasses `SelectTarget` entirely
- `TowerManager.BuildAttackZone(wallSet)`: shared helper returning all in-bounds tiles 1 step outside the connected wall set in all 8 directions. Used by `DrawWallRangeIndicator` (hover preview), `FindWallNetworkTarget` (targeting), and `UpdateWallFrenzy`. `FindWallNetworkTarget` prefers closest non-slowed enemy; falls back to closest slowed enemy
- `Tower.ApplyDecayDamage(float dt)`: accumulates fractional HP loss; used by wall segments decaying at 1 HP/sec
- Wall segments: `TowerType.WallSegment`, 30 HP, 10k movement cost, placed by `TowerManager.TryPlaceWall()`. Must be 4-directionally adjacent to the walling champion or a connected wall (BFS). `BuildConnectedWallSet()` BFS from champion position — result cached as `_cachedWallConnectedSet` once per `Update`, reused by placement validation, decay suppression, attack targeting, and range drawing. `UpdateWallDecay()` skips segments in the connected set; orphaned segments (disconnected by selling a bridging tile) decay at 1 HP/sec per exposed cardinal side. Runs before the dead-tower sweep in `TowerManager.Update()`
- `TowerManager.IsAdjacentToWallingNetwork()`: public so `GameplayScene` can use it for per-frame hover feedback without duplicating BFS
- `TowerStats.AbilityEffect: Action<Tower>?`: delegate stored per tower type in its stats file. Called by `TowerManager.TriggerChampionAbility()` on the champion and all matching generics. For `ChampionWalling` (no generics), invoked on the champion only
- `Tower.ActivateAbilityBuff(damageMult, fireRateSpeedMult)`: saves originals via `_hasStoredAbilityStats` guard, applies multipliers, sets `IsAbilityBuffActive = true`, runs for `TowerStats.AbilityDuration` (per-type). Re-triggering resets timer without stacking. `DeactivateAbilityBuff()` only restores stats if `_hasStoredAbilityStats` is set. Gold aura circles drawn while active
- `Tower.ActivateFrenzy(float duration)`: sets `IsAbilityBuffActive = true` + `_abilityTimer` without modifying stats. Used by `ChampionWalling` ability. `TowerManager.UpdateWallFrenzy()` handles the multi-target attack loop while active; normal `WallNetworkTargetFinder` is nulled during frenzy to prevent double-hits
- `TowerStats.AbilityCooldown`: per-champion-type cooldown duration read by `ChampionManager.StartAbilityCooldown()`. Generics omit this field (defaults to 0)
- Ability flow: `UIPanel.OnAbilityTriggered` → `GameplayScene` → `ChampionManager.StartAbilityCooldown()` + `TowerManager.TriggerChampionAbility()`
- `Tower.DrawPosition`: smooth visual position during movement (use instead of `WorldPosition` for visual tracking). `WorldPosition` always snapped to grid
- `TowerManager.MoveTower(tower, dest)`: ghost origin tile → set `destTile.ReservedByTower = tower` → reroute enemies → `StartMoving()`. `HandleMovementComplete`: clear reservation → re-occupy dest → reroute enemies. `RemoveTower()` calls `ClearReservationFor()` on mid-movement death to free the reserved tile
- `TowerPathfinder`: tower-specific Dijkstra via `Pathfinder.ComputeHeatMap()` with custom cost function (Path=1, HighGround=2, occupied tower=10, Rock=impassable). Ignores enemies

## Map Loading
- Maps are Tiled `.tmx` files in `Content/Maps/`. `TmxLoader.TryLoad(id)` parses XML → `MapData.TileGrid` (column-major `[col,row]`)
- `MapDataRepository.GetAvailableMaps()` scans `Content/Maps/*.tmx` at runtime — no C# changes needed when adding/removing maps
- `Map.InitializeTiles()`: TileGrid fast-path fills directly; legacy rectangle path still supported
- `TextureManager.DrawTile()`: draws from `terrain.png` spritesheet (col = `(int)TileType`); falls back to colored rect
- **Multi-spawn/exit**: `MapData` holds `Dictionary<string, Point> SpawnPoints` and `ExitPoints`. Objects named `spawn*`/`exit*` in the Tiled Markers layer are collected. Lane pairing: `spawn_a` → `exit_a` by suffix match; falls back to first exit. `Map` computes one heatmap per exit; `Map.ActivePaths` maps each spawn name to its path. `Map.ActivePath` (singular) returns first path for legacy callers. `Enemy._spawnName` stores the spawn name at construction and passes it to `Map.ComputePathFromPosition()` on reroute, preserving lane assignment after tower changes

## Tile System
- `TileType` enum: HighGround, Path, Rock
- Stats in `Engine/TileTypes/<Type>Tile.cs`; registry: `TileData.GetStats()`

## ChampionManager
- One champion per type alive at a time
- Global 10s cooldown after any champion placed
- Individual 15s respawn cooldown per type after death
- Generics placeable only if their champion variant alive
- Champion death calls `UpdateChampionStatus(false)` on matching generics. `ChampionWalling` skips generic debuff loop (no generic variant)
- Public API: `GlobalCooldownRemaining`, `GetRespawnCooldown(type)`, `IsChampionAlive(type)`, `StartAbilityCooldown(type)`, `GetAbilityCooldownRemaining(type)`, `IsAbilityReady(type)`

## UIPanel
- `UISelectionMode` enum: `None`, `PlaceTower`, `PlaceHighGround`, `SpawnEnemy`
- Consolidated buttons (one per tower type): champion mode when champion dead (free, shows cooldown), generic mode when champion alive (costs gold). `HandleConsolidatedTowerClick()` dispatches to champion or generic placement logic.
- Button sub-labels: "Place Champion" (green), "Global: X.Xs" / "Respawn: X.Xs" (yellow, cooldown active), "Can't Afford" (red)
- Walling champion: dedicated `_wallTowerButton` + `_wallAbilityButton` (no generic slot). `HandleWallChampionClick()` only places the champion; wall placement uses world-space button on the tower
- Debug buttons: Place High Ground (grid click mode), Spawn Enemy (instant, uses `WaveManager.CurrentWaveDefinition`)
- Ability buttons: one per tower type (Gun/Cannon only), below the tower button. States: disabled (no champion), CD with timer, ready (green). Fires `OnAbilityTriggered` only when `IsAbilityReady()`

## Wall Placement Mode (GameplayScene)
- `_wallPlacementMode` bool: toggled by clicking the 18×18 "+" world-space button top-right of the selected walling champion. Grid left-clicks place wall segments while active; right-click move is blocked
- `GetWallPlacementButtonRect(tower)`: computes button rect from `tower.DrawPosition` (interpolated) so it tracks during movement cooldown
- Hover feedback: `DarkGreen * 0.5f` if valid placement (buildable + adjacent to network), `Red * 0.3f` otherwise; grid outline becomes `DarkGreen`
- Mode clears on: ESC, selecting another tower/enemy, UI panel click with `SelectedTowerType`, selling the champion

## Enemy Selection
- `GameplayScene._selectedEnemy` via `GetEnemyAt()` (15px radius). Mutually exclusive with tower selection. Auto-clears on death/end

## Wave System
- Wave definitions loaded from `Content/Waves/{mapId}.json` via `WaveLoader.TryLoad()` (`System.Text.Json`). Falls back to `FallbackWaves()` in `GameplayScene` if no JSON file exists
- Schema: `{ waves: [ { wave, spawns: [ { at, spawnPoint, name, health, speed, bounty, attackDamage, color } ] } ] }`. `at` = seconds from wave start; `spawnPoint` must match a key in `Map.ActivePaths`
- `WaveManager` takes `Func<string, List<Point>?>` (spawn name → path) and `List<WaveData>`. Dequeues entries by elapsed time each frame; wave ends when pending list is empty
- `WaveLoader.ParseColor(string)`: resolves XNA `Color` property by name via reflection

## Other
- `TextureManager.DrawSprite()`: optional `origin` param (default 0.5,0.5 centered)
