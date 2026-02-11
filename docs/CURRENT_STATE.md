# Project Manifest

## Active Systems (Implemented)
- **Core Loop**: `Game1` → `SceneManager` → `MapSelectionScene` → `GameplayScene` (Mediator).
- **Rendering**: Colored rectangles (placeholder). `TextureManager` uses centered origins. Pre-generated filled circle textures with anti-aliased edges for range indicators and AoE effects.
- **Map Selection**: Pre-game scene showing 3 maps (classic_s, straight, maze_test) with visual previews. Supports hover and click selection.
- **Map**: 3 map types with different terrain layouts. Maps defined in `MapDataRepository` via `MapData` record with `SpawnPoint`, `ExitPoint`, `WalkableAreas`, and optional `RockAreas` (list of rectangles). TileType: `HighGround` (impassable, buildable), `Path` (walkable cost 1, buildable), `Rock` (impassable, unbuildable). Towers are overlays tracked via `Tile.OccupyingTower` that modify movement cost without changing terrain type. No predefined path lists — Dijkstra computes routes from tile costs. All 3 current maps have rock strips at top and bottom edges.
- **Pathfinding**: Dijkstra flood-fill heat map (`Pathfinder.ComputeHeatMap`) from exit to all tiles. Path extracted via gradient descent (`ExtractPath`). Towers have per-type movement costs (Gun: 300, Cannon: 500) — enemies prefer routing through Gun towers over Cannon towers. Path is never fully blocked. Heat map recomputed on every tower placement. Per-enemy pathfinding: each enemy computes a fresh path from its current position when towers change, preventing diagonal cuts across obstacles.
- **Towers**: 2 Types (Gun, Cannon) with distinct movement costs for strategic maze control. Supports placement (L-Click). Per-type health (Gun: 100, Cannon: 150). Blocking capacity system: Gun (3 slots), Cannon (2 slots) — limits simultaneous enemy attacks. Health bar shown when damaged. Blue capacity bar shows remaining engagement slots. Range indicator (filled circle) visible on hover and during placement preview. AoE impact visual (expanding orange circle) on Cannon projectile hits. Destroyed towers clear tile occupancy reference and trigger pathfinding recomputation.
- **Enemies**: Base class with state machine (Moving, Attacking). Follows path to exit. Detects towers blocking next waypoint and attempts engagement via `TryEngage()` — if tower at capacity, continues moving. In Attacking state, deals damage every 1 second. Resumes moving when tower dies or path updates. Cleanup via `OnDestroy()` releases engagement slot. Visual feedback: red tint while attacking. Per-wave AttackDamage property scales with wave difficulty.
- **Waves**: 10 hardcoded waves.
- **UI**: Side panel (Selection, Start Wave). Floating Text feedback (Money +/-).
- **Game Flow**: Map Selection → Play → Victory/Defeat Screens (Return to Map Selection via 'R').
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
- [x] **Range Rings**: Visual radius on hover and placement preview.
- [ ] **Wave Preview**: "Next Wave" info.