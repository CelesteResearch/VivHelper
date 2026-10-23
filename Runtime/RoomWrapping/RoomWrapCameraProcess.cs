// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Camera;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.RoomWrapping
{
    public sealed class RoomWrapCameraProcess : IDisposable
    {
        private readonly RoomWrapCameraContext context;
        private readonly ILHook hook;

        public RoomWrapCameraProcess(RoomWrapCameraContext context)
        {
            this.context = context;
            hook = new ILHook(typeof(RoomCamera).GetMethod(nameof(RoomCamera.Follow)), ModifyFollow);
        }

        private void ModifyFollow(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdfld<CameraPlayerStateRoles>(nameof(CameraPlayerStateRoles.NoOffsetAndSnap))))
                throw new InvalidOperationException("VivHelper RoomWrap camera snap comparison was not found.");
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<int, ISourceCameraPlayer, int>>(GetSnapState);
        }

        private int GetSnapState(int original, ISourceCameraPlayer player)
        {
            if (player is RoomPlayerSession actual && actual.Scene is OriginalLevelSceneRoot level &&
                ReferenceEquals(level.EngineContext, context.Engine) && context.Session.LockCamera != 0)
            {
                context.Session.LockCamera--;
                if (context.Session.LockCamera == 0 && player.ForceCameraUpdate) player.ForceCameraUpdate = false;
                return player.State;
            }

            return original;
        }

        public void Dispose() => hook.Dispose();
    }
}
