// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Runtime.RoomWrapping;

namespace Celeste.Mods.VivHelper.Entities.Controllers.RoomWrapping
{
    public partial class RoomWrapController
    {
        private static readonly string sourcePlayerRegistryKey =
            typeof(global::Celeste.Simulation.RoomPlayerSession).FullName;

        public override void Update()
        {
            base.Update();
            IntRect bounds = level != null ? level.Bounds : default;
            if (bounds.Equals(default(IntRect)))
            {
                level = (OriginalLevelSceneRoot)Scene;
                return;
            }

            bool cameraMode = setByCamera;
            var camera = cameraMode ? level.Camera : null;
            var players = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(sourcePlayerRegistryKey);
            var player = players.Count == 0 ? null : players[0] as RoomPlayerSession;
            if (player == null || !RoomWrapFlags.All(level, flag)) return;
            if (cameraMode)
            {
                if (scrollB && player.Top > camera.Bottom - playerOffsets[2]) QueueWrap(player, bounds, 0, true);
                if (scrollR && player.Left > camera.Right - playerOffsets[1]) QueueWrap(player, bounds, 3, true);
                if (scrollT && player.Bottom < camera.Top - playerOffsets[0]) QueueWrap(player, bounds, 2, true);
                if (scrollL && player.Right < camera.Left - playerOffsets[3]) QueueWrap(player, bounds, 1, true);
                return;
            }

            if (scrollB && player.Top > bounds.Bottom - 8f + playerOffsets[2]) QueueWrap(player, bounds, 0, false);
            if (scrollR && player.Left > bounds.Right - 8f + playerOffsets[1]) QueueWrap(player, bounds, 3, false);
            if (scrollT && player.Bottom < bounds.Top + 8f + playerOffsets[0]) QueueWrap(player, bounds, 2, false);
            if (scrollL && player.Right < bounds.Left + 8f + playerOffsets[3]) QueueWrap(player, bounds, 1, false);
        }

        private void QueueWrap(RoomPlayerSession player, IntRect bounds, int destination, bool cameraMode)
        {
            level.Scheduler.OnEndOfFrame += () =>
            {
                switch (destination)
                {
                    case 0: player.Bottom = bounds.Top + 8f + playerOffsets[0]; break;
                    case 3: player.Right = bounds.Left + 8f + playerOffsets[3]; break;
                    case 2: player.Top = bounds.Bottom - 8f + playerOffsets[2]; break;
                    case 1: player.Left = bounds.Right - 8f + playerOffsets[1]; break;
                }

                player.Speed = SourceVector2Math.Add(player.Speed, a_ES[destination]);
                if (cameraMode && lockCamera)
                {
                    foreach (var entry in ((ISourceScene)level).SourceScene.Entities.GetTrackedEntities(
                                 "Celeste.Trigger"))
                    {
                        var trigger = (SourceTriggerEntityState)entry;
                        if (SourceEntityCollision.Check(player, trigger) &&
                            trigger.GetType().Name.IndexOf("camera", StringComparison.OrdinalIgnoreCase) > -1)
                            player.PlayerTriggers.UpdateContact(player, trigger, true);
                    }

                    cameraContext.Session.LockCamera = 1;
                }
            };
        }
    }
}
