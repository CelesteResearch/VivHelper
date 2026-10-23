// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Scene.Tracking;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Mods.VivHelper.Runtime.HoldableBarriers;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.VivHelper.Entities.HoldableBarriers
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.HoldableBarrierColorController")]
    [CustomEntity("VivHelper/HoldableBarrierController = OldLoad", "VivHelper/HoldableBarrierController2 = NewLoad")]
    public class HoldableBarrierColorController : SceneEntityState
    {
        public PackedColor ParticleColor = SpawnPointRules.ParseColor("5a6ee1");
        public PackedColor BaseColor = SpawnPointRules.ParseColor("5a6ee1");
        public Vector2 ParticleDirection = Vector2.UnitY;
        public bool SolidOnRelease = true;
        public bool SaveToSession;
        public bool ToggleBloomRendering = true;
        public int Version;
        private readonly HoldableBarrierContext context;

        public static HoldableBarrierColorController OldLoad(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new(((ISourceEngineContextScene)level).EngineContext, data, offset, 0);

        public static HoldableBarrierColorController NewLoad(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new(((ISourceEngineContextScene)level).EngineContext, data, offset, 1);

        public HoldableBarrierColorController(ISourceEngineContext engine, EntityData data, Vector2 offset, int version)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            context = HoldableBarrierContext.For(engine);
            // The original controller remains at (0,0), and does not assign its Version field.
            ParticleColor = SpawnPointRules.ParseColor(data.Attr("ParticleColor", "5a6ee1"));
            BaseColor = SpawnPointRules.ParseColor(data.Attr("EdgeColor", "5a6ee1"));
            ParticleDirection = ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitX,
                -AngleVersion(data.Float("ParticleAngle", version == 1 ? 270f : 4.712389f), version));
            SolidOnRelease = data.Bool("SolidOnRelease", true);
            SaveToSession = data.Bool("Persistent", false);
            ToggleBloomRendering = data.Bool("renderBloom", true);
        }

        internal static float AngleVersion(float value, int version) => version switch
        {
            0 => value,
            1 => value * 0.0174533f,
            _ => throw new Exception(
                "Invalid version type. This should never appear, and if you see it DM or ping in Celestecord @Viv#1113 on Discord, and send this error log with it.")
        };

        public override void Added(object scene)
        {
            base.Added(scene);
            if (SaveToSession) context.Save((OriginalLevelSceneRoot)scene, this);
        }
    }
}
