# Project Manifest

## Active Systems (Implemented)
- **Core Loop**: `Game1` → `SceneManager` → `MapSelectionScene` → `GameplayScene` (Mediator).
- **Rendering**: Colored rectangles (placeholder). `TextureManager` uses centered origins.
- **Map Selection**: Pre-game scene showing 3 maps (classic_s, straight, maze_test) with visual previews. Supports hover and click selection.
- **Map**: 3 map types with different terrain layouts. Maps defined in `MapDataRepository` via `MapData` record with `SpawnPoint`, `ExitPoint`, `WalkableAreas`, and optional `RockAreas` (list of rectangles). TileType: `HighGround` (impassable, buildable), `Path` (walkable cost 1, buildable), `Rock` (impassable, unbuildable). Towers are overlays tracked via `Tile.OccupyingTower` that modify movement cost without changing terrain type. No predefined path lists — Dijkstra computes routes from tile costs. All 3 current maps have rock strips at top and bottom edges.
- **Pathfinding**: Dijkstra flood-fill heat map (`Pathfinder.ComputeHeatMap`) from exit to all tiles. Path extracted via gradient descent (`ExtractPath`). Towers have per-type movement costs (Gun: 300, Cannon: 500, Sniper: 700) — enemies prefer routing through Gun towers over Sniper towers. Path is never fully blocked. Heat map recomputed on every tower placement. Per-enemy pathfinding: each enemy computes a fresh path from its current position when towers change, preventing diagonal cuts across obstacles.
- **Towers**: 3 Types (Gun, Cannon, Sniper) with distinct movement costs for strategic maze control. Supports placement (L-Click) and Upgrading (R-Click). Per-type health (Gun: 100, Cannon: 150, Sniper: 80). Health bar shown when damaged. Destroyed towers clear tile occupancy reference and trigger pathfinding recomputation.
- **Enemies**: Base class only. Follows path. Per-wave AttackDamage property (data only, no attack AI yet).
- **Waves**: 10 hardcoded waves.
- **UI**: Side panel (Selection, Start Wave). Floating Text feedback (Money +/-).
- **Game Flow**: Map Selection → Play → Victory/Defeat Screens (Return to Map Selection via 'R'). Debug: 'K' kills all towers.
- **Code Quality**: csharpier for formatting, SonarAnalyzer.CSharp for static analysis.

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