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

[CustomEntity("VivHelper/FlagConditionBlock = LegacyLoad", "VivHelper/FlagConditionBlock2 = Load")]
[RuntimeTypeAlias("VivHelper.Entities.FlagConditionBlock")]
public class FlagConditionBlock : SourceSolidEntityState
{
    private static readonly string playerKey = typeof(RoomPlayerSession).FullName;
    public string flag;
    public bool active;
    private char tileType;
    private bool blendIn, invert, startVal, ignoreStartVal;
    private float delay, timer;

    public static SceneEntityState LegacyLoad(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
        OriginalLevelEntityData data) => new FlagConditionBlock(data, offset, 0,
        level.SolidWorldFactory.Create(data.Position + offset, true));

    public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
        OriginalLevelEntityData data) => new FlagConditionBlock(data, offset, 1,
        level.SolidWorldFactory.Create(data.Position + offset, true));

    public FlagConditionBlock(OriginalLevelEntityData data, Vector2 offset, int legacy, ISourceSolidWorld world) : base(
        data.Position + offset, data.Width, data.Height, true, world)
    {
        flag = data.Attr("Flag", "");
        tileType = data.Has("tiletype") ? data.Char("tiletype", '3') : data.Char("tileType", '3');
        blendIn = data.Bool("blendIn", false);
        SurfaceSoundIndex =
            ((IOriginalTileSurfaceEnvironment)SourceWorld.EngineContext).SurfaceIndices.TileToIndex[tileType];
        EnableAssistModeChecks = false;
        ignoreStartVal = data.Bool("IgnoreStartVal", true);
        startVal = data.Bool("StartVal", false);
        invert = data.Bool("InvertFlag", false);
        delay = data.Float("Delay", 0f);
    }

    public override void Awake(object scene)
    {
        base.Awake(scene);
        var level = (OriginalLevelSceneRoot)Scene;
        var autotiler =
            ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)SourceWorld.EngineContext).GameGraphics)
            .ForegroundAutotiler;
        TileGrid grid;
        if (!blendIn) grid = autotiler.GenerateBox(tileType, (int)Width / 8, (int)Height / 8).TileGrid;
        else
        {
            IntRect bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
            grid = autotiler.GenerateOverlay(tileType, (int)(X / 8f) - bounds.Left, (int)(Y / 8f) - bounds.Top,
                (int)Width / 8, (int)Height / 8, level.SolidsData).TileGrid;
            Depth = -10501;
        }

        Add(grid);
        Add(new TileInterceptorState(grid, true));
        Add(new LightOccludeState(1f));
        if (SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities, playerKey, this))
            RemoveSelf();
        timer = delay;
        if (!ignoreStartVal) level.Session.SetFlag(flag, startVal);
    }

    public override void Update()
    {
        base.Update();
        bool value = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag);
        if (Collidable && (invert ? value : !value)) EnableStaticMovers();
        else if (!Collidable && (invert ? !value : value)) DisableStaticMovers();
        Collidable = Visible = invert ? !value : value;
    }
}
