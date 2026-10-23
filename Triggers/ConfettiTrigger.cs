using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Maps;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Checkpoints.Summit;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.VivHelper.Triggers
{
    [CustomEntity("VivHelper/ConfettiTrigger")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("VivHelper.Triggers.ConfettiTrigger")]
    public sealed class ConfettiTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly MapEntityId id;
        private readonly float cooldownTimer;
        private float cooldown;
        public readonly Vector2 PositionOfConfetti;
        public readonly bool OnlyOnce;
        public readonly bool Permanent;

        public ConfettiTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset, MapEntityId id)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            this.id = id;
            PositionOfConfetti = data.Nodes[0] + offset;
            OnlyOnce = data.Bool("onlyOnce", true);
            Permanent = data.Bool("permanent");
            cooldownTimer = data.Float("RepeatOnCycle");
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            Emit();
            if (cooldownTimer > 0) cooldown = cooldownTimer;
            if (OnlyOnce) RemoveSelf();
            if (!Permanent) return;
            ((OriginalLevelSceneRoot)Scene).Session.DoNotLoad.Add(id);
            RemoveSelf();
        }

        public override void OnStay(RoomPlayerSession player)
        {
            base.OnStay(player);
            if (OnlyOnce || !(cooldownTimer > 0)) return;
            cooldown -= engine.DeltaTime;
            if (!(cooldown <= 0)) return;
            Emit();
            cooldown = cooldownTimer;
        }

        private void Emit()
        {
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/07_summit/checkpoint_confetti",
                PositionOfConfetti);
            ((OriginalLevelSceneRoot)Scene).Scheduler.QueueSourceEntity(
                new OriginalSummitCheckpointState.ConfettiRenderer(engine, PositionOfConfetti));
        }
    }
}
