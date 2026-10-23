using System;
using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Simulation;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;

namespace Celeste.Mods.VivHelper.Triggers
{
    public partial class InstantTeleportTrigger
    {
        private void ModifySpeed(RoomPlayerSession player)
        {
            if (vel2 == -1f && (vel[0] != 0f || vel[1] != 0f))
            {
                player.Data.Motion.Normal.Speed = (Vector2)(velMod
                    ? new Vector2(player.Data.Motion.Normal.Speed.X * vel[0],
                        player.Data.Motion.Normal.Speed.Y * vel[1])
                    : (player.Data.Motion.Normal.Speed + new Vector2(vel[0], vel[1])));
            }

            if (vel2 != -1f && vel[0] == 0f && vel[1] == 0f)
            {
                player.Data.Motion.Normal.Speed = (velMod
                    ? (player.Data.Motion.Normal.Speed * vel2)
                    : (player.Data.Motion.Normal.Speed +
                       new Vector2(vel2 * (float)Math.Cos(Vector2Geometry.Angle(player.Data.Motion.Normal.Speed)),
                           vel2 * (float)Math.Sin(Vector2Geometry.Angle(player.Data.Motion.Normal.Speed)))));
            }

            player.Data.Motion.Normal.Speed = (rotMod
                ? VectorArithmetic.RotateTowards(player.Data.Motion.Normal.Speed, rot, 6.3f)
                : VectorArithmetic.Rotate(player.Data.Motion.Normal.Speed, rot));
        }

        private void CameraStuff(Level level, RoomPlayerSession player, Vector2 v)
        {
            if (legacyCamera == 0)
            {
                level.Camera.Position = v;
                return;
            }

            Vector2 position = player.Position;
            Vector2 val = default(Vector2);
            val = new Vector2(player.X - 160f, player.Y - 90f);
            float x = val.X;
            IntRect bounds = level.Bounds;
            float num = bounds.Left;
            bounds = level.Bounds;
            position.X = Clamp(x, num, (float)(bounds.Right - 320));
            float y = val.Y;
            bounds = level.Bounds;
            float num2 = bounds.Top;
            bounds = level.Bounds;
            position.Y = Clamp(y, num2, (float)(bounds.Bottom - 180));
            level.Camera.Position = position;
        }

        private void NewCamera(Level level, RoomPlayerSession player, Vector2[] vals,
            Tuple<Vector2, Vector2, bool, bool> c)
        {
            level.CameraOffset = vals[3];
            Vector2 position = player.Position;
            Vector2 val = new Vector2(player.X - 160f, player.Y - 90f) + vals[3];
            float x = val.X;
            IntRect bounds = level.Bounds;
            float num = bounds.Left;
            bounds = level.Bounds;
            position.X = Clamp(x, num, (float)(bounds.Right - 320));
            float y = val.Y;
            bounds = level.Bounds;
            float num2 = bounds.Top;
            bounds = level.Bounds;
            position.Y = Clamp(y, num2, (float)(bounds.Bottom - 180));
            level.Camera.Position = position;
            if (legacyCamera == 4)
            {
                var camera = level.Camera;
                camera.Position += vals[4];
            }

            int num3 = legacyCamera;
            int num4 = num3;
            if (num4 == 3)
            {
                player.CameraAnchor = c.Item1;
                player.CameraAnchorLerp = Vector2.Zero;
                player.CameraAnchorIgnoreX = c.Item3;
                player.CameraAnchorIgnoreY = c.Item4;
            }
            else
            {
                player.CameraAnchor = c.Item1;
                player.CameraAnchorLerp = c.Item2;
                player.CameraAnchorIgnoreX = c.Item3;
                player.CameraAnchorIgnoreY = c.Item4;
            }
        }
    }
}
