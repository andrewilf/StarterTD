# StarterTD — MonoGame Tower Defense Starter

A complete, compile-ready MonoGame (DesktopGL) tower defense starter project built with .NET 9. Designed as a foundation for building tower defense games using AI-assisted development workflows.

## Features (MVP)

- **3 Tower Types**: Gun (fast/low damage), Cannon (slow/AOE), Sniper (long range)
- **10 Waves** of enemies with increasing difficulty
- **Tower Upgrades**: Right-click to upgrade towers (Level 1 → Level 2)
- **Placeholder Graphics**: All rendering uses code-generated colored rectangles (no external assets needed)
- **Clean Architecture**: Interfaces, Managers, and Scenes for easy extensibility

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- MonoGame 3.8.4.1 (automatically restored via NuGet)

### Build & Run

```bash
dotnet restore
dotnet build
dotnet run
```

### Controls

| Action | Input |
|---|---|
| Select tower type | Click tower button in right panel |
| Place tower | Left-click on green (buildable) tile |
| Upgrade tower | Right-click on placed tower |
| Deselect | Press ESC |
| Start wave | Click "Start Wave" button |

## Project Structure

```
StarterTD/
├── Engine/          # Core systems (TextureManager, SceneManager, Map, Settings)
├── Entities/        # Game objects (Tower, Enemy, Projectile, TowerData)
├── Interfaces/      # Contracts (ITower, IEnemy, IScene)
├── Managers/        # Subsystem managers (WaveManager, TowerManager, InputManager)
├── Scenes/          # Game scenes (GameplayScene)
├── UI/              # UI components (UIPanel)
├── docs/            # AI workflow documentation (Context Bank)
├── Game1.cs         # MonoGame entry point
└── Program.cs       # .NET entry point
```

## AI Workflow (Context Bank)

This project includes a `docs/` folder designed to serve as long-term memory for AI coding assistants like Claude. See:

- `docs/ARCHITECTURE.md` — Class structure and data flow
- `docs/CONCEPTS.md` — C# concepts explained via TypeScript/Python analogies
- `docs/CURRENT_STATE.md` — Implementation checklist
- `docs/CLAUDE_WORKFLOW.md` — Prompt templates for AI sessions

## License

MIT
