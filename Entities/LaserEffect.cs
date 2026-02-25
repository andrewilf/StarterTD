using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Interfaces;

namespace StarterTD.Entities;

/// <summary>
/// The particle cannon laser deployed by the Champion Cannon's super ability.
/// Two-phase effect:
///   Wind-up (1s): converging rings appear at the initial target position.
///   Beam (20s):   a beam fires from a fixed top-left off-screen focal point through
///                 _currentContact (the live damage point), extending far past it.
///                 _currentContact sweeps toward enemies or player-ordered positions.
///                 Damage and particles are applied at _currentContact, not the beam tip.
/// </summary>
public class LaserEffect
{
    public bool IsActive { get; private set; } = true;

    public void Cancel() => IsActive = false;

    /// <summary>True when the player has left-clicked the beam to redirect it.</summary>
    public bool IsSelected { get; set; }

    // --- Phase tracking ---
    private const float WindUpDuration = 1f;
    private const float BeamDuration = 20f;
    private float _windUpElapsed;
    private float _beamElapsed;
    private bool _beamActive;

    // --- Contact point tracking ---
    // _currentContact: the live world-space point where the beam focuses (where damage is dealt).
    // _orderedTarget: player-issued destination (null = auto-track nearest enemy).
    // The beam visual originates from BeamOrigin, passes through _currentContact, and extends beyond.
    private Vector2 _currentContact;
    private Vector2? _orderedTarget;

    // px/s — ordered moves are faster than auto-tracking
    private const float SweepSpeed = 100f;
    private const float TrackSpeed = 50f;

    // Fixed pivot: top-right, off-screen. Beam is drawn from here down to the contact point.
    private static readonly Vector2 BeamOrigin = new(1224f, -200f);

    // Wind-up anchor (world position where rings converge)
    private readonly Vector2 _windUpTarget;

    // --- Damage ---
    private const float DamageRadius = 48f;
    private const float DamagePerPulse = 20f;
    private const float PulseInterval = 0.25f;
    private float _pulseTimer;

    // --- Particles ---
    private readonly List<LaserParticle> _particles = new();
    private readonly Random _rng = new();

    public LaserEffect(Vector2 initialTarget)
    {
        _windUpTarget = initialTarget;
        _currentContact = initialTarget;
        _orderedTarget = initialTarget;
    }

    /// <summary>
    /// Player-issued redirect: beam sweeps at SweepSpeed px/s to worldPos.
    /// Clears auto-tracking until this move completes.
    /// </summary>
    public void SetTarget(Vector2 worldPos)
    {
        _orderedTarget = worldPos;
    }

    /// <summary>
    /// Returns true if worldPos is within click distance of the beam line.
    /// </summary>
    public bool ContainsPoint(Vector2 worldPos)
    {
        if (!_beamActive)
            return false;

        Vector2 dir = _currentContact - BeamOrigin;
        float len = dir.Length();
        if (len < 0.001f)
            return false;

        return PerpendicularDistance(BeamOrigin, _currentContact, worldPos) <= 18f;
    }

