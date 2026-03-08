# Project Manifest

## Active Systems
- Core loop, stack-based scene management, Tiled `.tmx` maps (dynamic — drop files in `Content/Maps/`, no code changes)
- Display starts in windowed maximized mode (not fullscreen). Main map-selection screen includes a clickable Exit button
- Map selection layout auto-reflows to current viewport size; map preview tiles scale to fit card bounds
- Gameplay rendering uses world-space + screen-space spritebatch passes; map is centered via `_worldMatrix` translation and gameplay input is converted through `ScreenToWorld`
- Tile sizes are decoupled: display tile size is 32 while terrain spritesheet source tile size stays 40; TMX object marker conversion uses TMX tilewidth/tileheight
- Pause: P to toggle, ESC/Resume button
- MonoGame.Extended: `CountdownTimer` for cooldowns, `RectangleF` for UI bounds
- Dijkstra pathfinding with per-enemy rerouting (`Math.Max` tile/tower costs prevents pathing through HighGround towers). Enemies store `_spawnName` and pass it on reroute so multi-lane maps keep correct exit assignment after tower changes
- `TowerPathfinder`: tower-specific Dijkstra with footprint-aware movement validation (1x1 generics, 2x2 champions). Path steps must fit the full footprint
- 7 tower types: 3 Generic (Gun, Cannon, Walling) + 4 Champions (ChampionGun, ChampionCannon, ChampionWalling, ChampionHealing). No gold economy — placement is throttled by per-pool cooldown timers (see placement cooldown system below)
- Champion towers are true 2x2 blockers. Placement/movement uses center-anchor snapping (nearest valid 2x2 centerpoint), while generics remain 1x1. Champion placement and movement destinations require a uniform buildable tile type across the full 2x2 base (intermediate movement steps may cross mixed terrain during traversal)
- Champion placeholder render size tracks footprint scale: `2 * GameSettings.TileSize` (currently `64x64` with `TileSize=32`)
- ChampionWalling: Places wall segments (low HP, grow over time to max HP, very high movement cost) adjacent to its network. Drag/click path reserves future tiles first (`ReservedForPendingWallBy`) so they cannot be built on, then segments spawn sequentially along the path as each prior segment finishes growing (visual "grow outward" behavior). Reserved-only tiles do not affect pathing until a segment actually spawns. If the anchor is removed/dies, pending reservations for that anchor are cancelled and current active growth on that anchor stops. If the currently growing segment is destroyed before completion, remaining pending tiles in that chain are released. Segments BFS-connected to any walling anchor are protected from decay; orphaned segments decay per exposed cardinal side. World-space "+" button toggles wall-placement mode; click-drag creates a single-corner L path (preview while dragging, commit on release), and successful placement exits walling mode. L direction locks to whichever axis the user drags first; returning to a straight line resets the lock. Falls back to the other L orientation only if it has a strictly longer valid prefix (blocked tiles); placement stops at first invalid tile
- ChampionWalling attack: Attack zone = all tiles 1 step outside the connected wall network in all 8 directions. Instant spike damage + timed slow on hit. Prefers non-slowed targets. `SpikeEffect` visual spawns at hit tile. Hover shows white strip over attack zone
- ChampionWalling super ability: Timed frenzy. All enemies in attack zone hit at fire rate. Normal single-target suppressed during frenzy. Gold aura visible while active
- ChampionHealing: champion-only support tower that deploys multiple `HealingDrone` companions. Drones have an energy pool; healing ticks consume energy while restoring target HP. Drones pick closest damaged valid allies (excluding wall segments and the owner champion), retarget on full heal, return to owner when empty to recharge, and redeploy once full. Extras dock at the champion when no valid targets remain. Drones cannot heal the same tower simultaneously. Drone pathing uses uniform-cost traversal across all in-bounds tiles and follows walking towers while attached
- Walling generic: Stationary. Each tower gets a BFS seeded from its occupied anchor tiles (champion walling seeds all 4 footprint tiles; generic walling seeds 1 tile); attack zone = tiles 1 step outside that connected set. Can place wall segments adjacent to its own wall network (same "+" button UI and L-drag behavior as champion). Super ability triggers independent frenzy per generic. Wall decay protection covers all walls reachable from any walling anchor (champion or generic)
- Wall visuals: generic Walling anchor uses `Color.DarkGreen`; placed `WallSegment` tiles use `Color.Green` for clearer differentiation
- Tower sprites are loaded from canonical art assets:
  - Generic towers: `Content/Sprites/Towers/generic_{gun,cannon,walling}.png`
  - Champion towers: `Content/Sprites/Towers/champion_{gun,cannon,walling}.png`
