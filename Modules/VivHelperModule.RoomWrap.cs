// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Mods.VivHelper.Runtime.RoomWrapping;

namespace Celeste.Mods.VivHelper.Modules
{
    public partial class VivHelperModule
    {
        private void LoadRoomWrap()
        {
            var context = RoomWrapCameraContext.For(SourceEngine);
            hooks.Add(context.Bind(this));
            hooks.Add(new RoomWrapCameraProcess(context));
        }
    }
}
