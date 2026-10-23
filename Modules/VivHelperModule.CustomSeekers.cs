// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Mods.VivHelper.Entities.Enemies.Seekers;
using Celeste.Mods.VivHelper.Runtime.Enemies.Seekers;

namespace Celeste.Mods.VivHelper.Modules
{
    public sealed partial class VivHelperModule
    {
        private void LoadCustomSeekers()
        {
            hooks.Add(SourceEngine.EntityPools.RegisterScoped(() => new CustomSeekerRecoverBlast(SourceEngine)));
            hooks.Add(CustomSeekerEffectsHooks.Install(SourceEngine));
        }
    }
}
