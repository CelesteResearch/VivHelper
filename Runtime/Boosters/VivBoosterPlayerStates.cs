// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Runtime.DashBlocks;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Dash;

namespace Celeste.Mods.VivHelper.Runtime.Boosters
{
    internal sealed class VivBoosterPlayerStates : SourceComponentState
    {
        private static readonly Func<RoomPlayerSession, PlayerBoostSequence> GetSequence = CreateSequenceGetter();
        private VivBoosterStateProcess process;
        private readonly RoomPlayerSession player;
        private readonly PlayerBoostSequence sequence;
        private int pink;
        private int orange;
        private int wind;

        internal VivBoosterPlayerStates(VivBoosterStateProcess process, RoomPlayerSession player) : base(false, false)
        {
            this.process = process;
            this.player = player;
            sequence = GetSequence(player);
            var machine = (ISourceNamedStateMachine)player.Machine;
            pink = machine.AddState("Viv Pink Boost", PinkUpdate, PinkCoroutine, PinkBegin, End);
            orange = machine.AddState("Viv Orange Boost", OrangeUpdate, OrangeCoroutine, OrangeBegin, OrangeEnd);
            wind = machine.AddState("Viv Wind Boost", WindUpdate, WindCoroutine, WindBegin, End);
            process.States.RegisterPink(pink);
            process.States.RegisterOrange(orange);
            process.States.RegisterWindBoost(wind);
        }

        internal void Rebind(VivBoosterStateProcess owner)
        {
            process = owner;
            owner.States.RegisterPink(pink);
            owner.States.RegisterOrange(orange);
            owner.States.RegisterWindBoost(wind);
        }

        private void PinkBegin()
        {
            Refill();
            process.PinkTimer.Value = .2f;
            process.PinkCanDash = false;
        }

        private void OrangeBegin()
        {
            Refill();
            process.OrangeTimer.Value = .25f;
        }

        private void WindBegin()
        {
            Refill();
            process.WindTimer.Value = .2f;
        }

        private void Refill()
        {
            player.RefillDash();
            player.RefillStamina();
        }

        private void End()
        {
            player.Position = new Vector2(player.Position.X, (float)Math.Round(player.Position.Y));
            player.Sprite.Visible = true;
        }

        private void OrangeEnd()
        {
            player.Sprite.Visible = true;
            player.Position = new Vector2(player.Position.X, (float)Math.Round(player.Position.Y));
            process.SetOrangeSpeed(220f);
        }

        private int PinkUpdate() => Update(pink, process.PinkTimer.Value, process.PinkParticles);
        private int OrangeUpdate() => Update(orange, process.OrangeTimer.Value, process.OrangeParticles);
        private int WindUpdate() => Update(wind, process.WindTimer.Value, process.WindParticles);

        private int Update(int state, float timer,
            Celeste.Simulation.Presentation.Particles.Definitions.ParticleTypePresentation particle)
        {
            player.LastBooster = null;
            if (timer > 0f)
            {
                player.Center = player.BoostTarget;
                return state;
            }

            var level = (OriginalLevelSceneRoot)player.Scene;
            if (level.Scheduler.Clock.OnInterval(.02f, process.Engine))
                level.ParticlesBG.Emit(particle, 2, player.Center + new Vector2(0f, -2f), new Vector2(3f, 3f),
                    (float)Math.PI / 2f);
            int next = sequence.RedDashUpdate();
            return next == 5 ? state : next;
        }

        private IEnumerator PinkCoroutine()
        {
            yield return DashFix(process.PinkTimer, .25f);
            player.Speed = CorrectPrecision(player.Data.LastAim) * 240f;
            while (true)
            {
                ContinueBoost();
                yield return null;
                float angle = Vector2Geometry.Angle(player.Speed);
                Vector2 aim = player.Input.Aim.Value;
                if (aim != Vector2.Zero)
                    angle = ScalarMath.AngleApproach(Vector2Geometry.Angle(player.Speed), Vector2Geometry.Angle(aim),
                        .0349f);
                player.Speed = new Vector2(240f * (float)Math.Cos(angle), 240f * (float)Math.Sin(angle));
            }
        }

