// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Events.LevelLoading;
using Celeste.Game.Content.Maps;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Entities.HoldableBarriers;

namespace Celeste.Mods.VivHelper.Runtime.HoldableBarriers
{
    public sealed class HoldableBarrierProcess : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly HoldableBarrierContext context;
        private readonly HoldableBarrierHooks hooks;

        public HoldableBarrierProcess(ISourceEngineContext engine)
        {
            this.engine = engine;
            context = new HoldableBarrierContext(engine);
            hooks = new HoldableBarrierHooks(engine, context);
            EverestLevelLoaderSourceEvents.OnLoadingThread += Loading;
        }

        private void Loading(OriginalLevelSceneRoot level)
        {
            if (!ReferenceEquals(((ISourceEngineContextScene)level).EngineContext, engine)) return;
            foreach (var room in ((OriginalChapterMapData)level.Session.MapData).Levels)
            {
                // The installed helper resets its flag for every room, not once for the map.
                foreach (var entity in room.Entities)
                    if (entity.Name == "VivHelper/HoldableBarrier")
                    {
                        level.Scheduler.QueueSourceEntity(new HoldableBarrierRenderer(engine,
                            (ISourceRenderEnvironment)engine));
                        break;
                    }
            }
        }

        public void Dispose()
        {
            EverestLevelLoaderSourceEvents.OnLoadingThread -= Loading;
            hooks.Dispose();
            context.Dispose();
        }
    }
}
