# Game Development TODO

## Legend
- **Priority**: P0 (blocker/critical), P1 (high), P2 (medium), P3 (nice-to-have)
- **Effort**: S (small, single system), M (medium, 2-3 systems), L (large, cross-cutting), XL (multi-session)
- **Status**: `[ ]` not started, `[~]` in progress, `[x]` done

---

## 1. Bug Fixes

### 1.1 Enemy Pathing — Exit Selection After Tile Destruction
- **Priority**: P0 | **Effort**: S
- **Problem**: When a HighGround tile is destroyed, enemies reroute to the *closest* exit instead of staying committed to their original spawn-lane exit (e.g., `spawn_a → exit_a`).
- **Root Cause**: The reroute call likely recalculates using a global heatmap rather than the enemy's assigned lane heatmap.
- **Fix**: When calling `enemy.UpdatePath(map)`, ensure each enemy uses the heatmap for its *original* exit assignment (stored on the enemy at spawn time), not the nearest exit.
- **Acceptance Criteria**: An enemy spawned from `spawn_a` always paths to `exit_a`, even after terrain changes mid-wave.

---

## 2. Tower Rework & Rebalance

### 2.1 Rename & Rebalance Towers
- **Priority**: P1 | **Effort**: S
- **Current State**: 4 tower types (Gun, Cannon, ChampionGun, ChampionCannon) with placeholder balance values.
- **Tasks**:
  - [ ] Decide on new tower names and theme (fantasy, sci-fi, etc.)
  - [ ] Rebalance stat values (damage, fire rate, range, cost, HP, block capacity)
  - [ ] Update `TowerType` enum, all `*Tower.cs` stat files, `TowerData.GetStats()`, and UI labels
  - [ ] Verify wave JSON enemy stats still make sense against new tower numbers
- **Note**: Coordinate with wave rebalance (section 6.1) — changing tower DPS affects wave difficulty curves.

### 2.2 Gun Tower — Lowest HP Targeting (Aggro Logic)
- **Priority**: P1 | **Effort**: S
- **Current Behavior**: Towers target enemies but targeting priority is not specialized.
- **Change**: Gun-type towers should prioritize the enemy with the **lowest current HP** within range (finish off weak enemies to reduce total enemy count quickly).
- **Tasks**:
  - [ ] Add a `TargetingStrategy` enum or delegate to `TowerStats` (e.g., `LowestHP`, `MostGrouped`, `First`, `Closest`)
  - [ ] Implement `LowestHP` selection: filter enemies in range, sort by `CurrentHealth` ascending, pick first
  - [ ] Assign `LowestHP` to Gun and ChampionGun tower stats
  - [ ] Unit-testable: targeting logic should be a pure function over a list of enemies + tower position + range

### 2.3 Cannon Tower — Most Grouped Targeting (Aggro Logic)
- **Priority**: P1 | **Effort**: M
- **Change**: Cannon-type towers should target the enemy that has the **most other enemies within the cannon's AoE radius** around it — maximizing splash value.
- **Tasks**:
  - [ ] Implement `MostGrouped` selection: for each enemy in range, count how many *other* enemies fall within `AoERadius` of that enemy's position, pick the one with the highest count (tie-break: lowest HP)
  - [ ] Assign `MostGrouped` to Cannon and ChampionCannon tower stats
  - [ ] Performance consideration: this is O(n*m) per tower per frame where n=enemies in range, m=total enemies. If enemy count is high (50+), consider spatial partitioning or only recalculating every few frames
- **Design Note**: This makes cannons *feel* strategic — players will see cannons waiting for clumps rather than wasting splash on lone targets.

### 2.4 Cannon Champion Super Ability — "Orbital Strike"
- **Priority**: P2 | **Effort**: M
- **Current State**: Champion abilities apply a damage/fire-rate buff to champion + generics. The cannon super is just a stat buff.
- **New Design**: When activated, the cannon champion fires a cinematic **Orbital Strike**:
  1. Screen darkens (semi-transparent black overlay, ~60% opacity)
  2. Game pauses for ~3 seconds (freeze `Update()` on gameplay entities, but still draw + animate the strike effect)
  3. Large AoE explosion centered on the cannon champion's target (or a clicked position — decide which)
  4. Deals heavy damage to all enemies in a large radius
  5. Screen returns to normal, gameplay resumes
