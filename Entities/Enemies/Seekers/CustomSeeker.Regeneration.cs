// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Game.Gameplay.Holdables.TheoCrystal;
using Celeste.Game.Gameplay.Mechanisms.Switches;
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
        protected virtual void RegenerateBegin()
        {
            engine.SpatialAudio.Play("event:/game/general/thing_booped", this.Position);
            boopedSfx.Play(boopedSFX, (string)null, 0f);
            sprite.Play("takeHit", false, false);
            this.Collidable = false;
            State.Locked = true;
            Light.StartRadius = 16f;
            Light.EndRadius = 32f;
            numberOfBounces++;
        }

        protected virtual void RegenerateEnd()
        {
            reviveSfx.Play(reviveSFX, (string)null, 0f);
            this.Collidable = true;
            Light.StartRadius = 32f;
            Light.EndRadius = 64f;
        }

        protected virtual int RegenerateUpdate()
        {
            Speed.X = Approach(Speed.X, 0f, 150f * engine.DeltaTime);
            Speed = Approach(Speed, Vector2.Zero, 150f * engine.DeltaTime);
            return 6;
        }

        protected virtual IEnumerator RegenerateCoroutine()
        {
            yield return 1f * RegenerateTimerMult;
            shaker.On = true;
            if (numberOfBounces == maxNumberOfBounces)
            {
                yield return seekerDeath(0.15f * RegenerateTimerMult);
                yield break;
            }

            yield return 0.2f * RegenerateTimerMult;
            sprite.Play("pulse", false, false);
            yield return 0.5f * RegenerateTimerMult;
            sprite.Play("recover", false, false);
            CustomSeekerRecoverBlast.Spawn(engine, this.Position, CustomShockwavePath);
            yield return 0.15f * RegenerateTimerMult;
            base.Collider = pushRadius;
            RoomPlayerSession player =
                CollideFirst<RoomPlayerSession>(context.PlayerRegistryKey);

            if (player != null && SourceSceneCollision.FirstLine<ISourceEntityCollider>(
                    ((OriginalLevelSceneRoot)Scene).Scheduler.Entities, "Celeste.Solid", Position, player.Center) ==
                null)
            {
                player.ExplodeLaunch(Position);
            }

            TheoCrystalState theoCrystal = CollideFirst<TheoCrystalState>("Celeste.TheoCrystal");
            if (theoCrystal != null && SourceSceneCollision.FirstLine<ISourceEntityCollider>(
                    ((OriginalLevelSceneRoot)Scene).Scheduler.Entities, "Celeste.Solid", Position,
                    theoCrystal.Center) ==
                null)
            {
                theoCrystal.ExplodeLaunch(Position);
            }

            foreach (global::Celeste.Game.Gameplay.Walls.Breakable.OriginalTempleCrackedBlockState entity in
                     ((OriginalLevelSceneRoot)Scene).Scheduler.Entities
                     .GetTrackedEntities("Celeste.TempleCrackedBlock"))
            {
                if (SourceEntityCollision.Check(this, entity))
                {
                    entity.Break(Position);
                }
            }

            foreach (OriginalTouchSwitchState entity2 in ((OriginalLevelSceneRoot)Scene).Scheduler.Entities
                     .GetTrackedEntities("Celeste.TouchSwitch"))
            {
                if (SourceEntityCollision.Check(this, entity2))
                {
                    entity2.TurnOn();
                }
            }

            base.Collider = physicsHitbox;
            OriginalLevelSceneRoot level = (OriginalLevelSceneRoot)Scene;
            level.Displacement.AddBurst(Position, 0.4f, 12f, 36f, 0.5f);
            level.Displacement.AddBurst(Position, 0.4f, 24f, 48f, 0.5f);
            level.Displacement.AddBurst(Position, 0.4f, 36f, 60f, 0.5f);
            if (!disableAllParticles)
                for (float num = 0f; num < (float)Math.PI * 2f; num += 0.17453292f)
                {
                    Vector2 position = base.Center + Vector2Geometry.AngleToVector(
                        num + SourceRandomMath.Range(engine.Random, -(float)Math.PI / 90f, (float)Math.PI / 90f),
                        SourceRandomMath.Range(engine.Random, 12, 18));
                    level.Particles.Emit(engine.SeekerParticles.P_Regen, position, num);
                }

            shaker.On = false;
            State.Locked = false;
            State.State = 7;
        }

        protected virtual IEnumerator ReturnedCoroutine()
        {
            yield return 0.3f;
            State.State = 0;
        }
    }
}