    public void Update(GameTime gameTime, List<IEnemy> enemies)
    {
        if (!IsActive)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (!_beamActive)
            UpdateWindUp(dt);
        else
            UpdateBeam(dt, enemies);

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            _particles[i].Update(dt);
            if (!_particles[i].IsAlive)
                _particles.RemoveAt(i);
        }
    }

    private void UpdateWindUp(float dt)
    {
        _windUpElapsed += dt;
        if (_windUpElapsed >= WindUpDuration)
        {
            _beamActive = true;
            _windUpElapsed = WindUpDuration;
        }
    }

    private void UpdateBeam(float dt, List<IEnemy> enemies)
    {
        _beamElapsed += dt;
        if (_beamElapsed >= BeamDuration)
        {
            IsActive = false;
            return;
        }

        if (_orderedTarget.HasValue)
        {
            Vector2 destination = _orderedTarget.Value;
            Vector2 diff = destination - _currentContact;
            float dist = diff.Length();
            float maxMove = SweepSpeed * dt;

            if (dist <= maxMove)
            {
                _currentContact = destination;
                _orderedTarget = null; // order fulfilled — resume auto-tracking
            }
            else
            {
                _currentContact += Vector2.Normalize(diff) * maxMove;
            }
        }
        else
        {
            // Auto-track: move toward the closest live enemy
            IEnemy? closest = FindClosestEnemy(enemies);
            if (closest != null)
            {
                Vector2 diff = closest.Position - _currentContact;
                float dist = diff.Length();
                float maxMove = TrackSpeed * dt;
                if (dist <= maxMove)
                    _currentContact = closest.Position;
                else
                    _currentContact += Vector2.Normalize(diff) * maxMove;
            }
        }

        // Pulse damage at the contact point
        _pulseTimer += dt;
        if (_pulseTimer >= PulseInterval)
        {
            _pulseTimer -= PulseInterval;
            ApplyDamagePulse(enemies);
            SpawnImpactParticles();
        }
    }

    private IEnemy? FindClosestEnemy(List<IEnemy> enemies)
    {
        IEnemy? closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead || enemy.ReachedEnd)
                continue;

            float dist = Vector2.Distance(_currentContact, enemy.Position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    private void ApplyDamagePulse(List<IEnemy> enemies)
    {
        foreach (var enemy in enemies)
        {
            if (!enemy.IsDead && Vector2.Distance(_currentContact, enemy.Position) <= DamageRadius)
                enemy.TakeDamage(DamagePerPulse);
        }
    }

    private void SpawnImpactParticles()
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = (float)(_rng.NextDouble() * Math.PI * 2);
            float speed = 80f + (float)(_rng.NextDouble() * 120f);
            var velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            _particles.Add(new LaserParticle(_currentContact, velocity));
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
            return;

        if (!_beamActive)
            DrawWindUp(spriteBatch);
        else
            DrawBeam(spriteBatch);

        foreach (var particle in _particles)
            particle.Draw(spriteBatch);
    }

    private void DrawWindUp(SpriteBatch spriteBatch)
    {
        float progress = _windUpElapsed / WindUpDuration;
        float opacity = progress;

        const int ringCount = 4;
        const float maxRingRadius = 60f;

        for (int i = 0; i < ringCount; i++)
        {
            float phase = (float)i / ringCount;
            float ringProgress = (progress + phase) % 1f;
            float radius = maxRingRadius * (1f - ringProgress);
            float ringOpacity = opacity * (1f - ringProgress);

            TextureManager.DrawFilledCircle(
                spriteBatch,
                _windUpTarget,
                radius,
                Color.Red * ringOpacity
            );
        }

        TextureManager.DrawFilledCircle(spriteBatch, _windUpTarget, 4f, Color.White * opacity);
    }

    private void DrawBeam(SpriteBatch spriteBatch)
    {
        Vector2 dir = _currentContact - BeamOrigin;
        float distToContact = dir.Length();
        if (distToContact < 0.001f)
            return;

        Vector2 unitDir = Vector2.Normalize(dir);
        float angle = MathF.Atan2(unitDir.Y, unitDir.X);

        // Midpoint of the beam segment (origin → contact point) for centred DrawSprite
        Vector2 midpoint = BeamOrigin + unitDir * (distToContact * 0.5f);

        if (IsSelected)
        {
            TextureManager.DrawSprite(
                spriteBatch,
                midpoint,
                new Vector2(distToContact, 38f),
                Color.Yellow * 0.15f,
                rotation: angle
            );
        }

        // Outer glow
        TextureManager.DrawSprite(
            spriteBatch,
            midpoint,
            new Vector2(distToContact, 33f),
            Color.Red * 0.12f,
            rotation: angle
        );

        // Mid glow
        TextureManager.DrawSprite(
            spriteBatch,
            midpoint,
            new Vector2(distToContact, 14f),
            Color.Red * 0.40f,
            rotation: angle
        );

        // Core beam
        TextureManager.DrawSprite(
            spriteBatch,
            midpoint,
            new Vector2(distToContact, 8f),
            Color.OrangeRed * 0.90f,
            rotation: angle
        );

        // Bright inner core
        TextureManager.DrawSprite(
            spriteBatch,
            midpoint,
            new Vector2(distToContact, 4f),
            Color.White * 0.95f,
            rotation: angle
        );

        // Impact circle at the contact point (where damage happens)
        float beamProgress = _beamElapsed / BeamDuration;
        float pulseRadius = 24f + 24f * MathF.Abs(MathF.Sin(_beamElapsed * MathF.PI * 4));
        TextureManager.DrawFilledCircle(
            spriteBatch,
            _currentContact,
            pulseRadius,
            Color.OrangeRed * (0.65f * (1f - beamProgress * 0.3f))
        );

        // Faint damage radius indicator
        TextureManager.DrawFilledCircle(
            spriteBatch,
            _currentContact,
            DamageRadius,
            Color.Red * 0.12f
        );
    }

    private static float PerpendicularDistance(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ab = b - a;
        float length = ab.Length();
        if (length < 0.001f)
            return Vector2.Distance(a, p);

        float t = Vector2.Dot(p - a, ab) / (length * length);
        t = Math.Clamp(t, 0f, 1f);
        Vector2 closest = a + ab * t;
        return Vector2.Distance(p, closest);
    }

    private sealed class LaserParticle
    {
        private Vector2 _position;
        private readonly Vector2 _velocity;
        private const float Lifetime = 0.4f;
        private float _elapsed;

        public bool IsAlive => _elapsed < Lifetime;

        public LaserParticle(Vector2 startPosition, Vector2 velocity)
        {
            _position = startPosition;
            _velocity = velocity;
        }

        public void Update(float dt)
        {
            _elapsed += dt;
            _position += _velocity * dt;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            float progress = _elapsed / Lifetime;
            float opacity = 1f - progress;
            Color color = Color.Lerp(Color.White, Color.OrangeRed, progress);
            float size = 3f * (1f - progress * 0.5f);

            TextureManager.DrawSprite(
                spriteBatch,
                _position,
                new Vector2(size, size),
                color * opacity
            );
        }
    }
}
