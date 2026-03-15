# StarterTD

A Tower Defense game built with **MonoGame** and **.NET 9**.

## Current Features
* **Dynamic Mazing**: Dijkstra pathfinding with per-tower movement costs for strategic maze control.
* **Tiled Maps**: Maps are `.tmx` files in `Content/Maps/`, edited in [Tiled map editor](https://www.mapeditor.org).
* **Multi-Spawn Lanes**: Maps support multiple named spawn/exit points (`spawn_a`/`exit_a`, `spawn_b`/`exit_b`, etc.), each with an independent path.
* **JSON Wave Config**: Waves are defined in `Content/Waves/{mapId}.json` — each enemy entry specifies spawn point, timing, and stats. Falls back to hardcoded waves if no file exists.
* **Entrance Warnings**: Per-lane spawn indicators render in world-space as pulsing `!` markers with flash/burst effects; warnings are spawn-driven with speed-based pre-spawn lead time and a 5s per-lane cooldown to avoid spam.
* **Tower System**: 3 Generic (Gun, Cannon, Walling) + 4 Champion tower types (ChampionGun, ChampionCannon, ChampionWalling, ChampionHealing) with walking, abilities, and blocking capacity.
* **ChampionHealing Modes + Ult**: ChampionHealing deploys 3 healing drones (non-overlapping heal targets), passively regenerates `2 HP/s` (1s ticks), and swaps between Healing/Attack modes. Ult uses a shared `50s` cooldown: Healing mode grants a 15s support window (drone refill + free drone healing + +30% attack speed for attacking towers), while Attack mode arms 5 instant railgun shots (`+75%` main-hit damage, `50%` pierce/impact AoE damage, `1.5x` base fire interval during charged shots).
* **Enemy Combat**: State machine (Moving/Attacking) with tower engagement system.
* **Game Loop**: Map Selection → Gameplay → Victory/Defeat.
* **Windowing**: Launches in windowed maximized mode with native window controls. Map selection includes an `Exit` button.

## Build & Run
    # Restore dependencies
    dotnet restore

    # Build the project
    dotnet build

    # Run the game
    dotnet run

    # Format with CSharpier
    dotnet csharpier format .

## Controls

| Input | Context | Action |
| :--- | :--- | :--- |
| **Left Click** | Map Selection | Select Map |
| **Left Click** | Map Selection | Click `Exit` button to quit |
| **Left Click** | Gameplay | Place Tower |
| **Key 'R'** | Game Over | Restart / Return to Menu |

## Map Workflow

Maps are `.tmx` files created in [Tiled map editor](https://www.mapeditor.org) (free).
The tileset is `Content/Maps/terrain32.png` — a horizontal spritesheet, currently 32×32px source tiles (rendered at 32×32 in-game):

| Column | GID | Tile type | Meaning |
| :--- | :--- | :--- | :--- |
| 1 | 1 | HighGround | Towers can be placed; enemies cannot walk here |
| 2 | 2 | Path | Walkable; towers can also be placed |
| 3 | 3 | Rock | Impassable and unbuildable |

### Create a new map

1. Open Tiled → **File → New → New Map**
   - Orientation: Orthogonal, Layer Format: **CSV**
   - Map size: **20 × 15** tiles, Tile size: **32 × 32** px (matches current source assets)
2. Add the tileset: click **+** in the Tilesets panel → choose `Content/Maps/terrain32.png`, tile size 32×32
3. Paint a **Terrain** tile layer using GIDs 1/2/3
4. Add an **Object Layer** named `Markers` with point objects for spawn and exit tiles:
   - At minimum: one object named `spawn` and one named `exit`
   - For multiple lanes: use `spawn_a`/`exit_a`, `spawn_b`/`exit_b`, etc. Lane pairing is by suffix — `spawn_a` routes to `exit_a`
   - Pixel coords = `col × tilewidth`, `row × tileheight` (top-left of tile)
5. Optionally set a display name: **Map → Map Properties → +** → string property `name`
6. Save as `Content/Maps/<your_id>.tmx`
7. Run `dotnet build` — the file is picked up automatically via the `*.tmx` glob in the project

### Edit or remove a map

Open the `.tmx` in Tiled, save, then build. To remove a map, delete its `.tmx` file. No other changes needed.

## Wave Workflow

Each map can have a matching wave file at `Content/Waves/{mapId}.json`. Drop the file in and rebuild — it is picked up automatically via the `*.json` glob.

### Schema

```json
{
  "waves": [
    {
      "wave": 1,
      "spawns": [
        {
          "at": 0.0,
          "spawnPoint": "spawn_a",
          "name": "Goblin",
          "health": 300,
          "speed": 90,
          "attackDamage": 5,
          "color": "Purple"
        }
      ]
    }
  ]
}
```

| Field | Type | Description |
| :--- | :--- | :--- |
| `at` | float | Seconds from wave start when this enemy spawns |
| `spawnPoint` | string | Must match a named spawn object in the map's Markers layer |
| `color` | string | Any `Microsoft.Xna.Framework.Color` property name (e.g. `"Red"`, `"Cyan"`) |

If no JSON file exists for the selected map, the game falls back to 5 built-in waves spawning from `"spawn"`.

### Tileset tips

- Keep the tileset image at exactly **96 × 32 px** (3 tiles of 32×32 source). Runtime display matches source dimensions.
- `terrain32.png` is embedded in each `.tmx` via the tileset reference — the image path is used by Tiled and also loaded at runtime via `Maps/terrain32` content asset.

## License
MIT
