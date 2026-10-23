// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Events.Player;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Mods.VivHelper.Runtime.DashBlocks;
using Celeste.Simulation;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.VivHelper.Runtime.Boosters
{
    internal sealed partial class VivBoosterStateProcess : IDisposable
    {
        internal readonly ISourceEngineContext Engine;
        internal readonly CustomDashBlockPlayerStates States;
        internal readonly Func<float> GetOrangeSpeed;
        internal readonly Action<float> SetOrangeSpeed;
        internal readonly VivBoosterTimer OrangeTimer = new();
        internal readonly VivBoosterTimer PinkTimer = new();
        internal readonly VivBoosterTimer WindTimer = new();
        internal bool PinkCanDash;
        internal ParticleTypePresentation OrangeParticles, PinkParticles, WindParticles;

        internal VivBoosterStateProcess(ISourceEngineContext engine, Func<float> getOrangeSpeed,
            Action<float> setOrangeSpeed)
        {
            Engine = engine;
            States = CustomDashBlockPlayerStates.For(engine);
            GetOrangeSpeed = getOrangeSpeed;
            SetOrangeSpeed = setOrangeSpeed;
            InstallDashStartHook();
            EverestPlayerSourceEvents.OnRegisterStates += Register;
            try
            {
                if (engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot level)
                    foreach (var entity in level.Scheduler.Entities.GetTrackedEntities(typeof(RoomPlayerSession)
                                 .FullName))
                        Register((RoomPlayerSession)entity);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Register(RoomPlayerSession player)
        {
            if (!ReferenceEquals(player.EngineContext, Engine)) return;
            if (OrangeParticles == null)
            {
                OrangeParticles = CreateParticle(new PackedColor(240, 120, 72));
                PinkParticles = CreateParticle(new PackedColor(252, 124, 244));
                WindParticles = CreateParticle(new PackedColor(192, 192, 192));
            }

            var states = player.OwnedComponents.Get<VivBoosterPlayerStates>();
            if (states == null)
                player.Add(new VivBoosterPlayerStates(this, player));
            else states.Rebind(this);
        }

        private ParticleTypePresentation CreateParticle(PackedColor color) => new()
        {
            Source = ((ISourceGameGraphicsEnvironment)Engine).GameGraphics.GameAtlas["particles/blob"],
            Color = color, FadeMode = ParticleTypePresentation.FadeModes.None,
            LifeMin = .5f, LifeMax = .8f, Size = .7f, SizeRange = .25f, ScaleOut = true,
            Direction = 4.712389f, DirectionRange = .17453292f, SpeedMin = 10f, SpeedMax = 20f,
            SpeedMultiplier = .01f, Acceleration = new Vector2(0f, 90f)
        };

        public void Dispose()
        {
            EverestPlayerSourceEvents.OnRegisterStates -= Register;
            dashStartHook.Dispose();
        }
    }
}
