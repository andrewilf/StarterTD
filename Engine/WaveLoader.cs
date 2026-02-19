using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// Loads wave definitions from Content/Waves/{mapId}.json.
/// Returns null when no file exists so callers can fall back to hardcoded waves.
/// </summary>
public static class WaveLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Tries to load wave data for the given map ID.
    /// Returns null if Content/Waves/{mapId}.json does not exist.
    /// Throws on malformed JSON.
    /// </summary>
    public static List<WaveData>? TryLoad(string mapId)
    {
        string path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Content",
            "Waves",
            $"{mapId}.json"
        );

        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        var file =
            JsonSerializer.Deserialize<WaveFileData>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Wave file '{path}': failed to deserialize");

        return file.Waves;
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
