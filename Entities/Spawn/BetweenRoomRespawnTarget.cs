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

[CustomEntity("VivHelper/InterRoomSpawnTarget = Load", "VivHelper/InterRoomSpawnTarget2 = Load")]
[RuntimeTypeAlias("VivHelper.Entities.BetweenRoomRespawnTarget")]
public class BetweenRoomRespawnTarget : SpawnPoint
{
    public int tag;

    public static BetweenRoomRespawnTarget Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
        Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

    public BetweenRoomRespawnTarget(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data,
        offset) => tag = data.Int("tag", 0);
}
