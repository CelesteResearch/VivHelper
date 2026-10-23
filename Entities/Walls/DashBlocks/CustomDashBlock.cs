// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Walls.Breakable;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;

namespace Celeste.Mods.VivHelper.Entities.Walls.DashBlocks
{
    [CustomEntity("VivHelper/CustomDashBlock = Load")]
    [EverestTrackedAs(typeof(OriginalDashBlockState))]
    [RuntimeTypeAlias("VivHelper.Entities.CustomDashBlock")]
    public class CustomDashBlock : OriginalDashBlockState
    {
        protected bool permanent;
        protected MapEntityId id;
        private readonly CustomDashBlockBehavior behavior;
        private readonly Action removePermanent;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
        {
            var id = new MapEntityId(room.Name, data.ID);
            return data.Bool("disableFallingBlocksBreak", false)
                ? new CustomDashBlockAlt(data, offset, id, level.SolidWorldFactory)
                : new CustomDashBlock(data, offset, id, level.SolidWorldFactory);
        }

        public CustomDashBlock(EntityData data, Vector2 offset, MapEntityId id, ISourceSolidWorldFactory worlds)
            : base(data, offset, id, worlds)
        {
            Depth = -9000;
            this.id = id;
            permanent = data.Bool("permanent", true);
            behavior = new CustomDashBlockBehavior(data, SourceWorld, false);
            OnDashCollide = OnDashed;
            removePermanent = RemoveAndFlagAsGone;
        }

        public override void Awake(object scene) => behavior.Awake(this, SourceWorld);

        public void Break2(Vector2 from, Vector2 direction, bool playSound = true, bool playDebrisSound = true) =>
            behavior.Break(this, SourceWorld, from, playSound, playDebrisSound, false, permanent, removePermanent);

        protected virtual DashCollisionResult OnDashed(RoomPlayerSession player, Vector2 direction)
        {
            if (!behavior.CanBreak(this, player)) return DashCollisionResult.NormalCollision;
            bool boost = behavior.States.IsBoost(player.Machine.State);
            Break(player.Center, direction, behavior.AudioEvent != "");
            return boost ? DashCollisionResult.Ignore : DashCollisionResult.Rebound;
        }
    }
}
