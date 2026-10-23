// SPDX-License-Identifier: MIT

using Celeste.Core.Mathematics;
using Celeste.Core.Components;
using Celeste.Graphics.Content;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content.Tiles;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Scenes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.VivHelper.Entities.Walls.Concealment
{
    [CustomEntity("VivHelper/CustomCoverupWall")]
    [RuntimeTypeAlias("VivHelper.Entities.CustomCoverupWall")]
    public class CustomCoverupWall : SceneEntityState
    {
        private static readonly string sourcePlayerRegistryKey =
            typeof(global::Celeste.Simulation.RoomPlayerSession).FullName;

        protected char fillTile;
        protected TileGrid tiles;
        protected EffectCutoutState cutout;
        public float alpha, currentAlpha;
        public string flag;
        public bool inverted, renderPlayerOver, instant, blendIn;
        private readonly ISourceEngineContext engine;

        public CustomCoverupWall(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            fillTile = data.Char("tiletype", '3');
            Depth = data.Int("Depth", -13000);
            Collider = new HitboxColliderGeometry(data.Width, data.Height);
            Add(cutout = new EffectCutoutState());
            alpha = ScalarMath.Clamp(data.Float("alpha", 1f), 0f, 1f);
            flag = data.Attr("flag", "");
            inverted = data.Bool("inverted", false);
            instant = data.Bool("instant", true);
            renderPlayerOver = data.Bool("RenderPlayerOver", false);
            blendIn = data.Bool("blendIn", false);
            if (data.Has("lightOcclude")) Add(new LightOccludeState(data.Float("lightOcclude", 1f)));
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new CustomCoverupWall(level.EngineContext, data, offset);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var autotiler = ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                .ForegroundAutotiler;
            if (!blendIn) tiles = autotiler.GenerateBox(fillTile, (int)Width / 8, (int)Height / 8).TileGrid;
            else
            {
                var level = (OriginalLevelSceneRoot)Scene;
                var bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
                int x = (int)(X / 8f) - bounds.Left;
                int y = (int)(Y / 8f) - bounds.Top;
                tiles = autotiler.GenerateOverlay(fillTile, x, y, (int)Width / 8, (int)Height / 8, level.SolidsData)
                    .TileGrid;
            }

            Add(tiles);
            Add(new TileInterceptorState(tiles, true));
            tiles.Alpha = cutout.Alpha = flag != "" && ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag) == inverted
                ? 0f
                : alpha;
        }

        public override void Update()
        {
            base.Update();
            currentAlpha = flag != "" && ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag) == inverted ? 0f : alpha;
            tiles.Alpha = cutout.Alpha = ScalarMath.Approach(tiles.Alpha, currentAlpha,
                instant ? 2f : engine.Frame.DeltaTime / 3f);
        }

        public override void Render(Celeste.Core.Rendering.ISourceRenderContext context)
        {
            base.Render(context);
            if (!renderPlayerOver) return;
            var scene = engine.Frame.Scenes.CurrentScene as ISourceScene;
            var player = scene?.SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(sourcePlayerRegistryKey);
            if (player != null && !player.Dead && SourceEntityCollision.Check(this, player)) player.Render(context);
        }
    }
}
