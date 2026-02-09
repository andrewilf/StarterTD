# CLAUDE.md

## Developer Context
I am a Python/TypeScript engineer new to C#.
- **Tone**: Educational. Explain C# patterns (Delegates, LINQ, Structs) using Python/TS analogies.
- **Code Style**: Readable over clever. Prioritize clear, explicit logic.

## Project Architecture (Source of Truth: `docs/ARCHITECTURE.md`)
- **Pattern**: `Game1` → `SceneManager` → `GameplayScene` (The Mediator).
- **Mediator Rule**: `GameplayScene` owns all managers (`WaveManager`, `TowerManager`, etc.).
- **Data Flow**:
    - **Down**: Scene passes data to managers via `Update()`.
    - **Up**: Managers notify Scene via `Action` callbacks.
    - **Strict Isolation**: Managers **never** reference each other directly.

## Documentation Strategy
- **Status & Backlog**: Check `docs/CURRENT_STATE.md` for features and **To-Do** items.
- **Update Rule**: Update `CURRENT_STATE.md` immediately after finishing a feature.
- **Gotcha Rule**: If we hit a recurring MonoGame/C# quirk, ask to append it to "Known Gotchas" below.

## Feature Implementation Rules
1.  **Feature Briefs**: If I provide a structured brief (Data/Logic/Visuals), implement closely and only ask questions if there are major concerns with the implementation.
2.  **Event Handling**: Use `Action<T>` for cross-system events. Avoid `EventHandler`.
3.  **State**: Global state (Money, Lives) resides in `GameplayScene`.
4.  **Verification**: Run `dotnet build` after generating code.

## Known Gotchas
- **Positioning**: `TextureManager.DrawSprite` uses a **CENTERED** origin.
- **Value Types**: `Vector2` is a `struct` (copy on assignment).
- **Fonts**: `SpriteFont` is passed down via `Draw()` arguments.
- **Assets**: Prefer `.png`. Check `Content/Content.mgcb` on build fail.
- **Game Loop**: Always call `base.Update()` and `base.Draw()` in `Game1`.