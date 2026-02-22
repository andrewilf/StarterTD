---
name: optimizing-ai-content
description: Reviews and refactors project .md files to maximize token density. Strips out human-readable fluff and leaves only essential, accurate facts, constraints, and architecture rules for AI ingestion.
---

When invoked, execute these steps sequentially on the target `.md` files: `docs/ARCHITECTURE.md` and `docs/CURRENT_STATE.md`.

1. Analyze the file's content. Your goal is maximum token density and accuracy for an LLM reader. Human readability is not a priority.
2. Remove all conversational language, pleasantries, theoretical explanations, redundant historical context, and lengthy tutorials.
3. Distill core engine constraints (e.g., Mediator event routing, SceneManager usage, MonoGame Extended implementations) into strict, isolated bullet points. 
4. Preserve exact entity names, data structures, and mathematical/logic rules without summarization (e.g., Dijkstra pathfinding weights, Breach system block values, 0-gold Champion placement logic).
5. Rewrite the remaining content using clear hierarchical headers (`##`, `###`) and terse lists. Do not use blockquotes or flowing prose paragraphs.
6. Print a brief summary of the token-wasting content you removed and the files you successfully compressed.
