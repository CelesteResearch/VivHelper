// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.VivHelper.Entities.Tiles
{
    [SceneTracked]
    [CustomEntity("VivHelper/CustomDepthTileEntity")]
    [RuntimeTypeAlias("VivHelper.Entities.CustomDepthTileEntity")]
    public class CustomDepthTileEntity : SourceSolidEntityState
    {
        private TileGrid tiles;
        private readonly char tileType;
        private CustomDepthTileEntity master;
        public List<CustomDepthTileEntity> Group;
        public IntPoint GroupBoundsMin;
        public IntPoint GroupBoundsMax;
        public bool HasGroup { get; private set; }
        public bool MasterOfGroup { get; private set; }

        public static CustomDepthTileEntity Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset,
            level.SolidWorldFactory.Create(data.Position + offset, true));

        public CustomDepthTileEntity(EntityData data, Vector2 offset, ISourceSolidWorld world)
            : this(data.Position + offset, data.Width, data.Height, data.Char("tiletype", '3'),
                data.Int("Depth", -9000), world)
        {
        }

        public CustomDepthTileEntity(Vector2 position, float width, float height, char tileType, int depth,
            ISourceSolidWorld world) : base(position, width, height, true, world)
        {
            this.tileType = tileType;
            Depth = System.Math.Min(System.Math.Max(depth, -300000), 20000);
            Add(new LightOccludeState());
            try
            {
                SurfaceSoundIndex =
                    ((IOriginalTileSurfaceEnvironment)world.EngineContext).SurfaceIndices.TileToIndex[tileType];
            }
            catch
            {
                // This fallback is present in the actual 1.14.10 constructor.
                SurfaceSoundIndex = 8;
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (HasGroup) return;
            MasterOfGroup = true;
            Group = new List<CustomDepthTileEntity>();
            GroupBoundsMin = new IntPoint((int)X, (int)Y);
            GroupBoundsMax = new IntPoint((int)Right, (int)Bottom);
            AddToGroupAndFindChildren(this);
            var bounds = new IntRect(GroupBoundsMin.X / 8, GroupBoundsMin.Y / 8,
                (GroupBoundsMax.X - GroupBoundsMin.X) / 8 + 1,
                (GroupBoundsMax.Y - GroupBoundsMin.Y) / 8 + 1);
            var map = new VirtualMap<char>(bounds.Width, bounds.Height, '0');
            foreach (var member in Group)
            {
                int x = (int)(member.X / 8f) - bounds.X;
                int y = (int)(member.Y / 8f) - bounds.Y;
                int width = (int)(member.Width / 8f);
                int height = (int)(member.Height / 8f);
                for (int i = x; i < x + width; i++)
                for (int j = y; j < y + height; j++)
                    map[i, j] = tileType;
            }

            tiles = ((ISourceAutotilingResources)SourceWorld.EngineContext).ForegroundAutotiler.GenerateMap(map,
                new AutotileBehavior
                {
                    EdgesExtend = false, EdgesIgnoreOutOfLevel = false, PaddingIgnoreOutOfLevel = false
                }).TileGrid;
            tiles.Position = new Vector2(GroupBoundsMin.X - X, GroupBoundsMin.Y - Y);
            Add(tiles);
        }

        private void AddToGroupAndFindChildren(CustomDepthTileEntity from)
        {
            if (from.X < GroupBoundsMin.X) GroupBoundsMin = new IntPoint((int)from.X, GroupBoundsMin.Y);
            if (from.Y < GroupBoundsMin.Y) GroupBoundsMin = new IntPoint(GroupBoundsMin.X, (int)from.Y);
            if (from.Right > GroupBoundsMax.X) GroupBoundsMax = new IntPoint((int)from.Right, GroupBoundsMax.Y);
            if (from.Bottom > GroupBoundsMax.Y) GroupBoundsMax = new IntPoint(GroupBoundsMax.X, (int)from.Bottom);
            from.HasGroup = true;
            Group.Add(from);
            if (from != this) from.master = this;
            foreach (object entry in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(CustomDepthTileEntity)))
            {
                var entity = (CustomDepthTileEntity)entry;
                if (!entity.HasGroup && entity.tileType == tileType &&
                    (SourceSceneCollision.CheckRect(new IntRect((int)from.X - 1, (int)from.Y,
                         (int)from.Width + 2, (int)from.Height), entity) ||
                     SourceSceneCollision.CheckRect(new IntRect((int)from.X, (int)from.Y - 1,
                         (int)from.Width, (int)from.Height + 2), entity)))
                    AddToGroupAndFindChildren(entity);
            }
        }
    }
}
