using Celeste.Core.Components;
using System.Numerics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.VivHelper.Persistence;
using Celeste.Mods.VivHelper.Runtime.RefillCancellation;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.VivHelper.Triggers
{
    [CustomEntity("VivHelper/RCSParticleCountTrigger")]
    [RuntimeTypeAlias("VivHelper.Triggers.RefillCancelParticleTrigger")]
    public sealed class RefillCancelParticleTrigger : SourceTriggerEntityState
    {
        private readonly RefillCancelContext context;
        private ColorRefillType previousValue;
        public ColorRefillType SetValue;
        public bool RevertOnLeave;

        public RefillCancelParticleTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            context = RefillCancelContext.For(engine);
            SetValue = data.Enum("SetValue", ColorRefillType.Normal);
            RevertOnLeave = data.Bool("RevertOnLeave");
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            if (RevertOnLeave) previousValue = context.Settings.DecreaseParticles;
            base.OnEnter(player);
            context.Settings.DecreaseParticles = SetValue;
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            if (RevertOnLeave) context.Settings.DecreaseParticles = previousValue;
        }
    }
}
