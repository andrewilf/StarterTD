using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StarterTD.Engine;
using StarterTD.Entities;

namespace StarterTD.Scenes;

/// <summary>
/// Wall drag placement helpers for GameplayScene.
/// </summary>
public partial class GameplayScene
{
    private Tower? GetSelectedWallingAnchor()
    {
        var tower = _towerManager.SelectedTower;
        if (tower == null)
            return null;

        return tower.TowerType.IsWallingChampion() || tower.TowerType.IsWallingGeneric()
            ? tower
            : null;
    }

    private void StartWallDrag(Point startGrid)
    {
        var wallingAnchor = GetSelectedWallingAnchor();
        if (!_wallPlacementMode || wallingAnchor == null)
            return;

        _isWallDragActive = true;
        _wallDragStartGrid = startGrid;
        _wallDragCurrentGrid = startGrid;
        UpdateWallDragPreview(wallingAnchor);
    }

    private void UpdateWallDrag()
    {
        if (!_isWallDragActive)
            return;

        var wallingAnchor = GetSelectedWallingAnchor();
        if (!_wallPlacementMode || wallingAnchor == null)
        {
            CancelWallDrag();
            return;
        }

        _wallDragCurrentGrid = Map.WorldToGrid(_inputManager.MousePositionVector);
        UpdateWallDragPreview(wallingAnchor);
    }

    private void CommitWallDrag()
    {
        if (!_isWallDragActive)
            return;

        var wallingAnchor = GetSelectedWallingAnchor();
        if (_wallPlacementMode && wallingAnchor != null)
        {
            UpdateWallDragPreview(wallingAnchor);

            if (_wallDragPreviewPath != null && _wallDragPreviewPath.Count > 0)
                _towerManager.TryPlaceWallPath(_wallDragPreviewPath, wallingAnchor);
        }

        CancelWallDrag();
    }

    private void CancelWallDrag()
    {
        _isWallDragActive = false;
        _wallDragPreviewPath = null;
        _wallDragValidPrefixLength = 0;
    }

    private void UpdateWallDragPreview(Tower wallingAnchor)
    {
        var (candidateA, candidateB) = BuildLCandidates(_wallDragStartGrid, _wallDragCurrentGrid);

        int prefixA = _towerManager.GetWallPathValidPrefixLength(candidateA, wallingAnchor);
        int prefixB = _towerManager.GetWallPathValidPrefixLength(candidateB, wallingAnchor);

        bool chooseA;
        if (prefixA > prefixB)
        {
            chooseA = true;
        }
        else if (prefixB > prefixA)
        {
            chooseA = false;
        }
        else if (candidateA.Count < candidateB.Count)
        {
            chooseA = true;
        }
        else if (candidateB.Count < candidateA.Count)
        {
            chooseA = false;
        }
        else
        {
            // Tie-breaker default: horizontal then vertical (candidate A).
            chooseA = true;
        }

        _wallDragPreviewPath = chooseA ? candidateA : candidateB;
        _wallDragValidPrefixLength = chooseA ? prefixA : prefixB;
    }

    private static (
        List<Point> horizontalThenVertical,
        List<Point> verticalThenHorizontal
    ) BuildLCandidates(Point start, Point end)
    {
        var hvCorner = new Point(end.X, start.Y);
        var vhCorner = new Point(start.X, end.Y);

        var horizontalThenVertical = BuildLPath(start, hvCorner, end);
        var verticalThenHorizontal = BuildLPath(start, vhCorner, end);

        return (horizontalThenVertical, verticalThenHorizontal);
    }

    private static List<Point> BuildLPath(Point start, Point corner, Point end)
    {
        var path = new List<Point>();

        AppendStraightSegment(path, start, corner, includeStart: true);
        AppendStraightSegment(path, corner, end, includeStart: false);

        if (path.Count == 0)
            path.Add(start);

        return path;
    }

    private static void AppendStraightSegment(
        List<Point> path,
        Point from,
        Point to,
        bool includeStart
    )
    {
        if (from.X != to.X && from.Y != to.Y)
            return;

        int stepX = to.X.CompareTo(from.X);
        int stepY = to.Y.CompareTo(from.Y);

        if (!includeStart && stepX == 0 && stepY == 0)
            return;

        int x = from.X;
        int y = from.Y;
        if (!includeStart)
        {
            x += stepX;
            y += stepY;
        }

        while (true)
        {
            path.Add(new Point(x, y));

            if (x == to.X && y == to.Y)
                break;

            x += stepX;
            y += stepY;
        }
    }
}
