// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.VivHelper.Entities.Solids
{
    [CustomEntity("VivHelper/CornerBoostBlock = Create")]
    [EverestTrackedAs(typeof(CornerBoostSolid))]
    [RuntimeTypeAlias("VivHelper.Entities.CornerBoostBlock")]
    public class CornerBoostBlock : CornerBoostSolid
    {
        private MapEntityId id;
        private char tileType;
        private bool blendIn;

        public static CornerBoostBlock Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(data.Position + offset, data.Char("tiletype", 'm'), data.Width, data.Height,
                data.Bool("blendin"), new MapEntityId(room.Name, data.ID), data.Bool("perfectCornerBoost"),
                level.SolidWorldFactory.Create(data.Position + offset, true));

        public CornerBoostBlock(Vector2 position, char tileType, float width, float height, bool blendIn,
            MapEntityId id, bool perfectCB, ISourceSolidWorld world)
            : base(position, width, height, true, perfectCB, world)
        {
            Depth = -12999;
            this.id = id;
            this.tileType = tileType;
            this.blendIn = blendIn;
            SurfaceSoundIndex = ((IOriginalTileSurfaceEnvironment)Engine).SurfaceIndices.TileToIndex[tileType];
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var autotiler = ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)Engine).GameGraphics)
                .ForegroundAutotiler;
            TileGrid tileGrid;
            if (!blendIn) tileGrid = autotiler.GenerateBox(tileType, (int)Width / 8, (int)Height / 8).TileGrid;
            else
            {
                var level = (OriginalLevelSceneRoot)Scene;
                IntRect bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
                tileGrid = autotiler.GenerateOverlay(tileType, (int)(X / 8f) - bounds.Left,
                    (int)(Y / 8f) - bounds.Top, (int)Width / 8, (int)Height / 8, level.SolidsData).TileGrid;
                Depth = -10501;
            }

            Add(tileGrid);
            Add(new TileInterceptorState(tileGrid, true));
            Add(new LightOccludeState(1f));
            if (SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities,
                    typeof(RoomPlayerSession).FullName, this)) RemoveSelf();
        }
    }
}
