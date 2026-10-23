// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Geometry;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using Player = Celeste.Simulation.RoomPlayerSession;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.VivHelper.Triggers.Teleportation
{
    public partial class InstantTeleportTrigger1Way
    {
        private void RunPersistence(Session session)
        {
            switch (persistence)
            {
                case TriggerPersistence.OncePerMapPlay:
                    session.DoNotLoad.Add(ID);
                    break;
                case TriggerPersistence.OncePerRetry:
                    this.RemoveSelf();
                    break;
            }
        }

        private IEnumerator ErrorRoutine(Level level, string roomName)
        {
            yield return null;
            ((Celeste.Game.Execution.Processes.IOriginalGameplayProcess)engine).AudioSession.SetMusic(null, true, true);
            ((Celeste.Game.Chapters.Flow.IOriginalLevelEnterStaticEnvironment)engine).LevelEnterStatics.ErrorMessage =
                "Failed to enter " + roomName + " because there was no valid TeleportTarget in that room.";
            var keys = ((Celeste.Game.Execution.Areas.IOriginalAreaKeyEnvironment)engine).AreaKeys;
            var area = level.Session != null ? keys.GetSessionArea(level.Session) : keys.SetSID(keys.Create(1, 0), "");
            var session =
                ((Celeste.Game.Execution.Session.IOriginalSessionEnvironment)engine).Sessions.CreateSession(area, null,
                    null);
            Celeste.Game.Chapters.Flow.LevelEnterSequence.Go(session, false,
                (Celeste.Game.Chapters.Flow.IOriginalLevelEnterProcess)engine);
        }

        private IEnumerator LockRoom(Player player, IntRect bounds)
        {
            if (player == null || player.Dead)
            {
                yield break;
            }

            while (preActive)
            {
                if (player.Left < (float)bounds.Left + 1f)
                {
                    player.Left = bounds.Left;
                    player.OnBoundsH();
                }
                else if (player.Right > (float)bounds.Right - 1f)
                {
                    player.Right = bounds.Right;
                    player.OnBoundsH();
                }
                else if (player.Top < (float)bounds.Top + 1f)
                {
                    player.Top = bounds.Top;
                    player.OnBoundsV();
                }
                else if (player.Bottom > (float)bounds.Bottom - 1f)
                {
                    if (((Celeste.Game.Execution.Processes.IOriginalGameplayProcess)engine).SaveContext.CurrentSave
                        .Assists.Invincible)
                    {
                        player.Play("event:/game/general/assist_screenbottom", (string)null, 0f);
                        player.Bounce((float)bounds.Bottom);
                    }
                    else if (player != null && !player.Dead && player.Bottom < (float)bounds.Bottom)
                    {
                        player.Die(Vector2.Zero, false, true);
                    }
                }

                yield return null;
            }
        }

        private void LockRoom(IntRect bounds, Player player)
        {
            if (player == null || player.Dead)
            {
                object obj = ((player != null) ? player.Scene : null);
                ClearTeleportFunction((Level)(object)((obj is Level) ? obj : null));
            }
            else if (player.Left < (float)bounds.Left + 1f)
            {
                player.Left = bounds.Left;
                player.OnBoundsH();
            }
            else if (player.Right > (float)bounds.Right - 1f)
            {
                player.Right = bounds.Right;
                player.OnBoundsH();
            }
            else if (player.Top < (float)bounds.Top + 1f)
            {
                player.Top = bounds.Top;
                player.OnBoundsV();
            }
            else if (player.Bottom > (float)bounds.Bottom - 1f)
            {
                if (((Celeste.Game.Execution.Processes.IOriginalGameplayProcess)engine).SaveContext.CurrentSave.Assists
                    .Invincible)
                {
                    player.Play("event:/game/general/assist_screenbottom", (string)null, 0f);
                    player.Bounce((float)bounds.Bottom);
                }
                else if (player != null && !player.Dead && player.Bottom < (float)bounds.Bottom)
                {
                    player.Die(Vector2.Zero, false, true);
                }
            }
        }
    }
}
