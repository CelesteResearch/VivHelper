using Celeste.Core.Audio;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;

namespace Celeste.Mods.VivHelper.Runtime.Audio.Changes
{
    [RuntimeTypeAlias("VivHelper.Event")]
    public class AudioEvent
    {
        public string Name;
        public AudioParam[] Params;

        public AudioEvent(string name, params AudioParam[] parameters)
        {
            Name = name;
            Params = parameters;
        }

        public void SetParamsToEvent(ISourceAudioEventReference instance, ISourceEngineContext engine)
        {
            if (Params == null) return;
            var level = engine.Frame.Scenes.CurrentScene as OriginalLevelSceneRoot;
            foreach (AudioParam parameter in Params)
            {
                var session = level?.Session;
                if (session != null && !string.IsNullOrWhiteSpace(parameter.Flag) &&
                    session.GetFlag(parameter.Flag) != parameter.FlagInvert)
                    instance.SetParameter(parameter.Name, parameter.IfFlag);
                else if (parameter.Normal.HasValue)
                    instance.SetParameter(parameter.Name, parameter.Normal.Value);
            }
        }
    }
}
