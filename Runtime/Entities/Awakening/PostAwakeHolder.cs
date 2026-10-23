using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Runtime.Entities.Awakening
{
    [RuntimeTypeAlias("VivHelper.PostAwakeHolder")]
    public interface PostAwakeHolder
    {
        void PostAwake(object scene);
    }
}
