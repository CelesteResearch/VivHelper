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
        protected virtual void SpottedBegin()
        {
            aggroSfx.Play(aggroSFX, (string)null, 0f);
            RoomPlayerSession entity = FirstPlayer();
            if (entity != null)
            {
                TurnFacing(entity.X - this.X, "spot");
            }

            spottedLosePlayerTimer = SpottedLosePlayerTime;
            spottedTurnDelay = 1f;
        }

        protected virtual int SpottedUpdate()
        {
            if (!canSeePlayer && !AlwaysSeePlayer)
            {
                spottedLosePlayerTimer -= engine.DeltaTime;
                if (spottedLosePlayerTimer < 0f)
                {
                    return 0;
                }
            }
            else
            {
                spottedLosePlayerTimer = SpottedLosePlayerTime;
            }

            float speedMagnitude = GetSpeedMagnitude(SpottedTargetSpeed);
            Vector2 val = ((!lastPathFound)
                ? engine.VectorArithmetic.SafeNormalize(FollowTarget - this.Center, speedMagnitude)
                : GetPathSpeed(speedMagnitude));
            if (engine.VectorArithmetic.DistanceSquared(this.Center, FollowTarget) < 2500f && this.Y < FollowTarget.Y)
            {
                float num = Vector2Geometry.Angle(val);
                if (this.Y < FollowTarget.Y - 2f)
                {
                    num = ScalarMath.AngleLerp(num, (float)Math.PI / 2f, 0.5f);
                }
                else if (this.Y > FollowTarget.Y + 2f)
                {
                    num = ScalarMath.AngleLerp(num, -(float)Math.PI / 2f, 0.5f);
                }

                val = Vector2Geometry.AngleToVector(num, SpottedTargetSpeed);
                Vector2 val2 = Vector2.UnitX * (float)Math.Sign(this.X - lastSpottedAt.X) * 48f;
                if (Math.Abs(this.X - lastSpottedAt.X) < XDistanceFromWindUp &&
                    !CollideCheck("Celeste.Solid", this.Position + val2) &&
                    !CollideCheck("Celeste.Solid", lastSpottedAt + val2))
                {
                    val.X = Math.Sign(this.X - lastSpottedAt.X) * 60;
                }
            }

            Speed = Approach(Speed, val, Accel * engine.DeltaTime);
            spottedTurnDelay -= engine.DeltaTime;
            if (spottedTurnDelay <= 0f)
            {
                TurnFacing(Speed.X, "spotted");
            }

            return 2;
        }

        protected virtual IEnumerator SpottedCoroutine()
        {
            yield return SpottedMinAttackTime;
            while (!CanAttack())
            {
                yield return null;
            }

            State.State = 3;
        }

        protected virtual bool CanAttack()
        {
            if (Math.Abs(this.Y - lastSpottedAt.Y) > SpottedMaxYDist)
            {
                return false;
            }

            if (Math.Abs(this.X - lastSpottedAt.X) < AttackMinXDist)
            {
                return false;
            }

            Vector2 val = engine.VectorArithmetic.SafeNormalize(FollowTarget - this.Center);
            if (engine.VectorArithmetic.Dot(-Vector2.UnitY, val) > 0.5f ||
                engine.VectorArithmetic.Dot(Vector2.UnitY, val) > 0.5f)
            {
                return false;
            }

            if (CollideCheck("Celeste.Solid",
                    this.Position + Vector2.UnitX * (float)Math.Sign(lastSpottedAt.X - this.X) * SpottedMaxYDist))
            {
                return false;
            }

            if (numberOfDashes + 1 > maxNumberOfDashes && maxNumberOfDashes != -1)
            {
                return false;
            }

            return true;
        }

        protected virtual void AttackBegin()
        {
            engine.SpatialAudio.Play("event:/game/05_mirror_temple/seeker_dash", this.Position);
            attackWindUp = true;
            attackSpeed = AttackWindUpSpeed;
            Speed = engine.VectorArithmetic.SafeNormalize(FollowTarget - this.Center, AttackWindUpSpeed);
            numberOfDashes++;
        }

        protected virtual int AttackUpdate()
        {
            if (!attackWindUp)
            {
                Vector2 val = engine.VectorArithmetic.SafeNormalize(FollowTarget - this.Center);
                if (engine.VectorArithmetic.Dot(engine.VectorArithmetic.SafeNormalize(Speed), val) <
                    DirectionDotThreshold && (!finalDash || numberOfDashes != maxNumberOfDashes))
                {
                    return 5;
                }

                attackSpeed = Approach(attackSpeed, AttackTargetSpeed, AttackAccel * engine.DeltaTime);
                Speed = engine.VectorArithmetic.SafeNormalize(
                    engine.VectorArithmetic.RotateTowards(Speed, Vector2Geometry.Angle(val),
                        AttackMaxRotateRadians * engine.DeltaTime), attackSpeed);
                if (ParticleEmitInterval > 0f && !disableAllParticles &&
                    ((ISourceScene)Scene).SourceScene.Clock.OnInterval(ParticleEmitInterval, engine) &&
                    ((OriginalLevelSceneRoot)Scene).InsideCamera(this.Position, 10f))
                {
                    Vector2 val2 = engine.VectorArithmetic.SafeNormalize(-Speed);
                    ((OriginalLevelSceneRoot)Scene).Particles.Emit(engine.SeekerParticles.P_Attack, 2,
                        this.Position + val2 * 4f, Vector2.One * 4f, Vector2Geometry.Angle(val2));
                }

                if (TrailCreateInterval > 0f &&
                    ((ISourceScene)Scene).SourceScene.Clock.OnInterval(TrailCreateInterval, engine) &&
                    ((OriginalLevelSceneRoot)Scene).InsideCamera(this.Position, 10f))
                {
                    CreateTrail();
                }
            }

            return 3;
        }

        protected virtual IEnumerator AttackCoroutine()
        {
            TurnFacing(lastSpottedAt.X - this.X, "windUp");
            yield return AttackWindUpTime;
            attackWindUp = false;
            attackSpeed = AttackStartSpeed;
            Speed = engine.VectorArithmetic.SafeNormalize(lastSpottedAt - Vector2.UnitY * 2f - this.Center, 180f);
            SnapFacing(Speed.X);
        }
    }
}
