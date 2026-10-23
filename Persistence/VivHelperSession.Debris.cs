using YamlDotNet.Serialization;

namespace Celeste.Mods.VivHelper.Persistence
{
    public sealed partial class VivHelperSession
    {
        [YamlMember(Alias = "DebrisLimiter")] public float DebrisLimiter { get; set; } = 0f;
    }
}
