# Project Manifest

## Active Systems
- Core loop, scene management, map selection (3 maps)
- Dijkstra pathfinding with per-enemy rerouting
- 4 tower types: 2 Generic (Gun, Cannon) + 2 Champions (ChampionGun, ChampionCannon)
  - Generic towers cost gold, Champions are free (cost 0)
  - Champions render 1.5x taller (DrawScale) using bottom-center origin (grows upward)
  - Only one Champion of each type can be placed simultaneously
- **ChampionManager** system with full visual integration:
  - Global 10s cooldown blocks all champion placement after any champion is placed
  - Individual 15s respawn cooldown per champion type after death
  - Generic towers can ONLY be placed if their champion variant is alive
  - Champion death triggers `UpdateChampionStatus(false)` callback on matching generics (hook is wired, awaiting implementation)
  - **Public API for UI**: `GlobalCooldownRemaining`, `GetRespawnCooldown()`, `IsChampionAlive()`
- **UI Integration**:
  - Champion buttons show placement state: "Limit Reached" (alive), "Global: X.Xs" (global CD), "Respawn: X.Xs" (respawn CD)
  - Generic buttons gray out and show "Champion Dead" text when their champion dies (respawn active)
  - At game start, generic buttons just gray out (distinct from "champion died" state)
- Enemy state machine (Moving/Attacking)
- 10 hardcoded waves
- UI panel with tower selection buttons for Generics and Champions
- Floating text, range indicators, AoE visuals
- Victory/Defeat flow

## Backlog / To-Do

### High Priority
- [ ] **Champion Debuff Implementation**: Implement stat debuffs in `Tower.UpdateChampionStatus()` when champions die.
- [ ] **Sprite Integration**: Replace rects with `Texture2D` assets.
- [ ] **Sound**: Basic SFX (shoot, hit) and BGM manager.

### Gameplay
- [ ] **Enemy Variants**: Fast (Low HP) and Tank (High HP).
- [ ] **Tower Abilities**: Slow, Splash, Poison.
- [x] **Sell**: Right-click towers (60% base refund scaled by health). Champions trigger death cooldown.
- [ ] **Pause**: Toggle game state.

### UI Polish
- [ ] **Health Bars**: Enemy overlays.
- [x] **Range Rings**: Visual radius on hover and placement preview.
- [ ] **Wave Preview**: "Next Wave" info.
