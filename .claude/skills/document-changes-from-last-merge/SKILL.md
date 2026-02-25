---
name: document-changes-from-last-merge
description: Updates project .md documentation based on changes since the last merge commit.
---

When invoked, execute these steps sequentially:

1. Find the last merge commit using `git log --merges -1 --format=%H`. Read the diffs since that commit using `git diff <merge_commit_hash>` to understand what logic or features were added or modified. Also review the commit messages since that merge (`git log <merge_commit_hash>..HEAD --oneline`) as they often contain useful context about intent and changes.
2. Review the core `.md` files in the repository. Do not include `.md` files in the `.claude/skills` or `.codex/skills` folder.
3. Update the relevant markdown files with *only* the essential, highly relevant context regarding these recent code changes.
4. Do not add unnecessary fluff, speculative future features, or verbose explanations. Keep the additions concise and strictly tied to the diffs.
4a. Do not include exact numbers (damage values, cooldown durations, speeds, radii, costs, HP values, etc.) — these are subject to change. Document architecture, behavior, and feature patterns only.
5. Print a brief list of the `.md` files you updated.