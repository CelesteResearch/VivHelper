// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Engine.Actions;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;

namespace Celeste.Mods.VivHelper.Entities.Walls.DashBlocks
{
    [RuntimeTypeAlias("VivHelper.Entities.CustomDashBlockAlt")]
    public class CustomDashBlockAlt : SourceSolidEntityState
    {
        protected bool permanent;
        protected MapEntityId id;
        private readonly CustomDashBlockBehavior behavior;
        private readonly Action removePermanent;

        public CustomDashBlockAlt(EntityData data, Vector2 offset, MapEntityId id, ISourceSolidWorldFactory worlds)
            : base(data.Position + offset, data.Width, data.Height, true, worlds.Create(data.Position + offset, true))
        {
            Depth = -12999;
            this.id = id;
            permanent = data.Bool("permanent", true);
            behavior = new CustomDashBlockBehavior(data, SourceWorld, true);
            OnDashCollide = OnDashed;
            removePermanent = RemoveAndFlagAsGone;
            SurfaceSoundIndex = ((Celeste.Game.Audio.Surfaces.IOriginalTileSurfaceEnvironment)SourceWorld.EngineContext)
                .SurfaceIndices.TileToIndex[data.Char("tiletype", '3')];
        }

        public override void Awake(object scene) => behavior.Awake(this, SourceWorld);

        public override void Removed(object scene)
        {
            base.Removed(scene);
            OriginalCelesteFreeze.Freeze(SourceWorld.EngineContext, .05f);
        }

        public void Break2(Vector2 from, Vector2 direction, bool playSound = true, bool playDebrisSound = true) =>
            behavior.Break(this, SourceWorld, from, playSound, playDebrisSound, true, permanent, removePermanent);

        public void RemoveAndFlagAsGone()
        {
            RemoveSelf();
            ((OriginalLevelSceneRoot)Scene).Session.DoNotLoad.Add(id);
        }

        protected virtual DashCollisionResult OnDashed(RoomPlayerSession player, Vector2 direction)
        {
            if (!behavior.CanBreak(this, player)) return DashCollisionResult.NormalCollision;
            bool boost = behavior.States.IsBoost(player.Machine.State);
            Break2(player.Center, direction, behavior.AudioEvent != "");
            return boost ? DashCollisionResult.Ignore : DashCollisionResult.Rebound;
        }
    }
}
