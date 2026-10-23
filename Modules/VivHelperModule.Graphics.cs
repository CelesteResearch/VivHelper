using Celeste.Mods.VivHelper.Runtime.Graphics;

namespace Celeste.Mods.VivHelper.Modules
{
    public sealed partial class VivHelperModule
    {
        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);
            VivHelperGraphicsResources.For(SourceEngine).LoadContent();
            bumperWrappers?.LoadContent();
        }
    }
}
