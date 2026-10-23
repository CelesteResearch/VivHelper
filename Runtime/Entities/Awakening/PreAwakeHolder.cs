using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Runtime.Entities.Awakening
{
    [RuntimeTypeAlias("VivHelper.PreAwakeHolder")]
    public interface PreAwakeHolder
    {
        void PreAwake(object scene);
    }
}
