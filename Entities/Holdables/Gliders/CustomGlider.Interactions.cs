using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Graphics.Rendering;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Game.Controls.Source;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Interaction.Springs;

namespace Celeste.Mods.VivHelper.Entities.Holdables.Gliders
{
    public partial class CustomGlider
    {
        private void WindMode(Vector2 wind)
        {
            if (Hold.IsHeld) return;
            if (wind.X != 0) MoveH(wind.X * .5f);
            if (wind.Y != 0) MoveV(wind.Y);
        }

        private Vector2 PlatformAdd(int i) => new(-12 + i,
            -5 + (int)Math.Round(
                Math.Sin(((ISourceScene)Scene).SourceScene.Clock.TimeActive + i * .2f) * 1.7999999523162842));

        private static PackedColor PlatformColor(int i) => PackedColor.White * (i <= 1 || i >= 22 ? .4f : .8f);

        private void OnCollideH(PixelCollision collision)
        {
            if (collision.Hit is OriginalDashSwitchState dashSwitch)
                dashSwitch.OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            PlaySound(
                Speed.X < 0
                    ? "event:/new_content/game/10_farewell/glider_wallbounce_left"
                    : "event:/new_content/game/10_farewell/glider_wallbounce_right", Position);
            Speed.X *= -1;
            sprite.Scale = new Vector2(.8f, 1.2f);
        }

        private void OnCollideV(PixelCollision collision)
        {
            if (Math.Abs(Speed.Y) > 8)
            {
                sprite.Scale = new Vector2(1.2f, .8f);
                PlaySound("event:/new_content/game/10_farewell/glider_land", Position);
            }

            if (Speed.Y < 0) Speed.Y *= -.5f;
            else Speed.Y = 0;
        }

        private void OnPickup()
        {
            if (bubble)
                for (int i = 0; i < 24; i++)
                    level.Particles.Emit(P_Platform, Position + PlatformAdd(i), PlatformColor(i));
            AllowPushing = false;
            Speed = Vector2.Zero;
            AddTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);
            highFrictionTimer = .5f;
            bubble = false;
            wiggler.Start();
            tutorial = false;
        }

        private void OnRelease(Vector2 force)
        {
            if (force.X == 0) PlaySound("event:/new_content/char/madeline/glider_drop", Position);
            AllowPushing = true;
            RemoveTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);
            force.Y *= .5f;
            if (force.X != 0 && force.Y == 0) force.Y = -.4f;
            Speed = force * 100;
            wiggler.Start();
        }

        protected override void OnSquish(PixelCollision collision)
        {
            if (!TrySquishWiggle(collision)) RemoveSelf();
        }

        public bool HitSpring(ISourceSpringEntity spring)
        {
            if (Hold.IsHeld) return false;
            if (spring.Orientation == SpringOrientation.Floor && Speed.Y >= 0)
            {
                Speed.X *= .5f;
                Speed.Y = -160;
                noGravityTimer = .15f;
                wiggler.Start();
                return true;
            }

            if (spring.Orientation == SpringOrientation.WallLeft && Speed.X <= 0)
            {
                MoveTowardsY(spring.Center.Y + 5, 4);
                Speed.X = 160;
                Speed.Y = -80;
                noGravityTimer = .1f;
                wiggler.Start();
                return true;
            }

            if (spring.Orientation == SpringOrientation.WallRight && Speed.X >= 0)
            {
                MoveTowardsY(spring.Center.Y + 5, 4);
                Speed.X = -160;
                Speed.Y = -80;
                noGravityTimer = .1f;
                wiggler.Start();
                return true;
            }

            return false;
        }

        private IEnumerator DestroyAnimationRoutine()
        {
            PlaySound("event:/new_content/game/10_farewell/glider_emancipate", Position);
            sprite.Play("death");
            yield return 1f;
            RemoveSelf();
        }

        private void PlayOpen()
        {
            if (sprite.CurrentAnimationID == "fall" || sprite.CurrentAnimationID == "fallLoop") return;
            sprite.Play("fall");
            sprite.Scale = new Vector2(1.5f, .6f);
            level.Particles.Emit(P_Expand, 16,
                Center + ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitY * -12,
                    sprite.Rotation),
                new Vector2(8, 3), -(float)Math.PI / 2 + sprite.Rotation);
            if (Hold.IsHeld)
                ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Medium,
                    (int)RumbleLength.Short);
        }

        public override void Render(ISourceRenderContext context)
        {
            if (!destroyed) sprite.DrawSimpleOutline(context);
            base.Render(context);
            if (bubble)
                for (int i = 0; i < 24; i++)
                    SourcePrimitiveDrawing.Point(context, this, Position + PlatformAdd(i), PlatformColor(i));
        }
    }
}
