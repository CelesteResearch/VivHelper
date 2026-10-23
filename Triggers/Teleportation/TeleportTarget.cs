// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Game.Execution.Startup;

namespace Celeste.Mods.VivHelper.Triggers.Teleportation
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Triggers.TeleportTarget")]
    [CustomEntity("VivHelper/TeleportTarget = Load")]
    public partial class TeleportTarget : SourceTriggerEntityState
    {
        internal static List<float> defaultWH = new() { 8f };
        public static string[] ValidTeleportTargetNames = { "VivHelper/TeleportTarget", "VivHelper/ITPT2Way" };
        protected readonly bool rotateToAngle;
        protected readonly bool rotateBefore;
        protected readonly float? rotVal;
        protected readonly SpeedDefiner speedChange;
        private readonly ISourceVectorArithmetic arithmetic;
        public bool modifyVelocity;
        public string targetID;
        public string roomName;
        public bool addTriggerOffset;
        public List<float> DebugWidths;
        public List<float> DebugHeights;
        public Vector2? forceSpawnpoint;
        public string NewRoom => (Scene as OriginalLevelSceneRoot)?.Session?.Level;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, OriginalLevelEntityData entityData)
        {
            List<float> widths = null, heights = null;
            if (entityData.Bool("AddTriggerOffset", false))
            {
                var map = level?.Session?.MapData as OriginalChapterMapData;
                if (map != null)
                    foreach (var room in map.Levels)
                    foreach (var entity in room.Entities)
                        if (entity.Name == "VivHelper/ITPT1Way" &&
                            entity.Attr("TargetID", "") == entityData.Attr("TargetID", null))
                        {
                            if (widths == null) widths = new List<float>(entity.Width);
                            else widths.Add(entity.Width);
                            if (heights == null) heights = new List<float>(entity.Height);
                            else heights.Add(entity.Height);
                        }
            }

            return new TeleportTarget(level.EngineContext, entityData, offset, widths ?? defaultWH,
                heights ?? defaultWH);
        }

        public TeleportTarget(ISourceEngineContext engine, OriginalLevelEntityData data, Vector2 offset,
            List<float> visualWidth, List<float> visualHeight)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            arithmetic = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            DebugWidths = visualWidth;
            DebugHeights = visualHeight;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.FrozenUpdate.Value;
            roomName = data.Level.Name;
            targetID = ReadNonEmpty(data, "TargetID", "hello");
            addTriggerOffset = data.Bool("AddTriggerOffset", false);
            try
            {
                forceSpawnpoint = data.Nodes != null ? data.Nodes[0] + offset : (Vector2?)null;
            }
            catch
            {
                forceSpawnpoint = null;
            }

            if (data.Has("RotationValue"))
            {
                rotateBefore = data.Bool("RotateBeforeSpeedChange", false);
                rotateToAngle = data.Bool("RotateToAngle", false);
                rotVal = data.Float("RotationValue", 0f);
            }

            if (data.Has("SpeedModifier"))
            {
                var modifier = data.Enum("SpeedModifier", SpeedModifier.NoChange);
                float fallback = modifier == SpeedModifier.Multiply ? 1f : 0f;
                speedChange = data.Has("SpeedChangeStrength")
                    ? new SpeedDefiner(arithmetic, modifier, data.Float("SpeedChangeStrength", fallback))
                    : new SpeedDefiner(arithmetic, modifier, data.Float("SpeedChangeX", fallback),
                        data.Float("SpeedChangeY", fallback));
            }
            else speedChange = new SpeedDefiner(arithmetic, SpeedModifier.NoChange, 0f);
        }

        internal static string ReadNonEmpty(EntityData data, string key, string fallback = null)
        {
            string value = data.Attr(key, fallback);
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public Vector2 ModifyVelocity(Vector2 input)
        {
            Vector2 value = input;
            if (rotateBefore && rotVal.HasValue)
                value = rotateToAngle
                    ? arithmetic.RotateTowards(value, rotVal.Value, 6.3f)
                    : arithmetic.Rotate(value, rotVal.Value);
            value = speedChange.ModifySpeed(value);
            if (!rotateBefore && rotVal.HasValue)
                value = rotateToAngle
                    ? arithmetic.RotateTowards(value, rotVal.Value, 6.3f)
                    : arithmetic.Rotate(value, rotVal.Value);
            return value;
        }
    }
}
