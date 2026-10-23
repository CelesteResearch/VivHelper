// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Rendering;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Graphics.Rendering;

namespace Celeste.Mods.VivHelper.Entities.Spawn;

[CustomEntity("VivHelper/InterRoomSpawner")]
[RuntimeTypeAlias("VivHelper.Entities.BetweenRoomRespawn")]
public class BetweenRoomRespawn : SpawnPoint
{
    private readonly ISourceEngineContext engine;
    public int tag;

    public BetweenRoomRespawn(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data, offset)
    {
        this.engine = engine;
        tag = data.Int("tag", 0);
    }

    public override void DebugRender(ISourceRenderContext context, object actualCamera)
    {
        var level = (OriginalLevelSceneRoot)Scene;
        SourcePrimitiveDrawing.Circle(context, this, Position - Vector2.UnitY,
            5f - level.Scheduler.Clock.TimeActive * 5f % 5f, PackedColor.LightPink, 8,
            ((ISourceVectorEnvironment)engine).VectorArithmetic);
    }
}
