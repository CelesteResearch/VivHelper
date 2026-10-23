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
    public sealed class WindRainRegistration : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly EverestLevelUpdateEvents events;

        public WindRainRegistration(ISourceEngineContext engine)
        {
            this.engine = engine;
            events = ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents;
            events.OnLoadBackdrop += Load;
        }

        private BackdropPresentation Load(OriginalChapterMapData map, BinaryPackerElement child,
            BinaryPackerElement above) =>
            child.Name.Equals("VivHelper/WindRainFG", StringComparison.OrdinalIgnoreCase)
                ? new WindRainFG(engine, new Vector2(child.AttrFloat("Scrollx", 0f), child.AttrFloat("Scrolly", 0f)),
                    child.Attr("colors", ""), child.AttrFloat("windStrength", 0f))
                : null;

        public void Dispose() => events.OnLoadBackdrop -= Load;
    }
}
