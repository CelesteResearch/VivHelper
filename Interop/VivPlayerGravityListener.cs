// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Components;
using Celeste.Scene.Entities;
using Celeste.Simulation;

namespace Celeste.Mods.VivHelper.Interop
{
// A dormant owner component keeps the real exported listener attachable across dependency reloads.
// It has no Update/Render loop; all changes occur at owned interop registration boundaries.
    public sealed class VivPlayerGravityListener : SourceComponentState
    {
        private readonly VivSpikeGravityCapability capability;
        private readonly Action<SceneEntityState, int, float> callback;
        private Func<Action<RoomPlayerSession, int, float>, SourceComponentState> factory;
        private SourceComponentState actual;
        private bool awake;

        internal VivPlayerGravityListener(VivSpikeGravityCapability capability,
            Action<SceneEntityState, int, float> callback) : base(false, false)
        {
            this.capability = capability;
            this.callback = callback;
        }

        public override void Added(object entity)
        {
            base.Added(entity);
            Refresh();
        }

        public override void Removed(object entity)
        {
            Detach();
            awake = false;
            base.Removed(entity);
        }

        public override void EntityAwake()
        {
            awake = true;
            // The actual component is in the entity's normal component list and receives its own Awake.
        }

        internal void Refresh()
        {
            if (Entity is not SceneEntityState entity) return;
            var next = capability.Imports.CreatePlayerGravityListener;
            if (Equals(factory, next)) return;
            Detach();
            factory = next;
            if (next == null) return;
            actual = next((player, gravity, multiplier) => callback(player, gravity, multiplier));
            if (actual == null)
                throw new InvalidOperationException("GravityHelper returned a null player gravity listener.");
            entity.Add(actual);
            if (awake && Scene != null) actual.EntityAwake();
        }

        private void Detach()
        {
            if (actual != null && Entity is SceneEntityState entity) entity.Remove(actual);
            actual = null;
            factory = null;
        }
    }
}
