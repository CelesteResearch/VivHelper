// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Mods.VivHelper.Entities.Spikes;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Simulation.Gameplay.Player.Collision.Contacts;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Modules
{
    public sealed class VivSpikeHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly string customSpikeKey;
        private readonly ILHook wallJumpHook;

        public VivSpikeHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            customSpikeKey = typeof(CustomSpike).AssemblyQualifiedName;
            wallJumpHook = new ILHook(typeof(PlayerContacts).GetMethod(nameof(PlayerContacts.WallJumpCheck)),
                ModifyWallJumpCheck);
        }

        public void Dispose() => wallJumpHook.Dispose();

        private void ModifyWallJumpCheck(ILContext il)
        {
            // The source hook transforms the DashAttacking boolean that controls extended reach.
            // Preserve normal reach and the native spike checks; never replace the entire result.
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.Before, instruction => instruction.MatchStloc(1),
                instruction => instruction.MatchLdloc(1));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<bool, PlayerContacts, int, bool>>(FilterExtended);
        }

        public bool FilterExtended(bool previous, PlayerContacts contacts, int direction)
        {
            var player = contacts.Body.SourceColliderOwner as RoomPlayerSession;
            if (player?.Scene is not ISourceScene scene ||
                !ReferenceEquals(((ISourceEngineContextScene)scene).EngineContext, engine)) return previous;
            var entities = scene.SourceScene.Entities;
            string key = customSpikeKey;
            if (!entities.IsTracked(key)) return previous;
            var dangerousSide = direction > 0 ? SpikeDirection.Left : SpikeDirection.Right;
            foreach (var entity in entities.GetTrackedEntities(key))
            {
                var spike = (CustomSpike)entity;
                if (!spike.BlocksWallJumpSide(dangerousSide) || spike.CanWallbounce) continue;
                Vector2 saved = player.Position;
                player.Position += Vector2.UnitX * direction * 5f;
                bool collides = SourceEntityCollision.Check(player, spike);
                player.Position = saved;
                if (collides) return false;
            }

            return previous;
        }
    }
}
