// SPDX-License-Identifier: AGPL-3.0-only

using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.VivHelper.Runtime.DashBlocks
{
    public sealed class CustomDashBlockPlayerStates
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, CustomDashBlockPlayerStates> States = new();
        private int orange = -1;
        private int windBoost = -1;
        private int pink = -1;
        internal int Orange => orange;
        internal int Wind => windBoost;
        internal int Pink => pink;
        public static CustomDashBlockPlayerStates For(ISourceEngineContext engine) => States.GetOrCreateValue(engine);
        public void RegisterOrange(int state) => orange = state;
        public void RegisterWindBoost(int state) => windBoost = state;
        public void RegisterPink(int state) => pink = state;
        public bool IsBoost(int state) => state == orange || state == windBoost || state == pink;
    }
}
