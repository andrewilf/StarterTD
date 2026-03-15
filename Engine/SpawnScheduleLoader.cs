using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// Loads spawn schedules from Content/SpawnSchedules/{mapId}.json.
/// Returns null when no file exists so callers can fall back to a built-in schedule.
/// </summary>
public static class SpawnScheduleLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Tries to load spawn data for the given map ID.
    /// Returns null if Content/SpawnSchedules/{mapId}.json does not exist.
    /// Throws on malformed JSON.
    /// </summary>
    public static SpawnScheduleData? TryLoad(string mapId)
    {
        string path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Content",
            "SpawnSchedules",
            $"{mapId}.json"
        );

        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SpawnScheduleData>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                $"Spawn schedule file '{path}': failed to deserialize"
            );
    }

    /// <summary>
    /// Resolves a color name string (e.g. "Purple") to an XNA Color via property lookup.
    /// Falls back to Color.White if the name is not a valid Color property.
    /// </summary>
    public static Color ParseColor(string colorName)
    {
        var prop = typeof(Color).GetProperty(
            colorName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
        );

        if (prop?.GetValue(null) is Color c)
            return c;

        return Color.White;
    }
}