- All tower sprites render with their configured draw sizing and anchors; no legacy fallback file names are used.
- Champion occupancy guide now uses translucent gray for all footprints before tower sprite/body draw, keeping footprint readable under overhanging art.
- Black sprite outlines are no longer drawn for tower bodies.
- Enemy slow debuff: `IEnemy.ApplySlow(float)` / `IsSlowed`. Timer-based; only refreshes if new duration exceeds remaining time (prevents shorter hits cutting off a longer slow). Slowed enemies render with a blue tint (`CornflowerBlue` 50% blend)
- ChampionManager: global placement CD, per-type respawn CD, one-per-type limit, generics require alive champion; per-champion ability CD (from `TowerStats.AbilityCooldown`)
- Champion super abilities: click ability button starts per-champion cooldown (`TowerStats.AbilityCooldown`) and applies per-type effect. Gun/Cannon/Walling retain buff-driven effects; ChampionHealing currently uses a placeholder no-op gameplay effect with a short visual aura window
- ChampionCannon super ability: Deploys a `LaserEffect` — two-phase (wind-up then beam). Diagonal beam from top-right off-screen origin, targeting the last-targeted living enemy (fallback: closest enemy; fallback: 1 tile left of tower). Auto-tracks nearest enemy; left-click beam to select it, left-click-drag to set redirect destination with red preview line, right-click deselects selected laser. Pulsed AoE damage at the contact point. Suppresses normal cannon firing. Interrupted immediately if the tower moves or is destroyed
- Placement cooldown system: each tower type has a shared pool (`_placementCooldowns` in `GameplayScene`). On placement: `pool += BaseCooldown + CooldownPenalty × (towers already in pool)`. On sell: `pool -= CooldownPenalty` (capped at 0). All champions share one pool (keyed on `ChampionGun`). Pools tick down using scaled game time (time-slow extends the wait). `GameplayScene.GetCooldownPoolKey()` maps any champion type → `ChampionGun`
- UI: consolidated tower buttons for Gun/Cannon/Walling (champion mode when dead, generic mode when alive), plus a dedicated champion-only Healing Champion button; cooldown/"Locked" sub-labels; ability button per champion type (disabled/CD/ready states)
- Tower state machine: `TowerState` enum (Active, Moving, Cooldown) with `Update()` dispatch
- Movement input: selected walkable champions move with left-click drag-and-release. Drag can start from any occupied champion tile and destination snaps footprint-aware; move preview/path nodes are anchor-centered and the destination occupied footprint is shown with a white translucent overlay; right-click no longer redirects laser, it deselects the selected laser only
- Sell flow: selected tower exposes an in-world `X` button next to it for selling; right-click no longer triggers any tower sell action
- Tower targeting strategies: `TargetingStrategy` enum on `TowerStats`/`Tower`. Gun types: `LowestHP`. Cannon types: `MostGrouped` (most enemies within AoE radius, tie-break lowest HP). Default: `Closest`
- Enemy FSM (Moving/Attacking); waves driven by `Content/Waves/{mapId}.json` (fallback to hardcoded waves if no file)
- Multi-spawn support: maps can define multiple named spawn/exit points (`spawn_a`/`exit_a`, etc.); each gets an independent path. Wave JSON assigns each enemy to a named spawn point
- Info panel: click tower/enemy for stats overlay (bottom-right). Dismiss: ESC/empty tile/new selection
- Selection indicators: yellow outline, auto-deselect on death/end. Single-selection invariant enforced via `DeselectAll()` — selecting any object (tower, enemy, laser beam) clears all others
- Range indicators, AoE visuals, victory/defeat flow
- Placement range preview is derived at draw-time from `UIPanel.SelectedTowerType` stats (no cached range state), so champion and generic non-zero-range tower selections render consistently; walling (`Range = 0`) shows no circle
- Debug sidebar: Place High Ground tiles, Spawn Enemy
- Time-slow: toggle button in UI panel scales all game systems (enemies, towers, effects, wave spawning) to half speed via a scaled `GameTime`. Has a regenerating bank — drains while active, regens while inactive. Auto-deactivates at 0. Activation blocked below a minimum threshold. Bank uses real (wall-clock) time, unaffected by the slowdown itself

## Backlog

See `docs/TODO.md` for the full prioritized backlog.
