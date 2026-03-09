# Architecture

## Scene Stack
- `SceneManager`: `SetScene()` replaces, `PushScene()` overlays, `PopScene()` removes top. Only top scene updates/draws
- Scenes: `MapSelectionScene`, `GameplayScene`, `PauseScene`

## Data Flow
- Down: Scene -> managers via `Update()` args
- Up: Managers -> Scene via `Action<T>` callbacks. Managers never reference each other
- Rendering: `SpriteBatch`/fonts passed via `Draw()`
- Gameplay uses split coordinate spaces: world-space for map entities, screen-space for UI/overlays

## GameplayScene Owns
`Map`, `WaveManager`, `ChampionManager`, `TowerManager`, `InputManager`, `UIPanel`, `AoEEffects`, `SpikeEffects`, `RailgunEffects`

## Tower System
- `TowerType` enum: Generic (Gun, Cannon, Walling) + Champion (ChampionGun, ChampionCannon, ChampionWalling, ChampionHealing) + WallSegment
- Stats: `Entities/Towers/Stats/<Type>TowerStats.cs`; registry: `TowerData.GetStats()`. Each stat record carries `BaseCooldown` (flat seconds added to pool on placement), `CooldownPenalty` (additional seconds per tower already in that pool), `FootprintTiles`, and `PlaceholderDrawSize`. Champions use `FootprintTiles = 2x2`; generics/walls use `1x1`
- Variant mapping: `TryGetChampionVariant()` / `TryGetGenericVariant()` (safe for champion-only types); throwing helpers `GetChampionVariant()` / `GetGenericVariant()` remain for strict callers
- `TowerTypeExtensions`: `IsChampion()`, `IsWallingChampion()`, `IsWallingGeneric()`, `IsWallSegment()`
- **Tower class hierarchy**: `Tower` (base) → `WallSegmentTower` (growth/decay), `WallingTower` (Walling + ChampionWalling; frenzy), `CannonChampionTower` (laser), `HealingChampionTower` (passive self-regen + healing/attack mode toggle with 4s switch cooldown + attack-mode ult railgun state). ChampionHealing attack-mode combat uses `ChampionHealingTowerStats.Stats` (`Range`/`Damage`/`FireRate`); healing mode suppresses direct attack by forcing range/damage to 0. Gun/Cannon/ChampionGun use `Tower` directly. Instantiate via `Tower.Create(type, pos)` factory — never `new Tower(...)` directly
- `HealingDrone` (`Entities/HealingDrone.cs`): managed by `TowerManager`; 3 drones are spawned for `ChampionHealing`. `TowerManager` updates drones in deterministic order with a shared claimed-target set so two drones cannot heal the same tower simultaneously. Drones heal closest damaged valid allied towers, return/recharge on empty energy, and redeploy when conditions are met. ChampionHealing ult refills drone energy to max immediately and removes healing energy cost while active. While ChampionHealing is in attack mode, drones are forced to return and only dock/recharge
- Virtual hooks on base: `HealthBarCapacity`, `IsFiringSuppressed`, `OnUpdateStart(dt)`, `OnAbilityDeactivated()`, `UpdateChampionStatus(bool)`
- Tiles: `OccupyingTower` (present) + `ReservedByTower` (movement destination) + `ReservedForPendingWallBy` (deferred wall spawn reservation). For 2x2 champions, all 4 footprint tiles are marked occupied/reserved. `Map.CanBuildFootprint(topLeft, size, ignoreTower?, requireUniformTileType?)` is the source of truth for placement/move validation; champion placement/move destination checks pass `requireUniformTileType: true` so all footprint tiles must share one buildable type. `Map.CanBuild()` is the 1x1 convenience wrapper
- `DrawScale`: multiplier applied to `PlaceholderDrawSize` (champions use `2 * GameSettings.TileSize`, so with `TileSize=32` they render at `64x64`)
- `Tower` position model: canonical `GridAnchor` (half-tile coordinates), derived `GridPosition` (top-left footprint tile), and `OccupiedTiles` (all currently blocked tiles)
- `Tower.UpdateChampionStatus(bool)`: virtual hook for debuffs on champion death
- AoE chain: `Projectile.OnAOEImpact` or ChampionHealing railgun impact → `Tower` → `TowerManager` → `GameplayScene` spawns visual
- Railgun beam visual chain: `Tower.OnRailgunShot` → `TowerManager.OnRailgunShot` → `GameplayScene` spawns `RailgunEffect`
- Projectile tracking (`Projectile.Update`): follows live target position while alive, then locks to last-known position; impact resolves on hit-radius or same-frame overshoot to avoid visual pass-through
- Wall-attack chain: `Tower.WallNetworkTargetFinder` (set per-frame by `TowerManager`) → on fire: instant `TakeDamage` + `ApplySlow(_abilityDuration)` → `Tower.OnWallAttack` → `TowerManager.OnWallAttack` → `GameplayScene` spawns `SpikeEffect`. `_abilityDuration` = `TowerStats.AbilityDuration` (ChampionWalling: 5s, Walling: 3s)
- `CanWalk`: Champions `true`, Generics `false`. `MoveSpeed`/`CooldownDuration` default `0f`, only set when `CanWalk: true`
- `TowerState` (Active/Moving/Cooldown). Moving: `_drawPosition` interpolates, `GridAnchor`/`GridPosition` update on cell arrival, origin footprint is cleared (ghost). Cooldown: timer only — tower still fires. Active+Cooldown both run `UpdateActive()`. `OnMovementComplete` → `TowerManager` re-occupies destination footprint + triggers reroute
- `TowerManager.GetPreviewPath(destTopLeft)`: `List<Point>?` for move hover — checks `CanWalk`, `Active`, `CanBuildFootprint` (ignoring self occupancy), and footprint-aware path viability. `GameplayScene.DrawTowerMovePreview` renders anchor-centered nodes/segments and a white translucent destination footprint overlay (all occupied end tiles)
- `Tower.UpdateActive()`: guarded by `Range <= 0f && WallNetworkTargetFinder == null` — skips fire loop entirely (avoids `CountdownTimer` overflow). Wall targeting uses `WallNetworkTargetFinder` delegate, deals instant damage (no projectile). `TryResolveCustomShot(...)` virtual hook allows subtype-specific instant attacks (used by ChampionHealing railgun shots)
- `TargetingStrategy` enum (`Closest`, `LowestHP`, `MostGrouped`) on `TowerStats.Targeting`/`Tower._targeting`. Gun+ChampionGun: `LowestHP`. Cannon+ChampionCannon: `MostGrouped` (count alive enemies within `AOERadius`, tie-break lowest HP). Wall bypasses `SelectTarget`
- `TowerManager.BuildAttackZone(wallSet)`: all in-bounds tiles 1 step outside `wallSet` in all 8 directions. Used by `DrawWallRangeIndicatorForSet`, `FindWallNetworkTarget`, `UpdateWallFrenzy`. Target preference: closest non-slowed → closest slowed
- `WallSegmentTower.ApplyDecayDamage(float dt)`: accumulates fractional HP; used by wall segment decay
- Wall segments: `TowerType.WallSegment` → `WallSegmentTower`, high movement cost, deferred spawn pipeline. `TryPlaceWallPath()` reserves valid path tiles first (`ReservedForPendingWallBy`), then spawns segments sequentially as each prior segment reaches cap. Spawned segment starts at low max/current HP and grows via `WallSegmentTower.InitializeWallGrowth()` + `StartWallGrowth()`. On anchor loss: pending reservations canceled and active segment growth stopped. If active segment dies pre-cap: remaining pending reservations in that chain are canceled. `_wallConnectedSets: Dictionary<Tower, HashSet<Point>>` — rebuilt each `Update` as BFS per `WallingTower` seeded from `OccupiedTiles` (champion walling uses all 4 anchor footprint tiles); disconnected towers don't share zones. `UpdateWallDecay()` unions all sets; orphaned segments decay 1 HP/sec per exposed cardinal side. Runs before dead-tower sweep
- `TowerManager.DrawPendingWallReservations()`: renders ghost tiles for reserved-but-not-yet-spawned wall segments
- `TowerManager.IsAdjacentToWallingNetwork(Point, Tower)`: public; uses `_wallConnectedSets[tower]` cache, falls back to fresh BFS if called before first `Update`
- `TowerManager.TryPlaceWallPath(path, tower)`: validates contiguous prefix, reserves pending path, and starts deferred sequential spawning. `GetWallPathValidPrefixLength(path, tower)` simulates contiguous prefix validity without mutating map state (preview/corner choice only)
- `TowerStats.AbilityEffect: Action<Tower>?`: called by `TriggerChampionAbility()` on champion + mapped generic (when a generic variant exists). ChampionWalling always buffs all Walling generics even if champion is dead. ChampionHealing uses custom handling in `TowerManager` (15s global support ult) instead of `AbilityEffect`. Lambdas that target subclass methods downcast explicitly (e.g. `((WallingTower)tower).ActivateFrenzy(...)`) — safe because the factory guarantees the correct subtype per `TowerType`
- `Tower.ActivateAbilityBuff(damageMult, fireRateSpeedMult)`: saves originals via `_hasStoredAbilityStats` guard, sets `IsAbilityBuffActive = true`, runs for `AbilityDuration`. Re-trigger resets timer, no stacking. `DeactivateAbilityBuff()` only restores if guard set. Gold aura while active
- `CannonChampionTower.ActivateLaser()`: sets `IsAbilityBuffActive = true` + `IsLaserActive = true`; suppresses projectile firing via `IsFiringSuppressed` override. `Tower.CancelAbility()`: public wrapper that calls `DeactivateAbilityBuff()` if active; `OnAbilityDeactivated()` hook clears `IsLaserActive`
- Laser ability chain: `TriggerChampionAbility(type, enemies)` resolves initial target (last living target → closest enemy → 1 tile left of tower) → fires `TowerManager.OnLaserActivated` → `GameplayScene` spawns `LaserEffect`. Interruption handled by centralized `EndChampionUltEffects` (see below)
- Champion ult cleanup: `EndChampionUltEffects(championType)` centralizes all ult teardown — cancels ability buffs on champion + matched generics, clears frenzy timers, ends healing support ult, cancels remaining healing railgun shots, and fires `OnLaserCancelled` if applicable. Called from `RemoveTower` (sell/death) and `EndChampionUltsWithDeadCasters` (per-frame check before tower updates). `MoveTower` still cancels the laser directly for mid-move interruption
- `LaserEffect` (`Entities/LaserEffect.cs`): self-contained wind-up + beam effect. `_currentContact` tracks live damage point; beam renders from `BeamOrigin` (top-right off-screen) to `_currentContact`. `GameplayScene` sets redirect targets via left-click drag while the beam is selected; right-click deselects laser selection. `Cancel()` sets `IsActive = false`
- `WallingTower.ActivateFrenzy(float duration)`: sets `IsAbilityBuffActive = true` + `_abilityTimer`, no stat change. `UpdateWallFrenzy(WallingTower, wallSet)` multi-hits all enemies in attack zone at `tower.EffectiveFireInterval` (so global attack-speed buffs apply). `WallNetworkTargetFinder` nulled during frenzy to prevent double-hits
- ChampionHealing ultimate (`TowerManager`): mode-conditional with shared cooldown. Healing mode starts a timed support window (instant drone refill, free drone healing, external attack-speed multiplier for attacking towers). Attack mode arms 5 powered railgun shots on the champion: instant hitscan, +75% base damage to the main target, 50% base beam pass-through damage, cannon-sized impact AoE for 50% base damage, and 1.5x base fire interval while empowered. Active healing support ult is terminated immediately when mode switches to attack; switching attack->healing cancels remaining railgun shots; sell/destroy cleanup is centralized via `EndChampionUltEffects`
- `TowerStats.AbilityCooldown`: per-champion ability CD; generics default 0
- Ability flow: `UIPanel.OnAbilityTriggered` → `GameplayScene` → `ChampionManager.StartAbilityCooldown()` + `TowerManager.TriggerChampionAbility()`
- `Tower.DrawPosition`: interpolated visual position. `WorldPosition`: anchor-snapped (center of 1x1 or 2x2 footprint)
- `TowerManager.MoveTower(tower, destTopLeft)`: clear origin footprint → reserve destination footprint → reroute → `StartMoving()`. `HandleMovementComplete`: clear destination reservation footprint → re-occupy footprint → reroute. `RemoveTower()` calls `ClearReservationFor()` on mid-move death
- `TowerPathfinder`: Dijkstra via `Pathfinder.ComputeHeatMap()` over top-left footprint tiles. Each candidate step validates the full footprint (bounds/terrain/reservations), so 2x2 champions cannot pass through 1-tile corridors. Mixed Path/HighGround footprints are still blocked at champion placement and movement destination checks via `CanBuildFootprint(..., requireUniformTileType: true)`

