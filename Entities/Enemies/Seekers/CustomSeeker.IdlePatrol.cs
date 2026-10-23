using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        protected virtual int IdleUpdate()
        {
            if (canSeePlayer)
            {
                return 2;
            }

            Vector2 val = Vector2.Zero;
            if (spotted && engine.VectorArithmetic.DistanceSquared(this.Center, FollowTarget) > 64f)
            {
                float speedMagnitude = GetSpeedMagnitude(IdleSpeed);
                val = ((!lastPathFound)
                    ? engine.VectorArithmetic.SafeNormalize(FollowTarget - this.Center, speedMagnitude)
                    : GetPathSpeed(speedMagnitude));
            }

            if (val == Vector2.Zero)
            {
                val.X = idleSineX.Value * 6f;
                val.Y = idleSineY.Value * 6f;
            }

            Speed = Approach(Speed, val, IdleAccel * engine.DeltaTime);
            if (engine.VectorArithmetic.LengthSquared(Speed) > 400f)
            {
                TurnFacing(Speed.X);
            }

            if (spriteFacing == facing)
            {
                sprite.Play("idle", false, false);
            }

            return 0;
        }

        protected virtual IEnumerator IdleCoroutine()
        {
            if (patrolPoints != null && patrolPoints.Length != 0 && spotted)
            {
                while (engine.VectorArithmetic.DistanceSquared(this.Center, FollowTarget) > 64f)
                {
                    yield return null;
                }

                yield return 0.3f;
                State.State = 1;
            }
        }

        protected virtual Vector2 GetPathSpeed(float magnitude)
        {
            if (pathIndex >= path.Count)
            {
                return Vector2.Zero;
            }

            if (engine.VectorArithmetic.DistanceSquared(this.Center, path[pathIndex]) < 36f)
            {
                pathIndex++;
                return GetPathSpeed(magnitude);
            }

            return engine.VectorArithmetic.SafeNormalize(path[pathIndex] - this.Center, magnitude);
        }

        protected virtual float GetSpeedMagnitude(float baseMagnitude)
        {
            RoomPlayerSession entity = FirstPlayer();
            if (entity != null)
            {
                if (engine.VectorArithmetic.DistanceSquared(this.Center, entity.Center) > FarDistSq)
                {
                    return baseMagnitude * 1.5f * FarSpeedMultiplier;
                }

                return baseMagnitude * 1.5f;
            }

            return baseMagnitude;
        }

        protected virtual void PatrolBegin()
        {
            State.State = ChoosePatrolTarget();
            patrolWaitTimer = 0f;
        }

        protected virtual int PatrolUpdate()
        {
            if (canSeePlayer || AlwaysSeePlayer)
            {
                return 2;
            }

            if (patrolWaitTimer > 0f)
            {
                patrolWaitTimer -= engine.DeltaTime;
                if (patrolWaitTimer <= 0f)
                {
                    return ChoosePatrolTarget();
                }
            }
            else if (engine.VectorArithmetic.DistanceSquared(this.Center, lastSpottedAt) < 144f)
            {
                patrolWaitTimer = PatrolWaitTime;
            }

            float speedMagnitude = GetSpeedMagnitude(PatrolSpeed);
            Vector2 val = ((!lastPathFound)
                ? engine.VectorArithmetic.SafeNormalize(FollowTarget - this.Center, speedMagnitude)
                : GetPathSpeed(speedMagnitude));
            Speed = Approach(Speed, val, Accel * engine.DeltaTime);
            if (engine.VectorArithmetic.LengthSquared(Speed) > 100f)
            {
                TurnFacing(Speed.X);
            }

            if (spriteFacing == facing)
            {
                sprite.Play("search", false, false);
            }

            return 1;
        }

        protected virtual int ChoosePatrolTarget()
        {
            RoomPlayerSession entity = FirstPlayer();
            if (entity == null)
            {
                return 0;
            }

            for (int i = 0; i < 3; i++)
            {
                patrolChoices[i].Distance = 0f;
            }

            int num = 0;
            Vector2[] array = patrolPoints;
            Vector2[] array2 = array;
            foreach (Vector2 val in array2)
            {
                if ((double)engine.VectorArithmetic.DistanceSquared(this.Center, val) < 1.5 * (double)AttackMinXDist)
                {
                    continue;
                }

                float num2 = engine.VectorArithmetic.DistanceSquared(val, entity.Center);
                for (int k = 0; k < 3; k++)
                {
                    if (num2 < patrolChoices[k].Distance || patrolChoices[k].Distance <= 0f)
                    {
                        num++;
                        for (int num3 = 2; num3 > k; num3--)
                        {
                            patrolChoices[num3].Distance = patrolChoices[num3 - 1].Distance;
                            patrolChoices[num3].Point = patrolChoices[num3 - 1].Point;
                        }

                        patrolChoices[k].Distance = num2;
                        patrolChoices[k].Point = val;
                        break;
                    }
                }
            }

            if (num <= 0)
            {
                return 0;
            }

            lastSpottedAt = patrolChoices[random.Next(Math.Min(3, num))].Point;
            lastPathTo = lastSpottedAt;
            pathIndex = 0;
            lastPathFound =
                ((OriginalLevelSceneRoot)Scene).Pathfinder.Find(ref path, this.Center, FollowTarget, true, false);
            return 1;
        }
    }
}
