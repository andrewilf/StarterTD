# Game Development TODO

This file tracks active work only. Completed systems belong in `docs/CURRENT_STATE.md`.

## Legend
- **Priority**: P0 (blocker/critical), P1 (high), P2 (medium), P3 (nice-to-have)
- **Effort**: S (small, single system), M (medium, 2-3 systems), L (large, cross-cutting), XL (multi-session)

---

## 1. Core Gameplay Flow

### 1.1 Rebalance Towers for the Cooldown Economy
- **Priority**: P1 | **Effort**: M
- **Current State**: Cooldown-based placement is live, but tower stat values and cooldown tuning are still placeholder-heavy.
- **Tasks**:
  - [ ] Rebalance tower DPS, range, survivability, and utility around cooldown-limited placement instead of gold cost
  - [ ] Tune `BaseCooldown` and `CooldownPenalty` per tower pool
  - [ ] Verify spawn schedule enemy stats still feel fair against the new numbers
  - [ ] Rename tower labels only after a visual theme is chosen; do not block balance on naming work

### 1.2 Champion Death Debuff
- **Priority**: P1 | **Effort**: S
- **Current State**: `ChampionManager` already calls `Tower.UpdateChampionStatus(bool)`, but the base implementation is still a no-op.
- **Tasks**:
  - [ ] Decide how strongly generic towers should be penalized when their champion is dead
  - [ ] Implement per-type debuff behavior and stat restoration on champion return
  - [ ] Surface the debuff clearly in the tower info UI

### 1.3 Spawn Timeline Preview UI
- **Priority**: P2 | **Effort**: M
- **Current State**: Enemy spawns are driven by `Content/SpawnSchedules/{mapId}.json` and play out as one continuous match timeline. The HUD already shows a prematch countdown and spawned-count summary; the missing piece is a forward-looking preview of incoming pressure.
- **Tasks**:
  - [ ] Show upcoming spawns over a short configurable look-ahead window
  - [ ] Group the preview by lane and enemy type so each lane's next burst is readable at a glance
  - [ ] Summarize incoming pressure with count, density, or estimated HP pressure instead of a single total-round number
  - [ ] Use the same pending-spawn data as the entrance warning system so both UI features stay synchronized

---

## 2. Combat Readability & Feedback

### 2.1 Improve Tower and Enemy Stats Display
- **Priority**: P1 | **Effort**: M
- **Current State**: The info panel works, and fire-rate display already reflects temporary attack-speed buffs. The missing work is readability, comparison, and summary.
- **Tasks**:
  - [ ] Add DPS display for towers
  - [ ] Add normalized stat bars for HP, range, and damage
  - [ ] Show stat deltas when hovering a placement button while a tower is selected
  - [ ] Show enemy progress and active status effects more clearly
  - [ ] Add concise tooltips for abilities and special UI states
  - [ ] Track per-tower kill counts
  - [ ] Add a short post-match summary (damage dealt, kills, towers lost)

### 2.2 Enemy Health Bars
- **Priority**: P2 | **Effort**: S
- **Tasks**:
  - [ ] Draw simple HP bars above enemy sprites
  - [ ] Use a green -> yellow -> red gradient based on remaining health
  - [ ] Keep the presentation readable in dense enemy bursts without adding numeric clutter

### 2.3 Sound System
- **Priority**: P2 | **Effort**: M
- **Tasks**:
  - [ ] Add a central SFX/BGM manager
  - [ ] Hook up key gameplay cues: tower fire, enemy death, spawn burst start/end, abilities, UI clicks
  - [ ] Add a looping background track strategy (per map or global)

### 2.4 Enemy Crowding
- **Priority**: P3 | **Effort**: L
- **Current State**: Enemies can overlap freely. This is a design enhancement, not a blocker.
- **Tasks**:
  - [ ] Start with a simple per-tile speed penalty model before considering sub-grid movement
  - [ ] Add lightweight visual offsetting so stacked enemies are easier to read
  - [ ] Rebalance cannon grouping logic and chokepoint interactions after the mechanic lands

---

## 3. Content Pipeline & Authoring

### 3.1 Replace Placeholder Terrain Art
- **Priority**: P1 | **Effort**: M
- **Current State**: The game already uses native `32x32` terrain source tiles; the remaining gap is visual quality and tileset coverage.
- **Tasks**:
  - [ ] Choose a cohesive terrain set with edge/corner support
  - [ ] Replace the placeholder terrain sheet, or split it into multiple sheets if that reads better
  - [ ] Update `Content.mgcb` and Tiled tileset metadata as needed
  - [ ] Finalize tower naming/theme only if the chosen art direction makes a rename worthwhile

### 3.2 Add Auto-Tiling
- **Priority**: P1 | **Effort**: L
- **Depends On**: 3.1 terrain art with usable edge/corner variants
- **Tasks**:
  - [ ] Pick one approach: 4/8-bit bitmasking, Wang tiles, or Tiled terrain metadata
  - [ ] Select tile variants based on neighbor context at draw/load time
  - [ ] Handle map borders and runtime tile changes such as debug HighGround placement
  - [ ] Cache or recompute tile variants without adding avoidable per-frame work

### 3.3 Expand Tiled Custom Property Support
- **Priority**: P2 | **Effort**: M
- **Current State**: `TmxLoader` already reads the map-level `name` property. The next step is broader property support, not a greenfield parser.
- **Tasks**:
  - [ ] Parse custom properties on tiles, objects, and layers
  - [ ] Store parsed values in `MapData` or related map-loading structures
  - [ ] Use properties for buildability overrides, movement-cost hints, spawn metadata, or rendering hints

### 3.4 Enemy Variants / Presets
- **Priority**: P2 | **Effort**: S
- **Tasks**:
  - [ ] Define reusable archetype presets such as Fast, Tank, and Swarm
  - [ ] Decide whether presets live in spawn schedule authoring, loader expansion, or both
  - [ ] Keep inline per-spawn overrides available for one-off encounters

### 3.5 Spawn Schedule JSON Authoring Tooling
- **Priority**: P3 | **Effort**: M
- **Depends On**: 3.4 stable enemy schema
- **Current State**: `Content/SpawnSchedules/{mapId}.json` is still hand-authored.
- **Tasks**:
  - [ ] Define a spreadsheet or CSV template for spawn schedule authoring
  - [ ] Build a conversion/validation script
  - [ ] Validate enemy types, spawn names, and required fields before emitting JSON
  - [ ] Document the workflow briefly once the schema settles

---

## Suggested Implementation Order

1. Rebalance towers and finish the champion-death penalty so combat rules are coherent.
2. Add a spawn-timeline preview so players can read upcoming pressure in the continuous spawn flow.
3. Improve combat readability with stats, enemy health bars, and sound feedback.
4. Replace placeholder terrain art, then add auto-tiling against the chosen tileset.
5. Expand Tiled metadata and spawn schedule tooling only after the content schema is stable.
6. Revisit crowding last, because it changes encounter balance and cannon value significantly.

## Open Questions

1. Should tower renaming wait for the art pass, or do we want a theme locked before balance tuning?
2. How punishing should champion-death debuffs be before the game feels snowbally?
3. In the continuous spawn flow, how far ahead should the preview look: next few enemies, next burst window, or the next several seconds?
4. Do we want auto-tiling driven primarily by code bitmasks or Tiled-authored metadata?
5. Should enemy presets be authoring sugar only, or part of the runtime spawn schedule schema?
