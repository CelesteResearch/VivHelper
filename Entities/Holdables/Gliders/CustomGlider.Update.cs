using Celeste.Input.Gameplay;
using System;
using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Game.Controls.Source;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Enemies.Seeker;
using Celeste.Physics.Collision;
using Celeste.Input.Gameplay.Feedback;

namespace Celeste.Mods.VivHelper.Entities.Holdables.Gliders
{
    public partial class CustomGlider
    {
        public override void Update()
        {
            var scheduler = ((ISourceScene)Scene).SourceScene;
            if (scheduler.Clock.OnInterval(.05f, engine))
                level.Particles.Emit(P_Glow, 1, Center + Vector2.UnitY * -9, new Vector2(10, 4));
            float target = !Hold.IsHeld
                ? 0
                : !Hold.Holder.OnGround()
                    ? ScalarMath.ClampedMap(Hold.Holder.Speed.X, -300, 300, (float)Math.PI / 3, -(float)Math.PI / 3)
                    : ScalarMath.ClampedMap(Hold.Holder.Speed.X, -300, 300, .6981317f, -.6981317f);
            var extrema = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic;
            sprite.Rotation = ScalarMath.Approach(sprite.Rotation, target, (float)Math.PI * engine.DeltaTime, extrema);
            if (Hold.IsHeld && !Hold.Holder.OnGround() &&
                (sprite.CurrentAnimationID == "fall" || sprite.CurrentAnimationID == "fallLoop"))
            {
                if (!fallingSfx.Playing)
                {
                    PlaySound("event:/new_content/game/10_farewell/glider_engage", Position);
                    fallingSfx.Play("event:/new_content/game/10_farewell/glider_movement");
                }

                var speed = Hold.Holder.Speed;
                var vector = new Vector2(speed.X * .5f, speed.Y < 0 ? speed.Y * 2 : speed.Y);
                fallingSfx.Param("glider_speed",
                    ScalarMath.Map(((ISourceVectorEnvironment)engine).VectorArithmetic.Length(vector), 0, 120, 0, .7f));
            }
            else fallingSfx.Stop();

            base.Update();
            if (destroyed)
            {
                Position += Speed * engine.DeltaTime;
                return;
            }

            foreach (var actual in scheduler.Entities.GetTrackedEntities("Celeste.SeekerBarrier"))
            {
                var barrier = (SeekerBarrierState)actual;
                barrier.Collidable = true;
                bool collided = SourceEntityCollision.Check(this, barrier);
                barrier.Collidable = false;
                if (!collided) continue;
                destroyed = true;
                Collidable = false;
                if (Hold.IsHeld)
                {
                    var heldSpeed = Hold.Holder.Speed;
                    Hold.Holder.Drop();
                    Speed = heldSpeed * .333f;
                    ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Medium,
                        (int)RumbleLength.Medium);
                }

                Add(new SimulationCoroutine(engine, DestroyAnimationRoutine()));
                return;
            }

            if (Hold.IsHeld) prevLiftSpeed = Vector2.Zero;
            else if (!bubble)
            {
                if (highFrictionTimer > 0) highFrictionTimer -= engine.DeltaTime;
                GliderMotion.AdvanceSpeed(this, engine, Hold, ref Speed, ref prevLiftSpeed, ref noGravityTimer,
                    highFrictionTimer, ref level.Wind);
                MoveH(Speed.X * engine.DeltaTime, onCollideH);
                MoveV(Speed.Y * engine.DeltaTime, onCollideV);
                if (Left < level.Bounds.Left)
                {
                    Left = level.Bounds.Left;
                    OnCollideH(new PixelCollision(-Vector2.UnitX, Vector2.Zero, Vector2.Zero));
                }
                else if (Right > level.Bounds.Right)
                {
                    Right = level.Bounds.Right;
                    OnCollideH(new PixelCollision(Vector2.UnitX, Vector2.Zero, Vector2.Zero));
                }

                if (Top < level.Bounds.Top)
                {
                    Top = level.Bounds.Top;
                    OnCollideV(new PixelCollision(-Vector2.UnitY, Vector2.Zero, Vector2.Zero));
                }
                else if (Top > level.Bounds.Bottom + 16)
                {
                    RemoveSelf();
                    return;
                }

                Hold.CheckAgainstColliders();
            }
            else Position = startPos + Vector2.UnitY * platformSine.Value * 1;

            var scale = Vector2.One;
            if (!Hold.IsHeld)
            {
                if (level.Wind.Y < 0) PlayOpen();
                else sprite.Play("idle");
            }
            else if (Hold.Holder.Speed.Y > 20 || level.Wind.Y < 0)
            {
                if (scheduler.Clock.OnInterval(.04f, engine))
                {
                    if (level.Wind.Y < 0)
                        level.ParticlesBG.Emit(P_GlideUp, 1, Position - Vector2.UnitY * 20, new Vector2(6, 4));
                    else level.ParticlesBG.Emit(P_Glide, 1, Position - Vector2.UnitY * 10, new Vector2(6, 4));
                }

                PlayOpen();
                int moveY = ((ISourceGameInputEnvironment)engine).Input.GliderMoveY.Value;
                if (moveY > 0) scale = new Vector2(.7f, 1.4f);
                else if (moveY < 0) scale = new Vector2(1.2f, .8f);
                ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Climb,
                    (int)RumbleLength.Short);
            }
            else sprite.Play("held");

            sprite.Scale.Y = ScalarMath.Approach(sprite.Scale.Y, scale.Y, engine.DeltaTime * 2, extrema);
            sprite.Scale.X = ScalarMath.Approach(sprite.Scale.X, Math.Sign(sprite.Scale.X) * scale.X,
                engine.DeltaTime * 2,
                extrema);
            if (tutorialGui != null) tutorialGui.Open = tutorial && !Hold.IsHeld && (OnGround(4) || bubble);
        }
    }
}
