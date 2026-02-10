# Project Manifest

## Active Systems (Implemented)
- **Core Loop**: `Game1` → `SceneManager` → `MapSelectionScene` → `GameplayScene` (Mediator).
- **Rendering**: Colored rectangles (placeholder). `TextureManager` uses centered origins.
- **Map Selection**: Pre-game scene showing 3 maps (classic_s, straight, maze_test) with visual previews. Supports hover and click selection.
- **Map**: 3 map types with different terrain layouts. Maps defined in `MapDataRepository` via `MapData` record with `SpawnPoint`, `ExitPoint`, `WalkableAreas` (list of rectangles). TileType: `HighGround` (impassable, buildable), `Path` (walkable cost 1, buildable), `Occupied` (expensive cost 500, passable). No predefined path lists — Dijkstra computes routes from tile costs.
- **Pathfinding**: Dijkstra flood-fill heat map (`Pathfinder.ComputeHeatMap`) from exit to all tiles. Path extracted via gradient descent (`ExtractPath`). Towers are cost 500 (expensive but passable) — path is never fully blocked. Heat map recomputed on every tower placement; all enemies rerouted via `UpdatePath()`.
- **Towers**: 3 Types (Gun, Cannon, Sniper). Supports placement (L-Click) and Upgrading (R-Click).
- **Enemies**: Base class only. Follows path.
- **Waves**: 10 hardcoded waves.
- **UI**: Side panel (Selection, Start Wave). Floating Text feedback (Money +/-).
- **Game Flow**: Map Selection → Play → Victory/Defeat Screens (Return to Map Selection via 'R').

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
- [ ] **Range Rings**: Visual radius on hover.
- [ ] **Wave Preview**: "Next Wave" info.