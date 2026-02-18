# StarterTD

A Tower Defense game built with **MonoGame** and **.NET 9**.

## Current Features
* **Dynamic Mazing**: Dijkstra pathfinding with per-tower movement costs for strategic maze control.
* **Tiled Maps**: Maps are `.tmx` files in `Content/Maps/`, edited in [Tiled map editor](https://www.mapeditor.org).
* **Tower System**: 2 tower types (Gun, Cannon) with blocking capacity.
* **Enemy Combat**: State machine (Moving/Attacking) with tower engagement system.
* **Game Loop**: Map Selection → Gameplay → Victory/Defeat.

## Build & Run
    # Restore dependencies
    dotnet restore

    # Build the project
    dotnet build

    # Run the game
    dotnet run

    # Lint the code with CSharpier
    dotnet csharpier format .

## Controls

| Input | Context | Action |
| :--- | :--- | :--- |
| **Left Click** | Map Selection | Select Map |
| **Left Click** | Gameplay | Place Tower |
| **Key 'R'** | Game Over | Restart / Return to Menu |

## Map Workflow

Maps are `.tmx` files created in [Tiled map editor](https://www.mapeditor.org) (free).
The tileset is `Content/Maps/terrain.png` — a horizontal spritesheet, 40×40px per tile:

| Column | GID | Tile type | Meaning |
| :--- | :--- | :--- | :--- |
| 1 | 1 | HighGround | Towers can be placed; enemies cannot walk here |
| 2 | 2 | Path | Walkable; towers can also be placed |
| 3 | 3 | Rock | Impassable and unbuildable |

### Create a new map

1. Open Tiled → **File → New → New Map**
   - Orientation: Orthogonal, Layer Format: **CSV**
   - Map size: **20 × 15** tiles, Tile size: **40 × 40** px
2. Add the tileset: click **+** in the Tilesets panel → choose `Content/Maps/terrain.png`, tile size 40×40
3. Paint a **Terrain** tile layer using GIDs 1/2/3
4. Add an **Object Layer** named `Markers` with two point objects:
   - Name `spawn` — top-left corner of the spawn tile (pixel = `col × 40`, `row × 40`)
   - Name `exit` — top-left corner of the exit tile
5. Optionally set a display name: **Map → Map Properties → +** → string property `name`
6. Save as `Content/Maps/<your_id>.tmx`
7. Run `bash sync_maps.sh` to register the map (see below)
8. Run `dotnet build` to verify

### Edit an existing map

Open the `.tmx` file in Tiled, make changes, save. Run the game — changes are picked up immediately on next build (the file is copied to the output directory automatically). No script needed.

### Remove a map

1. Delete the `.tmx` file from `Content/Maps/`
2. Run `bash sync_maps.sh` to deregister it

### sync_maps.sh

`sync_maps.sh` automatically syncs `StarterTD.csproj` and `Engine/MapData.cs` to match whatever `.tmx` files are present in `Content/Maps/`. Run it any time you add or remove a map file.

The script requires **bash** (not `sh`). Use `bash sync_maps.sh` or run it directly with `./` (which reads the shebang). Do **not** use `sh sync_maps.sh` — macOS's default `sh` is POSIX-only and will error.

```bash
# Preview changes without writing anything
bash sync_maps.sh --dry-run

# Apply changes
bash sync_maps.sh
```

### Tileset tips

- Keep the tileset image at exactly **120 × 40 px** (3 tiles of 40×40). Adding tile types requires updating `TileType.cs`, `TileData.cs`, and `TmxLoader.cs`.
- `terrain.png` is embedded in each `.tmx` via the tileset reference — the image path (`terrain.png`) is only used by Tiled's editor display and is not loaded at runtime.

## License
MIT
