// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Walls.Breakable;
using Celeste.Mods.VivHelper.Entities.Walls.DashBlocks;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.DashBlocks
{
    public sealed class CustomDashBlockHooks : IDisposable
    {
        private static readonly Func<OriginalDashBlockState, ISourceEngineContext> Owner = BindOwner();

        private static Func<OriginalDashBlockState, ISourceEngineContext> BindOwner()
        {
            var method = new DynamicMethod("CustomDashBlockSourceOwner", typeof(ISourceEngineContext),
                new[] { typeof(OriginalDashBlockState) }, typeof(CustomDashBlockHooks), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld,
                typeof(SourcePlatformEntityState).GetField("SourceWorld",
                    BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Callvirt, typeof(ISourceSolidWorld).GetProperty("EngineContext").GetMethod);
            il.Emit(OpCodes.Ret);
            return (Func<OriginalDashBlockState, ISourceEngineContext>)method.CreateDelegate(
                typeof(Func<OriginalDashBlockState, ISourceEngineContext>));
        }

        private readonly ISourceEngineContext engine;
        private readonly CustomDashBlockPlayerStates states;
        private readonly Hook breakHook;
        private readonly Hook dashedHook;

        public CustomDashBlockHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            states = CustomDashBlockPlayerStates.For(engine);
            breakHook = new Hook(typeof(OriginalDashBlockState).GetMethod(nameof(OriginalDashBlockState.Break)),
                (Action<Action<OriginalDashBlockState, Vector2, Vector2, bool, bool>, OriginalDashBlockState,
                    Vector2, Vector2, bool, bool>)Break);
            try
            {
                dashedHook = new Hook(
                    typeof(OriginalDashBlockState).GetMethod("OnDashed",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    (Func<Func<OriginalDashBlockState, RoomPlayerSession, Vector2, DashCollisionResult>,
                        OriginalDashBlockState, RoomPlayerSession, Vector2, DashCollisionResult>)Dashed);
            }
            catch
            {
                breakHook.Dispose();
                throw;
            }
        }

        private void Break(Action<OriginalDashBlockState, Vector2, Vector2, bool, bool> original,
            OriginalDashBlockState block, Vector2 from, Vector2 direction, bool sound, bool debrisSound)
        {
            if (ReferenceEquals(Owner(block), engine) && block is CustomDashBlock custom)
                custom.Break2(from, direction, sound, debrisSound);
            else original(block, from, direction, sound, debrisSound);
        }

        private DashCollisionResult Dashed(
            Func<OriginalDashBlockState, RoomPlayerSession, Vector2, DashCollisionResult> original,
            OriginalDashBlockState block, RoomPlayerSession player, Vector2 direction)
        {
            if (ReferenceEquals(Owner(block), engine) && states.IsBoost(player.Machine.State))
            {
                block.Break(player.Center, direction, true);
                return DashCollisionResult.Ignore;
            }

            return original(block, player, direction);
        }

        public void Dispose()
        {
            dashedHook.Dispose();
            breakHook.Dispose();
        }
    }
}
