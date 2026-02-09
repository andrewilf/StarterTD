
# Claude Workflow: Prompting Guide

This document provides instructions and templates for prompting an AI assistant like Claude to effectively contribute to this codebase.
The goal is to provide the AI with a consistent "Context Bank" so it can make intelligent, non-breaking changes.

## Session Start: Priming the AI

Always begin a new coding session with a priming prompt. This loads the project's architecture and current state into the AI's context window, acting as its long-term memory.

### Session Start Template

> **SYSTEM**
> You are a Senior C# Game Architect specializing in the MonoGame framework. Your task is to help me, a TypeScript/Python developer, build a tower defense game. You must adhere to the existing architecture and coding style.
>
> Before you do anything, you MUST read the following files to understand the project's context. I will provide them to you.
>
> - `docs/ARCHITECTURE.md`: The high-level class structure and data flow.
> - `docs/CONCEPTS.md`: A guide to C# concepts for non-C# developers.
> - `docs/CURRENT_STATE.md`: A checklist of what is currently implemented.
>
> After you have read and understood these files, please confirm by saying "Ready."

*(You would then paste the contents of each file into the chat)*

## Session End: Maintaining the Context Bank

At the end of a session where you've made changes (e.g., added a new feature, fixed a bug), you must ask the AI to update the `CURRENT_STATE.md` file. This keeps the context bank accurate for the next session.

### Session End Template

> **USER**
> Great, that feature is working now. Please update the `docs/CURRENT_STATE.md` file to reflect the changes we just made. Here is the original file content:
>
> ```markdown
> (Paste the content of `docs/CURRENT_STATE.md` here)
> ```
>
> Specifically, I need you to check the box for the feature we just added: `[ ] No Selling Towers`.
>
> Provide me with the full, updated content of the file.

## Example Task Prompts

Here are some examples of how you might ask the AI to perform specific tasks after the initial priming.

### Example 1: Adding a New Tower

> **USER**
> I want to add a new tower called "Slow Tower". It should not do any damage, but it should slow down enemies within its range.
>
> 1.  It should implement the `ITower` interface.
> 2.  Its stats should be defined in `Entities/TowerData.cs`. Let's give it a range of 150f and a cost of 70.
> 3.  The slowing effect will require adding a `SlowFactor` property to the `IEnemy` interface and `Enemy` class. When a projectile from this tower hits, it should set the enemy's `SlowFactor` to 0.5 for 2 seconds.
> 4.  Update the `Enemy.Update` method to account for this `SlowFactor` when calculating movement speed.
> 5.  Finally, add a new button for it in `UI/UIPanel.cs`.
>
> Please provide the complete code for each new or modified file.

### Example 2: Adding a SpriteFont for Text

> **USER**
> The UI currently uses colored blocks instead of text. I want to use a real font.
>
> I have already used the MGCB Editor to create a `DefaultFont.spritefont` file and it's in the `Content` directory.
>
> Please modify the code to:
>
> 1.  Load the `SpriteFont` in `GameplayScene.cs`.
> 2.  Pass the font to the `UIPanel`.
> 3.  Modify the `UIPanel.Draw` method to use `spriteBatch.DrawString` to render the player's Money, Lives, and Wave count, as well as the labels on the tower buttons.
> 4.  Remove the old `DrawButtonNoFont` fallback method.
>
> Show me the modified `GameplayScene.cs` and `UI/UIPanel.cs` files.
