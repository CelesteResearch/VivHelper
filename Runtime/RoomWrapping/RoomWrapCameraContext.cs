// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Mods.VivHelper.Modules;
using Celeste.Mods.VivHelper.Persistence;

namespace Celeste.Mods.VivHelper.Runtime.RoomWrapping
{
    public sealed class RoomWrapCameraContext
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, RoomWrapCameraContext> contexts = new();
        private VivHelperModule owner;
        private VivHelperSession session = new();
        private RoomWrapCameraContext(ISourceEngineContext engine) => Engine = engine;
        public ISourceEngineContext Engine { get; }
        public VivHelperSession Session => owner != null ? owner.Session : session;

        public static RoomWrapCameraContext For(ISourceEngineContext engine) =>
            contexts.GetValue(engine, static e => new(e));

        public IDisposable Bind(VivHelperModule module)
        {
            owner = module;
            return new Binding(this, module);
        }

        private sealed class Binding(RoomWrapCameraContext context, VivHelperModule module) : IDisposable
        {
            public void Dispose()
            {
                if (!ReferenceEquals(context.owner, module)) return;
                context.session = context.Session;
                context.owner = null;
            }
        }
    }
}
