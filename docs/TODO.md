# Game Development TODO

## Legend
- **Priority**: P0 (blocker/critical), P1 (high), P2 (medium), P3 (nice-to-have)
- **Effort**: S (small, single system), M (medium, 2-3 systems), L (large, cross-cutting), XL (multi-session)
- **Status**: `[ ]` not started, `[~]` in progress, `[x]` done

---

## 1. Tower Rework & Rebalance

### 1.1 Rename & Rebalance Towers
- **Priority**: P1 | **Effort**: S
- **Current State**: 7 tower types (Gun, Cannon, Walling, ChampionGun, ChampionCannon, ChampionWalling, ChampionHealing) with placeholder balance values.
- **Tasks**:
  - [ ] Decide on new tower names and theme (fantasy, sci-fi, etc.)
  - [ ] Rebalance stat values (damage, fire rate, range, BaseCooldown, CooldownPenalty, HP, block capacity)
  - [ ] Update `TowerType` enum, all `*Tower.cs` stat files, `TowerData.GetStats()`, and UI labels
  - [ ] Verify wave JSON enemy stats still make sense against new tower numbers
- **Note**: Coordinate with cooldown system (section 5.1) — towers now free to place, so balance becomes purely DPS/cost-effectiveness.

---

## 2. Healing Champion Tower (Drone Support, v1)

### 2.1 ChampionHealing Tower + Drone Lifecycle
- **Priority**: P2 | **Effort**: M | **Status**: `[x]` done
- **Concept**: A **champion-only** healing tower that deploys three healing drones. No generic tower variant and no attack mode in v1.
- **Tasks (Completed)**:
  - [x] Add `TowerType.ChampionHealing` with champion-only tower stats registration.
  - [x] Support champion-only variant mapping safely in tower type helpers.
  - [x] Add `Tower.Heal(int amount)` clamped to max health.
  - [x] Spawn exactly 3 healing drones for `ChampionHealing`.
  - [x] Prevent simultaneous duplicate targets across drones (shared claimed-target coordination).
  - [x] Heal at `+1 HP / 0.1s` with `-1 energy / 0.1s` while attached to a damaged valid tower.
  - [x] Retarget to nearest valid damaged tower; exclude wall segments and owner healing champion.
  - [x] Return to owner at zero energy and recharge at `+1 energy / 0.1s` until full.
  - [x] Redeploy only when fully recharged and a valid unclaimed target exists.
  - [x] Follow attached moving tower and use uniform all-tile pathing with hardcoded movement speed.
- **Implemented**: `ChampionHealing` enum/stats/registration, champion-only UI button, `Tower.Heal()`, 3 `HealingDrone` instances with energy/tick/retarget/recharge lifecycle, shared claimed-target set prevents duplicate healing, uniform-cost drone pathing, wall segment and owner exclusion.

### 2.2 ChampionHealing Ultimate + Passive Regen
- **Priority**: P3 | **Effort**: S | **Status**: `[x]` done
- **Tasks (Completed)**:
  - [x] Replace placeholder ChampionHealing ultimate with a 15s gameplay effect.
  - [x] On cast, instantly refill all healing drones to max energy and force returning/recharging drones to redeploy.
  - [x] Make drone healing consume no energy while the ultimate is active.
  - [x] Apply +30% attack speed to all attacking towers (including walling towers) during the ultimate window.
  - [x] Add a separate white/gold sparkle aura on towers affected by the +30% speed buff.
  - [x] Add passive self-regeneration for ChampionHealing: `+2 HP` per `1s` tick.
  - [x] Set ChampionHealing ultimate cooldown to `50s`.
- **Implemented**: ChampionHealing now provides a full support ultimate (drone refill + free drone healing + global 30% attack speed buff with separate sparkle aura) and passive 1-second self-regeneration.

---

## 3. Auto-Tiled Map Logic

### 3.1 Implement Auto-Tiling System
- **Priority**: P1 | **Effort**: L
- **Current State**: Each tile renders as a single flat sprite from `terrain.png` (3 tile types, each a solid 40x40 square). No edge blending or variation.
- **Goal**: Tiles should automatically select the correct sprite variant based on their neighbors (e.g., a path tile next to a rock tile shows a path-edge sprite, corners connect smoothly).
- **Tasks**:
  - [ ] Research auto-tile approaches:
    - **Wang tiles** (2-edge or 3-corner) — simple, 16 variants per type
    - **Bitmasking** (4-bit or 8-bit) — standard for 2D games, 16 or 47 variants per type
    - **Tiled's native terrain system** — can export auto-tile data in `.tmx`, read via `TmxLoader`
  - [ ] Read Tiled custom properties documentation: https://doc.mapeditor.org/en/stable/manual/custom-properties/
  - [ ] Decide approach: manual bitmask in code vs. leverage Tiled's terrain brushes + export
  - [ ] Create or source a tileset with edge/corner variants for each tile type (see section 6 for asset packs)
  - [ ] Update `TextureManager.DrawTile()` to select sprite variant based on neighbor context
  - [ ] Handle edge cases: map borders (treat out-of-bounds as a specific type), runtime tile changes (HighGround placement)
  - [ ] Recalculate auto-tile when tiles change at runtime (e.g., debug HighGround placement)
  - [ ] Complexity Warning: Auto-tiling touches rendering, map loading, and runtime tile changes. Plan carefully before implementing.

