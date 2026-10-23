// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps;
using Celeste.Game.Gameplay.Hazards.Lightning;
using Celeste.Scene.Following;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using LevelData = Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData;
using Player = Celeste.Simulation.RoomPlayerSession;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using SceneState = Celeste.Scene.Scenes.SourceSceneState;
using Color = Celeste.Core.Display.PackedColor;
using EntityID = Celeste.Core.Maps.MapEntityId;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using Tween = Celeste.Core.Components.Animation.SourceTweenState;
using Trigger = Celeste.Simulation.Gameplay.Triggers.Foundation.SourceTriggerEntityState;

namespace Celeste.Mods.VivHelper.Triggers.Teleportation
{
    public partial class InstantTeleportTrigger1Way
    {
        public List<object> PreTeleportFunction(Player player, Level level)
        {
            if (context.DefineSetState(setState, out var output) && player.Machine.State != output)
            {
                int state = player.Machine.State;
                int num = state;
                if (num == 10)
                {
                    player.Speed = new Vector2(player.Speed.X, -140f);
                    player.Data.Motion.AutoJump = true;
                    player.Data.Motion.Normal.VariableJumpSpeed = -140f;
                }
            }

            return new List<object> { player.Machine.State };
        }

        public void Teleport(Player player, Level level)
        {
            Player obj = player;
            if (obj == null || obj.Dead)
            {
                ((Celeste.Everest.Diagnostics.Logging.IEverestLoggerProcessEnvironment)engine).Logger.Log(
                    Celeste.Everest.EverestLogLevel.Info, "VivHelperModule",
                    "Attempted to Teleport but player is either dead or null.");
                ClearTeleportFunction(level);
                return;
            }

            Vector2 val = player.TopLeft - this.TopLeft;
            Session session = level.Session;
            object obj2;
            if (session == null)
            {
                obj2 = null;
            }
            else
            {
                OriginalChapterMapData mapData = (OriginalChapterMapData)session.MapData;
                obj2 = ((mapData != null) ? mapData.Get(specificRoom) : null);
            }

            LevelData val2 = (LevelData)obj2;
            if (setState == null)
            {
                try
                {
                    setState = TeleportTarget.ReadNonEmpty(
                        val2.Triggers.First((EntityData e) =>
                            e.Name == "VivHelper/TeleportTarget" &&
                            TeleportTarget.ReadNonEmpty(e, "TargetID", "") == targetID), "SetState", "-1");
                }
                catch
                {
                    setState = "-1";
                }
            }

            EntityData val3 = val2.Triggers.FirstOrDefault((EntityData e) =>
                e.Name == "VivHelper/TeleportTarget" && e.Attr("TargetID", "") == targetID);
            int facing = player.Data.Facing;
            int dashes = player.Data.Dashes;
            Vector2 zero = Vector2.Zero;
            if (context.MatchDashState(player.Machine.State))
            {
                zero = player.Data.Direction;
            }

            Vector2 val4 = level.Camera.Position - player.CameraTarget;
            if (endCutscene)
            {
                level.EndCutscene();
            }

            Vector2 topLeft = player.TopLeft;
            LeaderState leader = player.Leader;
            foreach (FollowerState follower in leader.Followers)
            {
                if (follower.Entity != null)
                {
                    Entity entity = (Entity)follower.Entity;
                    entity.Position -= topLeft;
                    ((Entity)follower.Entity).AddTag(GlobalTag.Bits);
                    if (!SameEntityID(follower.ParentEntityID, new EntityID("null", -1)))
                    {
                        level.Session.DoNotLoad.Add(follower.ParentEntityID);
                    }
                }
            }

            for (int num = 0; num < leader.PastPoints.Count; num++)
            {
                List<Vector2> pastPoints = leader.PastPoints;
                int index = num;
                pastPoints[index] -= topLeft;
            }

            if (bringHoldableThrough && player.Holding.Holding != null)
            {
                ((Entity)player.Holding.Holding.Entity).AddTag(GlobalTag.Bits);
            }

            int output;
            bool flag = context.DefineSetState(setState, out output) && player.Machine.State != output;
            if (flag)
            {
                int state = player.Machine.State;
                int num2 = state;
                if (num2 == 10)
                {
                    player.Speed = new Vector2(player.Speed.X, -140f);
                    player.Data.Motion.AutoJump = true;
                    player.Data.Motion.Normal.VariableJumpSpeed = -140f;
                }
            }

            player.CleanUpPlayerTriggers();
            if (resetDashes)
            {
                player.RefillDash();
            }

            level.RemoveEntity(player);
            context.RemoveTeleportEntities(level);
            level.Displacement.Clear();
            if (roomName != specificRoom)
            {
                level.ParticlesBG.Clear();
                level.Particles.Clear();
                level.ParticlesFG.Clear();
                Celeste.Simulation.Presentation.Effects.Trails.Lifecycle.TrailManagerState.Clear(engine);
            }

            level.UnloadLevel();
            level.Session.Level = specificRoom;
            bool flag2 = false;
            if (val3 != null)
            {
                Vector2? val5 = val3.FirstNodeNullable((Vector2?)val2.Position);
                if (val5.HasValue)
                {
                    level.Session.RespawnPoint = val5.Value;
                }
                else
                {
                    try
                    {
                        level.Session.RespawnPoint = level.Session.GetSpawnPoint(val2.Position + val3.Position);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        level.Session.RespawnPoint = val2.Position + val3.Position + new Vector2(4f, 8f);
                    }
                }

                flag2 = true;
            }

            level.Session.FirstLevel = false;
            level.Scheduler.QueueSourceEntity(player);
            level.LoadLevel(PlayerIntroType.Transition, false);
            TeleportTarget firstEntity = level.Scheduler.Entities.GetTrackedEntities(typeof(TeleportTarget))
                .Cast<TeleportTarget>().FirstOrDefault(t => t.targetID == targetID);
            if (firstEntity == null)
            {
                throw new Exception(
                    "TeleportTarget not found! This error usually means that no TeleportTarget exists in the room set in your Instant Teleport Trigger.");
            }

            if (!flag2)
            {
                if (firstEntity.forceSpawnpoint.HasValue)
                {
                    level.Session.RespawnPoint = firstEntity.forceSpawnpoint;
                }
                else
                {
                    level.Session.RespawnPoint = level.Session.GetSpawnPoint(firstEntity.Position);
                }
            }

            if (firstEntity.addTriggerOffset)
            {
                player.TopLeft = firstEntity.TopLeft + val;
            }
            else
            {
                player.Center = firstEntity.Center;
                player.Position = Floor(player.Position);
            }

            if (player.CollideCheck("Celeste.Solid"))
            {
                Player obj4 = player;
                obj4.Position = obj4.Position - Vector2.UnitY;
                if (player.CollideCheck("Celeste.Solid"))
                {
                    Player obj5 = player;
                    obj5.Position = obj5.Position + Vector2.UnitY;
                }
            }

            player.Hair.MoveHairBy(player.TopLeft - topLeft);
            if (!resetDashes)
            {
                player.Data.Dashes = dashes;
            }

            if (bringHoldableThrough)
            {
                player.Holding.UpdateCarry();
                Celeste.Simulation.Gameplay.Holdables.HoldableState holding = player.Holding.Holding;
                if (holding != null)
                {
                    Entity entity2 = (Entity)holding.Entity;
                    if (entity2 != null)
                    {
                        entity2.RemoveTag(GlobalTag.Bits);
                    }
                }
            }

            foreach (FollowerState follower2 in leader.Followers)
            {
                if (follower2.Entity != null)
                {
                    Entity entity3 = (Entity)follower2.Entity;
                    entity3.Position += player.TopLeft;
                    ((Entity)follower2.Entity).RemoveTag(GlobalTag.Bits);
                    if (!SameEntityID(follower2.ParentEntityID, new EntityID("null", -1)) &&
                        !level.Session.Keys.Contains(follower2.ParentEntityID))
                    {
                        level.Session.DoNotLoad.Remove(follower2.ParentEntityID);
                    }
                }
            }

            for (int num3 = 0; num3 < leader.PastPoints.Count; num3++)
            {
                List<Vector2> pastPoints = leader.PastPoints;
                int index = num3;
                pastPoints[index] += player.Position;
            }

            leader.TransferFollowers();
            level.WindController.SetPattern(val2.WindPattern);
            level.WindController.SnapWind();
            player.Data.Facing = facing;
            player.Speed = firstEntity.ModifyVelocity(player.Speed);
            player.Data.Direction = firstEntity.ModifyVelocity(player.Data.Direction);
            foreach (Trigger item in from t in level.Scheduler.Entities.GetTrackedEntities(typeof(Trigger))
                         .Cast<Trigger>()
                     where Celeste.Physics.Collision.SourceEntityCollision.Check(player, t) &&
                           t.GetType().Name.IndexOf("camera", StringComparison.OrdinalIgnoreCase) > -1
                     select t)
            {
                Trigger trigger = item;
                player.PlayerTriggers.UpdateContact(player, trigger, true);
            }

            level.Camera.Position = player.CameraTarget + val4;
            Vector2 position = level.Camera.Position;
            if (flag && output == 10)
            {
                player.SummitLaunch(player.X);
            }

            switch (transition)
            {
                case TransitionType.Glitch:
                {
                    GlitchType g = (GlitchType)transitionInfo[1];
                    Tween val6 = Tween.Create(engine.TweenPool, Tween.TweenMode.Oneshot, g.GlitchEaser, g.GlitchLength,
                        false);
                    val6.OnUpdate = delegate(Tween t) { engine.Effects.Glitch = g.GlitchStrength * (1f - t.Eased); };
                    val6.OnComplete = delegate
                    {
                        if (freezeGameOnTeleport)
                        {
                            level.Frozen = false;
                        }
                    };
                    context.GetHelperEntity((SceneState)(object)level).Add((SourceComponentState)(object)val6);
                    if (freezeGameOnTeleport)
                    {
                        level.Frozen = true;
                    }

                    val6.Start();
                    break;
                }
                case TransitionType.Lightning:
                {
                    float num4 = player.CameraTarget.X + 90f;
                    bool[] array = new bool[2]
                    {
                        (bool)transitionInfo[0],
                        (bool)transitionInfo[1]
                    };
                    float[] array2 = (float[])transitionInfo[2];
                    int num5 = (int)transitionInfo[3];
                    float[] array3 = (float[])transitionInfo[4];
                    float num6 = array2[0];
                    if (array[0])
                    {
                        level.Flash(Color.White, false);
                    }

                    if (array[1])
                    {
                        level.Shake(0.3f);
                    }

                    for (int num7 = 0; num7 < num5; num7++)
                    {
                        level.Scheduler.QueueSourceEntity(new LightningStrikeState(
                            new Vector2(Range(num4 + array3[0], num4 + array3[1]), player.CameraTarget.Y - 32f),
                            (10 + 30 * num7) % 63, 255f, engine, num6));
                        num6 += (float)(((Celeste.Core.Randomness.ISourceRandomEnvironment)engine).Random.NextDouble() *
                                        (double)array2[1]);
                    }

                    break;
                }
                case TransitionType.Wipe:
                    context.HackedFocusPoint = player.Position - position - new Vector2(0f, 8f);
                    DoScreenWipe(level, true, (Action)delegate
                    {
                        context.HackedFocusPoint = null;
                        if (freezeGameOnTeleport)
                        {
                            level.Frozen = false;
                        }
                    });
                    if (freezeGameOnTeleport)
                    {
                        level.Frozen = true;
                    }

                    break;
            }

            context.GetHelperEntity((SceneState)(object)level).Add(
                (SourceComponentState)(object)Celeste.Core.Components.Timing.SourceAlarmState.Create(engine.AlarmPool,
                    Celeste.Core.Components.Timing.SourceAlarmState.AlarmMode.Oneshot,
                    (Action)delegate
                    {
                        this.Tags = new Celeste.Scene.Scheduling.SceneTagSet(this.Tags.Bits - GlobalTag.Bits);
                    }, 0.02f, true));
            level.PauseLock = false;
            if (flagsSet != null)
            {
                string[] array4 = flagsSet;
                foreach (string text in array4)
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        level.Session.SetFlag(text, true);
                    }
                }
            }

            ClearTeleportFunction(level);
            RunPersistence(level.Session);
        }
    }
}
