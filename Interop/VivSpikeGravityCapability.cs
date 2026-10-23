// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Interop;
using Celeste.Mods.VivHelper.Modules;
using Celeste.Scene.Entities;

namespace Celeste.Mods.VivHelper.Interop
{
    public sealed class VivSpikeGravityCapability : IVivSpikeGravityCapability, IDisposable
    {
        private readonly SourceModInteropProcess interop;
        private readonly IDisposable importLease;
        private readonly List<WeakReference<VivPlayerGravityListener>> listeners = [];
        internal GravityHelperImports Imports { get; } = new();
        private bool disposed;

        public VivSpikeGravityCapability(SourceModInteropProcess interop)
        {
            this.interop = interop ?? throw new ArgumentNullException(nameof(interop));
            importLease = interop.RegisterOwned(new SourceModInteropActualTypeBinding(typeof(GravityHelperImports),
                Imports, exportedMethods: Array.Empty<MethodInfo>()));
            interop.ImportsRefreshed += RefreshListeners;
        }

        public bool IsPlayerInverted() => !disposed && (Imports.IsPlayerInverted?.Invoke() ?? false);

        public SourceComponentState CreatePlayerGravityListener(Action<SceneEntityState, int, float> callback)
        {
            if (disposed) throw new ObjectDisposedException(nameof(VivSpikeGravityCapability));
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            var listener = new VivPlayerGravityListener(this, callback);
            listeners.Add(new WeakReference<VivPlayerGravityListener>(listener));
            return listener;
        }

        private void RefreshListeners()
        {
            for (int index = listeners.Count - 1; index >= 0; index--)
            {
                if (listeners[index].TryGetTarget(out var listener)) listener.Refresh();
                else listeners.RemoveAt(index);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            interop.ImportsRefreshed -= RefreshListeners;
            try
            {
                importLease.Dispose();
            }
            finally
            {
                RefreshListeners();
                listeners.Clear();
            }
        }
    }
}
