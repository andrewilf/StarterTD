# Project Manifest

## Active Systems
- Core loop, scene management, map selection (3 maps)
- Dijkstra pathfinding with per-enemy rerouting
- 4 tower types: 2 Generic (Gun, Cannon) + 2 Champions (ChampionGun, ChampionCannon)
  - Generic towers cost gold, Champions are free (cost 0)
  - Champions render 1.5x taller (DrawScale) using same texture
  - Only one Champion of each type can be placed simultaneously
- Enemy state machine (Moving/Attacking)
- 10 hardcoded waves
- UI panel with tower selection buttons for Generics and Champions
- Floating text, range indicators, AoE visuals
- Victory/Defeat flow

## Backlog / To-Do

### High Priority
- [ ] **Champion Debuff System**: When a Champion dies, apply stat debuffs to all active Generic towers (via `UpdateChampionStatus()` virtual hook).
- [ ] **Sprite Integration**: Replace rects with `Texture2D` assets.
- [ ] **Sound**: Basic SFX (shoot, hit) and BGM manager.

### Gameplay
- [ ] **Enemy Variants**: Fast (Low HP) and Tank (High HP).
- [ ] **Tower Abilities**: Slow, Splash, Poison.
- [ ] **Sell**: Refund mechanic.
- [ ] **Pause**: Toggle game state.

### UI Polish
- [ ] **Health Bars**: Enemy overlays.
- [x] **Range Rings**: Visual radius on hover and placement preview.
- [ ] **Wave Preview**: "Next Wave" info.
