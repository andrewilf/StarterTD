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