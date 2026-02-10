# Project Manifest

## Active Systems (Implemented)
- **Core Loop**: `Game1` → `SceneManager` → `MapSelectionScene` → `GameplayScene` (Mediator).
- **Rendering**: Colored rectangles (placeholder). `TextureManager` uses centered origins.
- **Map Selection**: Pre-game scene showing 3 maps (classic_s, straight, maze_test) with visual previews. Supports hover and click selection.
- **Map**: 3 map types with different path layouts. Maps defined in `MapDataRepository`. TileType: `Buildable`, `Path`, `Occupied` only. Maze zones tracked via `Tile.MazeZone` reference.
- **Mazing**: Fully implemented. A* pathfinding (`Pathfinder.cs`). Maze test map has large buildable zone with path running through. Tower placement triggers A* rerouting around obstacles. Path block prevention (validation callback), live enemy rerouting (`Enemy.UpdatePath`), and visual debug path line (light blue dots).
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