        private IEnumerator OrangeCoroutine()
        {
            yield return DashFix(process.OrangeTimer, .2f);
            player.Speed = CorrectPrecision(player.Data.LastAim) * 220f;
            while (true)
            {
                ContinueBoost();
                yield return null;
                Vector2Geometry.Angle(player.Speed);
                if (player.Motor.Dash.CanDash)
                {
                    Vector2 aim = player.Input.Aim.Value;
                    if (aim != Vector2.Zero)
                    {
                        float angle = Vector2Geometry.Angle(Vector2Geometry.EightWayNormal(CorrectPrecision(aim)));
                        player.Speed = new Vector2(process.GetOrangeSpeed() * (float)Math.Cos(angle),
                            process.GetOrangeSpeed() * (float)Math.Sin(angle));
                    }
                }
            }
        }

        private IEnumerator WindCoroutine()
        {
            yield return DashFix(process.WindTimer, .2f);
            player.Speed = CorrectPrecision(player.Data.LastAim) * 240f;
            Vector2 previous = Vector2.Zero;
            var level = (OriginalLevelSceneRoot)player.Scene;
            while (true)
            {
                ContinueBoost();
                yield return null;
                Vector2 old = previous;
                previous = player.Input.GetAimVector(1);
                if (previous != Vector2.Zero)
                {
                    previous = InstalledVectorArithmetic.Shared.SafeNormalize(CorrectPrecision(previous));
                    float x, y;
                    if (level.WindController != null)
                    {
                        x = 40f;
                        y = previous.Y < 0f ? 30f : previous.Y > 0f ? 40f : 0f;
                    }
                    else x = y = 60f;

                    previous = new Vector2(previous.X * x, previous.Y * y);
                }

                player.Speed += previous - old;
            }
        }

        private void ContinueBoost()
        {
            player.Data.Motion.GliderBoostDirection = player.Data.Direction = player.Speed;
            ((OriginalLevelSceneRoot)player.Scene).DirectionalShake(player.Data.Direction, .2f);
            if (player.Data.Direction.X != 0f) player.Data.Facing = Math.Sign(player.Data.Direction.X);
        }

        private IEnumerator DashFix(VivBoosterTimer timer, float duration)
        {
            player.Input.Dash.ConsumePress();
            player.Input.CrouchDash.ConsumePress();
            timer.Value = duration;
            while (timer.Value >= process.Engine.DeltaTime)
            {
                timer.Value -= process.Engine.DeltaTime;
                yield return null;
                if (player.Input.Dash.Pressed)
                {
                    timer.Value = 0f;
                    player.Input.Dash.ConsumePress();
                    break;
                }

                if (player.Input.CrouchDashPressed)
                {
                    timer.Value = 0f;
                    player.Input.CrouchDash.ConsumePress();
                    break;
                }
            }

            timer.Value = 0f;
        }

        internal static Vector2 CorrectPrecision(Vector2 value)
        {
            if (value.X != 0f && Math.Abs(value.X) < .01f)
            {
                value.X = 0f;
                value.Y = Math.Sign(value.Y);
            }
            else if (value.Y != 0f && Math.Abs(value.Y) < .01f)
            {
                value.Y = 0f;
                value.X = Math.Sign(value.X);
            }

            return value;
        }

        private static Func<RoomPlayerSession, PlayerBoostSequence> CreateSequenceGetter()
        {
            var method = new DynamicMethod("VivBoosterSequence", typeof(PlayerBoostSequence),
                new[] { typeof(RoomPlayerSession) }, typeof(VivBoosterPlayerStates), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld,
                typeof(RoomPlayerSession).GetField("boostSequence", BindingFlags.NonPublic | BindingFlags.Instance));
            il.Emit(OpCodes.Ret);
            return (Func<RoomPlayerSession, PlayerBoostSequence>)method.CreateDelegate(
                typeof(Func<RoomPlayerSession, PlayerBoostSequence>));
        }
    }
}
