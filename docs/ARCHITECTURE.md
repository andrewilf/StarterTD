# Architecture Map

## Core Class Relationships

```mermaid
graph TD
    A[Program.cs] --> B[Game1.cs];
    B --> C{SceneManager};
    C --> D{Active Scene (IScene)};
    D --> E[MapSelectionScene];
    D --> F[GameplayScene];

    subgraph Game Loop
        B -- Update() / Draw() --> C;
        C -- Update() / Draw() --> D;
    end

    subgraph MapSelectionScene
        E -- Reads --> L[MapDataRepository];
        E -- Displays --> M[Map Previews];
        E -- Transitions to --> F;
    end

    subgraph GameplayScene Implementation
        F -- Manages --> G[Map];
        F -- Manages --> H[WaveManager];
        F -- Manages --> I[TowerManager];
        F -- Manages --> J[InputManager];
        F -- Manages --> K[UIPanel];
        F -- Manages --> N[FloatingTexts];
        F -- Transitions to --> E;
    end
```

**Key Pattern:** Multiple scenes implement `IScene`. `MapSelectionScene` is the entry point, `GameplayScene` is the main game logic. `Game1` is just a wrapper that ticks the `SceneManager`.

## Data Flow & Communication

Each scene acts as a **Mediator** for its subsystems. In `GameplayScene`, sibling systems (e.g., Towers and Enemies) never talk directly.

### 1. Hierarchy (Top-Down)
`Game1` → `SceneManager` → `[MapSelectionScene | GameplayScene]` → `Managers`

-   **Update Loop:** `GameplayScene` calls `Update()` on all child managers (`WaveManager`, `TowerManager`, etc.).
-   **Context Passing:** Global state (like the `_enemies` list) is passed down as an argument in the `Update()` call.
    -   *Example:* `TowerManager.Update(gameTime, _enemies)` allows towers to see enemies without owning the list.

### 2. Events (Bottom-Up)
Child managers use `Action<T>` callbacks to notify the `GameplayScene` of events.

-   **Example (Spawning):**
    1.  `WaveManager` fires `OnEnemySpawned?.Invoke(newEnemy)`.
    2.  `GameplayScene` listens: `_waveManager.OnEnemySpawned = e => _enemies.Add(e)`.
    3.  `GameplayScene` adds the enemy to the master list.

### 3. Rendering Flow
Rendering relies on dependency injection via the `Draw` call.

-   **SpriteBatch**: Passed down from `Game1` through every `Draw` method.
-   **Fonts/Textures**: `Game1` loads global fonts/textures. These are passed into `Draw()` methods (e.g., `Tower.Draw(spriteBatch, font)`), avoiding static resource references inside game entities.

## Subsystems

### Visual Feedback (FloatingText)
A fire-and-forget system for temporary UI (damage numbers, money changes).
-   **Lifecycle**: Objects update themselves (velocity, fade) and flag themselves for removal when finished.
-   **Trigger**: Centralized in `GameplayScene`. Any logic (selling, killing) requests a popup via the scene.

### Map System
-   **Data-Driven**: Maps are defined in `MapDataRepository` as `MapData` records.
-   **Selection**: `MapSelectionScene` reads map data for previews. `GameplayScene` receives `mapId` via constructor.

### Tower System
-   **Data-Driven**: Tower stats are defined in `TowerData` records, not hardcoded in classes.
-   **Targeting**: Towers scan the `enemies` list passed during `Update()` to find targets.
-   **Blocking Capacity**: Each tower has a `BlockCapacity` (configured per tower type: Gun 3, Cannon 2) limiting simultaneous enemy engagements. Enemies call `TryEngage()` before attacking — if tower is at capacity, the enemy continues moving through. Engagement is released on: tower death, enemy death, path update, or enemy reaching exit. Visual feedback via blue capacity bar (100% blue = full capacity, 0% = at limit).

### Enemy State Machine
-   **States**: `Moving` (following path) and `Attacking` (stationary, dealing damage to tower).
-   **State Transitions**:
    -   **Moving → Attacking**: When next waypoint has an alive tower AND `tower.TryEngage()` returns true (capacity available). If tower is at capacity, enemy continues moving through it.
    -   **Attacking → Moving**: When tower is destroyed (checked via `IsDead`) or path changes via `UpdatePath()`.
-   **Engagement Release**: Happens in four places: (1) tower dies in `UpdateAttackingState()`, (2) enemy dies in `GameplayScene` cleanup, (3) enemy reaches exit in `GameplayScene` cleanup, (4) path changes in `UpdatePath()`. All call `tower.ReleaseEngagement()` to decrement the engagement counter before state change.
-   **Cleanup Hook**: `OnDestroy()` method called before enemy removal ensures engagement is released, preventing "ghost slots" where a tower remains blocked by a dead enemy.

### Mazing System (Dynamic Pathfinding via Dijkstra)
-   **Architecture**: `TileType` enum has three values: `HighGround` (impassable, buildable), `Path` (walkable cost 1, buildable), `Rock` (impassable, unbuildable). Towers are overlays tracked via `Tile.OccupyingTower` (full Tower reference) — terrain type never changes after initialization.
-   **Terrain Definition**: Maps define walkable terrain via `MapData.WalkableAreas` and impassable rock terrain via optional `MapData.RockAreas` — both are `List<Rectangle>`. Rock processing happens after WalkableAreas so rocks can override paths. No predefined path list needed.
-   **Dijkstra Pathfinding**: `Pathfinder.cs` contains two functions: `ComputeHeatMap(target, columns, rows, movementCost)` and `ExtractPath(start, heatMap, columns, rows)`.
-   **Heat Map**: Dijkstra flood-fill from the exit point computes cost-to-exit for every tile. Towers have per-type movement costs (Gun: 300, Cannon: 500), creating strategic trade-offs between tower effectiveness and maze control. Enemies dynamically prefer cheaper paths (through Gun towers) over expensive ones (through Cannon towers).
-   **Per-Tower Movement Costs**: Each tower type has a `MovementCost` property in `TowerData.TowerStats`. The `Tile.MovementCost` property checks `OccupyingTower` first; if occupied, it returns the tower's movement cost. Otherwise, it returns the terrain's cost based on `Type`.
-   **Tower as Overlay**: When a tower is placed, `tile.OccupyingTower` is set to the Tower object. When destroyed, it's cleared to null. The underlying terrain `Type` never changes, keeping tile state simple and immutable.
-   **Path Extraction**: Gradient descent on the heat map extracts the optimal path from spawn to exit. Recomputed whenever towers change. `Map.ActivePath` is the reference path shown to the player via the blue line overlay.
-   **Per-Enemy Pathfinding**: `Map.ComputePathFromPosition(startPos)` computes a fresh path from any grid position using the current heat map. Each enemy gets its own path from its current location, preventing diagonal cuts across obstacles when towers change mid-route.
-   **Dynamic Rerouting**: When towers change, `Map.RecomputeActivePath()` recalculates the heat map. `Enemy.UpdatePath(map)` converts the enemy's current world position to a grid cell and computes a fresh path from there using the updated heat map.
-   **WaveManager Integration**: Takes `Func<List<Point>>` instead of a direct path list, so each spawned enemy gets the latest `ActivePath`.
-   **Tower Placement**: When a tower is placed, `OnTowerPlaced` fires. `GameplayScene` calls `RecomputeActivePath()` and `Enemy.UpdatePath(map)` on all living enemies. Each enemy smoothly reroutes from its current position rather than snapping to a global waypoint. Paths are never fully blocked since towers are expensive (300-700) not impassable (MaxValue).