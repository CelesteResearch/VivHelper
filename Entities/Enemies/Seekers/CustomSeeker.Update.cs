// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        protected virtual bool CanSeePlayer(RoomPlayerSession player)
        {
            if (player == null)
            {
                return false;
            }

            if (State.State != 2 && StupidCanSeePlayerFix(player))
            {
                return false;
            }

            Vector2 vector =
                engine.VectorArithmetic.SafeNormalize(Vector2Geometry.Perpendicular(player.Center - base.Center), 2f);
            if (SourceSceneCollision.FirstLine<ISourceEntityCollider>(
                    ((OriginalLevelSceneRoot)Scene).Scheduler.Entities,
                    "Celeste.Solid", base.Center + vector, player.Center + vector) == null)
            {
                return SourceSceneCollision.FirstLine<ISourceEntityCollider>(
                    ((OriginalLevelSceneRoot)Scene).Scheduler.Entities, "Celeste.Solid", base.Center - vector,
                    player.Center - vector) == null;
            }

            return false;
        }

        private bool StupidCanSeePlayerFix(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            if (((OriginalLevelSceneRoot)player.Scene).Session.AreaSID ==
                "Tardigrade/WaterbearMountain/WaterbearMountain")
                return !level.InsideCamera(Center, 0f) ||
                       engine.VectorArithmetic.DistanceSquared(Center, player.Center) > SightDistSq;
            if (IgnoreCamera) return engine.VectorArithmetic.DistanceSquared(Center, player.Center) > SightDistSq;
            return !level.InsideCamera(Center, 0f) &&
                   engine.VectorArithmetic.DistanceSquared(Center, player.Center) > SightDistSq;
        }

        public override void Update()
        {
            Light.Alpha = ScalarMath.Approach(Light.Alpha, 1f, engine.DeltaTime * 2f, engine.ExtremaArithmetic);
            foreach (SeekerBarrierState entity2 in
                     ((OriginalLevelSceneRoot)Scene).Scheduler.Entities.GetTrackedEntities(
                         "Celeste.SeekerBarrier"))
            {
                entity2.Collidable = true;
            }

            sprite.Scale.X = ScalarMath.Approach(sprite.Scale.X, 1f, 2f * engine.DeltaTime, engine.ExtremaArithmetic);
            sprite.Scale.Y = ScalarMath.Approach(sprite.Scale.Y, 1f, 2f * engine.DeltaTime, engine.ExtremaArithmetic);
            if (State.State == 6)
            {
                canSeePlayer = false;
            }
            else
            {
                RoomPlayerSession entity = FirstPlayer();
                canSeePlayer = CanSeePlayer(entity);
                if (canSeePlayer)
                {
                    spotted = true;
                    lastSpottedAt = entity.Center;
                }
            }

            if (lastPathTo != lastSpottedAt)
            {
                deLagCounter -= engine.DeltaTime;
                if (deLagCounter <= 0f)
                {
                    lastPathTo = lastSpottedAt;
                    pathIndex = 0;
                    lastPathFound =
                        ((OriginalLevelSceneRoot)Scene).Pathfinder.Find(ref path, base.Center, FollowTarget, true,
                            false);
                    deLagCounter = deLagValue;
                }
            }

            base.Update();
            lastPosition = Position;
            MoveH(Speed.X * engine.DeltaTime, onCollideH);
            MoveV(Speed.Y * engine.DeltaTime, onCollideV);
            OriginalLevelSceneRoot level = (OriginalLevelSceneRoot)Scene;
            if (base.Left < (float)level.Bounds.Left && Speed.X < 0f)
            {
                base.Left = level.Bounds.Left;
                onCollideH(default(PixelCollision));
            }
            else if (base.Right > (float)level.Bounds.Right && Speed.X > 0f)
            {
                base.Right = level.Bounds.Right;
                onCollideH(default(PixelCollision));
            }

            if (base.Top < (float)(level.Bounds.Top + -8) && Speed.Y < 0f)
            {
                base.Top = level.Bounds.Top + -8;
                onCollideV(default(PixelCollision));
            }
            else if (base.Bottom > (float)level.Bounds.Bottom && Speed.Y > 0f)
            {
                base.Bottom = level.Bounds.Bottom;
                onCollideV(default(PixelCollision));
            }

            foreach (CustomSeekerCollider component in
                     ((ISourceScene)Scene).SourceScene.Components.GetTrackedComponents(colliderRegistryKey))
                component.Check(this);
            MimicSeeker();

            if (State.State == 3 && Speed.X > 0f)
            {
                bounceHitbox.Width = 16f;
                bounceHitbox.Position.X = -10f;
            }
            else if (State.State == 3 && Speed.Y < 0f)
            {
                bounceHitbox.Width = 16f;
                bounceHitbox.Position.X = -6f;
            }
            else
            {
                bounceHitbox.Width = 12f;
                bounceHitbox.Position.X = -6f;
            }

            foreach (SeekerBarrierState entity3 in
                     ((OriginalLevelSceneRoot)Scene).Scheduler.Entities.GetTrackedEntities(
                         "Celeste.SeekerBarrier"))
            {
                entity3.Collidable = false;
            }
        }
    }
}
