// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Components;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Presentation.Effects.Trails.Lifecycle;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        private string colliderRegistryKey;

        private float Approach(float value, float target, float maxMove) =>
            ScalarMath.Approach(value, target, maxMove, engine.ExtremaArithmetic);

        private Vector2 Approach(Vector2 value, Vector2 target, float maxMove) =>
            engine.VectorArithmetic.Approach(value, target, maxMove);

        private void MimicSeeker()
        {
            List<SeekerColliderState> list = null;
            foreach (SeekerColliderState c in ((ISourceScene)Scene).SourceScene.Components.GetTrackedComponents(
                         "Celeste.SeekerCollider"))
                if (SourceEntityCollision.Check(this, (ISourceEntityCollider)c.Entity))
                {
                    if (list == null) list = new List<SeekerColliderState>();
                    list.Add(c);
                }

            if (list == null) return;
            var seeker = engine.SeekerFactory.Create(Position, patrolPoints, engine);
            foreach (var c in list) c.Check(seeker);
        }

        protected virtual void CreateTrail()
        {
            Vector2 scale = sprite.Scale;
            sprite.Scale *= 1f - .3f * scaleWiggler.Value;
            sprite.Scale.X *= spriteFacing;
            TrailManagerState.Add(engine, engine, this, TrailColor, .5f);
            sprite.Scale = scale;
        }
    }
}
