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
        protected void TeleportPlayer(RoomPlayerSession player, float distance, Vector2 speed, Directions d)
        {
            OriginalLevelSceneRoot val = Scene as OriginalLevelSceneRoot;
            if (val == null)
            {
                return;
            }

            Vector2 cameraDifferential = val.Camera.Position - player.CameraTarget;
            if (legacy)
            {
                switch (Direction)
                {
                    case Directions.Right:
                        if (center)
                        {
                            player.CenterRight = new Vector2(this.Left, this.CenterY);
                        }
                        else
                        {
                            player.BottomRight = new Vector2(this.Left, this.Bottom - distance);
                        }

                        break;
                    case Directions.Left:
                        if (center)
                        {
                            player.CenterLeft = new Vector2(this.Right, this.CenterY);
                        }
                        else
                        {
                            player.TopLeft = new Vector2(this.Right, this.Top + distance);
                        }

                        break;
                    case Directions.Up:
                        if (center)
                        {
                            player.BottomCenter = new Vector2(this.CenterX, this.Bottom);
                        }
                        else
                        {
                            player.BottomLeft = new Vector2(this.Left + distance, this.Bottom);
                        }

                        break;
                    case Directions.Down:
                        if (center)
                        {
                            player.TopCenter = new Vector2(this.CenterX, this.Top);
                        }
                        else
                        {
                            player.TopRight = new Vector2(this.Right - distance, this.Top);
                        }

                        break;
                }

                player.Position = new Vector2((float)Math.Ceiling(player.Position.X),
                    (float)Math.Ceiling(player.Position.Y));
                player.CleanUpPlayerTriggers();
                if (WiggleOutFail(player))
                {
                    player.Die(Vector2.Zero, false, true);
                    return;
                }

                player.Speed = SpeedMod(player, d);
                if (cameraWarp)
                {
                    object scene = player.Scene;
                    CameraShit(scene as OriginalLevelSceneRoot, player, cameraDifferential);
                }

                cooldown = CooldownTime;
                if (!string.IsNullOrEmpty(audio))
                {
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(audio, player.Position);
                }

                if (cooldown < 0f)
                {
                    cooldown = float.MaxValue;
                    this.Add(new SimulationCoroutine(engine, DisablePlayerCollision(player), true));
                }

                return;
            }

            val.SourceScene.OnEndOfFrame += delegate
            {
                switch (Direction)
                {
                    case Directions.Right:
                        player.BottomRight = new Vector2(this.Left, this.Bottom - distance);
                        if (AlwaysRetainSpeed && player.Speed.X == 0f)
                        {
                            var retained = player.Speed;
                            retained.X = player.Data.WallSpeedRetained;
                            player.Speed = retained;
                        }

                        break;
                    case Directions.Left:
                        player.TopLeft = new Vector2(this.Right, this.Top + distance);
                        if (AlwaysRetainSpeed && player.Speed.X == 0f)
                        {
                            var retained = player.Speed;
                            retained.X = player.Data.WallSpeedRetained;
                            player.Speed = retained;
                        }

                        break;
                    case Directions.Up:
                        player.BottomLeft = new Vector2(this.Left + distance, this.Bottom);
                        break;
                    case Directions.Down:
                        player.TopRight = new Vector2(this.Right - distance, this.Top);
                        break;
                }

                if (WiggleOutFail(player))
                {
                    player.Die(Vector2.Zero, false, true);
                }
                else
                {
                    player.Speed = SpeedMod(player, d);
                    if (cameraWarp)
                    {
                        InLevelTeleporter inLevelTeleporter = this;
                        object scene2 = player.Scene;
                        inLevelTeleporter.CameraShit(scene2 as OriginalLevelSceneRoot, player, cameraDifferential);
                    }

                    cooldown = CooldownTime;
                    if (!string.IsNullOrEmpty(audio))
                    {
                        ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(audio, player.Position);
                    }

                    if (cooldown < 0f)
                    {
                        cooldown = float.MaxValue;
                        this.Add(new SimulationCoroutine(engine, DisablePlayerCollision(player), true));
                    }
                }
            };
        }

        private IEnumerator DisablePlayerCollision(RoomPlayerSession player)
        {
            while (SourceEntityCollision.Check(this, player))
            {
                yield return null;
            }

            cooldown = 0.02f;
        }

        private Vector2 SpeedMod(RoomPlayerSession player, Directions d)
        {
            Vector2 speed = player.Speed;
            if (OHMod)
            {
                if (d == Directions.Up)
                {
                    speed.Y = Math.Max(speed.Y, 150f);
                }

                float num = (float)(Direction - d) * (float)Math.PI / 2f;
                speed = ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(speed, num);
                speed *= -1f;
                if (player.Machine.State == 2)
                {
                    player.Machine.State = 0;
                }

                if (player.Machine.State != 5)
                {
                    if (d == Directions.Up)
                    {
                        speed *= 1.5f;
                        if ((int)Direction % 2 == 0)
                        {
                            speed.Y -= 150f;
                        }
                    }
                    else if (d == Directions.Up)
                    {
                        speed *= 1.5f;
                    }
                }

                if (Direction == Directions.Down)
                {
                    speed.Y = Math.Min(player.Speed.Y, -150f);
                }
            }
            else
            {
                float num2 = (float)(Direction - d) * (float)Math.PI / 2f;
                speed = ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(speed, num2);
                speed *= -1f;
            }

            return speed + new Vector2(speedOut * (float)Math.Cos((float)Math.Atan2(speed.Y, speed.X)),
                speedOut * (float)Math.Sin((float)Math.Atan2(speed.Y, speed.X)));
        }
    }
}
