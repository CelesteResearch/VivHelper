// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Triggers.Camera;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Mods.VivHelper.Runtime.RoomWrapping;

namespace Celeste.Mods.VivHelper.Triggers.Camera
{
    [EverestTrackedAs(typeof(SourceTriggerEntityState))]
    [CustomEntity("VivHelper/ReenableableCameraTargetTrigger")]
    [RuntimeTypeAlias("VivHelper.Triggers.MultiflagCameraTargetTrigger")]
    public class MultiflagCameraTargetTrigger : OriginalCameraTargetTriggerState
    {
        public string[] flags;
        private OriginalLevelSceneRoot level;

        public static MultiflagCameraTargetTrigger Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, EntityData data) => new(data, offset, level.EngineContext);

        public MultiflagCameraTargetTrigger(EntityData data, Vector2 offset, ISourceEngineContext engine,
            string[] flagArray = null)
            : base(engine, data, offset)
        {
            flags = flagArray ?? (data.Attr("ComplexFlagData", "") == ""
                ? new[] { data.Attr("SingleFlag", "") }
                : data.Attr("ComplexFlagData", "").Split(','));
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            level = (OriginalLevelSceneRoot)Scene;
        }

        public override void OnStay(RoomPlayerSession player)
        {
            if (RoomWrapFlags.All(level, flags)) base.OnStay(player);
        }
    }
}
