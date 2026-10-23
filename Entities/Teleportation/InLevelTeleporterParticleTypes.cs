using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.VivHelper.Entities.Teleportation
{
    internal sealed class InLevelTeleporterParticleTypes
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, InLevelTeleporterParticleTypes> contexts =
            new();

        internal ParticleTypePresentation Glow;

        internal static InLevelTeleporterParticleTypes For(ISourceEngineContext engine) =>
            contexts.GetValue(engine, static _ => new());
    }
}
