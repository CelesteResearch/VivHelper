using Celeste.Core.Reflection.Runtime;
using Celeste.Formats.Map.Entities.Structure;

namespace Celeste.Mods.VivHelper.Runtime.Audio.Changes
{
    [RuntimeTypeAlias("VivHelper.SoundChange")]
    public abstract class SoundChange
    {
        protected static AudioEvent noneEvent = new("event:/none", null);
        protected static AudioEvent defaultEvent = new("default", null);
        public abstract AudioEvent GrabEvent();
        public abstract void AddOrChangeFromEntityData(EntityData data);
    }
}
