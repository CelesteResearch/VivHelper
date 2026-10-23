// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        protected virtual void OnAttackPlayer(RoomPlayerSession player)
        {
            if (State.State != 4)
            {
                player.Die(engine.VectorArithmetic.SafeNormalize(player.Center - this.Position), false, true);
                return;
            }

            ColliderGeometry collider = this.Collider;
            this.Collider = bounceHitbox;
            player.PointBounce(this.Center);
            Speed = engine.VectorArithmetic.SafeNormalize(this.Center - player.Center, 100f);
            scaleWiggler.Start();
            this.Collider = collider;
        }

        protected virtual void OnBouncePlayer(RoomPlayerSession player)
        {
            ColliderGeometry collider = this.Collider;
            this.Collider = attackHitbox;
            if (SourceEntityCollision.Check(this, player))
            {
                OnAttackPlayer(player);
            }
            else
            {
                player.Bounce(this.Top);
                GotBouncedOn(player);
            }

            this.Collider = collider;
        }

        protected virtual void GotBouncedOn(ISourceEntityCenter entity)
        {
            engine.Freeze(0.15f);
            Speed = engine.VectorArithmetic.SafeNormalize(this.Center - entity.Center, BounceSpeed);
            State.State = 6;
            sprite.Scale = new Vector2(1.4f, 0.6f);
            if (!disableAllParticles)
            {
                ((OriginalLevelSceneRoot)Scene).Particles.Emit(engine.SeekerParticles.P_Stomp, 8,
                    this.Center - Vector2.UnitY * 5f, new Vector2(6f, 3f));
            }
        }

        protected virtual void HitSpring()
        {
            Speed.Y = -150f;
        }

        protected virtual void OnHoldable(HoldableState holdable)
        {
            if (State.State != 6 && holdable.Dangerous(theo))
            {
                OriginalSeekerState val = engine.SeekerFactory.Create(this.Position, patrolPoints, engine);
                holdable.HitSeeker(val);
                val.RemoveSelf();
                State.State = 4;
                Speed = engine.VectorArithmetic.SafeNormalize(
                    this.Center - ((ISourceEntityCenter)holdable.Entity).Center, 120f);
                scaleWiggler.Start();
            }
            else if ((State.State == 3 || State.State == 5) && holdable.IsHeld)
            {
                holdable.Swat(theo, Math.Sign(Speed.X));
                State.State = 4;
                Speed = engine.VectorArithmetic.SafeNormalize(
                    this.Center - ((ISourceEntityCenter)holdable.Entity).Center, 120f);
                scaleWiggler.Start();
            }
        }
    }
}
