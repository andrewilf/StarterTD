using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;

namespace StarterTD.Entities;

public enum HealingDroneState
{
    Docked,
    TravelingToTarget,
    HealingTarget,
    ReturningToOwner,
    Recharging,
}

public class HealingDrone
{
    public const int MaxEnergy = 100;
    private const float TickIntervalSeconds = 0.1f;
    private const float MoveSpeed = 90f;
    private const float ArrivalDistance = 4f;

    private readonly Map _map;
    private Vector2 _position;
    private float _tickAccumulator;
    private Tower? _targetTower;
    private Queue<Point>? _path;
    private Point _pathDestinationGrid;
    private bool _hasPathDestination;

    public Tower Owner { get; }
    public int Energy { get; private set; } = MaxEnergy;
    public HealingDroneState State { get; private set; } = HealingDroneState.Docked;
    public Vector2 Position => _position;
    public Tower? CurrentTarget => _targetTower;

    public HealingDrone(Map map, Tower owner)
    {
        _map = map;
        Owner = owner;
        _position = owner.DrawPosition;
    }

    public void Update(
        GameTime gameTime,
        IReadOnlyList<Tower> towers,
        ISet<Tower> claimedTargets,
        bool isHealingUltActive,
        bool isAttackModeActive
    )
    {
        if (Owner.IsDead)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (
            isAttackModeActive
            && (
                State == HealingDroneState.TravelingToTarget
                || State == HealingDroneState.HealingTarget
            )
        )
        {
            StartReturnToOwner();
        }

        switch (State)
        {
            case HealingDroneState.Docked:
                UpdateDocked(towers, claimedTargets, isAttackModeActive);
                break;
            case HealingDroneState.TravelingToTarget:
                UpdateTravelToTarget(dt, towers, claimedTargets);
                break;
            case HealingDroneState.HealingTarget:
                UpdateHealingTarget(dt, towers, claimedTargets, isHealingUltActive);
                break;
            case HealingDroneState.ReturningToOwner:
                UpdateReturnToOwner(dt, towers, claimedTargets, isAttackModeActive);
                break;
            case HealingDroneState.Recharging:
                UpdateRecharging(dt, towers, claimedTargets, isAttackModeActive);
                break;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Color droneColor = State switch
        {
            HealingDroneState.Recharging => Color.Gold,
            HealingDroneState.HealingTarget => Color.LimeGreen,
            HealingDroneState.ReturningToOwner => Color.LightSkyBlue,
            HealingDroneState.TravelingToTarget => Color.LightGreen,
            _ => Color.MediumAquamarine,
        };

        TextureManager.DrawSprite(spriteBatch, _position, new Vector2(10f, 10f), droneColor);

        const int barWidth = 20;
        const int barHeight = 3;
        int fillWidth = (int)MathF.Round(barWidth * (Energy / (float)MaxEnergy));

        var bgRect = new Rectangle(
            (int)(_position.X - barWidth / 2f),
            (int)(_position.Y - 12f),
            barWidth,
            barHeight
        );
        TextureManager.DrawRect(spriteBatch, bgRect, Color.DarkSlateGray);
        if (fillWidth > 0)
        {
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(bgRect.X, bgRect.Y, fillWidth, barHeight),
                Color.LimeGreen
            );
        }
    }

    public void RefillForHealingUlt()
    {
        Energy = MaxEnergy;
        _tickAccumulator = 0f;

        if (State == HealingDroneState.ReturningToOwner || State == HealingDroneState.Recharging)
        {
            _targetTower = null;
            _path = null;
            _hasPathDestination = false;
            State = HealingDroneState.Docked;
        }
    }

    public void ForceReturnToOwnerForAttackMode()
    {
        _targetTower = null;
        _tickAccumulator = 0f;

        if (State == HealingDroneState.Docked)
        {
            _position = Owner.DrawPosition;
            State = Energy < MaxEnergy ? HealingDroneState.Recharging : HealingDroneState.Docked;
            _path = null;
            _hasPathDestination = false;
            return;
        }

        if (State == HealingDroneState.Recharging || State == HealingDroneState.ReturningToOwner)
            return;

        StartReturnToOwner();
    }

