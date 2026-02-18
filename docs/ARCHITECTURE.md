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
`Map`, `WaveManager`, `ChampionManager`, `TowerManager`, `InputManager`, `UIPanel`, `FloatingTexts`, `AoEEffects`

## Tower System
- `TowerType` enum: Generic (Gun, Cannon) + Champion (ChampionGun, ChampionCannon)
- Stats per type in `Entities/Towers/<Type>Tower.cs`; registry: `TowerData.GetStats()`
- Variant mapping: `TowerType.GetChampionVariant()` / `GetGenericVariant()`
- Towers overlay tiles: `Tile.OccupyingTower` ref; `TileType` immutable after init
- `DrawScale`: Generics {1,1}, Champions {1,1.5}. Champions use bottom-center origin (0.5,1.0), Y offset grows upward. Bars use `SpriteSize * DrawScale.Y`
- `Tower.Draw()`: origin conditional on `DrawScale.Y > 1.0f`; champions offset Y by `SpriteSize / 2f`
- `Tower.UpdateChampionStatus(bool)`: virtual hook for debuffs on champion death
- AoE chain: `Projectile.OnAOEImpact` → `Tower` → `TowerManager` → `GameplayScene` spawns visual
- `CanWalk` flag on `TowerStats` (and mirrored on `Tower`) gates movement. Champions: `true`; Generics: `false`. `MoveSpeed`/`CooldownDuration` are optional stats (default `0f`) — only specified when `CanWalk: true`
- State machine: `TowerState` (Active/Moving/Cooldown). Moving: `_drawPosition` interpolates tile-to-tile, `GridPosition` updates on cell arrival, origin tile cleared (ghost). Cooldown: move-ready timer only — tower still fires. Both Active+Cooldown run `UpdateActive()`. `OnMovementComplete` callback lets `TowerManager` re-occupy destination tile and trigger reroute
- `TowerManager.GetPreviewPath(dest)`: returns `List<Point>?` for hover preview — checks `CanWalk`, `Active` state, `CanBuild`. `GameplayScene` calls per-frame, draws gold dot+line path overlay between map and tower layers
- `Tower.DrawPosition`: smooth visual position during movement (use instead of `WorldPosition` for visual tracking). `WorldPosition` always snapped to grid
- `TowerManager.MoveTower(tower, dest)`: ghost origin tile → reroute enemies → `StartMoving()`. `HandleMovementComplete`: re-occupy dest → reroute enemies
- `TowerPathfinder`: tower-specific Dijkstra via `Pathfinder.ComputeHeatMap()` with custom cost function (Path=1, HighGround=2, occupied tower=10, Rock=impassable). Ignores enemies

## Tile System
- `TileType` enum: HighGround, Path, Rock
- Stats in `Engine/TileTypes/<Type>Tile.cs`; registry: `TileData.GetStats()`

## ChampionManager
- One champion per type alive at a time
- Global 10s cooldown after any champion placed
- Individual 15s respawn cooldown per type after death
- Generics placeable only if their champion variant alive
- Champion death calls `UpdateChampionStatus(false)` on matching generics
- Public API: `GlobalCooldownRemaining`, `GetRespawnCooldown(type)`, `IsChampionAlive(type)`

## UIPanel
- `UISelectionMode` enum: `None`, `PlaceTower`, `PlaceHighGround`, `SpawnEnemy`
- Champion buttons: "Limit Reached" (alive), "Global: X.Xs", "Respawn: X.Xs"
- Generic buttons: grayed + "Champion Dead" when respawn active; just grayed at game start
- `DrawGenericTowerButton()` helper deduplicates champion-death logic
- Debug buttons: Place High Ground (grid click mode), Spawn Enemy (instant, uses `WaveManager.CurrentWaveDefinition`)

## Enemy Selection
- `GameplayScene._selectedEnemy` via `GetEnemyAt()` (15px radius). Mutually exclusive with tower selection. Auto-clears on death/end

## Other
- `TextureManager.DrawSprite()`: optional `origin` param (default 0.5,0.5 centered)
- `WaveManager`: takes `Func<List<Point>>` for latest path per spawn