## Map Loading
- Tiled `.tmx` in `Content/Maps/`. `TmxLoader.TryLoad(id)` → `MapData.TileGrid` (column-major `[col,row]`)
- `MapDataRepository.GetAvailableMaps()` scans `Content/Maps/*.tmx` at runtime
- Tile sizing is decoupled: `GameSettings.TileSize` is display tile size (32), `GameSettings.TerrainSourceTileSize` is spritesheet source tile size (32)
- `TextureManager.DrawTile()`: `terrain32` spritesheet (col = `(int)TileType`) sampled at source size and scaled to display size; fallback colored rect
- `TmxLoader` converts object pixel coords to grid coords using TMX `tilewidth`/`tileheight` (not `GameSettings.TileSize`)
- Multi-spawn/exit: `MapData.SpawnPoints/ExitPoints: Dictionary<string, Point>`. Lane pairing: `spawn_a` → `exit_a` by suffix; fallback first exit. `Map.ActivePaths`: spawn name → path. `Enemy._spawnName` preserved on reroute via `Map.ComputePathFromPosition()`

## Rendering/Window
- `GameplayScene.Draw()` runs two `SpriteBatch` passes: world-space with `_worldMatrix` translation, then screen-space for panel/overlays
- Placement hover range preview is computed per-frame in `DrawHoverIndicator` from `UIPanel.SelectedTowerType` via `TowerData.GetStats(...)` (no cached range field in `GameplayScene`)
- Gameplay input converts screen coords to world coords (`ScreenToWorld`) before world hit tests and grid conversion
- Startup window mode is windowed maximized (not fullscreen); `GameSettings.ScreenWidth/ScreenHeight` sync to final client bounds

