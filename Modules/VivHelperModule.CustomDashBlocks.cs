// SPDX-License-Identifier: AGPL-3.0-only

namespace Celeste.Mods.VivHelper.Modules
{
    public sealed partial class VivHelperModule
    {
        private void LoadCustomDashBlockHooks()
        {
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.Boosters.VivBoosterStateProcess(SourceEngine,
                () => ((global::Celeste.Mods.VivHelper.Persistence.VivHelperSession)_Session).OrangeSpeed,
                value => ((global::Celeste.Mods.VivHelper.Persistence.VivHelperSession)_Session).OrangeSpeed = value));
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.DashBlocks.CustomDashBlockHooks(SourceEngine));
        }
    }
}
