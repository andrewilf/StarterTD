using System.Collections.Generic;

namespace StarterTD.Engine;

/// <summary>
/// A single enemy spawn event on the match timeline.
/// All stats are fully specified inline — no shared enemy archetype lookup.
/// </summary>
public record SpawnEntry(
    /// <summary>Absolute seconds from match start when this enemy spawns.</summary>
    float At,
    /// <summary>Must match a key in Map.ActivePaths (e.g. "spawn", "spawn_a").</summary>
    string SpawnPoint,
    string Name,
    float Health,
    float Speed,
    int AttackDamage,
    /// <summary>XNA Color property name (e.g. "Purple", "Red"). Case-insensitive.</summary>
    string Color
);

/// <summary>Root object of a per-map spawn schedule JSON file.</summary>
public record SpawnScheduleData(List<SpawnEntry> Spawns);