    private void UpdateDocked(
        IReadOnlyList<Tower> towers,
        ISet<Tower> claimedTargets,
        bool isAttackModeActive
    )
    {
        _position = Owner.DrawPosition;
        _tickAccumulator = 0f;

        // Redeploy only when fully re-energized.
        if (Energy < MaxEnergy)
        {
            State = HealingDroneState.Recharging;
            return;
        }

        if (isAttackModeActive)
            return;

        TryDeployToDamagedTower(towers, claimedTargets);
    }

    private void UpdateTravelToTarget(
        float dt,
        IReadOnlyList<Tower> towers,
        ISet<Tower> claimedTargets
    )
    {
        bool canKeepTarget =
            _targetTower != null
            && IsValidHealingTarget(_targetTower)
            && !claimedTargets.Contains(_targetTower);
        if (!canKeepTarget)
        {
            _targetTower = FindClosestDamagedTower(towers, _position, claimedTargets);
            if (_targetTower == null)
            {
                StartReturnToOwner();
                return;
            }

            QueuePathTo(_targetTower.DrawPosition);
        }

        Tower target = _targetTower!;
        claimedTargets.Add(target);

        bool arrived = MoveToward(dt, target.DrawPosition);
        if (!arrived)
            return;

        _position = target.DrawPosition;
        _tickAccumulator = 0f;
        State = HealingDroneState.HealingTarget;
    }

    private void UpdateHealingTarget(
        float dt,
        IReadOnlyList<Tower> towers,
        ISet<Tower> claimedTargets,
        bool isHealingUltActive
    )
    {
        bool canKeepTarget =
            _targetTower != null
            && IsValidHealingTarget(_targetTower)
            && !claimedTargets.Contains(_targetTower);
        if (!canKeepTarget)
        {
            if (!TryRetarget(towers, claimedTargets))
                StartReturnToOwner();
            return;
        }

        Tower target = _targetTower!;
        claimedTargets.Add(target);
        _position = target.DrawPosition;
        _tickAccumulator += dt;

        while (_tickAccumulator >= TickIntervalSeconds)
        {
            _tickAccumulator -= TickIntervalSeconds;

            if (!isHealingUltActive && Energy <= 0)
            {
                Energy = 0;
                StartReturnToOwner();
                return;
            }

            int healed = target.Heal(1);
            if (!isHealingUltActive && healed > 0)
                Energy -= healed;

            if (!isHealingUltActive && Energy <= 0)
            {
                Energy = 0;
                StartReturnToOwner();
                return;
            }

            if (target.CurrentHealth >= target.MaxHealth)
            {
                if (!TryRetarget(towers, claimedTargets))
                    StartReturnToOwner();
                return;
            }
        }
    }

    /// <summary>
    /// Find a new damaged tower to heal and begin traveling to it.
    /// Returns false if no valid target exists.
    /// </summary>
    private bool TryRetarget(IReadOnlyList<Tower> towers, ISet<Tower> claimedTargets)
    {
        var next = FindClosestDamagedTower(towers, _position, claimedTargets);
        if (next == null)
            return false;

        _targetTower = next;
        _tickAccumulator = 0f;
        State = HealingDroneState.TravelingToTarget;
        QueuePathTo(next.DrawPosition);
        claimedTargets.Add(next);
        return true;
    }

    private void UpdateReturnToOwner(
        float dt,
        IReadOnlyList<Tower> towers,
        ISet<Tower> claimedTargets,
        bool isAttackModeActive
    )
    {
        bool arrived = MoveToward(dt, Owner.DrawPosition);
        if (!arrived)
            return;

        _position = Owner.DrawPosition;
        _targetTower = null;
        _path = null;
        _hasPathDestination = false;
        _tickAccumulator = 0f;

        State = Energy < MaxEnergy ? HealingDroneState.Recharging : HealingDroneState.Docked;
        if (State == HealingDroneState.Docked && !isAttackModeActive)
            TryDeployToDamagedTower(towers, claimedTargets);
    }

    private void UpdateRecharging(
        float dt,
        IReadOnlyList<Tower> towers,
        ISet<Tower> claimedTargets,
        bool isAttackModeActive
    )
    {
        _position = Owner.DrawPosition;
        _tickAccumulator += dt;

        while (_tickAccumulator >= TickIntervalSeconds)
        {
            _tickAccumulator -= TickIntervalSeconds;
            if (Energy < MaxEnergy)
                Energy++;

            if (Energy >= MaxEnergy)
            {
                Energy = MaxEnergy;
                _tickAccumulator = 0f;
                State = HealingDroneState.Docked;
                if (!isAttackModeActive)
                    TryDeployToDamagedTower(towers, claimedTargets);
                return;
            }
        }
    }

