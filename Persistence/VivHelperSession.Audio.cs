using System.Collections.Generic;
using Celeste.Mods.VivHelper.Runtime.Audio.Changes;
using YamlDotNet.Serialization;

namespace Celeste.Mods.VivHelper.Persistence
{
    public sealed partial class VivHelperSession
    {
        [YamlIgnore] public Dictionary<string, SoundChange> AudioChanges = new();
    }
}
