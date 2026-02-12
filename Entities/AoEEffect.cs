using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Timers;
using StarterTD.Engine;

namespace StarterTD.Entities;

public class AoEEffect
{
    public Vector2 Position { get; }
    public float MaxRadius { get; }
    public bool IsActive => !_timer.State.HasFlag(TimerState.Completed);

    private readonly CountdownTimer _timer;
    private const float DurationSeconds = 0.5f;
    private readonly List<Particle> _particles = new();
    private const int ParticleCount = 6;

    public AoEEffect(Vector2 position, float maxRadius)
    {
        Position = position;
        MaxRadius = maxRadius;
        _timer = new CountdownTimer(DurationSeconds);
        _timer.Start();

        Random random = new Random();
        for (int i = 0; i < ParticleCount; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float speed = 100f + (float)(random.NextDouble() * 100f);
            Vector2 velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);

            _particles.Add(new Particle(Position, velocity));
        }
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive)
            return;

        _timer.Update(gameTime);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        foreach (var particle in _particles)
        {
            particle.Update(dt);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
            return;

        float progress = (float)_timer.CurrentTime.TotalSeconds / DurationSeconds;
        float currentRadius = progress * MaxRadius;
        float opacity = 1f - progress;

        TextureManager.DrawFilledCircle(
            spriteBatch,
            Position,
            currentRadius,
            Color.Orange * opacity
        );

        foreach (var particle in _particles)
        {
            particle.Draw(spriteBatch, opacity);
        }
    }

    private sealed class Particle
    {
        private Vector2 _position;
        private readonly Vector2 _velocity;
        private readonly float _size;

        public Particle(Vector2 startPosition, Vector2 velocity)
        {
            _position = startPosition;
            _velocity = velocity;
            Random random = new Random();
            _size = 2f + (float)(random.NextDouble() * 3f);
        }

        public void Update(float dt)
        {
            _position += _velocity * dt;
        }

        public void Draw(SpriteBatch spriteBatch, float opacity)
        {
            Color particleColor = Color.Lerp(Color.Yellow, Color.OrangeRed, 1f - opacity);
            TextureManager.DrawSprite(
                spriteBatch,
                _position,
                new Vector2(_size, _size),
                particleColor * opacity
            );
        }
    }
}
