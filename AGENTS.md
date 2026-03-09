# AGENTS.md

## Purpose
This file is the canonical agent policy for this repository. `CLAUDE.md` is legacy compatibility and does not override this file.

## Developer Context
- The developer is a Python/TypeScript engineer new to C#.
- Tone should be educational when relevant. Explain C# patterns (delegates, LINQ, structs) clearly.
- Prefer readability over cleverness.
- Write comments for intent and reasoning, not narration.

## Project Architecture
- Source of truth: `docs/ARCHITECTURE.md`.
- Follow architecture boundaries and ownership from that file.

## Documentation Workflow
- Source of state/backlog truth: `docs/CURRENT_STATE.md` and `docs/TODO.md`.
- Update `docs/CURRENT_STATE.md` immediately after feature completion.
- Keep docs concise and architecture-focused.
- Record recurring project-level quirks in `Known Gotchas`.
- If `docs/MEMORY.md` is used, keep it under 10 lines with high-value non-obvious gotchas.

## Implementation Rules
1. Use `Action<T>` for cross-system events; avoid `EventHandler`.
2. `GameplayScene` owns global state (for example Lives and wave progress).
3. Remove dead/redundant code introduced by changes.
4. Run `dotnet build` after code changes.
5. Run `dotnet csharpier format .` once after code changes.
6. Resolve SonarAnalyzer.CSharp warnings before finishing.
7. If build fails, inspect failing lines, make up to 3 fixes, then escalate.
8. Avoid `new` allocations in `Update()` and `Draw()` loops; use pooling for high-frequency objects.

## Known Gotchas
- `TextureManager.DrawSprite` uses centered origin. `TextureManager.DrawRect` uses top-left.
- `CountdownTimer.CurrentTime` is elapsed time, not remaining time.
- `CountdownTimer.CurrentTime` is `TimeSpan`; use `.TotalSeconds` for float values.
- Completion check: `State.HasFlag(TimerState.Completed)`.

## Skills
A skill is a local instruction set in `SKILL.md`. Use these when matched:
- document-changes-from-last-merge
- document-changes-uncommitted
- optimizing-ai-content
- refactor-and-build-from-last-merge
- refactor-and-build-uncommitted
- linear
- playwright
- skill-creator
- skill-installer

## Practical Use Notes
- Treat this file as instruction policy.
- Treat `docs/ARCHITECTURE.md` and `docs/CURRENT_STATE.md` as project knowledge/state.
