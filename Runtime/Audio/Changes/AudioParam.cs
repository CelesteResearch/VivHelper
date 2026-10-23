using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Runtime.Audio.Changes
{
    [RuntimeTypeAlias("VivHelper.AudioParam")]
    public struct AudioParam
    {
        public string Name;
        public string Flag;
        public bool FlagInvert;
        public float IfFlag;
        public float? Normal;
    }
}
