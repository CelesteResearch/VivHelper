// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Environment.Debris;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Mods.VivHelper.Runtime.Lighting;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.VivHelper.Entities.Lighting
{
    [CustomEntity("VivHelper/FloatyLight")]
    [EverestTrackedAs(typeof(OriginalFloatingDebrisState))]
    [RuntimeTypeAlias("VivHelper.Entities.FloatyFluorescentLight")]
    public sealed class FloatyFluorescentLight : OriginalFloatingDebrisState
    {
        private static readonly int[] RotationChoices = { -6, -4, -3, -2, -1, 1, 2, 3, 4, 6 };
        private readonly IOriginalFloatingDebrisProcess engine;
        private readonly FloatyLightContacts contacts;
        private readonly VertexLightState light;
        private readonly BloomPointState point;
        private readonly bool broken;

        public static FloatyFluorescentLight Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new((IOriginalFloatingDebrisProcess)level.EngineContext, data, offset);

        public FloatyFluorescentLight(IOriginalFloatingDebrisProcess engine, EntityData data, Vector2 offset)
            : base(data, offset, engine)
        {
            this.engine = engine;
            contacts = FloatyLightContacts.For(engine);
            Depth = -12000;
            Remove(Get<SpriteImage>());
            broken = data.Bool("broken");
            var texture = !broken ? engine.GameGraphics.GameAtlas["VivHelper/fluorescentLight/full"]
                : engine.Random.Next(0, 2) != 0 ? engine.GameGraphics.GameAtlas["VivHelper/fluorescentLight/half"]
                : engine.GameGraphics.GameAtlas["VivHelper/fluorescentLight/min"];
            DebrisImage = new SpriteImage(texture);
            DebrisImage.CenterOrigin();
            Add(DebrisImage);
            Add(light = new VertexLightState(engine, Vector2.Zero, PackedColor.White, 0f, 48, 64));
            Add(point = new BloomPointState(0f, 36f));
            RotationSpeed = (float)(RandomSelection.Choose(engine.Random, RotationChoices) * 10) *
                            ((float)Math.PI / 180f);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            float rotation = RotationSpeed * engine.Random.Next(2, 13) * engine.DeltaTime;
            DebrisImage.Rotation = rotation;
            bool nearby = contacts.Check(((ISourceScene)Scene).SourceScene.Entities, Position,
                Vector2Geometry.AngleToVector(rotation, 13f), broken);
            light.Alpha = nearby ? 1f : 0f;
            point.Alpha = nearby ? .7f : 0f;
        }

        public override void Update()
        {
            base.Update();
            bool nearby = contacts.Check(((ISourceScene)Scene).SourceScene.Entities, Position,
                Vector2Geometry.AngleToVector(DebrisImage.Rotation, 13f), broken);
            var arithmetic = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic;
            light.Alpha = ScalarMath.Approach(light.Alpha, nearby ? 1f : 0f, engine.DeltaTime * .6666f, arithmetic);
            point.Alpha = ScalarMath.Approach(point.Alpha, nearby ? .7f : 0f, engine.DeltaTime * .5f, arithmetic);
        }

        public override void DebugRender(ISourceRenderContext context, object actualCamera)
        {
            Vector2 tip = Vector2Geometry.AngleToVector(DebrisImage.Rotation, 12f);
            var pixel = ((ISourceDrawScope)context).Draw.Pixel;
            SourceAtlasDrawing.Draw(context, this, pixel, Position + tip, Vector2.One / 2f, PackedColor.Orange, 2f);
            SourceAtlasDrawing.Draw(context, this, pixel, Position - tip, Vector2.One / 2f, PackedColor.Orange, 2f);
        }
    }
}
