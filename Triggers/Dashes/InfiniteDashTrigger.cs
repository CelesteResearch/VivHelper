// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.VivHelper.Triggers.Dashes
{
    [CustomEntity("VivHelper/InfDashTrigger")]
    [RuntimeTypeAlias("VivHelper.Triggers.InfiniteDashTrigger")]
    public class InfiniteDashTrigger : SourceTriggerEntityState
    {
        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new InfiniteDashTrigger(level.EngineContext, data, offset);

        public InfiniteDashTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            player.Data.Dashes = 2;
        }

        public override void OnStay(RoomPlayerSession player)
        {
            base.OnStay(player);
            player.Data.Dashes = 2;
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            player.Data.Dashes = player.Inventory.Dashes;
        }
    }
}