## Tile System
- `TileType`: HighGround, Path, Rock. Stats: `Engine/TileTypes/<Type>Tile.cs`; registry: `TileData.GetStats()`

## ChampionManager
- One alive champion per type. Global 10s CD after any placement. Individual 15s respawn CD per type
- Generics placeable only if champion variant alive (for mapped generic/champion pairs)
- Champion death → `UpdateChampionStatus(false)` on all matching generics
- API: `GlobalCooldownRemaining`, `GetRespawnCooldown(type)`, `IsChampionAlive(type)`, `StartAbilityCooldown(type)`, `GetAbilityCooldownRemaining(type)`, `IsAbilityReady(type)`

## UIPanel
- `UISelectionMode`: `None`, `PlaceTower`, `PlaceHighGround`, `SpawnEnemy`
- Consolidated buttons for Gun/Cannon/Walling: champion mode when dead; generic mode when alive. `HandleConsolidatedTowerClick()` dispatches. Sub-labels: "Place Champion" (green), "Global/Respawn: X.Xs" (yellow), "Locked: X.Xs" (orange-red when placement pool CD > 0). Placement blocked (click ignored, swatch grayed) while pool CD > 0
- ChampionHealing uses a dedicated champion-only button (`HandleChampionOnlyTowerClick`) plus standard ability button
- `Draw()` and `HandleClick()` accept `IReadOnlyDictionary<TowerType, float> cooldowns` (pool key → remaining seconds) instead of a resource counter
- Ability button per type: disabled (no champion) / CD with timer / ready (green). Fires `OnAbilityTriggered` only when `IsAbilityReady()`
- Tower info panel fire-rate line reads `Tower.EffectiveFireInterval` (so active speed buffs are reflected in displayed APS)
- Debug: Place High Ground (grid click mode), Spawn Enemy (instant)
- Time-slow toggle: `IsTimeSlowed` property (set by `HandleClick`); `CanActivateTimeSlow` gating property (set by `GameplayScene` each frame); `ForceDeactivateTimeSlow()` called by `GameplayScene` when bank hits 0. `Draw()` accepts `timeSlowBankFraction` (0–1) for bar rendering. Bank logic and constants live in `GameplayScene` — UIPanel is purely presentational

