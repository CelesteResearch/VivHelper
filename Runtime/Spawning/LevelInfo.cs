// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Runtime.Spawning;

[RuntimeTypeAlias("VivHelper.Entities.LevelInfo")]
public class LevelInfo
{
    public Dictionary<Vector2, NoResetRespawnData> NoResetRespawns;
    public Dictionary<Vector2, InterRoomRespawnData> InterRoomRespawnSet;

    public LevelInfo()
    {
        NoResetRespawns = new();
        InterRoomRespawnSet = new();
    }

    public LevelInfo(string levelName) : this()
    {
    }

    public LevelInfo(LevelInfo original)
    {
        NoResetRespawns = new(original.NoResetRespawns);
        InterRoomRespawnSet = new(original.InterRoomRespawnSet);
    }

    public override string ToString() => "NoResetRespawns:  " + string.Join(Environment.NewLine, NoResetRespawns) +
                                         "\nInterRoomRespawnSet:  " +
                                         string.Join(Environment.NewLine, InterRoomRespawnSet);
}
