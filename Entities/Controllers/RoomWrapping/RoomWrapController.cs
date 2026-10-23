// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Mods.VivHelper.Runtime.RoomWrapping;

namespace Celeste.Mods.VivHelper.Entities.Controllers.RoomWrapping
{
    [SceneTracked(false)]
    [CustomEntity("VivHelper/RoomWrapController")]
    [RuntimeTypeAlias("VivHelper.Entities.RoomWrapController")]
    public partial class RoomWrapController : SceneEntityState
    {
        public bool scrollT, scrollR, scrollB, scrollL, allEntities, setByCamera;
        public float[] playerOffsets;
        public OriginalLevelSceneRoot level;
        public Vector2[] a_ES;
        public static float m_ES = 2400f;
        public string[] flag;
        public string flagstring;
        public bool automateCTT, lockCamera;
        private readonly ISourceEngineContext engine;
        private readonly RoomWrapCameraContext cameraContext;

        public static RoomWrapController Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, EntityData data) => new(data, offset, level.EngineContext);

        public RoomWrapController(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            cameraContext = RoomWrapCameraContext.For(engine);
            playerOffsets = new float[4];
            a_ES = new Vector2[4];
            scrollT = data.Bool("Top", false);
            flagstring = data.Attr("ZFlagsData", "");
            flag = data.Attr("ZFlagsData", "").Split(',');
            playerOffsets[0] = data.Float("TopOffset", 4f);
            if (scrollT)
                a_ES[0] = new(0f,
                    Math.Abs(data.Float("TopExitSpeedAdd", 0f)) < m_ES
                        ? Math.Abs(data.Float("TopExitSpeedAdd", 0f))
                        : m_ES);
            scrollR = data.Bool("Right", false);
            playerOffsets[1] = -data.Float("RightOffset", 5f);
            if (scrollR)
                a_ES[1] = new(
                    Math.Abs(data.Float("RightExitSpeedAdd", 0f)) < m_ES
                        ? -Math.Abs(data.Float("RightExitSpeedAdd", 0f))
                        : -m_ES, 0f);
            scrollB = data.Bool("Bottom", false);
            playerOffsets[2] = -data.Float("BottomOffset", 8f);
            if (scrollB)
                a_ES[2] = new(0f,
                    Math.Abs(data.Float("BottomExitSpeedAdd", 0f)) < m_ES
                        ? -Math.Abs(data.Float("BottomExitSpeedAdd", 0f))
                        : -m_ES);
            scrollL = data.Bool("Left", false);
            playerOffsets[3] = data.Float("LeftOffset", 5f);
            if (scrollL)
                a_ES[3] = new(
                    Math.Abs(data.Float("LeftExitSpeedAdd", 0f)) < m_ES
                        ? Math.Abs(data.Float("LeftExitSpeedAdd", 0f))
                        : m_ES, 0f);
            setByCamera = data.Bool("setByCamera", false);
            allEntities = data.Bool("allEntities", false);
            automateCTT = data.Bool("AutomateCameraTriggers", false);
            lockCamera = data.Bool("LockCameraOnTeleport", false);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            level = (OriginalLevelSceneRoot)Scene;
            if (automateCTT) AutomateCTT(level.Bounds, flag);
        }
    }
}
