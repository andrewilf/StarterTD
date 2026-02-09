'''# Current Project State

This file is a checklist of what is implemented vs. what is mocked or missing. Use this to quickly understand the current capabilities of the starter.

## Implemented Features

- [x] **Project Setup**: .NET 9 DesktopGL project created and configured.
- [x] **Git Repository**: `git init` has been run and a `.gitignore` file is present.
- [x] **Core Architecture**
    - [x] `Game1` -> `SceneManager` -> `GameplayScene` loop.
    - [x] `ITower`, `IEnemy`, `IScene` interfaces.
- [x] **Asset Management**
    - [x] **No MGCB**: The project does not use the MonoGame Content Pipeline.
    - [x] `TextureManager` static class.
    - [x] Programmatic 1x1 white pixel `Texture2D` created in memory.
    - [x] `DrawSprite` helper method that uses a centered origin.
- [x] **Gameplay Systems**
    - [x] **Map**: Static grid with a hardcoded `S`-shaped path.
    - [x] **Towers**: 3 basic types (Gun, Cannon, Sniper) defined in `TowerData`.
    - [x] **Tower Placement**: Left-click to select from UI, left-click on grid to place.
    - [x] **Tower Upgrading**: Right-click on a tower to upgrade it from Level 1 to Level 2.
    - [x] **Enemies**: Base `Enemy` class that follows the path and has health.
    - [x] **Waves**: `WaveManager` with 10 hardcoded waves of increasing difficulty.
    - [x] **Projectiles**: Towers fire projectiles that track and damage enemies.
    - [x] **Player State**: Money and Lives are tracked.
- [x] **UI**
    - [x] Basic right-side UI panel.
    - [x] Buttons for selecting towers.
    - [x] "Start Wave" button.
    - [x] Player stats display (Money, Lives, Wave).

## Mocked or Missing Features

This is the list of things you can ask an AI assistant to help you build next.

- [ ] **No Real Sprites**: All rendering uses colored rectangles. The `TextureManager` is ready, but you need to load `Texture2D` assets and pass them to the `Draw` calls.
- [ ] **No Sound**: There is no audio engine or sound effects.
- [ ] **No SpriteFont for Text**: The UI Panel has a fallback rendering mode. To display text, a `SpriteFont` file needs to be created with the MGCB tool and loaded in `GameplayScene.LoadContent()`.
- [ ] **No Main Menu**: The game starts directly in the `GameplayScene`.
- [ ] **No Game Over / Win Logic**: The game shows a colored overlay when the game ends, but there is no logic to restart or return to a menu.
- [ ] **Limited Enemy Variety**: Only one basic enemy type exists.
- [ ] **Limited Tower Variety**: The 3 towers are functional but simple.
- [ ] **No Special Abilities**: Towers do not have special abilities (e.g., slow, poison).
- [ ] **No Selling Towers**: You can place and upgrade, but not sell.
'''
