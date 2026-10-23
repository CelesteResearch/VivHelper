using Celeste.Mods.VivHelper.Runtime.Bumpers;

namespace Celeste.Mods.VivHelper.Modules
{
    public sealed partial class VivHelperModule
    {
        private BumperWrapperProcess bumperWrappers;

        private void LoadBumperWrappers()
        {
            bumperWrappers = new BumperWrapperProcess(SourceEngine);
            hooks.Add(bumperWrappers);
        }
    }
}
