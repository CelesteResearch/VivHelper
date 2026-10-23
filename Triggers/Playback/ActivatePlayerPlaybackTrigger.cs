// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.VivHelper.Entities.Playback;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;

namespace Celeste.Mods.VivHelper.Triggers.Playback
{
    [Celeste.Everest.Tracking.Attributes.EverestTrackedAs(typeof(SourceTriggerEntityState))]
    [CustomEntity("VivHelper/ActivateCPP", "VivHelper/ActivateCPPTrigger")]
    public sealed class ActivatePlayerPlaybackTrigger : SourceTriggerEntityState
    {
        public string[] IDs;
        public bool state;
        private readonly bool onlyOnce;
        private readonly PlayerPlaybackActivationMode mode;
        private bool triggered;

        public ActivatePlayerPlaybackTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            IDs = data.Attr("CPPID", "").Split(',');
            state = data.Bool("state", true);
            onlyOnce = data.Bool("onlyOnce", false);
            mode = data.Enum("mode", PlayerPlaybackActivationMode.OnPlayerEnter);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (mode == PlayerPlaybackActivationMode.OnLevelStart) Trigger();
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            if (mode == PlayerPlaybackActivationMode.OnPlayerEnter ||
                mode == PlayerPlaybackActivationMode.WhilePlayerInside) Trigger();
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            if (mode == PlayerPlaybackActivationMode.OnPlayerLeave) Trigger();
            if (mode == PlayerPlaybackActivationMode.WhilePlayerInside) Detrigger();
        }

        private void Trigger()
        {
            if (triggered) return;
            var entities = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(CustomPlayerPlayback));
            foreach (var id in IDs)
            foreach (CustomPlayerPlayback entity in entities)
                if (entity.customID == id)
                {
                    entity.active = state;
                    if (!state) entity.Restart();
                }

            if (onlyOnce) triggered = true;
        }

        private void Detrigger()
        {
            var entities = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(CustomPlayerPlayback));
            foreach (var id in IDs)
            foreach (CustomPlayerPlayback entity in entities)
                if (entity.customID == id)
                {
                    entity.active = !state;
                    if (state) entity.Restart();
                }
        }
    }
}
