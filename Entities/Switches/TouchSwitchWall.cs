// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Core.Display;
using Celeste.Physics.Collision;
using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Simulation.Rendering;
using Celeste.Simulation;
using Celeste.Graphics.Display;
using Celeste.Game.Gameplay.Mechanisms.Switches;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Graphics.Rendering;

namespace Celeste.Mods.VivHelper.Entities.Switches
{
    [CustomEntity("VivHelper/TouchSwitchWall = Load")]
    [RuntimeTypeAlias("VivHelper.Entities.TouchSwitchWall")]
    public class TouchSwitchWall : global::Celeste.Scene.Entities.SceneEntityState
    {
        protected PackedColor inactiveColor = PackedColor.FromHex("5fcde4");
        protected PackedColor activeColor = PackedColor.White;
        protected PackedColor finishColor = PackedColor.FromHex("f141df");
        private readonly IOriginalTouchSwitchProcess process;

        private global::Celeste.Game.Execution.Levels.OriginalLevelSceneRoot Level =>
            (global::Celeste.Game.Execution.Levels.OriginalLevelSceneRoot)Scene;

        public global::Celeste.Game.Components.Switches.OriginalSwitchComponent Switch;
        private float ease;
        private float timer;
        protected global::Celeste.Graphics.Animation.Sprite icon;
        private readonly bool disableParticles;
        private readonly global::Celeste.Simulation.Presentation.Particles.Definitions.ParticleTypePresentation fire;

        private static readonly
            System.Runtime.CompilerServices.ConditionalWeakTable<IOriginalTouchSwitchProcess, global::Celeste.Simulation
                .Presentation.Particles.Definitions.ParticleTypePresentation> fires = new();

        private global::Celeste.Scene.Components.Oscillation.WigglerComponent wiggler;
        private global::Celeste.Simulation.Presentation.PostProcessing.Bloom.BloomPointState bloom;
        private global::Celeste.Simulation.Audio.Sources.SoundSourceComponentState touchSfx;
        private Vector2 pulse = Vector2.One;

        public static TouchSwitchWall Load(global::Celeste.Game.Execution.Levels.OriginalLevelSceneRoot level,
            global::Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            global::Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data)
            => new(data, offset, (IOriginalTouchSwitchProcess)level.EngineContext);

        public TouchSwitchWall(global::Celeste.Formats.Map.Entities.Structure.EntityData data,
            Vector2 offset, IOriginalTouchSwitchProcess process)
            : base(
                ((global::Celeste.Core.Components.ISourceEntitySourceDataEnvironment)process)
                .EntitySourceDataRegistration, data.Position + offset)
        {
            this.process = process;
            fire = fires.GetValue(process, p =>
                new global::Celeste.Simulation.Presentation.Particles.Definitions.ParticleTypePresentation(
                        p.ParticleTypes.TouchSwitch.P_Fire)
                    { Color = PackedColor.FromHex("f141df") * .1f, Color2 = PackedColor.White * .1f });
            Depth = 100;
            Switch = new global::Celeste.Game.Components.Switches.OriginalSwitchComponent(false);
            Add(Switch);
            Collider = new global::Celeste.Physics.Collision.Shapes.HitboxColliderGeometry(data.Width, data.Height);
            Add(new global::Celeste.Simulation.Gameplay.Player.Collision.Components.PlayerColliderComponentState(
                OnPlayer, Collider, null));
            Add(bloom = new global::Celeste.Simulation.Presentation.PostProcessing.Bloom.BloomPointState(0f, 16f));
            bloom.Alpha = 0f;
            icon = new global::Celeste.Graphics.Animation.Sprite(process, process.GameGraphics.GameAtlas,
                data.Attr("IconPath", "objects/touchswitch/icon"));
            icon.Add("idle", "", 0f, 0);
            icon.Add("spin", "", .1f, new global::Celeste.Core.Randomness.Chooser<string>("spin", 1f), 0, 1, 2, 3, 4,
                5);
            icon.Play("spin");
            icon.Color = inactiveColor;
            icon.CenterOrigin();
            icon.Position = Collider.Center;
            Add(icon);
            if (data.Bool("AllowHoldables", true))
                Add(new global::Celeste.Simulation.Gameplay.Holdables.HoldableColliderState(OnHoldable, Collider));
            if (data.Bool("AllowSeeker", true))
                Add(new global::Celeste.Game.Gameplay.Enemies.Seekers.SeekerColliderState(OnSeeker, Collider));
            disableParticles = data.Bool("DisableParticles", false);
            Switch.OnActivate = () =>
            {
                wiggler.Start();
                if (!disableParticles)
                    Level.Particles.Emit(process.ParticleTypes.TouchSwitch.P_FireWhite, 32, Center,
                        new Vector2(Width / 2f, Height / 2f));

                icon.Rate = 4;
            };
            Switch.OnFinish = () => ease = 0;
            Switch.OnStartFinished = () =>
            {
                icon.Rate = .1f;
                icon.Play("idle");
                icon.Color = finishColor;
                ease = 1;
            };
            wiggler = process.WigglerPool.Create(.5f, 4f, value => pulse = Vector2.One * (1f + value * .25f));
            OwnedComponents.Add(wiggler);
            var light = new global::Celeste.Simulation.Presentation.Lighting.VertexLightState(process, Vector2.Zero,
                PackedColor.White, .8f, 16, 32);
            OwnedComponents.Add(light);
            touchSfx = process.SoundSources.Create();
            OwnedComponents.Add(touchSfx);
        }

