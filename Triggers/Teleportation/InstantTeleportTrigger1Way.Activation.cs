// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Scheduling;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps;
using Celeste.Simulation;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using LevelData = Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData;
using Player = Celeste.Simulation.RoomPlayerSession;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.VivHelper.Triggers.Teleportation
{
    public partial class InstantTeleportTrigger1Way
    {
        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (targetID.Trim() != "-")
            {
                delayAwakeAction = true;
            }
            else
            {
                DelayedAwakeAction(scene);
            }

            if (((Level)scene).Session.DoNotLoad.Contains(ID))
            {
                Console.WriteLine("ID `" + ID + "` is getting removed");
                this.RemoveSelf();
            }
        }

        public void DelayedAwakeAction(object scene)
        {
            Level val = (Level)(object)((scene is Level) ? scene : null);
            if (val == null)
            {
                return;
            }

            delayAwakeAction = false;
            Session val2 = val?.Session;
            if (transition == TransitionType.Wipe && val.Wipe != null)
            {
                customDelay = Math.Max(0f, customDelay - val.Wipe.Duration);
            }

            if (val2 == null || specificRoom == null)
            {
                return;
            }

            List<LevelData> levels = ((OriginalChapterMapData)val2.MapData).Levels;
            LevelData val3 = levels.FirstOrDefault((LevelData f) => f.Name == specificRoom);
            if (val3 == null)
            {
                return;
            }

            if (val3.Dummy && ignoreIfDummy)
            {
                this.Collidable = false;
            }

            if (specificRoom != null && IsValidRoomName(specificRoom, levels))
            {
                return;
            }

            foreach (LevelData item in levels)
            {
                foreach (EntityData entity in item.Entities)
                {
                    if (TeleportTarget.ValidTeleportTargetNames.Contains(entity.Name) &&
                        entity.Attr("TargetID", (string)null) == targetID)
                    {
                        specificRoom = item.Name;
                        setState = TeleportTarget.ReadNonEmpty(entity, "SetState");
                        return;
                    }
                }
            }

            throw new Exception("There is no TeleportTarget associated with the TargetID \"" + targetID +
                                "\" in any rooms in your map. Please make sure your TeleportTarget has that ID.");
        }

        public override void Update()
        {
            if (delayAwakeAction)
            {
                DelayedAwakeAction(this.Scene);
            }

            base.Update();
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            if (ExitDirection != 0)
            {
                int[] array = new int[4];
                array[0] = ((player.Speed.X < 0f) ? 1 : (-1));
                array[1] = ((player.Speed.Y > 0f) ? 1 : (-1));
                array[2] = -array[0];
                array[3] = -array[2];
                Vector2 val = player.TopRight - player.Speed;
                Vector2 val2 = player.BottomLeft - player.Speed;
                array[0] *= ((this.Left > val.X && this.Left <= player.TopRight.X) ? 1 : 0);
                array[1] *= ((this.Top > val2.Y && this.Top <= player.BottomLeft.Y) ? 1 : 0);
                array[2] *= ((this.Right < val2.X && this.Right >= player.BottomLeft.X) ? 1 : 0);
                array[3] *= ((this.Bottom < val.Y && this.Bottom >= player.TopRight.Y) ? 1 : 0);
                int num = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] > num)
                    {
                        EntrySide = 2 << i;
                        num = array[i];
                    }
                }
            }
            else
            {
                if (context.Session.TeleportState)
                {
                    return;
                }

                if (flagsNeeded != null)
                {
                    object scene = player.Scene;
                    if (!FlagsMatch((Level)(object)((scene is Level) ? scene : null), flagsNeeded, "and"))
                    {
                        return;
                    }
                }

                context.GetHelperEntity(this.Scene)
                    .Add((SourceComponentState)new SimulationCoroutine(engine, TeleportMaster(player), true));
            }
        }

        public override void OnLeave(Player player)
        {
            if (player == null || player.Dead || ExitDirection == 0)
            {
                return;
            }

            base.OnLeave(player);
            if (ExitDirection % 15 == 0)
            {
                if (flagsNeeded != null)
                {
                    object scene = player.Scene;
                    if (!FlagsMatch((Level)(object)((scene is Level) ? scene : null), flagsNeeded, "and"))
                    {
                        goto IL_008d;
                    }
                }

                context.GetHelperEntity(player.Scene)
                    .Add((SourceComponentState)new SimulationCoroutine(engine, TeleportMaster(player), true));
                return;
            }

            goto IL_008d;
            IL_008d:
            int exitSide = 0;
            int[] array = new int[4];
            array[0] = ((!(player.Speed.X > 0f)) ? 1 : (-1));
            array[1] = ((!(player.Speed.Y < 0f)) ? 1 : (-1));
            array[2] = -array[0];
            array[3] = -array[2];
            Vector2 val = player.TopRight - player.Speed;
            Vector2 val2 = player.BottomLeft - player.Speed;
            array[0] *= ((this.Left < val.X && this.Left >= player.TopRight.X) ? 1 : 0);
            array[1] *= ((this.Top < val2.Y && this.Top >= player.BottomLeft.Y) ? 1 : 0);
            array[2] *= ((this.Right > val2.X && this.Right <= player.BottomLeft.X) ? 1 : 0);
            array[3] *= ((this.Bottom > val.Y && this.Bottom <= player.TopRight.Y) ? 1 : 0);
            int num = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] > num)
                {
                    exitSide = 2 << i;
                    num = array[i];
                }
            }

            if (!TriggerCheck(exitSide))
            {
                return;
            }

            if (flagsNeeded != null)
            {
                object scene2 = player.Scene;
                if (!FlagsMatch((Level)(object)((scene2 is Level) ? scene2 : null), flagsNeeded, "and"))
                {
                    return;
                }
            }

            context.GetHelperEntity(player.Scene)
                .Add((SourceComponentState)new SimulationCoroutine(engine, TeleportMaster(player), true));
        }

        private bool TriggerCheck(int ExitSide)
        {
            return ExitDirection switch
            {
                -2 => ExitSide == EntrySide * 4 % 15,
                -1 => ExitSide != EntrySide,
                _ => (ExitDirection & ExitSide) > 0,
            };
        }
    }
}
