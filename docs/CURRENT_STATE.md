# Project Manifest

## Active Systems
- Core loop, stack-based scene management, Tiled `.tmx` maps (dynamic — drop files in `Content/Maps/`, no code changes)
- Pause: P to toggle, ESC/Resume button
- MonoGame.Extended: `CountdownTimer` for cooldowns, `RectangleF` for UI bounds
- Dijkstra pathfinding with per-enemy rerouting (`Math.Max` tile/tower costs prevents pathing through HighGround towers). Enemies store `_spawnName` and pass it on reroute so multi-lane maps keep correct exit assignment after tower changes
- `TowerPathfinder`: tower-specific Dijkstra (Path=1, HighGround=2, occupied=10, Rock=impassable; ignores enemies)
- 4 towers: 2 Generic (Gun, Cannon, cost gold) + 3 Champions (ChampionGun, ChampionCannon, ChampionWalling, free)
- ChampionWalling: no generic variant. Places wall segments (30 HP, 10k movement cost) adjacent to its network. Segments BFS-connected to the champion are protected from decay; orphaned segments (disconnected by selling a bridging tile) decay at 1 HP/sec per exposed cardinal side (max 4 HP/sec). World-space "+" button on champion toggles wall-placement mode
- ChampionWalling attack: Damage 3, FireRate 1.5s. Attack zone = all tiles 1 step outside the connected wall network (champion + segments) in all 8 directions; with no walls, just the 8 adjacent tiles. Instant spike damage + 5s slow (40% speed) on hit. Prefers non-slowed targets. `SpikeEffect` visual spawns at hit tile. Hover shows white strip over attack zone
- ChampionWalling super ability: 20s CD, 10s duration. All enemies in the attack zone are hit at the normal fire rate (every 1.5s). Normal single-target attack suppressed during frenzy. Gold aura visible while active
- Enemy slow debuff: `IEnemy.ApplySlow(float)` / `IsSlowed`. Timer-based, refreshes on re-hit. Slowed enemies move at 40% speed and render with a blue tint (`CornflowerBlue` 50% blend)
- ChampionManager: global 10s CD, 15s respawn CD, one-per-type limit, generics require alive champion; per-champion ability CD (from `TowerStats.AbilityCooldown`)
- Champion super abilities: click ability button → per-type buff duration (from `TowerStats.AbilityDuration`) on champion + all its generics; per-champion CD after use
- UI: consolidated tower buttons (one per type) — champion mode when dead, generic mode when alive; cooldown/affordability sub-labels; ability button per tower type (disabled/CD/ready states); walling champion has dedicated tower button + ability button
- Tower state machine: `TowerState` enum (Active, Moving, Cooldown) with `Update()` dispatch
- Tower targeting strategies: `TargetingStrategy` enum on `TowerStats`/`Tower`. Gun types: `LowestHP`. Cannon types: `MostGrouped` (most enemies within AoE radius, tie-break lowest HP). Default: `Closest`
- Enemy FSM (Moving/Attacking); waves driven by `Content/Waves/{mapId}.json` (fallback to 5 hardcoded waves if no file)
- Multi-spawn support: maps can define multiple named spawn/exit points (`spawn_a`/`exit_a`, etc.); each gets an independent path. Wave JSON assigns each enemy to a named spawn point
- Info panel: click tower/enemy for stats overlay (bottom-right). Dismiss: ESC/empty tile/new selection
- Selection indicators: red outline, auto-deselect on death/end
- Floating text, range indicators, AoE visuals, victory/defeat flow
- Debug sidebar: Place High Ground tiles, Spawn Enemy (fixed stats: 300hp, 90spd)

## Backlog

See `docs/TODO.md` for the full prioritized backlog.
