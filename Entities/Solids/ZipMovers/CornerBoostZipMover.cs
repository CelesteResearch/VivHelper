using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Game.Entities.Platforms.ZipMovers.Motion;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Core.Display;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Timing;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Audio.Sources;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Controls.Source;

namespace Celeste.Mods.VivHelper.Entities.Solids.ZipMovers
{
    [CustomEntity("VivHelper/CornerBoostZipMover")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.CornerBoostZipMover")]
    public partial class CornerBoostZipMover : CornerBoostSolid
    {
        public enum Themes
        {
            Normal,
            Moon
        }

        private Themes theme;

        private AtlasEntry[,] edges = new AtlasEntry[3, 3];

        private Sprite streetlight;

        private BloomPointState bloom;

        private ZipMoverPathRenderer pathRenderer;

        private List<AtlasEntry> innerCogs;

        private AtlasEntry temp = new AtlasEntry();

        private bool drawBlackBorder;

        private Vector2 start;

        private Vector2 target;

        private float percent;

        private static readonly PackedColor ropeColor = new PackedColor(102, 57, 49);

        private static readonly PackedColor ropeLightColor = new PackedColor(155, 97, 87);

        private readonly ISourceEngineContext engine;
        private readonly CornerBoostZipMoverParticleTypes particleTypes;

        private SoundSourceComponentState sfx;
        protected Sprite SourceStreetlight => streetlight;

        public CornerBoostZipMover(ISourceEngineContext engine, Vector2 position, int width, int height,
            Vector2 target,
            Themes theme, bool perfectCB, ISourceSolidWorld world)
            : base(position, width, height, false, perfectCB, world)
        {
            this.engine = engine;
            particleTypes = CornerBoostZipMoverParticleTypes.ForEngine(engine);
            sfx = new SoundSourceComponentState(engine);
            base.Depth = -9999;
            start = Position;
            this.target = target;
            this.theme = theme;
            OwnedComponents.Add(new SimulationCoroutine(engine, Sequence()));
            OwnedComponents.Add(new LightOccludeState());
            string path;
            string id;
            string key;
            if (theme == Themes.Moon)
            {
                path = "objects/zipmover/moon/light";
                id = "objects/zipmover/moon/block";
                key = "objects/zipmover/moon/innercog";
                drawBlackBorder = false;
            }
            else
            {
                path = "objects/zipmover/light";
                id = "objects/zipmover/block";
                key = "objects/zipmover/innercog";
                drawBlackBorder = true;
            }

            innerCogs = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtextures(key);
            OwnedComponents.Add(streetlight =
                new Sprite(engine, ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas, path));
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames");
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position = new Vector2(base.Width / 2f - streetlight.Width / 2f, 0f);
            OwnedComponents.Add(bloom = new BloomPointState(1f, 6f));
            bloom.Position = new Vector2(base.Width / 2f, 4f);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    edges[i, j] = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[id]
                        .GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }

            SurfaceSoundIndex = 7;
            sfx.Position =
                ((ISourceVectorEnvironment)engine).VectorArithmetic.Divide(new Vector2(base.Width, base.Height), 2f);
            OwnedComponents.Add(sfx);
        }

        public CornerBoostZipMover(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorld world)
            : this(engine, SourceVector2Math.Add(data.Position, offset), data.Width, data.Height,
                SourceVector2Math.Add(data.Nodes[0], offset),
                EntityAttributeReader.Enum(data.Values, "theme", Themes.Normal),
                EntityAttributeReader.Bool(data.Values, "PerfectCornerBoost", false), world)
        {
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            return new CornerBoostZipMover(level.EngineContext, data.Position + offset, data.Width, data.Height,
                data.Nodes[0] + offset, data.Enum("theme", Themes.Normal),
                data.Bool("PerfectCornerBoost", false), level.SolidWorldFactory.Create(data.Position + offset, false));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ((ISourceScene)scene).SourceScene.QueueSourceEntity(pathRenderer = new ZipMoverPathRenderer(this));
        }

        public override void Removed(object scene)
        {
            ((ISourceScene)scene).SourceScene.RemoveEntity(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        public override void Update()
        {
            base.Update();
            bloom.Y = streetlight.CurrentAnimationFrame * 3;
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 position = Position;
            Position = SourceVector2Math.Add(Position, base.Shake);
            ZipMoverDrawing.Body(context, this, percent, innerCogs, edges, temp, 1f);
            base.Render(context);
            Position = position;
        }
    }
}
