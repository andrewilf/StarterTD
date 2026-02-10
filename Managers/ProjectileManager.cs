using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Entities;
using StarterTD.Interfaces;

namespace StarterTD.Managers;

/// <summary>
/// Note: Projectiles are currently managed per-tower inside Tower.cs.
/// This manager exists as a future extension point if you want to
/// centralize projectile management (e.g., for global effects, pooling).
///
/// For now, tower.Update() and tower.Draw() handle their own projectiles.
/// </summary>
# pragma warning disable S2094 // Reserved for the future
public class ProjectileManager
{
    // Reserved for future use.
    // Currently, projectiles live inside each Tower instance.
    // If you need global projectile effects (e.g., chain lightning),
    // move projectile ownership here.
}
# pragma warning restore S2094
