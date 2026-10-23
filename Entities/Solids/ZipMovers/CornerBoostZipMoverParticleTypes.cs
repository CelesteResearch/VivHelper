using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Processes;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.VivHelper.Entities.Solids.ZipMovers
{
    public sealed class CornerBoostZipMoverParticleTypes
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, CornerBoostZipMoverParticleTypes> instances =
            new ConditionalWeakTable<ISourceEngineContext, CornerBoostZipMoverParticleTypes>();

        public ParticleTypePresentation P_Scrape;
        public ParticleTypePresentation P_Sparks;

        private CornerBoostZipMoverParticleTypes(ISourceEngineContext engine)
        {
            var original = ((IOriginalGameplayProcess)engine).ParticleTypes.ZipMover;
            P_Scrape = original.P_Scrape;
            P_Sparks = original.P_Sparks;
        }

        public static CornerBoostZipMoverParticleTypes ForEngine(ISourceEngineContext engine) =>
            instances.GetValue(engine, owner => new CornerBoostZipMoverParticleTypes(owner));
    }
}
