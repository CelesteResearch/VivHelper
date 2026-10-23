using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Persistence;
using YamlDotNet.Serialization;

namespace Celeste.Mods.VivHelper.Persistence
{
    [RuntimeTypeAlias("VivHelper.VivHelperModuleSession")]
    public sealed partial class VivHelperSession : EverestModuleSession
    {
        [YamlMember(Alias = "dashCount")] public int DashCount { get; set; }
        [YamlMember(Alias = "staminaCount")] public float StaminaCount { get; set; }
        [YamlMember(Alias = "lockCamera")] public int LockCamera { get; set; }
        public bool ShowIndicator { get; set; } = true;
    }
}
