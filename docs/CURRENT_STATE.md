# Project Manifest

## Active Systems
- Core loop, scene management, map selection (3 maps)
- Dijkstra pathfinding with per-enemy rerouting
- 2 tower types (Gun, Cannon) with blocking capacity
- Enemy state machine (Moving/Attacking)
- 10 hardcoded waves
- UI panel, floating text, range indicators, AoE visuals
- Victory/Defeat flow

## Backlog / To-Do

### High Priority
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
