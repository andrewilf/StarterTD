# Project Manifest

## Active Systems
- Core loop, stack-based scene management, Tiled `.tmx` maps (dynamic — drop files in `Content/Maps/`, no code changes)
- Pause: P to toggle, ESC/Resume button
- MonoGame.Extended: `CountdownTimer` for cooldowns, `RectangleF` for UI bounds
- Dijkstra pathfinding with per-enemy rerouting (`Math.Max` tile/tower costs prevents pathing through HighGround towers). Enemies store `_spawnName` and pass it on reroute so multi-lane maps keep correct exit assignment after tower changes
- `TowerPathfinder`: tower-specific Dijkstra (ignores enemies; tile costs defined in stats files)
- 5 towers: 3 Generic (Gun, Cannon, Walling) + 3 Champions (ChampionGun, ChampionCannon, ChampionWalling). No gold economy — placement is throttled by per-pool cooldown timers (see placement cooldown system below)
- ChampionWalling: Places wall segments (low HP, grow over time to max HP, very high movement cost) adjacent to its network. Drag/click path reserves future tiles first (`ReservedForPendingWallBy`) so they cannot be built on, then segments spawn sequentially along the path as each prior segment finishes growing (visual "grow outward" behavior). Reserved-only tiles do not affect pathing until a segment actually spawns. If the anchor is removed/dies, pending reservations for that anchor are cancelled and current active growth on that anchor stops. If the currently growing segment is destroyed before completion, remaining pending tiles in that chain are released. Segments BFS-connected to any walling anchor are protected from decay; orphaned segments decay per exposed cardinal side. World-space "+" button toggles wall-placement mode; click-drag creates a single-corner L path (preview while dragging, commit on release). L direction locks to whichever axis the user drags first; returning to a straight line resets the lock. Falls back to the other L orientation only if it has a strictly longer valid prefix (blocked tiles); placement stops at first invalid tile
- ChampionWalling attack: Attack zone = all tiles 1 step outside the connected wall network in all 8 directions. Instant spike damage + timed slow on hit. Prefers non-slowed targets. `SpikeEffect` visual spawns at hit tile. Hover shows white strip over attack zone
- ChampionWalling super ability: Timed frenzy. All enemies in attack zone hit at fire rate. Normal single-target suppressed during frenzy. Gold aura visible while active
- Walling generic: Stationary. Each tower gets a single-root BFS from its own position; attack zone = tiles 1 step outside that connected set. Can place wall segments adjacent to its own wall network (same "+" button UI and L-drag behavior as champion). Super ability triggers independent frenzy per generic. Wall decay protection covers all walls reachable from any walling anchor (champion or generic)
- Wall visuals: generic Walling anchor uses `Color.DarkGreen`; placed `WallSegment` tiles use `Color.Green` for clearer differentiation
- Enemy slow debuff: `IEnemy.ApplySlow(float)` / `IsSlowed`. Timer-based; only refreshes if new duration exceeds remaining time (prevents shorter hits cutting off a longer slow). Slowed enemies render with a blue tint (`CornflowerBlue` 50% blend)
- ChampionManager: global placement CD, per-type respawn CD, one-per-type limit, generics require alive champion; per-champion ability CD (from `TowerStats.AbilityCooldown`)
- Champion super abilities: click ability button → per-type buff duration (from `TowerStats.AbilityDuration`) on champion + all its generics; per-champion CD after use
- ChampionCannon super ability: Deploys a `LaserEffect` — two-phase (wind-up then beam). Diagonal beam from top-right off-screen origin, targeting the last-targeted living enemy (fallback: closest enemy; fallback: 1 tile left of tower). Auto-tracks nearest enemy; left-click beam to select it, right-click to redirect. Pulsed AoE damage at the contact point. Suppresses normal cannon firing. Interrupted immediately if the tower moves or is destroyed
- Placement cooldown system: each tower type has a shared pool (`_placementCooldowns` in `GameplayScene`). On placement: `pool += BaseCooldown + CooldownPenalty × (towers already in pool)`. On sell: `pool -= CooldownPenalty` (capped at 0). All champions share one pool (keyed on `ChampionGun`). Pools tick down using scaled game time (time-slow extends the wait). `GameplayScene.GetCooldownPoolKey()` maps any champion type → `ChampionGun`
- UI: consolidated tower buttons (one per type) — champion mode when dead, generic mode when alive; cooldown/"Locked" sub-labels; ability button per tower type (disabled/CD/ready states)
- Tower state machine: `TowerState` enum (Active, Moving, Cooldown) with `Update()` dispatch
- Tower targeting strategies: `TargetingStrategy` enum on `TowerStats`/`Tower`. Gun types: `LowestHP`. Cannon types: `MostGrouped` (most enemies within AoE radius, tie-break lowest HP). Default: `Closest`
- Enemy FSM (Moving/Attacking); waves driven by `Content/Waves/{mapId}.json` (fallback to hardcoded waves if no file)
- Multi-spawn support: maps can define multiple named spawn/exit points (`spawn_a`/`exit_a`, etc.); each gets an independent path. Wave JSON assigns each enemy to a named spawn point
- Info panel: click tower/enemy for stats overlay (bottom-right). Dismiss: ESC/empty tile/new selection
- Selection indicators: yellow outline, auto-deselect on death/end. Single-selection invariant enforced via `DeselectAll()` — selecting any object (tower, enemy, laser beam) clears all others
- Range indicators, AoE visuals, victory/defeat flow
- Debug sidebar: Place High Ground tiles, Spawn Enemy
- Time-slow: toggle button in UI panel scales all game systems (enemies, towers, effects, wave spawning) to half speed via a scaled `GameTime`. Has a regenerating bank — drains while active, regens while inactive. Auto-deactivates at 0. Activation blocked below a minimum threshold. Bank tracks real (wall-clock) time, unaffected by the slowdown itself

## Backlog

See `docs/TODO.md` for the full prioritized backlog.
