using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Persistence
{
    [RuntimeTypeAlias("VivHelper.VivHelperModuleSettings+ColorRefillType")]
    public enum ColorRefillType
    {
        Normal,
        Decreased,
        Minimal,
        None
    }
}
