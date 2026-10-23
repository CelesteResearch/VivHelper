// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Scheduling;
using System.Collections;
using Celeste.Core.Components;
using Celeste.Simulation;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using Player = Celeste.Simulation.RoomPlayerSession;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using SceneState = Celeste.Scene.Scenes.SourceSceneState;
using Color = Celeste.Core.Display.PackedColor;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using Tween = Celeste.Core.Components.Animation.SourceTweenState;

namespace Celeste.Mods.VivHelper.Triggers.Teleportation
{
    public partial class InstantTeleportTrigger1Way
    {
        private void ClearTeleportFunction(Level level)
        {
            object obj = level;
            if (obj == null)
            {
                object scene = this.Scene;
                obj = ((scene is Level) ? scene : null);
                if (obj == null)
                {
                    object scene2 = ((Entity)context.HelperEntity).Scene;
                    obj = ((scene2 is Level) ? scene2 : null);
                    if (obj == null)
                    {
                        object scene3 = engine.Frame.Scenes.CurrentScene;
                        obj = ((scene3 is Level) ? scene3 : null);
                    }
                }
            }

            Level val = (Level)obj;
            val.PauseLock = false;
            context.Session.TeleportState = false;
            context.Session.TeleportAction = null;
        }

        public IEnumerator TeleportMaster(Player player)
        {
            Console.WriteLine(context.Session.TeleportState);
            object scene = player.Scene;
            Level level = (Level)(object)((scene is Level) ? scene : null);
            int num;
            if (!context.Session.TeleportState && level != null)
            {
                _ = level.Bounds;
                num = ((context.Session.TeleportAction != null) ? 1 : 0);
            }
            else
            {
                num = 1;
            }

            if (num != 0)
            {
                yield break;
            }

            if (customDelay > 0f)
            {
                yield return customDelay;
            }

            Player obj = player;
            if (obj == null || obj.Dead)
            {
                ClearTeleportFunction(level);
                yield break;
            }

            preActive = true;
            context.GetHelperEntity((SceneState)(object)level)
                .Add((SourceComponentState)new SimulationCoroutine(engine, LockRoom(player, level.Bounds), true));
            context.Session.TeleportState = true;
            switch (transition)
            {
                case TransitionType.ColorFlash:
                    level.Flash((Color)transitionInfo[0], false);
                    PreTeleport(player, level);
                    break;
                case TransitionType.Glitch:
                {
                    GlitchType g = (GlitchType)transitionInfo[0];
                    Tween tween1 = Tween.Create(engine.TweenPool, Tween.TweenMode.Oneshot, g.GlitchEaser,
                        g.GlitchLength, false);
                    tween1.OnUpdate = delegate(Tween t) { engine.Effects.Glitch = g.GlitchStrength * t.Eased; };
                    tween1.OnComplete = delegate
                    {
                        if (freezeGameOnTeleport)
                        {
                            level.Frozen = false;
                        }

                        PreTeleport(player, level);
                    };
                    context.GetHelperEntity((SceneState)(object)level).Add((SourceComponentState)(object)tween1);
                    if (freezeGameOnTeleport)
                    {
                        level.Frozen = true;
                    }

                    tween1.Start();
                    break;
                }
                case TransitionType.Wipe:
                    if ((bool)transitionInfo[0])
                    {
                        DoScreenWipe(level, false, (Action)delegate
                        {
                            if (freezeGameOnTeleport)
                            {
                                level.Frozen = false;
                            }

                            context.Session.StallScreenWipe = true;
                            PreTeleport(player, level);
                        });
                        if (freezeGameOnTeleport)
                        {
                            level.Frozen = true;
                        }
                    }
                    else
                    {
                        PreTeleport(player, level);
                    }

                    break;
                default:
                    PreTeleport(player, level);
                    break;
            }

            level.Paused = false;
            level.PauseLock = true;
        }

        private void PreTeleport(Player player, Level level)
        {
            preActive = false;
            if (player == null || player.Dead)
            {
                ClearTeleportFunction(level);
                return;
            }

            context.Session.TeleportAction = Teleport;
            level.PauseLock = true;
        }
    }
}
