// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Generic;
using Celeste.Mods.VivHelper.Runtime.Spawning;

namespace Celeste.Mods.VivHelper.Persistence;

public sealed partial class VivHelperSession
{
    public Dictionary<string, LevelInfo> LevelInfoCache { get; set; } = new();
}
