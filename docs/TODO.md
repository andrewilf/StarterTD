# Game Development TODO

## Legend
- **Priority**: P0 (blocker/critical), P1 (high), P2 (medium), P3 (nice-to-have)
- **Effort**: S (small, single system), M (medium, 2-3 systems), L (large, cross-cutting), XL (multi-session)
- **Status**: `[ ]` not started, `[~]` in progress, `[x]` done

---

## 1. Tower Rework & Rebalance

### 1.1 Rename & Rebalance Towers
- **Priority**: P1 | **Effort**: S
- **Current State**: 5 tower types (Gun, Cannon, Walling, ChampionGun, ChampionCannon, ChampionWalling) with placeholder balance values.
- **Tasks**:
  - [ ] Decide on new tower names and theme (fantasy, sci-fi, etc.)
  - [ ] Rebalance stat values (damage, fire rate, range, cost, HP, block capacity)
  - [ ] Update `TowerType` enum, all `*Tower.cs` stat files, `TowerData.GetStats()`, and UI labels
  - [ ] Verify wave JSON enemy stats still make sense against new tower numbers
- **Note**: Coordinate with cooldown system (section 5.1) — towers now free to place, so balance becomes purely DPS/cost-effectiveness.

---

## 2. Healing Tower Mechanics (Dual-Mode Tower Type)

### 2.1 Healing Tower — Toggle Mode System
- **Priority**: P2 | **Effort**: M
- **Concept**: A dual-mode tower that **heals nearby allied towers in healing mode** or **attacks as a sniper tower in attack mode**. Toggle between modes via world-space UI button.
- **Healing Mode**:
  - [ ] Create `HealTower` type (enum, stats, registration)
  - [ ] Stats: heal amount per tick, heal interval, heal range (circular), cost, HP
  - [ ] Healing target selection: lowest HP ally tower in range
  - [ ] Implement `Tower.Heal(float amount)`: clamp to MaxHealth, skip if full HP
  - [ ] Visual: green pulse/ring effect on heal tick, green floating text showing heal amount
  - [ ] No projectile — instant heal
- **Attack Mode (Sniper)**:
  - [ ] Switch to single-target high-damage sniper tower (follows gun tower targeting: lowest HP enemy)
  - [ ] Stats differ from healing mode: damage, fire rate, range, attack projectile type
  - [ ] Projectile type: "railgun" — piercing ammo that travels further, may pass through targets or explode on impact
  - [ ] Visual distinction in UI: icon/label changes to indicate mode
- **Mode Toggle**:
  - [ ] World-space "H/S" button (or icon) to switch modes (similar to wall "+" button pattern)
  - [ ] Persist selection until toggled again
  - [ ] When switching modes, interrupt current action (finish attack/heal in progress, reset targeting)
  - [ ] Cooldown-based system: placing new champions increases cooldown before next tower placement (see section 5.1 below)

### 2.2 Healing Tower — Champion Variant
- **Priority**: P3 | **Effort**: S
- **Tasks**:
  - [ ] Create `ChampionHealTower` (free, walkable, starts in healing mode)
  - [ ] **Healing Mode Ultimate**: "Healing Overdrive" — burst heal all allied towers on the map for a percentage of their max HP; sustained AoE healing aura around champion for duration
  - [ ] **Attack Mode Ultimate**: "Railgun Overcharge" — fires a massive railgun shot that pierces through all enemies in a line; applies a slow debuff on hit
  - [ ] Register variant mappings, add UI elements

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
- **Complexity Warning**: Auto-tiling touches rendering, map loading, and runtime tile changes. Plan carefully before implementing.

### 3.2 Tiled Custom Properties Integration
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

## 4. Gameplay Flow & Tower Placement System

### 4.1 Replace Money System with Cooldown-Based Placement
- **Priority**: P1 | **Effort**: L
- **Current State**: Towers cost gold; player earns gold from enemy kills and wave completion.
- **New System**: Remove all gold mechanics. Tower placement operates on a **cooldown timer**:
  - [ ] Remove `GameplayScene._money`, `_moneyDisplay`, all gold earning logic
  - [ ] Remove cost values from all `TowerStats` — towers are now **free to place**
  - [ ] Add global `TowerPlacementCooldown` timer to `GameplayScene` (base duration, e.g., 15 seconds)
  - [ ] Cooldown **increases** each time a champion is placed:
    - Placing a champion adds a fixed duration to the current cooldown (e.g., +10 seconds per champion)
    - Generics increase cooldown less or not at all (configurable)
  - [ ] UI: Show cooldown timer when a tower is being placed (e.g., "Cooldown: 8.2s" or a progress bar)
  - [ ] Tower placement is **blocked** until cooldown expires
  - [ ] After cooldown expires, next tower placement **resets the timer** to base value and enters cooldown again immediately
