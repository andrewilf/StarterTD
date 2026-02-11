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
        F -- Manages --> I[TowerManager];
        F -- Manages --> J[InputManager];
        F -- Manages --> K[UIPanel];
        F -- Manages --> N[FloatingTexts];
        F -- Manages --> O[AoEEffects];
    end
```

## Data Flow

- **Down**: Scene passes data to managers via `Update()` args (e.g. enemies list).
- **Up**: Managers notify Scene via `Action<T>` callbacks.
- **Strict Isolation**: Managers never reference each other directly.
- **Rendering**: `SpriteBatch` and fonts passed down through `Draw()` calls.

## Key Non-Obvious Patterns

- **Towers as overlays**: `Tile.OccupyingTower` holds a Tower reference; underlying `TileType` never changes after init. Movement cost checks the tower first, then terrain.
- **Tower Types**: Enum-based with separate Generic (Gun, Cannon) and Champion (ChampionGun, ChampionCannon) variants. Check with `TowerType.IsChampion()` extension method.
- **Champion Tracking**: `TowerManager._placedChampions` enforces one-per-type rule during placement validation.
- **DrawScale**: All towers have `Vector2 DrawScale` (Generics: {1.0, 1.0}, Champions: {1.0, 1.5}). Scaling is upward-only; health/capacity bars use `SpriteSize * DrawScale.Y` for Y positioning.
- **Debuff Hook**: `Tower.UpdateChampionStatus(bool isChampionAlive)` is virtual for future system when Champions die.
- **AoE callback chain**: `Projectile.OnAOEImpact` → `Tower` → `TowerManager` → `GameplayScene` spawns the visual effect.
- **WaveManager** takes `Func<List<Point>>` so each spawned enemy gets the latest path.
