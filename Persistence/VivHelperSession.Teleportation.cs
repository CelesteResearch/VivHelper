// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using YamlDotNet.Serialization;

namespace Celeste.Mods.VivHelper.Persistence
{
    public sealed partial class VivHelperSession
    {
        [YamlIgnore] public Action<RoomPlayerSession, OriginalLevelSceneRoot> TeleportAction;
        public bool StallScreenWipe;
        public bool TeleportState { get; set; }
    }
}
