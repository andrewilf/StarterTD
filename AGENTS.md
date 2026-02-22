# AGENTS.md

## Purpose
This file defines how Codex should work in this repository.
Mirror existing team behavior from `CLAUDE.md` as closely as possible.

## Developer Context
- The developer is a Python/TypeScript engineer new to C#.
- Tone must be educational when relevant: explain C# patterns (delegates, LINQ, structs) clearly.
- Prefer readability over cleverness. Use explicit, maintainable logic.
- Comments should explain "why", not "what". Avoid redundant or conversational commentary.

## Project Architecture
- Source of truth: `docs/ARCHITECTURE.md`.
- When making code changes, follow architecture boundaries and ownership from that file.

## Documentation Workflow
- Status and backlog source: `docs/CURRENT_STATE.md` and `docs/TODO.md`.
- After completing a feature, update `docs/CURRENT_STATE.md` immediately.
- Keep documentation lean:
  - Only record essential architectural patterns, constraints, and relationships.
  - Do not duplicate implementation details already obvious from code.
- If a recurring MonoGame/C# quirk is discovered, propose adding it to the "Known Gotchas" section below.
- If `docs/MEMORY.md` is used, keep it under 10 lines and only store non-obvious, high-value gotchas.

## Implementation Rules
1. Event handling: use `Action<T>` for cross-system events. Avoid `EventHandler`.
2. Global state ownership: `GameplayScene` owns global state (for example Money, Lives).
3. Cleanup: remove dead or redundant code introduced by refactors/features.
4. Verification: run `dotnet build` after code changes.
5. Formatting: run `dotnet csharpier format .` once at the end of all code changes.
6. Linting: resolve SonarAnalyzer.CSharp warnings before finishing.

## Known Gotchas
- Positioning: `TextureManager.DrawSprite` uses centered origin; `DrawRect` uses top-left.
- Assets: prefer `.png`; on content build failure, check `Content/Content.mgcb`.
- MonoGame.Extended timers:
  - `CountdownTimer.CurrentTime` is elapsed time (counts up), not remaining time.
  - It is a `TimeSpan`; use `.TotalSeconds` for float conversions.
  - Check completion with `State.HasFlag(TimerState.Completed)`.

## Practical Use Notes
- Treat this file as instruction policy.
- Treat `docs/ARCHITECTURE.md` and `docs/CURRENT_STATE.md` as project knowledge/state.
