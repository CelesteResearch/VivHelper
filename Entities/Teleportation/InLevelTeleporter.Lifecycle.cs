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
        public override void Added(object scene)
        {
            base.Added(scene);
            if (particles.Glow == null)
            {
                particles.Glow =
                    new ParticleTypePresentation(((IOriginalGameplayProcess)engine).ParticleTypes.LightBeam.P_Glow)
                    {
                        SpeedMin = 8f,
                        SpeedMax = 10f,
                        LifeMin = 0.4f,
                        LifeMax = 1.1f
                    };
            }

            if (pair != null)
            {
                pair.pair = this;
                ((ISourceScene)scene).SourceScene.QueueSourceEntity(pair);
            }
            else
            {
                this.RemoveSelf();
            }
        }

        public override void Update()
        {
            base.Update();
            object scene = Scene;
            base.Collidable = (enabled = FlagsEnabled(scene as OriginalLevelSceneRoot));
            if (cooldown > 0f)
            {
                cooldown -= engine.Frame.DeltaTime;
            }

            if (particleColor != PackedColor.Transparent &&
                ((ISourceScene)Scene).SourceScene.Clock.OnInterval(0.1f, engine))
            {
                (Scene as OriginalLevelSceneRoot).Particles.Emit(P_Glow, (int)Math.Ceiling((float)length / 24f),
                    particlePosition, particleScope, particleColor * 0.3f, particleDir);
            }

            if (!allActors)
            {
                return;
            }

            bool previousUpdatingActors = updatingActors;
            updatingActors = true;
            try
            {
                foreach (SourceActorEntityState item in CollideActors(previousUpdatingActors))
                {
                    if (item is RoomPlayerSession) continue;
                    SourceActorEntityState val = item;
                    switch (Direction)
                    {
                        case Directions.Right:
                            if (val.Left > this.Left && val.CenterY <= this.Bottom && val.CenterY >= this.Top)
                            {
                                pair.TeleportEntity(val, val.Top - this.Top, Direction);
                            }

                            break;
                        case Directions.Left:
                            if (val.Right < this.Right && val.CenterY <= this.Bottom && val.CenterY >= this.Top)
                            {
                                pair.TeleportEntity(val, this.Bottom - val.Bottom, Direction);
                            }

                            break;
                        case Directions.Up:
                            if (val.Bottom < this.Bottom && val.CenterX <= this.Right && val.CenterX >= this.Left)
                            {
                                pair.TeleportEntity(val, this.Right - val.Right, Direction);
                            }

                            break;
                        case Directions.Down:
                            if (val.Top > this.Top && val.CenterX <= this.Right && val.CenterX >= this.Left)
                            {
                                pair.TeleportEntity(val, val.Left - this.Left, Direction);
                            }

                            break;
                    }
                }
            }
            finally
            {
                updatingActors = previousUpdatingActors;
            }
        }

        protected virtual void OnPlayer(RoomPlayerSession player)
        {
            if (useCount >= numOfUses || !(cooldown <= 0f))
            {
                return;
            }

            useCount++;
            switch (Direction)
            {
                case Directions.Right:
                    if (player.Left > this.Left && player.CenterY <= this.Bottom && player.CenterY >= this.Top)
                    {
                        pair.TeleportPlayer(player, player.Top - this.Top, player.Speed, Direction);
                    }

                    break;
                case Directions.Left:
                    if (player.Right < this.Right && player.CenterY <= this.Bottom && player.CenterY >= this.Top)
                    {
                        pair.TeleportPlayer(player, this.Bottom - player.Bottom, player.Speed, Direction);
                    }

                    break;
                case Directions.Up:
                    if (player.Bottom < this.Bottom && player.CenterX <= this.Right && player.CenterX >= this.Left)
                    {
                        pair.TeleportPlayer(player, this.Right - player.Right, player.Speed, Direction);
                    }

                    break;
                case Directions.Down:
                    if (player.Top > this.Top && player.CenterX <= this.Right && player.CenterX >= this.Left)
                    {
                        pair.TeleportPlayer(player, player.Left - this.Left, player.Speed, Direction);
                    }

                    break;
            }
        }
    }
}
