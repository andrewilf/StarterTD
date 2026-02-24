using System;
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
        _wallDragLockedHorizontalFirst = null;
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
        _wallDragLockedHorizontalFirst = null;
    }

    private void UpdateWallDragPreview(Tower wallingAnchor)
    {
        int dx = Math.Abs(_wallDragCurrentGrid.X - _wallDragStartGrid.X);
        int dy = Math.Abs(_wallDragCurrentGrid.Y - _wallDragStartGrid.Y);

        // Straight line — no L needed. Reset lock so next diagonal freely re-decides.
        if (dx == 0 || dy == 0)
        {
            var straight = BuildLPath(
                _wallDragStartGrid,
                _wallDragCurrentGrid,
                _wallDragCurrentGrid
            );
            int prefix = _towerManager.GetWallPathValidPrefixLength(straight, wallingAnchor);
            _wallDragPreviewPath = straight;
            _wallDragValidPrefixLength = prefix;
            _wallDragLockedHorizontalFirst = null;
            return;
        }

        // First time both axes are non-zero — lock the L direction based on which
        // axis was dragged first (the longer axis from start is the one dragged first).
        _wallDragLockedHorizontalFirst ??= dx >= dy;

        var (candidateA, candidateB) = BuildLCandidates(_wallDragStartGrid, _wallDragCurrentGrid);
        int prefixA = _towerManager.GetWallPathValidPrefixLength(candidateA, wallingAnchor);
        int prefixB = _towerManager.GetWallPathValidPrefixLength(candidateB, wallingAnchor);

        bool chooseA = _wallDragLockedHorizontalFirst.Value;

        // Only override the locked preference if the other candidate
        // has a strictly longer valid prefix (i.e. the preferred one is blocked).
        if (chooseA && prefixB > prefixA)
            chooseA = false;
        else if (!chooseA && prefixA > prefixB)
            chooseA = true;

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
