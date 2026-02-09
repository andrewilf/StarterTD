'''# C# Concepts for TypeScript/Python Developers

This document is a "Rosetta Stone" to help you, a TypeScript/Python developer, understand some key C# concepts used in this MonoGame project.

## `struct` vs `class` (Value vs. Reference Types)

This is one of the most fundamental distinctions in C# and has a big impact on performance and memory.

| Concept | C# `struct` | C# `class` | Python/TypeScript Analogy |
|---|---|---|---|
| **Type** | Value Type | Reference Type | In Python, think `int`, `float` (immutable primitives) vs. `list`, `dict` (mutable objects). In JS/TS, think `number`, `boolean` vs. `object`, `array`. |
| **Memory** | Stored directly where the variable is declared (e.g., on the stack). | A pointer/reference is stored, pointing to an object on the heap. | When you do `b = a` with a list in Python, both `a` and `b` point to the *same* list in memory. Modifying `b` also modifies `a`. This is how C# classes work. |
| **Assignment** | `var b = a;` creates a **full copy**. Modifying `b` does **not** affect `a`. | `var b = a;` copies the **reference**. Both variables point to the **same object**. Modifying `b` **does** affect `a`. | Same as the Python/TS analogy. |
| **Usage in this Project** | `Vector2`, `Point`, `Color`, `Rectangle`. These are small, data-only structures. | `Game1`, `SceneManager`, `Tower`, `Enemy`. These are larger objects with complex behavior (methods). |

**Why it matters:** Using `struct` for small, frequently-created objects like `Vector2` is much more efficient. It avoids creating lots of garbage on the heap that the garbage collector would have to clean up, which can cause performance stutters (hiccups) in games.

> **In this project:**
> -   `Microsoft.Xna.Framework.Vector2` is a `struct`. It represents a mathematical vector for things like position, direction, and velocity.
> -   `Microsoft.Xna.Framework.Point` is a `struct`. It represents an integer coordinate on the grid (column, row).
> -   `StarterTD.Entities.Tower` is a `class`. When you pass a tower to a method, you are passing a reference to the original tower object, not a copy.

## `Vector2` (Math) vs. `Point` (Grid Coords)

Both represent a 2D coordinate, but they have different types and purposes.

| Type | C# Type | Data Type | Purpose | Example Usage |
|---|---|---|---|---|
| **Vector2** | `struct` | `float` | World-space positions, physics calculations, direction, velocity. Continuous space. | `enemy.Position`, `tower.WorldPosition`, `projectile.Speed` |
| **Point** | `struct` | `int` | Grid-based coordinates, array indices. Discrete space. | `tower.GridPosition`, `map.Tiles[x, y]` |

**Analogy:** Think of `Vector2` as GPS coordinates (latitude/longitude) and `Point` as a street address (123 Main St). One is continuous and precise for movement, the other is discrete for locating something on a grid.

We use helper methods in the `Map` class to convert between them:

-   `Map.WorldToGrid(Vector2 worldPos)`: Takes a pixel position and tells you which grid tile it's in.
-   `Map.GridToWorld(Point gridPos)`: Takes a grid tile coordinate and gives you the pixel position of its center.

## Generic `List<T>` vs. Python Lists / TS Arrays

C# is a statically-typed language, so its collections are also typed. You can't have a list with mixed types like you can in Python.

| Feature | C# `List<T>` | Python `list` / TS `Array<T>` |
|---|---|---|
| **Typing** | **Strongly Typed**. `List<Enemy>` can *only* hold `Enemy` objects (or objects that inherit from `Enemy`). | **Dynamically Typed** (Python) or **Strongly Typed** (TS). A Python `list` can hold `[1, "hello", True]`. A TS `Array<any>` can do the same, but `Array<Enemy>` is enforced by the compiler. |
| **Declaration** | `List<IEnemy> _enemies = new List<IEnemy>();` | `enemies = []` (Python) or `let enemies: Enemy[] = [];` (TS) |
| **Performance** | Generally very fast. Because the type `T` is known, the compiler can optimize memory layout and access. | Python lists are flexible but have some overhead. V8 (JavaScript) is highly optimized for arrays. |

**What `T` means:** The `T` is a placeholder for a type parameter. This is called **Generics**. When you write `List<Enemy>`, you are creating a specialized version of the `List` class that is optimized to work specifically with `Enemy` objects.

> **In this project:**
> -   `_enemies` in `GameplayScene` is a `List<IEnemy>`. This means it can hold any object that implements the `IEnemy` interface.
> -   `_towers` in `TowerManager` is a `List<Tower>`. It can only hold `Tower` objects.
> -   `PathPoints` in `Map` is a `List<Point>`. It can only hold `Point` structs.
'''
