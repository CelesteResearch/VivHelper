using Celeste.Everest.Content.Maps.Binary;
using System;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Content.Maps;
using Celeste.Mods.VivHelper.Backdrops.Weather.Rain;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.VivHelper.Runtime.Backdrops.Rain
{
    public sealed class CustomRainRegistration : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly EverestLevelUpdateEvents events;

        public CustomRainRegistration(ISourceEngineContext engine)
        {
            this.engine = engine;
            events = ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents;
            events.OnLoadBackdrop += Load;
        }

        private BackdropPresentation Load(OriginalChapterMapData map, BinaryPackerElement child,
            BinaryPackerElement above) =>
            child.Name.Equals("VivHelper/CustomRain", StringComparison.OrdinalIgnoreCase)
                ? new CustomRain(engine, new Vector2(child.AttrFloat("Scrollx", 0f), child.AttrFloat("Scrolly", 0f)),
                    child.AttrFloat("angle", 270f), child.AttrFloat("angleDiff", 3f), child.AttrFloat("speedMult", 1f),
                    child.AttrInt("Amount", 240), child.Attr("colors", "161933"), child.AttrFloat("alpha", 0f))
                : null;

        public void Dispose() => events.OnLoadBackdrop -= Load;
    }
}
