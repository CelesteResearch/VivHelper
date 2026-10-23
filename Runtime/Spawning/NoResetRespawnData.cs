// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Runtime.Spawning;

[RuntimeTypeAlias("VivHelper.Entities.NoResetRespawnData")]
public struct NoResetRespawnData
{
    public bool NoResetOnRetry;
    public string Flag;
    public override string ToString() => Flag + "}";
}
