# StarterTD

A lean, AI-assisted Tower Defense engine built with **MonoGame** and **.NET 9**.
Designed for rapid iteration with LLMs (Claude/ChatGPT), focusing on clean architecture and readable patterns.

## 🌟 Current Features
* **Dynamic Mazing**: Implementation of A* Pathfinding (`Pathfinder.cs`) that allows for dynamic rerouting of enemies when towers are placed.
* **Data-Driven Maps**: Includes 3 distinct map layouts (Classic, Straight, Maze Test) defined in repositories.
* **Tower System**: 3 Tower types (Gun, Cannon, Sniper) with upgrade paths.
* **Game Loop**: Fully functional flow from Map Selection → Gameplay → Victory/Defeat.
* **Visual Debugging**: Current rendering uses color-coded primitives (Rectangles) for rapid prototyping before asset integration.

## 🚀 Quick Start

### Prerequisites
* .NET 9.0 SDK
* MonoGame Framework

### Build & Run
    # Restore dependencies
    dotnet restore

    # Build the project (Compiles logic and processes Content.mgcb)
    dotnet build

    # Run the game
    dotnet run

    # Lint the code with CSharpier
    dotnet csharpier format .

## 🎮 Controls

| Input | Context | Action |
| :--- | :--- | :--- |
| **Left Click** | Map Selection | Select Map |
| **Left Click** | Gameplay | Place Tower |
| **Right Click** | Gameplay | Upgrade Tower |
| **Key 'R'** | Game Over | Restart / Return to Menu |

## 🤖 AI Development Workflow
This project is optimized for coding assistants. If you are using Claude or ChatGPT to contribute, specific context files are located in the root and `docs/` folder.

* **`CLAUDE.md`**: The master prompt. Feed this to the AI at the start of a session to establish coding style (Python/TS analogies for C#), strict architectural rules, and known gotchas.
* **`docs/ARCHITECTURE.md`**: The source of truth for the `SceneManager` → `Mediator` pattern and data flow.
* **`docs/CURRENT_STATE.md`**: The live checklist. Always check this before starting new features to see what is implemented vs. backlog.

## 🏗 Architecture Overview
* **Pattern**: `Game1` acts as a wrapper. The core logic lives in `SceneManager`.
* **Mediator**: `GameplayScene` owns all managers (`WaveManager`, `TowerManager`). Managers **never** reference each other directly; they communicate via `Action<T>` events bubbled up to the Scene.
* **Rendering**: 
    * `TextureManager.DrawSprite`: Uses **CENTERED** origin.
    * `DrawRect`: Uses **TOP-LEFT** origin.

## 📝 License
MIT