### 3.2 Tiled Custom Properties Integration
- **Priority**: P2 | **Effort**: M
- **Reference**: https://doc.mapeditor.org/en/stable/manual/custom-properties/
- **Goal**: use Tiled's custom properties to embed game-specific data directly in `.tmx` files, reducing hardcoded values.
- **Tasks**:
  - [ ] Extend `TmxLoader` to parse `<properties>` on tiles, objects, and layers
  - [ ] Potential uses:
    - Tile properties: movement cost overrides, buildable flag, visual variant hints
    - Object properties: enemy spawn delay, wave triggers, scripted events
    - Layer properties: rendering order, parallax, visibility toggles
  - [ ] Store parsed properties in `MapData` for runtime access

---

## 4. Gameplay Flow & Tower Placement System

### 4.1 Replace Money System with Cooldown-Based Placement
- **Priority**: P1 | **Effort**: L | **Status**: `[x]` done
- **Implemented**: Per-pool cooldown timers replace gold. Each tower type has a `BaseCooldown` + `CooldownPenalty × existing pool count` added on placement. Champions share one pool. Selling refunds `CooldownPenalty`. Pools tick on scaled game time. UI shows "Locked: X.Xs" when blocked.
- **Remaining**: Balance cooldown values (base, penalty, per-type).

### 4.2 Auto-Start Waves After First Manual Start
- **Priority**: P1 | **Effort**: S
- **Current State**: Each wave requires a manual "Start Wave" button click.
- **New Behavior**:
  1. Wave 1: manual start (player needs setup time)
  2. Waves 2+: auto-start on a timer after previous wave ends (e.g., 10-15 second intermission countdown)
  3. Early invoke: after the intermission timer is **50% elapsed**, player can click "Start Wave" to skip the remaining wait
  4. Display countdown timer in UI (e.g., "Next wave in: 8s")
- **Tasks**:
  - [ ] Add intermission timer to `WaveManager` (starts after wave completion, triggers next wave on expiry)
  - [ ] Add early-start logic: `WaveManager.CanStartEarly()` returns true if timer > 50% elapsed
  - [ ] Update `UIPanel` to show countdown text and disable/enable "Start Wave" button accordingly

### 4.3 Enemy Crowding Mechanic
- **Priority**: P2 | **Effort**: L
- **Concept**: Enemies currently overlap freely on the same tile. A crowding mechanic would make dense groups of enemies interact — slowing each other down, spreading out, or taking crowd damage.
- **Design Options** (pick one or combine):
  - **Option A — Speed Penalty**: Enemies in the same cell move slower based on occupant count. Simple to implement, creates natural spreading.
  - **Option B — Collision Avoidance**: Enemies maintain minimum spacing. Requires per-frame position adjustments. More realistic but harder to tune.
  - **Option C — Sub-Grid Movement**: Increase grid resolution (e.g., 2x2 or 4x4 cells per tile) so enemies have more spatial granularity. Heavy refactor of pathfinding and rendering.
- **Tasks**:
  - [ ] Decide on approach (recommend Option A for simplicity, with Option C as a future enhancement)
  - [ ] If Option A: track enemy count per tile, apply speed multiplier (e.g., 1.0x for 1 enemy, 0.8x for 2, 0.6x for 3+)
  - [ ] If Option C: refactor `Map` grid to support sub-tile resolution, update pathfinding costs, update rendering coordinates
  - [ ] Visual feedback: show enemies visually offset within a tile so overlapping is less jarring even without full collision
  - [ ] Balance: crowding should create interesting tactical situations (funnel enemies into AoE kill zones) without making the game feel sluggish
- **Interacts With**: Cannon "most grouped" targeting — crowding makes cannons more valuable. Wall towers — walls create natural chokepoints that trigger crowding.

