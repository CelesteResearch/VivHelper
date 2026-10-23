using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Hazards.Lightning;
using Celeste.Scene.Entities;
using Celeste.Scene.Following;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Player.Intro;
using LevelData = Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData;
using Color = Celeste.Core.Display.PackedColor;

namespace Celeste.Mods.VivHelper.Triggers
{
    public partial class InstantTeleportTrigger
    {
        private void Teleport(RoomPlayerSession player)
        {
            triggered = true;
            float temp4 = ((transition == TransitionType.GlitchEffect) ? 0.5f : 0f);
            level.Scheduler.OnEndOfFrame += delegate
            {
                SourceTweenState val =
                    SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Oneshot, null, 0.3f, true);
                val.OnUpdate = delegate(SourceTweenState t) { engine.Effects.Glitch = temp4 * t.Eased; };
                player.OwnedComponents.Add(val);
                LevelData val2 = ((Celeste.Game.Content.Maps.OriginalChapterMapData)level.Session.MapData).Get(newRoom);
                int facing = player.Data.Facing;
                Vector2 levelOffset = level.LevelOffset;
                Vector2[] array = new Vector2[5]
                {
                    player.Position - levelOffset,
                    level.Camera.Position - levelOffset,
                    (player.Position.X < this.Position.X)
                        ? (player.Position - this.Position + new Vector2(8f, 0f))
                        : (player.Position - this.Position),
                    level.CameraOffset,
                    level.Camera.Position - player.CameraTarget
                };
                Tuple<Vector2, Vector2, bool, bool> c = new Tuple<Vector2, Vector2, bool, bool>(player.CameraAnchor,
                    player.CameraAnchorLerp, player.CameraAnchorIgnoreX, player.CameraAnchorIgnoreY);
                LeaderState leader = player.Leader;
                Dictionary<SceneEntityState, Vector2> dictionary = new Dictionary<SceneEntityState, Vector2>();
                foreach (FollowerState follower in leader.Followers)
                {
                    if (((SceneEntityState)follower.Entity) != null)
                    {
                        if (wackyFollowerOverride)
                        {
                            dictionary[((SceneEntityState)follower.Entity)] =
                                ((SceneEntityState)follower.Entity).Position - player.TopLeft;
                        }
                        else
                        {
                            SceneEntityState entity = ((SceneEntityState)follower.Entity);
                            entity.Position -= player.TopLeft;
                        }

                        ((SceneEntityState)follower.Entity).Tags |= GlobalTag;
                        if (!(follower.ParentEntityID == new MapEntityId("null", -1)))
                        {
                            level.Session.DoNotLoad.Add(follower.ParentEntityID);
                        }
                    }
                }

                for (int num = 0; num < leader.PastPoints.Count; num++)
                {
                    List<Vector2> pastPoints = leader.PastPoints;
                    int index = num;
                    pastPoints[index] -= player.Position;
                }

                if (bringHoldableThrough && player.Holding.Holding != null)
                {
                    ((SceneEntityState)player.Holding.Holding.Entity).Tags |= GlobalTag;
                }

                int state = player.Machine.State;
                player.CleanUpPlayerTriggers();
                player.CameraAnchorIgnoreX = true;
                player.CameraAnchorIgnoreY = true;
                int dashes = 0;
                Vector2 val3 = Vector2.Zero;
                if (!resetDashes)
                {
                    dashes = player.Data.Dashes;
                }

                if (MatchDashState(player.Machine.State))
                {
                    val3 = player.Data.Direction;
                }

                List<SourceComponentState> transitionOut = new List<SourceComponentState>();
                List<SourceComponentState> list = new List<SourceComponentState>();
                if (TransitionListeners && newRoom != currentRoom &&
                    level.Scheduler.Components.Components.TryGetValue("Celeste.TransitionListener", out var value))
                {
                    transitionOut = new List<SourceComponentState>(value);
                }

                level.RemoveEntity(player);
                RemoveTeleportEntities();
                level.UnloadLevel();
                level.Session.Level = newRoom;
                level.Session.Dreaming = dreaming;
                Vector2 val4;
                if (!(newPos.X >= 0f) ||
                    !(newPos.X <= (float)(level.Bounds.X + level.Bounds.Width) - level.LevelOffset.X) ||
                    !(newPos.Y >= 0f) ||
                    !(newPos.Y <= (float)(level.Bounds.Y + level.Bounds.Height) - level.LevelOffset.Y))
                {
                    val4 = ((!(array[0].X >= 0f) ||
                             !(array[0].X <= (float)(level.Bounds.X + level.Bounds.Width) - level.LevelOffset.X) ||
                             !(array[0].Y >= 0f) ||
                             !(array[0].Y <= (float)(level.Bounds.Y + level.Bounds.Height) - level.LevelOffset.Y))
                        ? (level.LevelOffset + new Vector2(1f, 1f))
                        : (level.LevelOffset + array[0]));
                }
                else
                {
                    val4 = level.LevelOffset + newPos;
                    if (addTriggerOffset)
                    {
                        val4 += array[2];
                    }
                }

                if (val2.Spawns.Count == 0)
                {
                    level.Session.RespawnPoint = val4;
                }
                else
                {
                    level.Session.RespawnPoint = level.GetSpawnPoint(val4);
                }

                level.Session.FirstLevel = false;
                level.Scheduler.QueueSourceEntity(player);
                level.LoadLevel(PlayerIntroType.Transition, false);
                if (!resetDashes)
                {
                    player.Data.Dashes = dashes;
                }

                Vector2 val5 = val4 - player.Position;
                player.Position = val4;
                player.Hair.MoveHairBy(val5);
                if (bringHoldableThrough)
                {
                    player.Holding.UpdateCarry();
                    HoldableState holding = player.Holding.Holding;
                    if (holding != null)
                    {
                        SceneEntityState entity2 = (SceneEntityState)holding.Entity;
                        if (entity2 != null)
                        {
                            entity2.Tags &= ~GlobalTag;
                        }
                    }
                }

                ModifySpeed(player);
                if (legacyCamera == 2 || legacyCamera == 4)
                {
                    NewCamera(level, player, array, c);
                }
                else
                {
                    CameraStuff(level, player, level.LevelOffset + array[1]);
                }

                if (forceNormalState)
                {
                    player.Machine.State = 0;
                }
                else
                {
                    if (MatchDashState(player.Machine.State))
                    {
                        player.Data.Direction = VectorArithmetic.RotateTowards(val3,
                            Vector2Geometry.Angle(player.Data.Motion.Normal.Speed), 6.3f);
                    }

                    if (state == 10)
                    {
                        player.SummitLaunch(player.Position.X);
                    }
                }

                for (int num2 = 0; num2 < leader.PastPoints.Count; num2++)
                {
                    List<Vector2> pastPoints = leader.PastPoints;
                    int index = num2;
                    pastPoints[index] += player.Position;
                }

                foreach (FollowerState follower2 in leader.Followers)
                {
                    if (((SceneEntityState)follower2.Entity) != null)
                    {
                        if (wackyFollowerOverride)
                        {
                            ((SceneEntityState)follower2.Entity).Position =
                                player.TopLeft + dictionary[((SceneEntityState)follower2.Entity)];
                        }
                        else
                        {
                            SceneEntityState entity3 = ((SceneEntityState)follower2.Entity);
                            entity3.Position += player.TopLeft;
                        }

                        ((SceneEntityState)follower2.Entity).Tags &= ~GlobalTag;
                        level.Session.DoNotLoad.Remove(follower2.ParentEntityID);
                    }
                }

                if (TransitionListeners && newRoom != currentRoom &&
                    level.Scheduler.Components.Components.TryGetValue("Celeste.TransitionListener", out var value2))
                {
                    list = new List<SourceComponentState>(value2);
                    list.RemoveAll((SourceComponentState item) => transitionOut.Contains(item));
                    foreach (RoomTransitionListener item in transitionOut)
                    {
                        RoomTransitionListener val6 = item;
                        val6?.OnOutBegin?.Invoke();
                        val6?.OnOut?.Invoke(1f);
                    }

                    foreach (RoomTransitionListener item2 in list)
                    {
                        RoomTransitionListener val7 = item2;
                        val7?.OnInBegin?.Invoke();
                        val7?.OnIn?.Invoke(1f);
                        val7?.OnInEnd?.Invoke();
                    }
                }

                ResetLavaPosition();
                leader.TransferFollowers();
                player.Data.Facing = facing;
                player.Hair.MoveHairBy(level.LevelOffset - levelOffset);
                SourceTweenState val8 =
                    SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Oneshot, null, 0.3f, true);
                val8.OnUpdate = delegate(SourceTweenState t) { engine.Effects.Glitch = temp4 * (1f - t.Eased); };
                player.OwnedComponents.Add(val8);
                if (transition == TransitionType.ColorFlash)
                {
                    level.Flash(flashColor, false);
                }
                else if (transition == TransitionType.Lightning)
                {
                    level.Flash(Color.White, false);
                    level.Shake(0.3f);
                    level.Scheduler.QueueSourceEntity(new LightningStrikeState(
                        new Vector2(player.X + 60f, player.Y - 180f),
                        10, 200f, engine, 0f));
                    level.Scheduler.QueueSourceEntity(
                        new LightningStrikeState(new Vector2(player.X + 220f, player.Y - 180f), 40, 200f, engine,
                            0.25f));
                    ((Celeste.Core.Audio.ISourceAudioEnvironment)engine).SourceAudio.Play(
                        "event:/new_content/game/10_farewell/lightning_strike");
                }

                if (timeSlowDown != 0f)
                {
                    OwnedComponents.Add(new SimulationCoroutine(engine, TimeSlowDown(timeSlowDown, 0.1f)));
                }
            };
        }
    }
}