- **Tasks**:
  - [ ] Implement a `CinematicPause` system: flag in `GameplayScene` that skips entity updates but still draws + allows effect animations
  - [ ] Create dark overlay rendering (draw a screen-sized black rect at 60% alpha before entity layer)
  - [ ] Design the strike visual: expanding circle, screen shake, particle sparks (or keep simple with a large AoE ring + flash)
  - [ ] Replace the cannon champion's `AbilityEffect` delegate with the new strike logic
  - [ ] Balance: damage amount, radius, cooldown — should feel powerful but not trivialize waves
- **Risk**: Pausing gameplay mid-wave is a significant UX change. Ensure it feels *cinematic* not *broken*. Consider a brief wind-up animation before the pause so it doesn't feel like a freeze.

---

## 3. Walling Tower Mechanics (New Tower Type)

### 3.1 Core Wall Tower Implementation
- **Priority**: P1 | **Effort**: L
- **Status**: [x] **Done**
- **What was built**:
  - `ChampionWallingTower`: free, walkable, no attack. Toggles wall-placement mode via world-space "+" button
  - `WallSegmentTower`: free, 30 HP, movement cost 10,000 (enemies strongly avoid but can attack through)
  - BFS-based connectivity (`BuildConnectedWallSet()`) determines which segments are in the champion's network
  - `TowerType` enum, stats files, and `TowerData` registration all in place

### 3.2 Wall Attack Range (Along-Wall Targeting)
- **Priority**: P1 | **Effort**: M
- **Status**: [ ] **Not started**
- **Concept**: Wall segments have a short-range attack hitting enemies **adjacent to the wall line**. Range extends along connected walls, not in a circle.
- **Tasks**:
  - [ ] Give `WallSegmentTower` a non-zero `Damage` and `FireRate` in stats
  - [ ] Define "wall line" range: BFS/linear scan along connected wall segments up to N tiles in each direction
  - [ ] Target selection within wall-line range (prefer non-slowed — see 3.5)
  - [ ] Visual: draw range indicator as a highlighted strip along the wall, not a circle
- **Design Decision Needed**: Does the attack hit *only* enemies on the wall tile, or also 1 tile deep perpendicularly?

### 3.3 Wall Decay — Exposure-Based Degradation
- **Priority**: P2 | **Effort**: M
- **Status**: [x] **Done**
- **What was built**:
  - Decay rate: 1 HP/sec per exposed cardinal side (max 4 HP/sec fully isolated)
  - Map boundaries count as exposed sides
  - **Suppressed** while champion is alive and directly adjacent to any wall in its network
  - BFS recalculates connectivity each frame — orphaned walls from mid-wave destruction decay immediately
- **Remaining**:
  - [ ] Visual feedback: tint decaying walls red/orange, particle crumble effect

### 3.4 Wall Tower — Slow Effect on Attack
- **Priority**: P2 | **Effort**: M
- **Status**: [ ] **Not started** (wall has no attack yet — depends on 3.2)
- **Concept**: Wall segment attacks apply a **slow debuff** to hit enemies.
- **Tasks**:
  - [ ] Implement `SlowEffect` on `Enemy`: speed multiplier (0.5x) + duration. Non-stacking (refresh on re-apply)
  - [ ] Apply slow on wall attack hit
  - [ ] Visual: tint slowed enemies blue
  - [ ] Slow only affects movement speed, not attack rate
- **Note**: First status effect in the game — design for extensibility (poison, stun future candidates)
- **Depends On**: 3.2 (Wall Attack)

### 3.5 Wall Tower — Prioritize Non-Slowed Enemies
- **Priority**: P2 | **Effort**: S
- **Status**: [ ] **Not started**
- **Concept**: Wall segments prefer targeting enemies not already slowed to maximize slow coverage.
- **Tasks**:
  - [ ] `PreferNonSlowed` targeting: non-slowed enemies first; fallback to lowest remaining slow duration
  - [ ] Assign to `WallSegmentTower` stats
- **Depends On**: 3.4 (Slow Effect)

### 3.6 Wall Champion Ability ("Fortify")
- **Priority**: P2 | **Effort**: S
- **Status**: [ ] **Not started** (`AbilityEffect` is `null` in current stats)
- **Tasks**:
  - [ ] Define "Fortify": all walls in network gain temporary HP regen or invulnerability for `AbilityDuration` seconds
  - [ ] Wire into existing champion ability button system
