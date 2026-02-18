using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// Parses a Tiled .tmx XML file and produces a MapData with a pre-built TileGrid.
///
/// GID convention (matches tileset order in Tiled):
///   0 = empty → HighGround, 1 = HighGround, 2 = Path, 3 = Rock
///
/// Pixel coordinates in Tiled Object layers are divided by TileSize to get grid coords.
/// Only CSV encoding is supported — re-save in Tiled with Layer Format = CSV if this throws.
/// </summary>
public static class TmxLoader
{
    private static readonly Dictionary<int, TileType> GidToTileType = new()
    {
        { 0, TileType.HighGround },
        { 1, TileType.HighGround },
        { 2, TileType.Path },
        { 3, TileType.Rock },
    };

    /// <summary>
    /// Looks for Content/Maps/{mapId}.tmx relative to the executable directory.
    /// Returns null if the file does not exist (caller falls back to hardcoded definitions).
    /// Throws on malformed XML, missing layers, or missing spawn/exit objects.
    /// </summary>
    public static MapData? TryLoad(string mapId)
    {
        string path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Content",
            "Maps",
            $"{mapId}.tmx"
        );

        if (!File.Exists(path))
            return null;

        return Parse(path, mapId);
    }

    private static MapData Parse(string filePath, string mapId)
    {
        var doc = XDocument.Load(filePath);
        var root =
            doc.Root ?? throw new InvalidOperationException($"TMX '{filePath}': empty document");

        int columns = (int)root.Attribute("width")!;
        int rows = (int)root.Attribute("height")!;

        var layerElement =
            root.Element("layer")
            ?? throw new InvalidOperationException($"TMX '{filePath}': no <layer> element found");

        var dataElement =
            layerElement.Element("data")
            ?? throw new InvalidOperationException($"TMX '{filePath}': no <data> element in layer");

        string encoding = (string?)dataElement.Attribute("encoding") ?? "xml";
        if (encoding != "csv")
            throw new NotSupportedException(
                $"TMX '{filePath}': only CSV encoding is supported. "
                    + "In Tiled: Edit → Preferences → set Layer Format to CSV, then re-save."
            );

        TileType[,] grid = ParseCsvData(dataElement.Value, columns, rows);

        var objectGroup =
            root.Element("objectgroup")
            ?? throw new InvalidOperationException(
                $"TMX '{filePath}': no <objectgroup> element found. "
                    + "Add an Object Layer named 'Markers' with 'spawn' and 'exit' point objects."
            );

        Point spawnPoint = default;
        Point exitPoint = default;
        bool foundSpawn = false;
        bool foundExit = false;

        foreach (var obj in objectGroup.Elements("object"))
        {
            string? name = (string?)obj.Attribute("name");
            // Tiled stores top-left pixel coords; integer-divide by TileSize for grid coords
            int gridX = (int)(float.Parse((string)obj.Attribute("x")!) / GameSettings.TileSize);
            int gridY = (int)(float.Parse((string)obj.Attribute("y")!) / GameSettings.TileSize);

            if (name == "spawn")
            {
                spawnPoint = new Point(gridX, gridY);
                foundSpawn = true;
            }
            if (name == "exit")
            {
                exitPoint = new Point(gridX, gridY);
                foundExit = true;
            }
        }

        if (!foundSpawn)
            throw new InvalidOperationException(
                $"TMX '{filePath}': no point object named 'spawn' in the object layer"
            );
        if (!foundExit)
            throw new InvalidOperationException(
                $"TMX '{filePath}': no point object named 'exit' in the object layer"
            );

        string mapName = ReadNameProperty(root) ?? mapId;

        var mapData = new MapData(
            Name: mapName,
            Id: mapId,
            SpawnPoint: spawnPoint,
            ExitPoint: exitPoint,
            WalkableAreas: new List<Rectangle>(),
            Columns: columns,
            Rows: rows
        )
        {
            TileGrid = grid,
        };

        mapData.Validate();
        return mapData;
    }

    /// <summary>
    /// Parses CSV tile data into a [col, row] indexed TileType grid.
    /// Tiled emits row-major CSV (row 0 first, left to right), so index i maps to
    /// col = i % columns, row = i / columns.
    /// </summary>
    private static TileType[,] ParseCsvData(string csv, int columns, int rows)
    {
        var grid = new TileType[columns, rows];

        string[] tokens = csv.Split(
            new[] { ',', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries
        );

        for (int i = 0; i < tokens.Length; i++)
        {
            int gid = int.Parse(tokens[i].Trim());
            int col = i % columns;
            int row = i / columns;

            if (col < columns && row < rows)
                grid[col, row] = GidToTileType.TryGetValue(gid, out var t)
                    ? t
                    : TileType.HighGround;
        }

        return grid;
    }

    /// <summary>
    /// Reads an optional "name" string custom property from the map's &lt;properties&gt; block.
    /// Returns null if not present.
    /// </summary>
    private static string? ReadNameProperty(XElement root) =>
        root.Element("properties")
            ?.Elements("property")
            .FirstOrDefault(p => (string?)p.Attribute("name") == "name")
            ?.Attribute("value")
            ?.Value;
}
