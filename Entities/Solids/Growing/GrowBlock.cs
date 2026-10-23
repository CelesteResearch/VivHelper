// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Components.Rendering;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.VivHelper.Entities.Solids.Growing
{
    [CustomEntity("VivHelper/CelsiusGrowBlock")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.GrowBlock")]
    public partial class GrowBlock : SourceSolidEntityState
    {
        private static readonly string PlayerKey = typeof(RoomPlayerSession).FullName;
        private readonly ISourceEngineContext engine;
        private readonly ISourceVectorArithmetic vectors;
        private readonly ISourceEasingArithmetic easing;
        private readonly char tileType;
        private readonly float width, height;
        private readonly Vector2 move;
        private readonly string flag;
        private readonly int factor;
        private TileGrid tileGrid;
        private LightOccludeState lightOcclude;
        private OriginalLevelSceneRoot level;
        private float alphaA, alphaB, alphaC;
        private Vector2 offset;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) =>
            new GrowBlock(data, offset, level.SolidWorldFactory.Create(data.Position + offset, true));

        public GrowBlock(EntityData data, Vector2 offset, ISourceSolidWorld world)
            : this(data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height,
                data.Int("moveX", 0), data.Int("moveY", 1), data.Int("factor", 56), data.Int("baseDist", 24),
                data.Attr("flag", ""), world)
        {
        }

        public GrowBlock(Vector2 position, char tileType, float width, float height, float moveX, float moveY,
            int factor, int baseDist, string flag, ISourceSolidWorld world) : base(position, width, height, true, world)
        {
            engine = world.EngineContext;
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            easing = ((ISourceEasingEnvironment)engine).EasingArithmetic;
            Depth = -9999;
            this.width = width;
            this.height = height;
            this.tileType = tileType;
            move = new Vector2(moveX, moveY);
            SurfaceSoundIndex = ((IOriginalTileSurfaceEnvironment)engine).SurfaceIndices.TileToIndex[tileType];
            BlockWaterfalls = false;
            this.flag = flag;
            this.factor = factor * factor;
            // The installed constructor does not assign its baseDist argument; Resize uses the field's zero value.
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.TransitionUpdate.Value;
            Add(new BeforeRenderCallbackComponentState(BeforeRender));
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (scene is OriginalLevelSceneRoot actual) level = actual;
            Add(tileGrid = ((ISourceAutotilingResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                .ForegroundAutotiler.GenerateBox(tileType, (int)width / 8, (int)height / 8).TileGrid);
            Add(lightOcclude = new LightOccludeState(1f));
            Resize();
            if (SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities, PlayerKey, this))
                this.RemoveSelf();
        }

        public override void Update()
        {
            base.Update();
            if (level != null && !string.IsNullOrEmpty(flag))
                Visible = Collidable = level.Session == null || level.Session.GetFlag(flag);
            Resize();
        }
    }
}
