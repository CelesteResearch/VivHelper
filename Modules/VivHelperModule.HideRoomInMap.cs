// SPDX-License-Identifier: MIT

using Celeste.Mods.VivHelper.Runtime.DebugMaps;

namespace Celeste.Mods.VivHelper.Modules
{
    public sealed partial class VivHelperModule
    {
        private void LoadHideRoomInMap() => hooks.Add(new HideRoomInMapProcess(SourceEngine));
    }
}
