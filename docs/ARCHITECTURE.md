'''# Architecture Map

This document outlines the high-level architecture of the StarterTD project. It serves as a "map" to help you understand how the different parts of the game engine talk to each other.

## Core Class Relationships

The game follows a standard `Game -> SceneManager -> Scene` pattern.

```mermaid
graph TD
    A[Program.cs] --> B[Game1.cs];
    B --> C{SceneManager};
    C --> D{Active Scene (IScene)};
    D --> E[GameplayScene];

    subgraph Game Loop
        B -- Update() / Draw() --> C;
        C -- Update() / Draw() --> D;
    end

    subgraph Scene Implementation
        E -- Manages --> F[Map];
        E -- Manages --> G[WaveManager];
        E -- Manages --> H[TowerManager];
        E -- Manages --> I[InputManager];
        E -- Manages --> J[UIPanel];
        E -- Manages --> K[FloatingTexts];
    end
```

1.  **`Program.cs`**: The .NET application entry point. It creates an instance of `Game1` and calls `Run()`.
2.  **`Game1.cs`**: The MonoGame entry point. It initializes the window, loads global content (like the `TextureManager` and a `SpriteFont`), and creates the `SceneManager`.
3.  **`SceneManager`**: Holds a reference to the *currently active scene*. In the `Update` and `Draw` loops of `Game1`, it simply calls `Update()` and `Draw()` on the active scene. This allows you to easily switch between a main menu, the gameplay, and a game-over screen.
4.  **`IScene`**: An interface that defines the contract for a scene (`LoadContent`, `Update`, `Draw`).
5.  **`GameplayScene.cs`**: The implementation of `IScene` where the actual tower defense game logic lives. It owns and coordinates all the major gameplay systems.

## Gameplay Subsystem Communication

Within `GameplayScene`, the various "manager" classes work together. `GameplayScene` acts as the central hub or "mediator."

### How does `Game1` talk to `WaveManager`?

It doesn't, directly. The flow is hierarchical:

`Game1` -> `SceneManager` -> `GameplayScene` -> `WaveManager`

1.  `Game1.Update()` calls `_sceneManager.Update()`.
2.  `SceneManager.Update()` calls `_currentScene.Update()` (which is `GameplayScene.Update()`).
3.  `GameplayScene.Update()` calls `_waveManager.Update()`.

When a wave spawns an enemy, `WaveManager` doesn't know about the global list of enemies. Instead, it uses a C# `Action` (similar to a callback function) to notify `GameplayScene`:

-   `GameplayScene` subscribes to the `OnEnemySpawned` event: `_waveManager.OnEnemySpawned = enemy => _enemies.Add(enemy);`
-   When `WaveManager` creates a new `Enemy`, it invokes this callback: `OnEnemySpawned?.Invoke(enemy);`
-   `GameplayScene` receives the new enemy and adds it to its master `_enemies` list.

### How do Towers find Enemies?

`GameplayScene` passes the master `_enemies` list down to the `TowerManager` during the update loop.

1.  `GameplayScene.Update()` calls `_towerManager.Update(gameTime, _enemies);`
2.  `TowerManager.Update()` loops through each `Tower` it manages and calls `tower.Update(gameTime, enemies);`
3.  Inside `Tower.Update()`, the tower loops through the provided `enemies` list to find the closest target within its range.

This one-way data flow (`GameplayScene` -> `TowerManager` -> `Tower`) keeps dependencies clean. A `Tower` doesn't need to know about `GameplayScene` at all.

### Visual Feedback System

The game provides visual feedback through two mechanisms:

**FloatingText for Money Transactions**

`GameplayScene` manages a list of `FloatingText` objects that display temporary money indicators:

-   When a tower is placed: Red "-$X" floats up from the placement location
-   When a tower is upgraded: Orange "-$X" floats up from the tower
-   When an enemy dies: Gold "+$X" floats up from the enemy's position

Each `FloatingText` has:
-   A lifetime of 1.5 seconds
-   Upward velocity (20 pixels/second)
-   Fade-out effect in the final 30% of its lifetime
-   Automatic cleanup when inactive

**Tower Upgrade Cost Display**

Towers display their upgrade cost above them when not at max level:

1.  `GameplayScene.Draw()` passes the `SpriteFont` to `TowerManager.Draw()`
2.  `TowerManager` passes it to each `Tower.Draw()`
3.  If `Level < 2`, the tower renders cyan "$X" text above itself

This pattern (passing font through the draw pipeline) keeps the architecture clean while enabling text rendering at any level.

## Key Interfaces

-   **`ITower`**: Defines what a tower is. Any class implementing this can be managed by `TowerManager`. This is key for adding new tower types.
-   **`IEnemy`**: Defines what an enemy is. Any class implementing this can be spawned by `WaveManager` and targeted by towers.
-   **`IScene`**: Defines a game screen. Allows `SceneManager` to handle any scene without knowing its specific implementation.
'''
