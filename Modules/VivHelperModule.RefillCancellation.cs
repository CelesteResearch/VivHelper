using System;
using Celeste.Mods.VivHelper.Persistence;

namespace Celeste.Mods.VivHelper.Modules
{
    public sealed partial class VivHelperModule
    {
        public override Type SettingsType => typeof(VivHelperSettings);
        public override Type SessionType => typeof(VivHelperSession);
        public VivHelperSettings Settings => (VivHelperSettings)_Settings;
        public VivHelperSession Session => (VivHelperSession)_Session;
    }
}
