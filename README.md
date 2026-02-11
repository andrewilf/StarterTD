# StarterTD

A Tower Defense game built with **MonoGame** and **.NET 9**.

## Current Features
* **Dynamic Mazing**: Dijkstra pathfinding with per-tower movement costs for strategic maze control.
* **Data-Driven Maps**: 3 map layouts defined in `MapDataRepository`.
* **Tower System**: 2 tower types (Gun, Cannon) with blocking capacity.
* **Enemy Combat**: State machine (Moving/Attacking) with tower engagement system.
* **Game Loop**: Map Selection → Gameplay → Victory/Defeat.

## Build & Run
    # Restore dependencies
    dotnet restore

    # Build the project
    dotnet build

    # Run the game
    dotnet run

    # Lint the code with CSharpier
    dotnet csharpier format .

## Controls

| Input | Context | Action |
| :--- | :--- | :--- |
| **Left Click** | Map Selection | Select Map |
| **Left Click** | Gameplay | Place Tower |
| **Key 'R'** | Game Over | Restart / Return to Menu |

## License
MIT
