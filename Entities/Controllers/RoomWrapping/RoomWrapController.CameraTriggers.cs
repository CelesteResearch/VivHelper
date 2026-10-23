// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Entities;
using Celeste.Mods.VivHelper.Triggers.Camera;

namespace Celeste.Mods.VivHelper.Entities.Controllers.RoomWrapping
{
    public partial class RoomWrapController
    {
        public void AutomateCTT(IntRect b, string[] flags)
        {
            var triggers = new List<SceneEntityState>();
            if (scrollT)
                Add(new(b.X, b.Y), b.Width, 8 * (13 + (int)playerOffsets[0]),
                    new(b.X + b.Width / 2f, b.Y + 8 * (14 + (int)playerOffsets[0])), false, true);
            if (scrollR)
                Add(new(b.X + b.Width - 8 * (21 + (int)playerOffsets[1]), b.Y), 20 + (int)playerOffsets[1], b.Height,
                    new(b.X + b.Width - 8 * (21 + (int)playerOffsets[1]), b.Y + b.Height / 2f), true, false);
            if (scrollB)
                Add(new(b.X, b.Y + b.Height + 8 * (14 + (int)playerOffsets[2])), b.Width, 13 + (int)playerOffsets[2],
                    new(b.X + b.Width / 2f, b.Y + b.Height + 8 * (14 + (int)playerOffsets[0])), false, true);
            if (scrollL)
                Add(new(b.X, b.Y), 20 + (int)playerOffsets[3], b.Height,
                    new(b.X + 8 * (21 + (int)playerOffsets[3]), b.Y + b.Height / 2f), true, false);
            foreach (var trigger in triggers) level.Scheduler.QueueSourceEntity(trigger);

            void Add(Vector2 position, int width, int height, Vector2 node, bool xOnly, bool yOnly)
            {
                var data = new EntityData
                {
                    Position = position, Width = width, Height = height, Nodes = new[] { node },
                    Values = new() { { "xOnly", xOnly }, { "yOnly", yOnly }, { "lerpStrength", 2f } }
                };
                triggers.Add(new MultiflagCameraTargetTrigger(data, Vector2.Zero, engine, flags));
            }
        }
    }
}
