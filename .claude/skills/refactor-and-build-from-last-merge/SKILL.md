---
name: refactor-and-build-from-last-merge
description: Reviews C# code changed since the last merge commit, removes redundancies, formats via CSharpier, and ensures a clean dotnet build. Use this before committing code.
---

When invoked, execute these steps sequentially:

1. Find the last merge commit using `git log --merges -1 --format=%H`. Identify all changes since that commit using `git diff <merge_commit_hash> --name-only` and `git diff <merge_commit_hash>`. Also review the commit messages since that merge (`git log <merge_commit_hash>..HEAD --oneline`) as they often contain useful context about intent and changes.
2. Analyze the modified C# files using Serena's symbolic tools (`get_symbols_overview`, `find_symbol`, `find_referencing_symbols`) rather than reading whole files. Refactor the code to remove any redundancies and ensure the logic is sound, maintaining the established architecture (e.g., SceneManager, Mediator).
3. Run `dotnet csharpier format .` in the terminal to format all files.
4. Run `dotnet build` in the terminal.
5. If the build fails or throws warnings (especially SonarAnalyzer warnings), autonomously fix the C# code, re-run `dotnet csharpier format .`, and re-run `dotnet build` until the build completes with zero errors and zero warnings.
6. Print a concise summary of the redundancies removed and confirm the build status. Stop execution here.