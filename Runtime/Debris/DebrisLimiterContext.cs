using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.VivHelper.Persistence;

namespace Celeste.Mods.VivHelper.Runtime.Debris
{
    internal sealed class DebrisLimiterContext
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, DebrisLimiterContext> contexts = new();
        private SourceEverestModule owner;
        private VivHelperSession session = new();

        private DebrisLimiterContext(ISourceEngineContext engine)
        {
            Engine = engine;
        }

        internal ISourceEngineContext Engine { get; }
        internal bool Locker;
        internal VivHelperSession Session => owner != null ? (VivHelperSession)owner._Session : session;

        internal static DebrisLimiterContext For(ISourceEngineContext engine) =>
            contexts.GetValue(engine, static e => new(e));

        internal IDisposable Bind(SourceEverestModule module)
        {
            owner = module;
            return new Binding(this, module);
        }

        private sealed class Binding : IDisposable
        {
            private readonly DebrisLimiterContext context;
            private readonly SourceEverestModule owner;

            internal Binding(DebrisLimiterContext context, SourceEverestModule owner)
            {
                this.context = context;
                this.owner = owner;
            }

            public void Dispose()
            {
                if (!ReferenceEquals(context.owner, owner)) return;
                context.session = context.Session;
                context.owner = null;
            }
        }
    }
}
