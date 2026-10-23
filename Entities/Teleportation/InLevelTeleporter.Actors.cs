using Celeste.Game.Execution.Processes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Core.Audio;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Content;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.VivHelper.Entities.Teleportation
{
    public partial class InLevelTeleporter
    {
        private void CameraShit(OriginalLevelSceneRoot level, RoomPlayerSession player, Vector2 cameraDifferential)
        {
            foreach (SourceTriggerEntityState item in Triggers(level))
            {
                if (!SourceEntityCollision.Check(player, item) ||
                    item.GetType().Name.IndexOf("camera", StringComparison.OrdinalIgnoreCase) < 0) continue;
                SourceTriggerEntityState trigger = item;
                player.PlayerTriggers.UpdateContact(player, trigger, true);
            }

            level.Camera.Position = player.CameraTarget + cameraDifferential;
        }

        private bool WiggleOutFail(SceneEntityState entity)
        {
            if (CollideSolid(entity))
            {
                Vector2 val = default(Vector2);
                for (int i = 1; i <= 5; i++)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        for (int k = 1; k <= 5; k++)
                        {
                            for (int l = -1; l <= 1; l += 2)
                            {
                                val = new Vector2(i * j, k * l);
                                if (!CollideSolidAt(entity, base.Position + val))
                                {
                                    entity.Position += val;
                                    return false;
                                }
                            }
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public void TeleportEntity(SceneEntityState entity, float distance, Directions d)
        {
            switch (Direction)
            {
                case Directions.Right:
                    entity.BottomRight = new Vector2(this.Left, this.Bottom - distance);
                    break;
                case Directions.Left:
                    entity.TopLeft = new Vector2(this.Right, this.Top + distance);
                    break;
                case Directions.Up:
                    entity.BottomLeft = new Vector2(this.Left + distance, this.Bottom);
                    break;
                case Directions.Down:
                    entity.TopRight = new Vector2(this.Right - distance, this.Top);
                    break;
            }

            if (WiggleOutFail(entity))
            {
                entity.RemoveSelf();
            }
        }

        public Vector2 TeleportEntityWithSpeed(SceneEntityState entity, float distance, Vector2 speed, Directions d)
        {
            float num = (float)(Direction - d) * (float)Math.PI / 2f;
            speed = ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(speed, num);
            speed *= -1f;
            switch (Direction)
            {
                case Directions.Right:
                    entity.BottomRight = new Vector2(this.Left, this.Bottom - distance);
                    break;
                case Directions.Left:
                    entity.TopLeft = new Vector2(this.Right, this.Top + distance);
                    break;
                case Directions.Up:
                    entity.BottomLeft = new Vector2(this.Left + distance, this.Bottom);
                    break;
                case Directions.Down:
                    entity.TopRight = new Vector2(this.Right - distance, this.Top);
                    break;
            }

            if (!string.IsNullOrEmpty(audio))
            {
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(audio, entity.Position);
            }

            if (WiggleOutFail(entity))
            {
                entity.RemoveSelf();
                return Vector2.Zero;
            }

            return speed + new Vector2(speedOut * (float)Math.Cos((float)Math.Atan2(speed.Y, speed.X)),
                speedOut * (float)Math.Sin((float)Math.Atan2(speed.Y, speed.X)));
        }

        private bool FlagsEnabled(OriginalLevelSceneRoot level)
        {
            if (level == null) return false;
            if (Flags == null || Flags.Length == 0 || (Flags.Length == 1 && Flags[0] == "")) return true;
            bool result = true;
            foreach (string flag in Flags)
                result &= flag[0] != '!' ? level.Session.GetFlag(flag) : !level.Session.GetFlag(flag.TrimStart('!'));
            return result;
        }

        private static PackedColor ReadColor(EntityData data) =>
            global::Celeste.Mods.VivHelper.Spawn.SpawnPointRules.ParseColor(data.Attr("Color", ""));

        private List<SourceActorEntityState> CollideActors(bool nested)
        {
            var found = nested ? new List<SourceActorEntityState>() : actorCollisions;
            found.Clear();
            var scene = (ISourceScene)Scene;
            if (scene.SourceScene.Entities.TryGetTrackedEntities(typeof(SourceActorEntityState), out var actors))
                foreach (SourceActorEntityState actor in actors)
                    if (SourceEntityCollision.Check(this, actor))
                        found.Add(actor);
            return found;
        }

        private static IEnumerable<SourceTriggerEntityState> Triggers(OriginalLevelSceneRoot level)
        {
            if (level.SourceScene.Entities.TryGetTrackedEntities(typeof(SourceTriggerEntityState), out var entries))
                foreach (SourceTriggerEntityState trigger in entries)
                    yield return trigger;
        }

        private bool CollideSolid(SceneEntityState entity)
        {
            var scene = (ISourceScene)Scene;
            if (scene.SourceScene.Entities.TryGetTrackedEntities(typeof(SourceSolidEntityState), out var solids))
                foreach (SourceSolidEntityState solid in solids)
                    if (SourceEntityCollision.Check(entity, solid))
                        return true;
            return false;
        }

        private bool CollideSolidAt(SceneEntityState entity, Vector2 at)
        {
            var previous = entity.Position;
            entity.Position = at;
            try
            {
                return CollideSolid(entity);
            }
            finally
            {
                entity.Position = previous;
            }
        }
    }
}
