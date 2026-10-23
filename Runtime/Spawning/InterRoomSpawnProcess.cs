// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Events.LevelLoading;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation.Runtime.Session;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.Spawning;

public sealed partial class InterRoomSpawnProcess : IDisposable
{
    private readonly ISourceEngineContext engine;
    private readonly Func<Dictionary<string, LevelInfo>> getCache;
    private readonly ILHook reload;

    public InterRoomSpawnProcess(ISourceEngineContext engine, Func<Dictionary<string, LevelInfo>> getCache)
    {
        this.engine = engine;
        this.getCache = getCache;
        reload = new ILHook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.Reload)),
            ModifyReload);
        LoadEditorHooks();
        EverestLevelLoaderSourceEvents.OnLoadingThread += Loading;
    }

    private void Loading(OriginalLevelSceneRoot level)
    {
        if (!ReferenceEquals(level.EngineContext, engine)) return;
        BuildCache(((OriginalChapterMapData)level.Session.MapData).Levels, getCache());
    }

    public static void BuildCache(List<OriginalLevelRoomData> rooms, Dictionary<string, LevelInfo> cache)
    {
        List<OriginalLevelEntityData> spawners = new();
        Dictionary<int, (string Room, Vector2 Position)> targets = new();
        foreach (var room in rooms)
        {
            LevelInfo info = new();
            foreach (var data in room.Entities)
            {
                if (data.Name == "VivHelper/Spawnpoint")
                {
                    if (data.Bool("NoResetRespawn", false))
                        info.NoResetRespawns.Add(room.Position + data.Position,
                            new NoResetRespawnData
                            {
                                NoResetOnRetry = data.Bool("NoResetOnRetry", false), Flag = NoEmptyString(data, "Flag")
                            });
                }
                else if (data.Name == "VivHelper/InterRoomSpawner") spawners.Add(data);
                else if (data.Name is "VivHelper/InterRoomSpawnTarget" or "VivHelper/InterRoomSpawnTarget2")
                {
                    int tag = data.Int("tag", 0);
                    if (tag != 0 && !targets.ContainsKey(tag))
                        targets[tag] = (room.Name, room.Position + data.Position);
                }
            }

            cache[room.Name] = info;
        }

        foreach (var data in spawners)
        {
            LevelInfo info = new(cache[data.Level.Name]);
            int tag = data.Int("tag", 0);
            if (targets.TryGetValue(tag, out var target))
            {
                info.InterRoomRespawnSet.Add(data.Position + data.Level.Position,
                    new InterRoomRespawnData
                    {
                        roomName = target.Room, position = target.Position,
                        Flag = NoEmptyString(data, "Flags") ?? NoEmptyString(data, "Flag")
                    });
                cache[data.Level.Name] = info;
            }
        }
    }

    private static string NoEmptyString(OriginalLevelEntityData data, string key)
    {
        string value = data.Attr(key, null);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private void ModifyReload(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(MoveType.Before,
            i => i.MatchCallOrCallvirt<OriginalLevelSceneRoot>(nameof(OriginalLevelSceneRoot.LoadLevel)));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdarg(0));
        cursor.EmitDelegate<Func<OriginalLevelSceneRoot, OriginalLevelSceneRoot>>(ModifyRoom);
    }

    private OriginalLevelSceneRoot ModifyRoom(OriginalLevelSceneRoot level)
    {
        if (ReferenceEquals(level.EngineContext, engine)) Apply(level.Session, getCache());
        return level;
    }

    public static void Apply(ChapterCompletionSession session, Dictionary<string, LevelInfo> cache)
    {
        string room = session.Level;
        Vector2 point = session.RespawnPoint ?? ((OriginalLevelRoomData)session.LevelData).Spawns[0];
        if (cache.TryGetValue(room, out var info) && info.InterRoomRespawnSet.TryGetValue(point, out var target))
            if (string.IsNullOrWhiteSpace(target.Flag) || session.GetFlag(target.Flag))
            {
                session.Level = target.roomName;
                session.RespawnPoint = target.position;
            }
    }

    public void Dispose()
    {
        EverestLevelLoaderSourceEvents.OnLoadingThread -= Loading;
        UnloadEditorHooks();
        reload.Dispose();
    }
}
