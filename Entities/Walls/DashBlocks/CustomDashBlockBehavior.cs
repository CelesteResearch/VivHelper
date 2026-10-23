// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Walls.Breakable;
using Celeste.Mods.VivHelper.Runtime.Debris;
using Celeste.Mods.VivHelper.Runtime.DashBlocks;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Debris;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Entities.Walls.DashBlocks
{
    internal sealed class CustomDashBlockBehavior
    {
        private static readonly string PlayerKey = typeof(RoomPlayerSession).FullName;
        private readonly DebrisLimiterContext debris;
        internal readonly CustomDashBlockPlayerStates States;
        private char tile;
        private float width;
        private float height;
        private readonly bool blend;
        private readonly bool breakStaticMovers;
        private readonly bool canDash;
        internal readonly string AudioEvent;
        private readonly string flagBreak;
        private readonly string flagDisable;

        internal CustomDashBlockBehavior(EntityData data, ISourceSolidWorld world, bool alternate)
        {
            tile = data.Char("tiletype", '3');
            width = data.Width;
            height = data.Height;
            blend = data.Bool("blendin", false);
            canDash = data.Bool("canDash", true);
            AudioEvent = data.Attr("AudioEvent", alternate ? "event:/game/general/wall_break_stone" : "gameDefault");
            flagBreak = data.Attr("FlagOnBreak", "");
            flagDisable = data.Attr("FlagToDisable", "");
            breakStaticMovers = data.Bool("BreakStaticMovers", false);
            debris = DebrisLimiterContext.For(world.EngineContext);
            States = CustomDashBlockPlayerStates.For(world.EngineContext);
        }

        internal void Awake(SourceSolidEntityState block, ISourceSolidWorld world)
        {
            var session = (block.Scene as OriginalLevelSceneRoot)?.Session;
            if (flagBreak != "") session?.SetFlag(flagBreak, false);
            if (flagDisable != "") session?.SetFlag(flagDisable, false);
            BreakableTileBlockVisuals.AddTiles(block, world, ref tile, blend, ref width, ref height);
            if (SourceTrackedEntityCollision.Check(((ISourceScene)block.Scene).SourceScene.Entities, PlayerKey, block))
                block.RemoveSelf();
        }

        internal bool CanBreak(SourceSolidEntityState block, RoomPlayerSession player)
        {
            if (!canDash && player.Machine.State != 5 && player.Machine.State != 10) return false;
            // The released PE tests FlagOnBreak here, despite gating this check with FlagToDisable.
            return flagDisable == "" || (block.Scene as OriginalLevelSceneRoot)?.Session?.GetFlag(flagBreak) != true;
        }

        internal void Break(SourceSolidEntityState block, ISourceSolidWorld world, Vector2 from, bool playSound,
            bool playDebrisSound, bool alternate, bool permanent, Action removePermanent)
        {
            if (playSound)
            {
                if (!alternate && AudioEvent == "gameDefault")
                    BreakableTileBlockEffects.PlayBreakSound(world, tile, block);
                else
                    ((ISourceSpatialAudioEnvironment)world.EngineContext).SpatialAudio.Play(AudioEvent, block.Position);
            }

            block.Collidable = false;
            if (debris.Session.DebrisLimiter < 1f)
                for (int x = 0; x < block.Width / 8f; x++)
                for (int y = 0; y < block.Height / 8f; y++)
                {
                    var position = block.Position + new Vector2(4 + x * 8, 4 + y * 8);
                    if (!SourceSceneCollision.CheckPoint(((ISourceScene)block.Scene).SourceScene.Entities,
                            "Celeste.Solid", position))
                        ((ISourceScene)block.Scene).SourceScene.QueueSourceEntity(world.EngineContext.EntityPools
                            .Create<DebrisState>()
                            .Init(position, tile, playDebrisSound).BlastFrom(from));
                }

            if (breakStaticMovers) block.DestroyStaticMovers();
            else block.DisableStaticMovers();
            if (flagBreak != "") (block.Scene as OriginalLevelSceneRoot)?.Session?.SetFlag(flagBreak, true);
            if (permanent) removePermanent();
            else block.RemoveSelf();
        }
    }
}
