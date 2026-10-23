// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Components;
using Celeste.Simulation;
using MonoMod.ModInterop;

namespace Celeste.Mods.VivHelper.Interop
{
    [ModImportName("GravityHelper")]
    public sealed class GravityHelperImports
    {
        public Func<bool> IsPlayerInverted;
        public Func<Action<RoomPlayerSession, int, float>, SourceComponentState> CreatePlayerGravityListener;
    }
}