        public void TurnOn()
        {
            if (!Switch.Activated)
            {
                touchSfx.Play("event:/game/general/touchswitch_any");
                if (Switch.Activate())
                {
                    global::Celeste.Simulation.Audio.Emitters.SoundEmitterState.Play(
                        "event:/game/general/touchswitch_last_oneshot", process);
                    var cutoff = process.SoundSources.Create();
                    cutoff.Play("event:/game/general/touchswitch_last_cutoff");
                    OwnedComponents.Add(cutoff);
                }
            }
        }

        private void OnPlayer(global::Celeste.Simulation.RoomPlayerSession player) => TurnOn();
        private void OnHoldable(global::Celeste.Simulation.Gameplay.Holdables.HoldableState holdable) => TurnOn();

        private void OnSeeker(global::Celeste.Game.Gameplay.Enemies.Seekers.OriginalSeekerState seeker)
        {
            if (Level.InsideCamera(Position, 10f)) TurnOn();
        }

        public override void Update()
        {
            timer += process.DeltaTime * 8;
            ease = global::Celeste.Core.Mathematics.ScalarMath.Approach(ease,
                Switch.Finished || Switch.Activated ? 1 : 0, process.DeltaTime * 2, process.ExtremaArithmetic);
            icon.Color = global::Celeste.Graphics.Display.Colors.SourceColorInterpolation.Lerp(inactiveColor,
                Switch.Finished ? finishColor : activeColor, ease, process.ScalarArithmetic);
            icon.Color *= .5f + ((float)Math.Sin(timer) + 1) / 2 * (1 - ease) * .5f + .5f * ease;
            bloom.Alpha = ease;
            if (Switch.Finished)
            {
                if (icon.Rate > .1f)
                {
                    icon.Rate -= 2 * process.DeltaTime;
                    if (icon.Rate <= .1f)
                    {
                        icon.Rate = .1f;
                        wiggler.Start();
                        icon.Play("idle");
                        Level.Displacement.AddBurst(Position, .6f, 4f, 28f, .2f);
                    }
                }
                else if (Level.SourceScene.Clock.OnInterval(.03f, process) && !disableParticles)
                    Level.ParticlesBG.Emit(fire, 1, Center, new Vector2(Width / 2f, Height / 2f));
            }

            base.Update();
        }

        public override void Render(global::Celeste.Core.Rendering.ISourceRenderContext context)
        {
            var borderColor = icon.Color;
            borderColor.A = (byte)(255f * .7f);
            SourcePrimitiveDrawing.HollowRect(context, this, X - 1f, Y - 1f, Width + 2f, Height + 2f, borderColor);
            SourcePrimitiveDrawing.Rect(context, this, X + 1f, Y + 1f, Width - 2f, Height - 2f,
                global::Celeste.Graphics.Display.Colors.SourceColorInterpolation.Lerp(icon.Color,
                    PackedColor.FromHex("0a0a0a"), .5f, process.ScalarArithmetic) * .3f);
            base.Render(context);
        }
    }
}
