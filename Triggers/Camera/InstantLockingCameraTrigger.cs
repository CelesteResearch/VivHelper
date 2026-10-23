// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Runtime.RoomWrapping;
using Celeste.Mods.VivHelper.Triggers.Teleportation;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.VivHelper.Triggers.Camera
{
    [CustomEntity("VivHelper/InstantCatchupTrigger")]
    [RuntimeTypeAlias("VivHelper.Triggers.InstantLockingCameraTrigger")]
    public class InstantLockingCameraTrigger : SourceTriggerEntityState
    {
        private readonly RoomWrapCameraContext context;
        public TriggerPersistence Persistence;
        public bool State;
        public MapEntityId id;
        public int prevValue;
        public bool resetOnLeave;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new InstantLockingCameraTrigger(level.EngineContext, data, offset,
            new MapEntityId(room.Name, data.ID));

        public InstantLockingCameraTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset,
            MapEntityId eid)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            context = RoomWrapCameraContext.For(engine);
            id = eid;
            Persistence = data.Enum("persistence", TriggerPersistence.Default);
            resetOnLeave = data.Bool("resetOnLeave", true);
            State = data.Bool("state", false);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            if (resetOnLeave) prevValue = context.Session.LockCamera;
            context.Session.LockCamera = State ? -1 : 0;
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            if (resetOnLeave) context.Session.LockCamera = prevValue;
            switch (Persistence)
            {
                case TriggerPersistence.OncePerRetry:
                    this.RemoveSelf();
                    break;
                case TriggerPersistence.OncePerMapPlay:
                    ((OriginalLevelSceneRoot)Scene).Session.DoNotLoad.Add(id);
                    this.RemoveSelf();
                    break;
            }
        }
    }
}