- **Note**: No generic wall variant is planned — walling champion has no generic counterpart by design

---

## 4. Healing Tower Mechanics (New Tower Type)

### 4.1 Core Healing Tower Implementation
- **Priority**: P2 | **Effort**: M
- **Concept**: A support tower that **heals nearby allied towers** instead of attacking enemies. First tower type with no offensive capability.
- **Tasks**:
  - [ ] Create `HealTower` type (enum, stats, registration)
  - [ ] Stats: heal amount per tick, heal interval, heal range (circular), cost, HP
  - [ ] Healing target selection: lowest HP ally tower in range (mirrors gun tower's enemy targeting philosophy)
  - [ ] Implement `Tower.Heal(float amount)`: clamp to MaxHealth, skip if full HP
  - [ ] Visual: green pulse/ring effect on heal tick, green floating text showing heal amount
  - [ ] No projectile — instant heal (or optional: slow-moving green projectile for visual clarity)
- **Design Decisions Needed**:
  - Can healing towers heal other healing towers? (Recommend: yes, but self-heal: no)
  - Can healing towers heal wall towers? (Recommend: yes — this creates wall + healer synergy)
  - Champion/Generic pairing? (Recommend: yes, follow established pattern)

### 4.2 Healing Tower — Champion Variant
- **Priority**: P3 | **Effort**: S
- **Tasks**:
  - [ ] Create `ChampionHealTower` (free, walkable, larger heal range, stronger heal)
  - [ ] Champion ability: "Mass Heal" — burst heal all allied towers on the map for a percentage of their max HP
  - [ ] Register variant mappings, add UI elements

---

## 5. Auto-Tiled Map Logic

### 5.1 Implement Auto-Tiling System
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
  - [ ] Create or source a tileset with edge/corner variants for each tile type (see section 8 for asset packs)
  - [ ] Update `TextureManager.DrawTile()` to select sprite variant based on neighbor context
  - [ ] Handle edge cases: map borders (treat out-of-bounds as a specific type), runtime tile changes (HighGround placement)
  - [ ] Recalculate auto-tile when tiles change at runtime (e.g., debug HighGround placement)
- **Complexity Warning**: Auto-tiling touches rendering, map loading, and runtime tile changes. Plan carefully before implementing.

### 5.2 Tiled Custom Properties Integration
- **Priority**: P2 | **Effort**: M
- **Reference**: https://doc.mapeditor.org/en/stable/manual/custom-properties/
- **Goal**: Use Tiled's custom properties to embed game-specific data directly in `.tmx` files, reducing hardcoded values.
- **Tasks**:
  - [ ] Extend `TmxLoader` to parse `<properties>` elements on tiles, objects, and layers
  - [ ] Potential uses:
    - Tile properties: movement cost overrides, buildable flag, visual variant hints
    - Object properties: enemy spawn delay, wave triggers, scripted events
    - Layer properties: rendering order, parallax, visibility toggles
  - [ ] Store parsed properties in `MapData` for runtime access

---

## 6. Wave & Gameplay Flow

### 6.1 Auto-Start Waves After First Manual Start
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
  - [ ] Consider a "fast forward" bonus: starting early gives bonus gold (reward aggressive play)

### 6.2 Enemy Crowding Mechanic
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
- **Interacts With**: Cannon "most grouped" targeting (2.3) — crowding makes cannons more valuable. Wall towers (3.x) — walls create natural chokepoints that trigger crowding.

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
  - [ ] **Wave Stats Summary**: After each wave, show brief stats (damage dealt, enemies killed, gold earned, towers lost)
  - [ ] **Enemy Info Enrichment**: Show enemy progress (% of path completed), current status effects (slow, etc.)
  - [ ] **Health Bars on Enemies**: Currently in backlog — implement colored health bars above enemy sprites (green > yellow > red gradient based on % HP)
  - [ ] **Tooltip System**: Hover over UI elements for explanatory text (especially useful for new players learning tower abilities)

### 7.2 Wave Preview UI
- **Priority**: P2 | **Effort**: M
- **Current State**: Listed in backlog but not implemented.
- **Tasks**:
  - [ ] Show upcoming wave composition before wave starts (enemy types, count, spawn points)
  - [ ] Display during intermission countdown (section 6.1)
  - [ ] Format: icon + count per enemy type, color-coded to match enemy colors
  - [ ] Optional: show total wave HP to help player gauge difficulty

---

## 8. Art & Asset Pipeline

### 8.1 Placeholder Tile Pack with Standard Tile Sizes
- **Priority**: P1 | **Effort**: M
- **Current State**: 3 solid-color 40x40 tiles in `terrain.png`. No visual variety or edge blending.
- **Goal**: Replace with a cohesive tileset that supports auto-tiling (section 5.1) and looks like an actual game.
- **Tile Size Decision**: Current game uses 40x40. Most free asset packs use **32x32** or **16x16**.
  - **Option A**: Switch to 32x32 (matches most free packs, industry standard for pixel art TD games)
  - **Option B**: Stay at 40x40 (avoids refactoring `GameSettings.TileSize` and all dependent calculations)
  - **Recommendation**: Switch to 32x32. The refactor is mechanical (change `TileSize` constant, update sprite sizes), and it unlocks access to a vast library of free assets.
- **Asset Packs to Evaluate**:
  - [Schwarnhild Basic Tileset (32x32)](https://schwarnhild.itch.io/basic-tileset-and-asset-pack-32x32-pixels) — simple, clean, good for prototyping
  - [itch.io 32x32 tag](https://itch.io/game-assets/tag-32x32) — browse for TD-appropriate packs
  - [Kenney Pixel Assets](https://kenney.nl/assets/tag:pixel) — high quality, public domain (CC0), consistent style
  - [Pixel Frog Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) — fantasy themed, may include tower/unit sprites
- **Tasks**:
  - [ ] Evaluate each pack for: tile variety, auto-tile compatibility (edge/corner variants), tower sprites, enemy sprites, UI elements
  - [ ] Decide on tile size (32x32 recommended)
  - [ ] If switching tile size: update `GameSettings.TileSize`, `TextureManager`, sprite dimensions, UI layout calculations, Tiled map tile size
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

---

## 10. Suggested Implementation Order

A recommended sequence that respects dependencies and delivers playable value early:

| Phase | Items | Rationale |
|-------|-------|-----------|
| **Phase 1: Foundation** | 1.1 (pathing bug), 2.1 (rename/rebalance), 6.1 (auto-waves) | Fix the critical bug, establish final tower identity, improve game flow |
| **Phase 2: Combat Depth** | 2.2 (gun aggro), 2.3 (cannon aggro), 9.1 (champion debuff) | Towers feel distinct and strategic |
| **Phase 3: Wall System** | ~~3.1~~ ~~3.3~~ (done), 3.2 (wall range), 3.4 (slow effect) | Major new mechanic, creates chokepoint gameplay |
| **Phase 4: Visual Upgrade** | 8.1 (tile pack), 5.1 (auto-tiling), 7.1 (UI stats) | Game looks and reads better |
| **Phase 5: Polish & Expand** | 3.5 (slow priority), 3.6 (wall generics), 4.1 (heal tower), 2.4 (cannon super) | Deepen mechanics |
| **Phase 6: Nice-to-Haves** | 6.2 (crowding), 5.2 (Tiled properties), 7.2 (wave preview), 9.2 (sound), 4.2 (heal champion) | Polish and completeness |

---

## Open Questions for PM

1. **Tower theme/names**: Fantasy (Archer/Catapult), Military (Turret/Mortar), Sci-fi (Laser/Railgun)? This affects art direction.
2. **Wall tower cost model**: Should walls be free like champions, cheap and spammable, or expensive and strategic?
3. **Cannon super — targeted or auto?**: Does the orbital strike hit the cannon's current target, or does the player click a location?
4. **Healing tower — does it heal enemies too?** (some TD games have neutral heal zones). Recommend: no, towers only.
5. **Tile size change**: Switching from 40x40 to 32x32 unlocks free asset packs but requires a refactor pass. Approve?
6. **Crowding approach**: Option A (speed penalty, simple) vs. Option C (sub-grid, major refactor)? Recommend A first.
7. **Enemy health bars**: Simple bar above sprite, or also show numerical HP? Bars-only is cleaner for dense waves.
