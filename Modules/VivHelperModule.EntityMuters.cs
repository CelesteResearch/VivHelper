using Celeste.Mods.VivHelper.Runtime.Audio.Muting;
using Celeste.Mods.VivHelper.Runtime.Entities.Awakening;

namespace Celeste.Mods.VivHelper.Modules
{
    public sealed partial class VivHelperModule
    {
        private void LoadEntityMuters()
        {
            var awakening = new EntityChangingInterfaces(SourceEngine);
            try
            {
                var audio = new EntityMuterHooks(EntityMuterContext.For(SourceEngine));
                hooks.Add(awakening);
                hooks.Add(audio);
            }
            catch
            {
                awakening.Dispose();
                throw;
            }
        }
    }
}
