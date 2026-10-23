// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Runtime.Spawning;

[RuntimeTypeAlias("VivHelper.Entities.InterRoomRespawnData")]
public struct InterRoomRespawnData
{
    public string roomName;
    public Vector2 position;
    public string Flag;
    public override string ToString() => Flag + "}";
}