- **Design Rationale**: Creates pacing mechanics where placing towers strategically (fewer champions = faster placement) becomes the core decision-making loop instead of resource scarcity.
- **Tasks**:
  - [ ] Implement `TowerPlacementCooldown` state machine in `GameplayScene`
  - [ ] Update tower placement UI to show cooldown status
  - [ ] Remove all gold-related code paths
  - [ ] Balance cooldown durations (base, per-champion increment)

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
- **Current State**: Champion towers freely move between all terrain types via `TowerPathfinder`.
- **Tasks**:
  - [ ] Extend `TowerPathfinder.ComputePath()` to accept terrain-type constraint (or check per-tower)
  - [ ] For `ChampionCannonTower`: when computing movement path, block transitions between `TileType.HighGround` ↔ `TileType.Path`
  - [ ] Movement already blocked if adjacent tile is unreachable; this adds a terrain-type barrier on top
  - [ ] Detect attempted crossing: if current tile is High Ground, disallow move to Path tile and vice versa
  - [ ] UI feedback: grayed-out or disabled movement button if target is on opposite terrain type
- **Note**: This applies only to `ChampionCannonTower`, not `ChampionGunTower` or `ChampionWallingTower` (for now)

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
  - [ ] **Wave Stats Summary**: After each wave, show brief stats (damage dealt, enemies killed, towers lost)
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
- **Current State**: 3 solid-color 40x40 tiles in `terrain.png`. No visual variety or edge blending.
- **Goal**: Replace with a cohesive tileset that supports auto-tiling (section 3.1) and looks like an actual game.
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
| **Phase 1: Foundation** | 4.1 (cooldown placement system), 4.2 (auto-waves), 5.1 (cannon crossing) | Remove gold, implement new pacing mechanics, terrain constraints |
| **Phase 2: Combat & Tower Identity** | 1.1 (rename/rebalance), 9.1 (champion debuff) | Towers feel distinct and strategic with new cooldown system |
| **Phase 3: Healing Tower** | 2.1 (dual-mode healing/sniper tower), 2.2 (champion variant) | New tower type with unique mechanics and ultimates |
| **Phase 4: Visual & UX Upgrade** | 4.4 (entrance indicator), 8.1 (tile pack), 3.1 (auto-tiling), 7.1 (UI stats) | Game looks, feels, and communicates better |
| **Phase 5: Polish & Completeness** | 6.1 (wave JSON tooling), 3.2 (Tiled properties), 9.2 (sound), 4.3 (crowding), 7.2 (wave preview), 9.3 (enemy variants) | Tooling, mechanics deepening, and completeness |

---

## Open Questions & Design Notes

1. **Tower theme/names**: Fantasy (Archer/Catapult), Military (Turret/Mortar), Sci-fi (Laser/Railgun)? This affects art direction.
2. **Cooldown system tuning**: Base placement cooldown (e.g., 15s), per-champion increment (e.g., +10s), per-generic increment (e.g., 0s or +2s)?
3. **Healing tower toggle UX**: Should mode-switch be instant or have a brief wind-up/transition animation?
4. **Healing tower rail-gun**: Piercing rounds vs. explosion-on-impact? Does it apply slow to all enemies hit or only on landing?
5. **Tile size change**: Switching from 40x40 to 32x32 unlocks free asset packs but requires a refactor pass. Approve?
7. **Crowding approach**: Option A (speed penalty, simple) vs. Option C (sub-grid, major refactor)? Recommend A first.
8. **Enemy health bars**: Simple bar above sprite, or also show numerical HP? Bars-only is cleaner for dense waves.
9. **Entrance indicator distance**: How far away should the "enemy coming" indicator activate? (e.g., 1 tile, 2 tiles, configurable per spawn)
