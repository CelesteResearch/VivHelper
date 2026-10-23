// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Startup;
using Celeste.Mods.VivHelper.Components.Solids;
using Celeste.Physics.Collision;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Contacts;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.CornerBoosts
{
    public sealed class CornerBoostProcess : IDisposable
    {
        private readonly CornerBoostRuntime context;
        private readonly List<IDisposable> hooks = new();

        public CornerBoostProcess(ISourceEngineContext engine)
        {
            context = CornerBoostRuntime.For(engine);
            try
            {
                hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.IsRiding),
                        new[] { typeof(ISourceEntityCollider) }),
                    (Func<Func<RoomPlayerSession, ISourceEntityCollider, bool>, RoomPlayerSession, ISourceEntityCollider
                        , bool>)IsRiding));
                hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.ClimbJump)),
                    (Action<Action<RoomPlayerSession>, RoomPlayerSession>)ClimbJump));
                hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.WallJump)),
                    (Action<Action<RoomPlayerSession, int>, RoomPlayerSession, int>)WallJump));
                using (new DetourConfigContext(new DetourConfig("VivHelper.SolidModifierComponent",
                           after: new[] { "MaxHelpingHand" })).Use())
                    hooks.Add(new ILHook(typeof(PlayerContacts).GetMethod(nameof(PlayerContacts.WallJumpCheck)),
                        WallJumpCheck));
                hooks.Add(new Hook(typeof(SimulationStateMachine).GetMethod(nameof(SimulationStateMachine.Update)),
                    (Action<Action<SimulationStateMachine>, SimulationStateMachine>)StateMachineUpdate));
                hooks.Add(new Hook(typeof(EverestGameLoaderScene).GetMethod(nameof(EverestGameLoaderScene.Begin)),
                    (Action<Action<EverestGameLoaderScene>, EverestGameLoaderScene>)LoaderBegin));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool Owns(RoomPlayerSession player) => ReferenceEquals(player.EngineContext, context.Engine);

        private bool IsRiding(Func<RoomPlayerSession, ISourceEntityCollider, bool> original,
            RoomPlayerSession player, ISourceEntityCollider solid)
        {
            if (original(player, solid)) return true;
            if (!Owns(player) || solid is not SourceSolidEntityState actual) return false;
            var modifier = actual.OwnedComponents.Get<SolidModifierComponent>();
            if (modifier == null) return false;
            if (modifier.bufferClimbJump && modifier.HasBeenBufferJumpedOn)
            {
                modifier.HasBeenBufferJumpedOn = false;
                return true;
            }

            return modifier.triggerClimbOnTouch && SourceEntityCollision.CheckAt(player, actual,
                       player.Position + Vector2.UnitX * player.Data.Facing)
                   || modifier.OnTouchFromBelow && SourceEntityCollision.CheckAt(player, actual,
                       player.Position - Vector2.UnitY);
        }

        private void ClimbJump(Action<RoomPlayerSession> original, RoomPlayerSession player)
        {
            if (!Owns(player) || context.CurrentActive == null)
            {
                original(player);
                return;
            }

            context.CurrentActive.HasBeenBufferJumpedOn = true;
            original(player);
            if (context.CurrentActive.CornerBoostBlock == -2 || context.CurrentActive.CornerBoostRetention)
                player.Data.WallSpeedRetained = player.Speed.X;
            context.CurrentActive = null;
        }

        private void WallJump(Action<RoomPlayerSession, int> original, RoomPlayerSession player, int direction)
        {
            if (Owns(player) && context.CurrentActive != null)
            {
                context.CurrentActive.HasBeenBufferJumpedOn = true;
                context.CurrentActive = null;
            }

            original(player, direction);
        }

        private void StateMachineUpdate(Action<SimulationStateMachine> original, SimulationStateMachine machine)
        {
            original(machine);
            if (machine.Entity is RoomPlayerSession player && Owns(player)) context.CurrentActive = null;
        }

        private void LoaderBegin(Action<EverestGameLoaderScene> original, EverestGameLoaderScene scene)
        {
            original(scene);
            if (ReferenceEquals(scene.EngineContext, context.Engine)) context.Initialized = true;
        }

        private void WallJumpCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.Before,
                i => i.MatchCall<PlayerContacts>(nameof(PlayerContacts.SolidAt)) ||
                     i.MatchCallvirt<PlayerContacts>(nameof(PlayerContacts.SolidAt)));
            var offset = new VariableDefinition(il.Import(typeof(Vector2)));
            il.Body.Variables.Add(offset);
            cursor.Emit(OpCodes.Stloc, offset);
            cursor.Emit(OpCodes.Ldloc, offset);
            cursor.Index++;
            // Consume earlier mods' completed boolean without replacing the native query.
            while (cursor.Next != null && cursor.Next.OpCode != OpCodes.Ret)
            {
                if (cursor.Next.OpCode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch)
                    throw new InvalidOperationException("Unexpected control flow after final wall-jump query");
                cursor.Index++;
            }

            if (cursor.Next == null) throw new InvalidOperationException("Missing native wall-jump boolean return");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldloc, offset);
            cursor.EmitDelegate<Func<bool, PlayerContacts, int, Vector2, bool>>(context.ModifyWallJumpCheck);
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            context.CurrentActive = null;
        }
    }
}