### 4.4 Enemy Entrance Indicator
- **Priority**: P2 | **Effort**: S
- **Concept**: Visual indicator at spawn point(s) warning the player when enemies are about to emerge.
- **Tasks**:
  - [ ] Detect when first enemy in a wave is ~1 tile away from spawn (or use a configurable "warning distance")
  - [ ] Display animated indicator at spawn point: pulsing red circle, arrow pointing outward, or "!" icon
  - [ ] Play optional SFX cue (low-priority, defer to sound system 9.2)
  - [ ] Dismiss indicator once first enemy exits spawn or wave completes
  - [ ] Support multi-spawn maps: show indicator at all active spawn points for the current wave

---

## 5. Champion Tower Movement Constraints

### 5.1 Champion Canon Tower — Prevent High Ground / Path Crossing
- **Priority**: P2 | **Effort**: S
- **Concept**: Champion cannon towers cannot cross between high ground and path tiles (and vice versa). Enforces arena-like spatial control — cannon champions must commit to one terrain type.
- **Current State**: Champion towers can traverse between terrain types via `TowerPathfinder`, but champion placement and movement destinations require a uniform tile type across the full 2x2 footprint.
- **Tasks**:
  - [ ] Extend `TowerPathfinder.ComputePath()` to accept terrain-type constraint (or check per-tower)
  - [ ] For `ChampionCannonTower`: when computing movement path, block transitions between `TileType.HighGround` ↔ `TileType.Path`
  - [ ] Movement already blocked if adjacent tile is unreachable; this adds a terrain-type barrier on top
  - [ ] Detect attempted crossing: if current tile is High Ground, disallow move to Path tile and vice versa
  - [ ] UI feedback: grayed-out or disabled movement button if target is on opposite terrain type
  - [ ] Note: This applies only to `ChampionCannonTower`, not `ChampionGunTower` or `ChampionWallingTower` (for now)

---

## 6. Wave Spawning & JSON Tooling

### 6.1 Excel and Script Tooling for Spawn Wave JSON
- **Priority**: P3 | **Effort**: M
- **Current State**: Wave JSON files (`Content/Waves/{mapId}.json`) are hand-written with enemy spawn timings, types, and exit lanes.
- **Goal**: Create a spreadsheet template and conversion script to reduce manual JSON editing.
- **Tasks**:
  - [ ] Design Excel/CSV template: columns for enemy type, count, spawn delay, exit lane, notes
  - [ ] Create Python or C# script to parse Excel → JSON (or CSV → JSON)
  - [ ] Script generates properly formatted wave array with correct JSON structure
  - [ ] Validate output: check for missing fields, invalid enemy types, duplicate spawn names
  - [ ] Document template usage (how to fill columns, how to run script)
  - [ ] Optional: Add a UI dialog in-game to load/preview waves from a spreadsheet (low priority)

---

## 7. UI & Stats Improvements

### 7.1 Improved Tower/Enemy Stats Display
- **Priority**: P1 | **Effort**: M
- **Current State**: Info panel shows basic stats (HP, damage, fire rate, speed, bounty) in plain text. No comparison, no DPS calculation, no visual hierarchy.
- **Tasks**:
  - [ ] **DPS Display**: Calculate and show effective DPS (Damage / FireRateInterval) for towers. More meaningful than raw damage + fire rate separately
  - [ ] **Stat Bars**: Replace plain text numbers with visual bars for HP, range, damage (normalized to max across all tower types for at-a-glance comparison)
  - [ ] **Tower Comparison**: When hovering a tower button while another tower is selected, show stat diff (+/- indicators)
  - [ ] **Kill Counter**: Track and display kills per tower (motivates strategic placement)
  - [ ] **Wave Stats Summary**: After each wave, show brief stats (damage, kills, towers lost)
  - [ ] **Enemy Info Enrichment**: Show enemy progress (% of path completed), current status effects (slow, etc.)
  - [ ] **Health Bars on Enemies**: Currently in backlog — implement colored health bars above enemy sprites (green > yellow > red gradient based on % HP)
  - [ ] **Tooltip System**: Hover over UI elements for explanatory text (especially useful for new players learning tower abilities)

### 7.2 Wave Preview UI
- **Priority**: P2 | **Effort**: M
- **Current State**: Listed in backlog but not implemented.
- **Tasks**:
  - [ ] Show upcoming wave composition before wave starts (enemy types, count, spawn points)
  - [ ] Display during intermission countdown (section 4.2)
  - [ ] Format: icon + count per enemy type, color-coded to match enemy colors
  - [ ] Optional: show total wave HP to help player gauge difficulty

---

## 8. Art & Asset Pipeline