## Wall Placement Mode
- `_wallPlacementMode`: toggled by 18×18 "+" button top-right of selected walling tower (champion or generic). In wall mode, left press/drag/release builds a single-corner Manhattan L path. L direction locks based on which axis the user drags first from the start tile (`_wallDragLockedHorizontalFirst`); returning to a straight line resets the lock. Falls back to the other candidate only if it has a strictly longer valid prefix (blocked tiles). On release, `TryPlaceWallPath()` commits, reserves valid tiles, and starts deferred growth from the first segment. Blocks right-click move
- `GetWallPlacementButtonRect(tower)`: uses `TowerDrawingHelper.GetVisualBounds` for champion anchor/sprite anchoring and correct 2x2 footprint-aligned placement.
- Hover: `DarkGreen * 0.5f` if buildable + adjacent; `Red * 0.3f` otherwise. During drag, preview draws valid prefix in dark green and blocked remainder in red
- Clears on: ESC, tower/enemy selection change, UI `SelectedTowerType` set, tower sold

## World-Space Tower Controls
- Selected tower always draws an in-world sell `X` button.
- Selected `HealingChampionTower` also draws an in-world mode button directly under sell. The icon switches between healing and attack glyphs; while mode cooldown is active, button is disabled and shows remaining seconds.

## Enemy Selection
- `GameplayScene._selectedEnemy` via `GetEnemyAt()` (15px radius). Mutually exclusive with tower. Auto-clears on death/end

## Wave System
- `Content/Waves/{mapId}.json` via `WaveLoader.TryLoad()`. Fallback: `FallbackWaves()` in `GameplayScene`
- Schema: `{ waves: [ { wave, spawns: [ { at, spawnPoint, name, health, speed, attackDamage, color } ] } ] }`. `at` = seconds from wave start
- `WaveManager(Func<string, List<Point>?>, List<WaveData>)`: dequeues by elapsed time; wave ends when list empty
- `WaveLoader.ParseColor(string)`: XNA `Color` by name via reflection

## TextureManager
- `DrawSprite()`: optional `origin` param (default 0.5,0.5 centered)
- `ChampionGunTowerSprite`, `ChampionCannonTowerSprite`, `ChampionWallingTowerSprite`, and `ChampionHealingTowerSprite`: canonical champion textures loaded from `Content/Sprites/Towers/champion_<type>.png`
- `GenericGunTowerSprite`, `GenericCannonTowerSprite`, and `GenericWallingTowerSprite`: canonical generic textures loaded from `Content/Sprites/Towers/generic_<type>.png`
- `DrawRect()`: top-left origin. `DrawSprite()`: centered origin. Do not confuse
