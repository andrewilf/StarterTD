# Project Manifest

## Active Systems
- Core loop, stack-based scene management, 3 maps
- Pause: P to toggle, ESC/Resume button
- MonoGame.Extended: `CountdownTimer` for cooldowns, `RectangleF` for UI bounds
- Dijkstra pathfinding with per-enemy rerouting (`Math.Max` tile/tower costs prevents pathing through HighGround towers)
- 4 towers: 2 Generic (Gun, Cannon, cost gold) + 2 Champions (ChampionGun, ChampionCannon, free)
- ChampionManager: global 10s CD, 15s respawn CD, one-per-type limit, generics require alive champion
- UI: champion/generic button states reflect cooldowns and champion alive status
- Enemy FSM (Moving/Attacking), 5 hardcoded waves
- Info panel: click tower/enemy for stats overlay (bottom-right). Dismiss: ESC/empty tile/new selection
- Selection indicators: red outline, auto-deselect on death/end
- Floating text, range indicators, AoE visuals, victory/defeat flow

## Backlog

### High Priority
- [ ] Champion debuff: implement `Tower.UpdateChampionStatus()` stat changes
- [ ] Sprite integration: replace rects with `Texture2D`
- [ ] Sound: SFX + BGM manager

### Gameplay
- [ ] Enemy variants: Fast (low HP), Tank (high HP)
- [ ] Tower abilities: Slow, Splash, Poison
- [x] Sell: right-click (60% base refund scaled by HP). Champions trigger death CD
- [x] Pause

### UI Polish
- [ ] Enemy health bars
- [x] Range rings on hover/placement
- [ ] Wave preview