### 8.1 Placeholder Tile Pack with Standard Tile Sizes
- **Priority**: P1 | **Effort**: M
- **Current State**: 3 solid-color source tiles in `terrain.png` (40x40 source, rendered at 32x32). No visual variety or edge blending.
- **Goal**: Replace with a cohesive tileset that supports auto-tiling (section 3.1) and looks like an actual game.
- **Tile Size Note**: Display tile size is already 32. Source tile size is currently 40 via `GameSettings.TerrainSourceTileSize`.
  - **Option A**: Keep 40x40 source tiles and continue scaling to 32 display
  - **Option B**: Move to native 32x32 source tiles and update `TerrainSourceTileSize`
- **Asset Packs to Evaluate**:
  - [Schwarnhild Basic Tileset (32x32)](https://schwarnhild.itch.io/basic-tileset-and-asset-pack-32x32-pixels) — simple, clean, good for prototyping
  - [itch.io 32x32 tag](https://itch.io/game-assets/tag-32x32) — browse for TD-appropriate packs
  - [Kenney Pixel Assets](https://kenney.nl/assets/tag:pixel) — high quality, public domain (CC0), consistent style
  - [Pixel Frog Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) — fantasy themed, may include tower/unit sprites
- **Tasks**:
  - [ ] Evaluate each pack for: tile variety, auto-tile compatibility (edge/corner variants), tower sprites, enemy sprites, UI elements
  - [ ] Decide source tile size policy (keep 40 source or move to native 32 source)
  - [ ] If moving source size: update `GameSettings.TerrainSourceTileSize`, source spritesheets, and Tiled tileset metadata
  - [ ] Create new `terrain.png` spritesheet (or multiple sheets) with selected assets
  - [ ] Update `Content.mgcb` if adding new texture files
  - [ ] Update Tiled tilesets to match new sprites

---

## 9. Existing Backlog (Carried Forward)

These items are from `CURRENT_STATE.md` and remain relevant:

### 9.1 Champion Debuff on Death
- **Priority**: P1 | **Effort**: S
- **Task**: Implement `Tower.UpdateChampionStatus(bool)` — when champion dies, apply stat debuffs to its generic towers (reduced damage, fire rate, or range). When champion revives, restore stats.

### 9.2 Sound System
- **Priority**: P2 | **Effort**: M
- **Task**: Implement SFX + BGM manager. Key sounds: tower fire, enemy death, wave start/end, ability activation, UI clicks. BGM: looping track per map or globally.

### 9.3 Enemy Variants
- **Priority**: P2 | **Effort**: S
- **Task**: Define archetype presets (Fast: low HP/high speed, Tank: high HP/low speed, Swarm: very low HP/very fast/low bounty). Can be implemented as named presets in wave JSON rather than code archetypes.

## 10. Suggested Implementation Order

A recommended sequence that respects dependencies and delivers playable value early:

| Phase | Items | Rationale |
|-------|-------|-----------|
| **Phase 1: Foundation** | ~~4.1 (cooldown placement system)~~, 4.2 (auto-waves), 5.1 (cannon crossing) | ~~Gold removed~~ (done), auto-waves, terrain constraints |
| **Phase 2: Combat & Tower Identity** | 1.1 (rename/rebalance), 9.1 (champion debuff) | Towers feel distinct and strategic with new cooldown system |
| **Phase 3: Healing Tower** | ~~2.1 (ChampionHealing + drones)~~, ~~2.2 (support ultimate + passive regen)~~ | Done — drone support champion with full ult + passive regen |
| **Phase 4: Visual & UX Upgrade** | 4.4 (entrance indicator), 8.1 (tile pack), 3.1 (auto-tiling), 7.1 (UI stats) | Game looks, feels, and communicates better |
| **Phase 5: Polish & Completeness** | 6.1 (wave JSON tooling), 3.2 (Tiled properties), 9.2 (sound), 4.3 (crowding), 7.2 (wave preview), 9.3 (enemy variants) | Tooling, mechanics deepening, and completeness |

## Open Questions & Design Notes

1. **Tower theme/names**: Fantasy (Archer/Catapult), Military (Turret/Mortar), Sci-fi (Laser/Railgun)? This affects art direction.
2. **Cooldown system tuning**: Values live in each `*TowerStats.cs` — tune `BaseCooldown` and `CooldownPenalty` per type. WallSegment intentionally zero.
3. **Source tile size policy**: Keep 40x40 source art with scaling, or move to native 32x32 source assets and update `TerrainSourceTileSize`?
7. **Crowding approach**: Option A (speed penalty, simple) vs Option C (sub-grid, major refactor)? Recommend A first.
8. **Enemy health bars**: Simple bar above sprite, or also show numerical HP? Bars-only is cleaner for dense waves.
