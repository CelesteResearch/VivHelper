// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Entities.FallingBlocks;
using Celeste.Mods.VivHelper.Runtime.FallingBlocks;
using Celeste.Simulation.Gameplay.Platforms.Collision;

namespace Celeste.Mods.VivHelper.Modules;

public partial class VivHelperModule
{
    private void LoadFallingBlocks()
    {
        hooks.Add(new CustomFallingBlockProcess(SourceEngine));
        ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadEntity += LoadFallingBlock;
    }

    private void UnloadFallingBlocks() =>
        ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadEntity -= LoadFallingBlock;

    private bool LoadFallingBlock(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
        OriginalLevelEntityData data)
    {
        if (data.Name != "VivHelper/CustomFallingBlock") return false;
        var keys = ((IOriginalAreaKeyEnvironment)SourceEngine).AreaKeys;
        string sid = keys.GetSID(keys.GetSessionArea(level.Session));
        if (sid is "Kaerra/DELTASCENEONE/DeltaSceneOne" or "ValentinesContest2021/1-Submissions/KAERRA"
            or "WinterCollab2021/1-Maps/DanTKO" or "WinterCollab2021/1-Maps/DanTKO_Viv")
            throw new NotSupportedException("VivHelper CustomFallingBlock_140 is not ported for this legacy map.");
        level.Scheduler.QueueSourceEntity(new CustomFallingBlock(data, offset, SourceEngine,
            ((ISourcePlatformCollisionScene)level).SolidWorldFactory));
        return true;
    }
}
