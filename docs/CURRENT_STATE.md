# Project Manifest

## Active Systems
- Core loop, stack-based scene management, Tiled `.tmx` maps (dynamic — add/remove files + `bash sync_maps.sh`)
- Pause: P to toggle, ESC/Resume button
- MonoGame.Extended: `CountdownTimer` for cooldowns, `RectangleF` for UI bounds
- Dijkstra pathfinding with per-enemy rerouting (`Math.Max` tile/tower costs prevents pathing through HighGround towers)
- `TowerPathfinder`: tower-specific Dijkstra (Path=1, HighGround=2, occupied=10, Rock=impassable; ignores enemies)
- 4 towers: 2 Generic (Gun, Cannon, cost gold) + 2 Champions (ChampionGun, ChampionCannon, free)
- ChampionManager: global 10s CD, 15s respawn CD, one-per-type limit, generics require alive champion; 15s per-champion ability CD
- Champion super abilities: click ability button → 5s buff (damage × mult, fire rate × mult) on champion + all its generics; 15s CD after use
- UI: consolidated tower buttons (one per type) — champion mode when dead, generic mode when alive; cooldown/affordability sub-labels; ability button per tower type (disabled/CD/ready states)
- Tower state machine: `TowerState` enum (Active, Moving, Cooldown) with `Update()` dispatch
- Enemy FSM (Moving/Attacking), 5 hardcoded waves
- Info panel: click tower/enemy for stats overlay (bottom-right). Dismiss: ESC/empty tile/new selection
- Selection indicators: red outline, auto-deselect on death/end
- Floating text, range indicators, AoE visuals, victory/defeat flow
- Debug sidebar: Place High Ground tiles, Spawn Enemy (uses current wave stats)

## Backlog

### High Priority
- [ ] Champion debuff: implement `Tower.UpdateChampionStatus()` stat changes
- [x] Sprite integration: terrain tiles use `terrain.png` spritesheet via `TextureManager.DrawTile()`
- [ ] Sound: SFX + BGM manager

### Gameplay
- [x] Tower walking: walkable towers (`CanWalk: true`) move on right-click (select → right-click empty buildable tile). Gold path preview on hover. Destination is reserved at commit time (`Tile.ReservedByTower`) — blocks placement and second-walker targeting mid-transit

- [ ] Enemy variants: Fast (low HP), Tank (high HP)
- [ ] Tower abilities: Slow, Splash, Poison
- [x] Sell: right-click (60% base refund scaled by HP). Champions trigger death CD
- [x] Pause

### UI Polish
- [ ] Enemy health bars
- [x] Range rings on hover/placement
- [ ] Wave preview
