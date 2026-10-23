// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Core.Display;
using Celeste.Graphics.Display;
using System.Numerics;
using Celeste.Core.Rendering;
using Celeste.Physics.Collision.Shapes;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        public override void Render(ISourceRenderContext context)
        {
            Vector2 position = Position;
            Position += shaker.Value;
            Vector2 scale = sprite.Scale;
            sprite.Scale *= 1f - .3f * scaleWiggler.Value;
            sprite.Scale.X *= spriteFacing;
            base.Render(context);
            Position = position;
            sprite.Scale = scale;
        }

        public override void DebugRender(ISourceRenderContext context, object actualCamera)
        {
            ColliderGeometry collider = Collider;
            Collider = attackHitbox;
            attackHitbox.Render(actualCamera, new PackedColor(255, 0, 0), context);
            Collider = bounceHitbox;
            bounceHitbox.Render(actualCamera, new PackedColor(0, 255, 255), context);
            Collider = collider;
        }
    }
}
