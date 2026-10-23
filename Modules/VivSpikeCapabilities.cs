// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.VivHelper.Modules
{
    public static class VivSpikeCapabilities
    {
        private sealed class OwnerState
        {
            public Registration Current;
        }

        private static readonly ConditionalWeakTable<ISourceEngineContext, OwnerState> Owners = new();

        public static void RegisterGravity(ISourceEngineContext engine, IVivSpikeGravityCapability gravity)
        {
            var owner = Owners.GetValue(engine, _ => new OwnerState());
            owner.Current = gravity == null ? null : new Registration(owner, gravity, null);
        }

        public static IDisposable RegisterGravityOwned(ISourceEngineContext engine, IVivSpikeGravityCapability gravity)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (gravity == null) throw new ArgumentNullException(nameof(gravity));
            var owner = Owners.GetValue(engine, _ => new OwnerState());
            var registration = new Registration(owner, gravity, owner.Current);
            owner.Current = registration;
            return registration;
        }

        public static IVivSpikeGravityCapability GetGravity(ISourceEngineContext engine)
            => Owners.TryGetValue(engine, out var owner) ? owner.Current?.Gravity : null;

        private sealed class Registration : IDisposable
        {
            private readonly OwnerState owner;
            private readonly Registration previous;
            private bool disposed;
            public IVivSpikeGravityCapability Gravity { get; }

            public Registration(OwnerState owner, IVivSpikeGravityCapability gravity, Registration previous)
            {
                this.owner = owner;
                this.previous = previous;
                Gravity = gravity;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                if (!ReferenceEquals(owner.Current, this)) return;
                var next = previous;
                while (next != null && next.disposed) next = next.previous;
                owner.Current = next;
            }
        }
    }
}
