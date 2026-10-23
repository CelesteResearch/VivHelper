using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.VivHelper.Persistence;

namespace Celeste.Mods.VivHelper.Runtime.RefillCancellation
{
    public sealed class RefillCancelContext
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, RefillCancelContext> contexts = new();
        private SourceEverestModule owner;
        private VivHelperSettings settings = new();
        private VivHelperSession session = new();

        private RefillCancelContext(ISourceEngineContext engine) => Engine = engine;
        public ISourceEngineContext Engine { get; }
        public bool InSpace, DashRefillRestrict, DashRestrict, StaminaRefillRestrict;
        internal PlayerRefillIndicator Indicator { get; set; }
        public VivHelperSettings Settings => owner != null ? (VivHelperSettings)owner._Settings : settings;
        public VivHelperSession Session => owner != null ? (VivHelperSession)owner._Session : session;

        public static RefillCancelContext For(ISourceEngineContext engine) =>
            contexts.GetValue(engine, static e => new(e));

        public IDisposable Bind(SourceEverestModule module)
        {
            owner = module;
            return new Binding(this, module);
        }

        private sealed class Binding(RefillCancelContext context, SourceEverestModule module) : IDisposable
        {
            public void Dispose()
            {
                if (!ReferenceEquals(context.owner, module)) return;
                context.settings = context.Settings;
                context.session = context.Session;
                context.owner = null;
            }
        }
    }
}
