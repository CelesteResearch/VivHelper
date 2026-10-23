// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.VivHelper.Entities.Solids;

[CustomEntity("VivHelper/EnterBlock = Load")]
[RuntimeTypeAlias("VivHelper.Entities.EnterBlock")]
public class EnterBlock : SourceSolidEntityState
{
    private static readonly string playerKey = typeof(RoomPlayerSession).FullName;
    private TileGrid tiles;
    private EffectCutoutState cutout;
    private float startAlpha;
    private char tileType;
    private bool primed;

    public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
        OriginalLevelEntityData data) => new EnterBlock(data.Position + offset, data.Width, data.Height,
        data.Char("tiletype", '3'), level.SolidWorldFactory.Create(data.Position + offset, true));

    public EnterBlock(Vector2 position, float width, float height, char tileType, ISourceSolidWorld world) : base(
        position, width, height, true, world)
    {
        Depth = -13000;
        this.tileType = tileType;
        Add(cutout = new EffectCutoutState());
        SurfaceSoundIndex =
            ((IOriginalTileSurfaceEnvironment)SourceWorld.EngineContext).SurfaceIndices.TileToIndex[tileType];
        EnableAssistModeChecks = false;
    }

    public override void Added(object scene)
    {
        base.Added(scene);
        var level = (OriginalLevelSceneRoot)Scene;
        IntRect bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
        var autotiler =
            ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)SourceWorld.EngineContext).GameGraphics)
            .ForegroundAutotiler;
        tiles = autotiler.GenerateOverlay(tileType, (int)(X / 8f) - bounds.Left, (int)(Y / 8f) - bounds.Top,
            (int)Width / 8, (int)Height / 8, level.SolidsData).TileGrid;
        Add(tiles);
        Add(new TileInterceptorState(tiles, false));
    }

    public override void Awake(object scene)
    {
        base.Awake(scene);
        cutout.Alpha = tiles.Alpha = .1f;
        Collidable = false;
        primed = false;
    }

    public override void Update()
    {
        base.Update();
        if (primed)
        {
            if (Collidable)
                cutout.Alpha = tiles.Alpha = ScalarMath.Approach(tiles.Alpha, 1f, SourceWorld.EngineContext.DeltaTime);
            else if (!SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities, playerKey, this))
            {
                Collidable = true;
                ((Celeste.Core.Audio.ISourceSpatialAudioEnvironment)SourceWorld.EngineContext).SpatialAudio.Play(
                    "event:/game/general/passage_closed_behind", Center);
            }
        }
        else if (SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities, playerKey, this))
        {
            primed = true;
            cutout.Alpha = tiles.Alpha = .175f;
        }
    }

    public override void Render(ISourceRenderContext context)
    {
        if (tiles.Alpha >= 1f)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            var bounds = level.Bounds;
            if (level.ShakeVector.X < 0f && level.Camera.X <= bounds.Left && X <= bounds.Left)
                tiles.RenderAt(Position + new Vector2(-3f, 0f), context);
            if (level.ShakeVector.X > 0f && level.Camera.X + 320f >= bounds.Right && X + Width >= bounds.Right)
                tiles.RenderAt(Position + new Vector2(3f, 0f), context);
            if (level.ShakeVector.Y < 0f && level.Camera.Y <= bounds.Top && Y <= bounds.Top)
                tiles.RenderAt(Position + new Vector2(0f, -3f), context);
            if (level.ShakeVector.Y > 0f && level.Camera.Y + 180f >= bounds.Bottom && Y + Height >= bounds.Bottom)
                tiles.RenderAt(Position + new Vector2(0f, 3f), context);
        }

        base.Render(context);
    }
}
