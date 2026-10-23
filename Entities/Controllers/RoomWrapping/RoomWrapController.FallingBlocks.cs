// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Game.Execution.Levels;

namespace Celeste.Mods.VivHelper.Entities.Controllers.RoomWrapping
{
    public partial class RoomWrapController
    {
        public static RoomWrapController FindForAllEntities(ISourceScene scene)
        {
            foreach (var entry in scene.SourceScene.Entities.GetTrackedEntities(typeof(RoomWrapController)))
                if (entry is RoomWrapController { allEntities: true } controller)
                    return controller;
            return null;
        }

        public static void WrapFallingBlock(SourcePlatformEntityState block, ref int repetitions)
        {
            if (repetitions == 0) return;
            var controller = FindForAllEntities((ISourceScene)block.Scene);
            if (controller == null) return;
            var level = (OriginalLevelSceneRoot)block.Scene;
            bool wrapped = false;
            if (block.Right < level.Bounds.Left + 8f + controller.playerOffsets[3] && controller.scrollL)
            {
                block.MoveToX(level.Bounds.Right - 8f + controller.playerOffsets[1]);
                wrapped = true;
            }

            if (block.Left > level.Bounds.Right - 8f + controller.playerOffsets[1] && controller.scrollR)
            {
                block.MoveToX(level.Bounds.Left + 8f + controller.playerOffsets[3] - block.Width);
                wrapped = true;
            }

            if (block.Top < level.Bounds.Top + 8f + controller.playerOffsets[0] && controller.scrollT)
            {
                block.MoveToY(level.Bounds.Bottom - 8f + controller.playerOffsets[2] - block.Height);
                wrapped = true;
            }

            if (block.Bottom > level.Bounds.Bottom - 8f + controller.playerOffsets[2] && controller.scrollB)
            {
                block.MoveToY(level.Bounds.Top + 8f + controller.playerOffsets[0]);
                wrapped = true;
            }

            if (wrapped) repetitions--;
        }
    }
}
