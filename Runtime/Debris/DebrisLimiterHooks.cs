using System;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Simulation.Gameplay.Actors.Debris;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.Debris
{
    public sealed class DebrisLimiterHooks : IDisposable
    {
        private readonly DebrisLimiterContext context;
        private readonly IDisposable binding;
        private Hook hook;

        public DebrisLimiterHooks(ISourceEngineContext engine, SourceEverestModule module)
        {
            context = DebrisLimiterContext.For(engine);
            binding = context.Bind(module);
            try
            {
                hook = new Hook(typeof(DebrisState).GetMethod(nameof(DebrisState.Added)),
                    (Action<Action<DebrisState, object>, DebrisState, object>)Added);
            }
            catch
            {
                binding.Dispose();
                throw;
            }
        }

        private void Added(Action<DebrisState, object> orig, DebrisState self, object scene)
        {
            orig(self, scene);
            if (scene is not ISourceEngineContextScene source ||
                !ReferenceEquals(source.EngineContext, context.Engine)) return;
            if (context.Session.DebrisLimiter == 1f) self.RemoveSelf();
            else if (context.Session.DebrisLimiter > 0f &&
                     !(SourceRandomMath.NextFloat(((ISourceRandomEnvironment)context.Engine).Random) <
                       context.Session.DebrisLimiter)) self.RemoveSelf();
        }

        public void Dispose()
        {
            hook?.Dispose();
            hook = null;
            binding.Dispose();
        }
    }
}
