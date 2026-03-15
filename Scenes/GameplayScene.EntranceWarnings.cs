using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Interfaces;

namespace StarterTD.Scenes;

/// <summary>
/// Enemy entrance warning helpers for GameplayScene.
/// </summary>
public partial class GameplayScene
{
    private const float EntranceWarningPulseSpeed = 8f;
    private const float EntranceWarningFlashSpeed = 17f;
    private const float EntranceWarningBaseRadius = 12f;
    private const float EntranceWarningPulseRadius = 7f;
    private const float EntranceWarningLeadDistanceMultiplier = 2.5f;
    private const float EntranceWarningLeadBufferSeconds = 0.4f;
    private const float EntranceWarningCooldownSeconds = 5f;

    private sealed class EntranceWarningLaneState
    {
        public Vector2 SpawnWorld { get; }
        public float LastSpawnTimeSeconds { get; set; } = float.NegativeInfinity;
        public IEnemy? TrackedEnemy { get; set; }
        public bool IsVisible { get; set; }
        public bool HasPendingSpawn { get; set; }
        public float PendingSpawnAtSeconds { get; set; }
        public float PendingSpawnSpeed { get; set; }

        public EntranceWarningLaneState(Vector2 spawnWorld)
        {
            SpawnWorld = spawnWorld;
        }

        public void ResetPendingSpawn()
        {
            HasPendingSpawn = false;
            PendingSpawnAtSeconds = 0f;
            PendingSpawnSpeed = 0f;
        }

        public void ResetRuntimeState()
        {
            LastSpawnTimeSeconds = float.NegativeInfinity;
            TrackedEnemy = null;
            IsVisible = false;
            ResetPendingSpawn();
        }
    }

    private void InitializeSpawnLaneLookup()
    {
        _spawnNameByGrid.Clear();
        _entranceWarningLanes.Clear();
        _entranceWarningBySpawn.Clear();
        _defaultSpawnLaneName = string.Empty;

        foreach (var (spawnName, spawnGrid) in _map.MapData.SpawnPoints)
        {
            _spawnNameByGrid[spawnGrid] = spawnName;

            if (_defaultSpawnLaneName.Length == 0)
                _defaultSpawnLaneName = spawnName;

            var lane = new EntranceWarningLaneState(Map.GridToWorld(spawnGrid));
            _entranceWarningLanes.Add(lane);
            _entranceWarningBySpawn[spawnName] = lane;
        }
    }

    private string ResolveSpawnLaneName(string? requestedSpawnName)
    {
        if (
            !string.IsNullOrWhiteSpace(requestedSpawnName)
            && _map.MapData.SpawnPoints.ContainsKey(requestedSpawnName)
        )
        {
            return requestedSpawnName;
        }

        return _defaultSpawnLaneName;
    }

    private bool IsEntranceWarningCooldownReady(EntranceWarningLaneState lane)
    {
        return _entranceWarningRuntimeSeconds - lane.LastSpawnTimeSeconds
            >= EntranceWarningCooldownSeconds;
    }

    private void CollectEntranceWarningPendingSpawns()
    {
        for (int i = 0; i < _entranceWarningLanes.Count; i++)
            _entranceWarningLanes[i].ResetPendingSpawn();

        if (_spawnScheduleManager.PendingSpawnCount == 0)
            return;

        for (int i = 0; i < _spawnScheduleManager.PendingSpawnCount; i++)
        {
            if (!_spawnScheduleManager.TryGetPendingSpawn(i, out SpawnEntry spawn))
                continue;

            string resolvedSpawnName = ResolveSpawnLaneName(spawn.SpawnPoint);
            if (!_entranceWarningBySpawn.TryGetValue(resolvedSpawnName, out var lane))
                continue;

            if (!lane.HasPendingSpawn || spawn.At < lane.PendingSpawnAtSeconds)
            {
                lane.HasPendingSpawn = true;
                lane.PendingSpawnAtSeconds = spawn.At;
                lane.PendingSpawnSpeed = spawn.Speed;
            }
        }
    }

    private void TrackEntranceWarningEnemy(IEnemy enemy)
    {
        if (_entranceWarningLanes.Count == 0 || _defaultSpawnLaneName.Length == 0)
            return;

        Point enemyGrid = Map.WorldToGrid(enemy.Position);
        if (!_spawnNameByGrid.TryGetValue(enemyGrid, out string? spawnName))
            spawnName = _defaultSpawnLaneName;

        spawnName = ResolveSpawnLaneName(spawnName);
        if (!_entranceWarningBySpawn.TryGetValue(spawnName, out var lane))
            return;

        bool cooldownReady = IsEntranceWarningCooldownReady(lane);
        lane.LastSpawnTimeSeconds = _entranceWarningRuntimeSeconds;

        if (!cooldownReady)
            return;

        lane.TrackedEnemy = enemy;
        lane.IsVisible = true;
    }

