using System.Collections.Generic;

namespace StarterTD.Engine;

/// <summary>
/// A single enemy spawn event within a wave.
/// All stats are fully specified inline — no shared enemy archetype lookup.
/// </summary>
public record SpawnEntry(
    /// <summary>Seconds from wave start at which this enemy spawns.</summary>
    float At,
    /// <summary>Must match a key in Map.ActivePaths (e.g. "spawn", "spawn_a").</summary>
    string SpawnPoint,
    string Name,
    float Health,
    float Speed,
    int Bounty,
    int AttackDamage,
    /// <summary>XNA Color property name (e.g. "Purple", "Red"). Case-insensitive.</summary>
    string Color
);

/// <summary>Wave definition loaded from JSON. Wave number is 1-based for human authoring.</summary>
public record WaveData(int Wave, List<SpawnEntry> Spawns);

/// <summary>Root object of a per-map wave JSON file.</summary>
public record WaveFileData(List<WaveData> Waves);
