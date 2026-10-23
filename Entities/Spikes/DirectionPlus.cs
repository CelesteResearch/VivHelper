// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Entities.Spikes
{
    [Flags]
    [RuntimeTypeAlias("VivHelper.Entities.DirectionPlus")]
    public enum DirectionPlus
    {
        Other = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        UpLeft = Up | Left,
        DownLeft = Down | Left,
        Right = 8,
        UpRight = Up | Right,
        DownRight = Down | Right
    }
}
