# Project Manifest

## Active Systems (Implemented)
- **Core Loop**: `Game1` → `SceneManager` → `GameplayScene` (Mediator).
- **Rendering**: Colored rectangles (placeholder). `TextureManager` uses centered origins.
- **Map**: Hardcoded static grid with S-shaped path.
- **Towers**: 3 Types (Gun, Cannon, Sniper). Supports placement (L-Click) and Upgrading (R-Click).
- **Enemies**: Base class only. Follows path.
- **Waves**: 10 hardcoded waves.
- **UI**: Side panel (Selection, Start Wave). Floating Text feedback (Money +/-).
- **Game Flow**: Start → Play → Victory/Defeat Screens (Restart via 'R').

## Backlog / To-Do

### High Priority
- [ ] **Sprite Integration**: Replace rects with `Texture2D` assets.
- [ ] **Sound**: Basic SFX (shoot, hit) and BGM manager.
- [ ] **Main Menu**: Pre-game scene.

### Gameplay
- [ ] **Enemy Variants**: Fast (Low HP) and Tank (High HP).
- [ ] **Tower Abilities**: Slow, Splash, Poison.
- [ ] **Sell**: Refund mechanic.
- [ ] **Pause**: Toggle game state.

### UI Polish
- [ ] **Health Bars**: Enemy overlays.
- [ ] **Range Rings**: Visual radius on hover.
- [ ] **Wave Preview**: "Next Wave" info.