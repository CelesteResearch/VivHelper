// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using System.Collections.Generic;
using Celeste.Core.Maps;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Events.LevelLoading;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Save.Runtime;
using Celeste.Simulation.Runtime.Session;

namespace Celeste.Mods.VivHelper.Runtime.Collectibles;

public sealed class PreviousBerriesToFlagProcess : IDisposable
{
    public const string Marker = "VivHelper/PreviousBerriesToFlag";
    private readonly ISourceEngineContext engine;

    public PreviousBerriesToFlagProcess(ISourceEngineContext engine)
    {
        this.engine = engine;
        EverestLevelLoaderSourceEvents.OnLoadingThread += Loading;
        ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents.OnLoadEntity += ConsumeMarker;
    }

    private bool ConsumeMarker(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
        OriginalLevelEntityData data) => ReferenceEquals(level.EngineContext, engine) && data.Name == Marker;

    private void Loading(OriginalLevelSceneRoot level)
    {
        if (!ReferenceEquals(level.EngineContext, engine)) return;
        var rooms = ((OriginalChapterMapData)level.Session?.MapData)?.Levels;
        if (rooms == null) return;
        foreach (var room in rooms)
        {
            if (room.Entities == null) continue;
            foreach (var data in room.Entities)
            {
                if (data.Name != Marker) continue;
                var keys = ((IOriginalAreaKeyEnvironment)engine).AreaKeys;
                var area = keys.GetSessionArea(level.Session);
                var stats = ((IOriginalSaveCounterEnvironment)engine).SaveCounters.GetAreasSafe(
                    ((IOriginalGameplayProcess)engine).SaveContext.CurrentSave)[area.ID].Modes[area.Mode];
                var mode = keys.GetArea(area).Mode[area.Mode];
                Apply(level.Session, mode, stats.Strawberries);
            }
        }
    }

    internal static void Apply(ChapterCompletionSession session, OriginalAreaModeData mode,
        HashSet<MapEntityId> collected)
    {
        if (mode.TotalStrawberries <= 0) return;
        int checkpoints = mode.Checkpoints == null ? 1 : mode.Checkpoints.Length + 1;
        for (int checkpoint = 0; checkpoint < checkpoints; checkpoint++)
        {
            int berries = checkpoint == 0 ? mode.StartStrawberries : mode.Checkpoints[checkpoint - 1].Strawberries;
            for (int berry = 0; berry < berries; berry++)
            {
                var data = mode.StrawberriesByCheckpoint[checkpoint, berry];
                if (data == null) continue;
                foreach (var previous in collected)
                    if (data.ID == previous.ID && data.Level.Name == previous.Level)
                        session.SetFlag($"VivHelper_PreviousCollectedBerries_{checkpoint}:{berry}", true);
            }
        }
    }

    public void Dispose()
    {
        EverestLevelLoaderSourceEvents.OnLoadingThread -= Loading;
        ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents.OnLoadEntity -= ConsumeMarker;
    }
}
