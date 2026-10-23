// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Mods.VivHelper.Runtime.Teleportation;
using Celeste.Mods.VivHelper.Triggers;

namespace Celeste.Mods.VivHelper.Modules
{
    public sealed partial class VivHelperModule
    {
        private void LoadInstantTeleport()
        {
            var context = VivTeleportContext.For(SourceEngine);
            hooks.Add(context.Bind(this, VivHelperSpinnerCapabilities.GetDashStatesForOwner(SourceEngine),
                InstantTeleportTrigger.GetDashStatesForOwner(SourceEngine)));
            hooks.Add(new VivTeleportHooks(context));
        }
    }
}
