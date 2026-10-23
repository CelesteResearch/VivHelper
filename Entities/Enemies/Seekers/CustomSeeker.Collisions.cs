// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Simulation.Gameplay.Mechanisms.Switches;
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
        protected virtual void SlammedIntoWall(PixelCollision data)
        {
            float num;
            float num2;
            if (data.Direction.X > 0f)
            {
                num = (float)Math.PI;
                num2 = this.Right;
            }
            else
            {
                num = 0f;
                num2 = this.Left;
            }

            if (!disableAllParticles)
            {
                ((OriginalLevelSceneRoot)Scene).Particles.Emit(engine.SeekerParticles.P_HitWall, 12,
                    new Vector2(num2, this.Y), Vector2.UnitY * 4f, num);
            }

            if (data.Hit is ISourceDashSwitch dashSwitch)
                dashSwitch.OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            this.Collider = breakWallsHitbox;
            foreach (global::Celeste.Game.Gameplay.Walls.Breakable.OriginalTempleCrackedBlockState entity in
                     ((OriginalLevelSceneRoot)Scene).Scheduler.Entities
                     .GetTrackedEntities("Celeste.TempleCrackedBlock"))
            {
                var val = entity;
                if (SourceEntityCollision.CheckAt(this, val, this.Position + Vector2.UnitX * (float)Math.Sign(Speed.X)))
                {
                    val.Break(this.Center);
                }
            }

            this.Collider = physicsHitbox;
            ((OriginalLevelSceneRoot)Scene).DirectionalShake(Vector2.UnitX * (float)Math.Sign(Speed.X), 0.3f);
            engine.RumbleSession.Rumble(1, 1);
            Speed.X = (float)Math.Sign(Speed.X) * (0f - StunXSpeed);
            Speed.Y *= 0.4f;
            sprite.Scale.X = 0.6f;
            sprite.Scale.Y = 1.4f;
            shaker.ShakeFor(0.5f, false);
            scaleWiggler.Start();
            State.State = 4;
            if (data.Hit is SeekerBarrierState barrier)
            {
                barrier.OnReflectSeeker();
                engine.SpatialAudio.Play("event:/game/05_mirror_temple/seeker_hit_lightwall", Position);
            }
            else
            {
                engine.SpatialAudio.Play("event:/game/05_mirror_temple/seeker_hit_normal", this.Position);
            }

            if (numberOfWallCollides == maxNumberOfWallCollides)
            {
                Add(new SimulationCoroutine(engine, seekerDeath(0.15f)));
            }
        }

        private IEnumerator seekerDeath(float f)
        {
            yield return f;
            SeekerDeath();
        }

        protected virtual void OnCollideH(PixelCollision data)
        {
            if (State.State == 3 && data.Hit != null)
            {
                int num = Math.Sign(Speed.X);
                if ((!this.CollideCheck("Celeste.Solid", this.Position + new Vector2((float)num, 4f)) &&
                     !this.MoveVExact(4)) ||
                    (!this.CollideCheck("Celeste.Solid", this.Position + new Vector2((float)num, -4f)) &&
                     !this.MoveVExact(-4)))
                {
                    return;
                }
            }

            if ((State.State == 3 || State.State == 5) && Math.Abs(Speed.X) >= WallCollideStunThreshold)
            {
                numberOfWallCollides++;
                SlammedIntoWall(data);
            }
            else
            {
                Speed.X *= -0.2f;
            }
        }

        protected virtual void OnCollideV(PixelCollision data)
        {
            if (State.State == 3)
            {
                Speed.Y *= -0.6f;
            }
            else
            {
                Speed.Y *= -0.2f;
            }
        }
    }
}
