// SPDX-License-Identifier: AGPL-3.0-only

using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Mods.VivHelper.Runtime.Bumpers.Launching;

namespace Celeste.Mods.VivHelper.Runtime.Bumpers
{
    public sealed class BumperWrapperContext
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, BumperWrapperContext> Owners = new();

        public static BumperWrapperContext For(ISourceEngineContext engine) =>
            Owners.GetValue(engine, static _ => new BumperWrapperContext());

        public bool DisableFreeze;
        public bool DetectFreeze;
        public BumperLaunchKind WrapperType;
        public PackedColor? EntityDebugColor;

        internal void Reset()
        {
            DisableFreeze = false;
            DetectFreeze = false;
            WrapperType = BumperLaunchKind.IgnoreAll;
            EntityDebugColor = null;
        }
    }
}
