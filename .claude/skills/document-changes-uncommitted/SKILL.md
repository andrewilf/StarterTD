---
name: document-changes-uncommitted
description: Updates project .md documentation based on the current uncommitted changes in the codebase.
---

When invoked, execute these steps sequentially:

1. Identify all uncommitted changes using `git diff HEAD`. Read the actual diffs to understand what logic or features were just added or modified.
2. Review the core `.md` files in the repository. Do not include `.md` files in the `.claude/skills` or `.codex/skills` folder.
3. Update the relevant markdown files with *only* the essential, highly relevant context regarding these recent code changes. 
4. Do not add unnecessary fluff, speculative future features, or verbose explanations. Keep the additions concise and strictly tied to the diffs.
4a. Do not include exact numbers (damage values, cooldown durations, speeds, radii, costs, HP values, etc.) — these are subject to change. Document architecture, behavior, and feature patterns only.
5. Print a brief list of the `.md` files you updated.