    private void UpdateEntranceWarnings(GameTime activeTime)
    {
        if (_entranceWarningLanes.Count == 0)
            return;

        float dt = (float)activeTime.ElapsedGameTime.TotalSeconds;
        _entranceWarningRuntimeSeconds += dt;
        _entranceWarningPulseTime += dt;
        CollectEntranceWarningPendingSpawns();

        for (int i = 0; i < _entranceWarningLanes.Count; i++)
        {
            EntranceWarningLaneState lane = _entranceWarningLanes[i];
            if (lane.TrackedEnemy != null)
            {
                IEnemy trackedEnemy = lane.TrackedEnemy;
                if (trackedEnemy.IsDead || trackedEnemy.ReachedEnd)
                {
                    CompleteEntranceWarningLane(lane);
                    continue;
                }

                float distanceFromSpawn = Vector2.Distance(trackedEnemy.Position, lane.SpawnWorld);
                if (distanceFromSpawn > EntranceWarningDistancePx)
                {
                    CompleteEntranceWarningLane(lane);
                    continue;
                }

                lane.IsVisible = true;
                continue;
            }

            lane.IsVisible = false;

            if (!lane.HasPendingSpawn || !IsEntranceWarningCooldownReady(lane))
                continue;

            float leadSeconds =
                EntranceWarningDistancePx
                    * EntranceWarningLeadDistanceMultiplier
                    / MathF.Max(lane.PendingSpawnSpeed, EntranceWarningMinSpeed)
                + EntranceWarningLeadBufferSeconds;
            float timeUntilSpawn =
                lane.PendingSpawnAtSeconds - _spawnScheduleManager.ElapsedSeconds;
            if (timeUntilSpawn <= leadSeconds)
                lane.IsVisible = true;
        }
    }

    private static void CompleteEntranceWarningLane(EntranceWarningLaneState lane)
    {
        lane.IsVisible = false;
        lane.TrackedEnemy = null;
    }

    private void ClearEntranceWarnings()
    {
        _entranceWarningRuntimeSeconds = 0f;
        _entranceWarningPulseTime = 0f;

        for (int i = 0; i < _entranceWarningLanes.Count; i++)
            _entranceWarningLanes[i].ResetRuntimeState();
    }

    private void DrawEntranceWarnings(SpriteBatch spriteBatch)
    {
        if (_entranceWarningLanes.Count == 0)
            return;

        float pulse =
            0.5f + 0.5f * MathF.Sin(_entranceWarningPulseTime * EntranceWarningPulseSpeed);
        float flash =
            0.5f + 0.5f * MathF.Sin(_entranceWarningPulseTime * EntranceWarningFlashSpeed);
        float outerRadius = EntranceWarningBaseRadius + EntranceWarningPulseRadius * pulse;
        float midRadius = outerRadius * (0.75f + 0.08f * flash);
        float coreRadius = outerRadius * (0.44f + 0.06f * pulse);
        float burstLength = outerRadius + 7f + 5f * flash;
        float burstThickness = 2f + 1.2f * flash;

        for (int i = 0; i < _entranceWarningLanes.Count; i++)
        {
            EntranceWarningLaneState lane = _entranceWarningLanes[i];
            if (!lane.IsVisible)
                continue;

            Vector2 center = lane.SpawnWorld;
            TextureManager.DrawFilledCircle(
                spriteBatch,
                center,
                outerRadius + 8f,
                Color.OrangeRed * (0.12f + 0.12f * flash)
            );
            TextureManager.DrawFilledCircle(
                spriteBatch,
                center,
                outerRadius,
                Color.Red * (0.24f + 0.2f * pulse)
            );
            TextureManager.DrawFilledCircle(
                spriteBatch,
                center,
                midRadius,
                Color.OrangeRed * (0.35f + 0.2f * flash)
            );
            TextureManager.DrawFilledCircle(
                spriteBatch,
                center,
                coreRadius,
                Color.DarkRed * (0.6f + 0.15f * pulse)
            );
            DrawEntranceWarningBurst(spriteBatch, center, burstLength, burstThickness, flash);
            DrawEntranceWarningExclamation(spriteBatch, center, pulse, flash);
        }
    }

    private static void DrawEntranceWarningBurst(
        SpriteBatch spriteBatch,
        Vector2 center,
        float length,
        float thickness,
        float flash
    )
    {
        Color rayColor = Color.OrangeRed * (0.26f + 0.26f * flash);
        Vector2 raySize = new(length, thickness);

        TextureManager.DrawSprite(spriteBatch, center, raySize, rayColor, rotation: 0f);
        TextureManager.DrawSprite(
            spriteBatch,
            center,
            raySize,
            rayColor,
            rotation: MathF.PI * 0.5f
        );
        TextureManager.DrawSprite(
            spriteBatch,
            center,
            raySize,
            rayColor,
            rotation: MathF.PI * 0.25f
        );
        TextureManager.DrawSprite(
            spriteBatch,
            center,
            raySize,
            rayColor,
            rotation: -MathF.PI * 0.25f
        );
    }

    private static void DrawEntranceWarningExclamation(
        SpriteBatch spriteBatch,
        Vector2 center,
        float pulse,
        float flash
    )
    {
        int barWidth = 4 + (flash > 0.84f ? 1 : 0);
        int barHeight = (int)(10f + 3f * pulse);
        int dotSize = 4 + (flash > 0.9f ? 1 : 0);
        int yJitter = flash > 0.8f ? -1 : 0;

        int barX = (int)MathF.Round(center.X - barWidth / 2f);
        int barY = (int)MathF.Round(center.Y - 8f + yJitter);
        int dotX = (int)MathF.Round(center.X - dotSize / 2f);
        int dotY = barY + barHeight + 2;
        Color glyphColor = flash > 0.72f ? Color.White : Color.MistyRose;

        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(barX - 1, barY - 1, barWidth + 2, barHeight + 2),
            Color.Black * 0.35f
        );
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(dotX - 1, dotY - 1, dotSize + 2, dotSize + 2),
            Color.Black * 0.35f
        );

        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(barX, barY, barWidth, barHeight),
            glyphColor
        );
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(dotX, dotY, dotSize, dotSize),
            glyphColor
        );
    }
}
