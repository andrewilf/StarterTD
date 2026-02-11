# Architecture Map

## Core Class Relationships

```mermaid
graph TD
    A[Program.cs] --> B[Game1.cs];
    B --> C{SceneManager};
    C --> D{Active Scene (IScene)};
    D --> E[MapSelectionScene];
    D --> F[GameplayScene];

    subgraph GameplayScene Implementation
        F -- Manages --> G[Map];
        F -- Manages --> H[WaveManager];
        F -- Manages --> CM[ChampionManager];
        F -- Manages --> I[TowerManager];
        F -- Manages --> J[InputManager];
        F -- Manages --> K[UIPanel];
        F -- Manages --> N[FloatingTexts];
        F -- Manages --> O[AoEEffects];
        CM -.-> I;
    end
```

## Data Flow

- **Down**: Scene passes data to managers via `Update()` args (e.g. enemies list).
- **Up**: Managers notify Scene via `Action<T>` callbacks.
- **Strict Isolation**: Managers never reference each other directly.
- **Rendering**: `SpriteBatch` and fonts passed down through `Draw()` calls.

## Key Non-Obvious Patterns

- **Towers as overlays**: `Tile.OccupyingTower` holds a Tower reference; underlying `TileType` never changes after init. Movement cost checks the tower first, then terrain.
- **Tower Types**: Enum-based with separate Generic (Gun, Cannon) and Champion (ChampionGun, ChampionCannon) variants. Map between them with `TowerType.GetChampionVariant()` / `GetGenericVariant()` extension methods.
- **Champion Placement Rules** (via `ChampionManager`):
  - Only one Champion per type can be alive on map
  - Global 10s cooldown blocks all Champion placements after any Champion is placed
  - Individual 15s respawn cooldown per Champion type after death
  - Generic towers can ONLY be placed if their Champion variant is alive
  - Champion death triggers `Tower.UpdateChampionStatus(false)` on matching Generic towers
- **DrawScale**: All towers have `Vector2 DrawScale` (Generics: {1.0, 1.0}, Champions: {1.0, 1.5}). Scaling is upward-only; health/capacity bars use `SpriteSize * DrawScale.Y` for Y positioning.
- **Debuff Hook**: `Tower.UpdateChampionStatus(bool isChampionAlive)` is virtual for implementing stat debuffs when Champions die.
- **AoE callback chain**: `Projectile.OnAOEImpact` → `Tower` → `TowerManager` → `GameplayScene` spawns the visual effect.
- **WaveManager** takes `Func<List<Point>>` so each spawned enemy gets the latest path.