    private void TryDeployToDamagedTower(IReadOnlyList<Tower> towers, ISet<Tower> claimedTargets)
    {
        var target = FindClosestDamagedTower(towers, _position, claimedTargets);
        if (target == null)
            return;

        _targetTower = target;
        _path = null;
        _hasPathDestination = false;
        _tickAccumulator = 0f;
        State = HealingDroneState.TravelingToTarget;
        claimedTargets.Add(target);
        QueuePathTo(target.DrawPosition);
    }

    private void StartReturnToOwner()
    {
        _targetTower = null;
        _tickAccumulator = 0f;
        State = HealingDroneState.ReturningToOwner;
        QueuePathTo(Owner.DrawPosition);
    }

    private Tower? FindClosestDamagedTower(
        IReadOnlyList<Tower> towers,
        Vector2 origin,
        ISet<Tower> claimedTargets
    )
    {
        Tower? best = null;
        float bestDistanceSq = float.MaxValue;

        for (int i = 0; i < towers.Count; i++)
        {
            var tower = towers[i];
            if (!IsValidHealingTarget(tower))
                continue;
            if (claimedTargets.Contains(tower))
                continue;

            float distSq = Vector2.DistanceSquared(origin, tower.DrawPosition);
            if (distSq < bestDistanceSq)
            {
                bestDistanceSq = distSq;
                best = tower;
            }
        }

        return best;
    }

    private bool IsValidHealingTarget(Tower tower)
    {
        if (tower.IsDead)
            return false;

        if (tower == Owner)
            return false;

        if (tower.TowerType.IsWallSegment())
            return false;

        return tower.CurrentHealth < tower.MaxHealth;
    }

    private bool MoveToward(float dt, Vector2 destinationWorld)
    {
        QueuePathTo(destinationWorld);

        float moveRemaining = MoveSpeed * dt;
        while (moveRemaining > 0f)
        {
            Vector2 waypoint = destinationWorld;
            if (_path != null && _path.Count > 0)
                waypoint = Map.GridToWorld(_path.Peek());

            Vector2 delta = waypoint - _position;
            float distance = delta.Length();
            if (distance <= 0.001f)
            {
                if (_path != null && _path.Count > 0)
                {
                    _path.Dequeue();
                    continue;
                }

                break;
            }

            if (distance <= moveRemaining)
            {
                _position = waypoint;
                moveRemaining -= distance;
                if (_path != null && _path.Count > 0)
                    _path.Dequeue();
            }
            else
            {
                _position += delta / distance * moveRemaining;
                moveRemaining = 0f;
            }
        }

        float arrivalDistanceSq = ArrivalDistance * ArrivalDistance;
        return Vector2.DistanceSquared(_position, destinationWorld) <= arrivalDistanceSq;
    }

    private void QueuePathTo(Vector2 destinationWorld)
    {
        Point destinationGrid = ClampToGrid(Map.WorldToGrid(destinationWorld));
        if (_hasPathDestination && destinationGrid == _pathDestinationGrid && _path != null)
            return;

        Point startGrid = ClampToGrid(Map.WorldToGrid(_position));
        _path = BuildUniformPath(startGrid, destinationGrid);
        if (_path != null && _path.Count > 0 && _path.Peek() == startGrid)
            _path.Dequeue();

        _pathDestinationGrid = destinationGrid;
        _hasPathDestination = true;
    }

    private Queue<Point>? BuildUniformPath(Point startGrid, Point destinationGrid)
    {
        var heatMap = Pathfinder.ComputeHeatMap(destinationGrid, _map.Columns, _map.Rows, _ => 1);
        var path = Pathfinder.ExtractPath(startGrid, heatMap, _map.Columns, _map.Rows);
        return path == null ? null : new Queue<Point>(path);
    }

    private Point ClampToGrid(Point point)
    {
        int x = Math.Clamp(point.X, 0, _map.Columns - 1);
        int y = Math.Clamp(point.Y, 0, _map.Rows - 1);
        return new Point(x, y);
    }
}
