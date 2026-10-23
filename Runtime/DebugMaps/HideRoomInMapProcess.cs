// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Everest.Editor;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Editor;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Save.Runtime;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Simulation.Runtime.Session;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.DebugMaps
{
    internal sealed class HideRoomInMapProcess : IDisposable
    {
        internal const string Marker = "VivHelper/HideRoomInMap";
        private readonly ISourceEngineContext engine;
        private readonly Hook construction;
        private readonly ILHook keyRendering;

        private static readonly FieldInfo MapData =
            typeof(MapEditorState).GetField("mapData", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo Templates =
            typeof(MapEditorState).GetField("levels", BindingFlags.NonPublic | BindingFlags.Instance);

        public HideRoomInMapProcess(ISourceEngineContext engine)
        {
            this.engine = engine;
            construction = new Hook(
                typeof(EverestMapEditorState).GetConstructor(new[]
                    { typeof(ISourceEngineContext), typeof(OriginalAreaKey), typeof(bool) }),
                (Action<Action<EverestMapEditorState, ISourceEngineContext, OriginalAreaKey, bool>,
                    EverestMapEditorState, ISourceEngineContext, OriginalAreaKey, bool>)Construct);
            keyRendering =
                new ILHook(
                    typeof(EverestMapEditorState).GetMethod("RenderKeys",
                        BindingFlags.NonPublic | BindingFlags.Instance), FilterKeyRooms);
            ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents.OnLoadEntity += ConsumeMarker;
        }

        private bool ConsumeMarker(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            System.Numerics.Vector2 offset, OriginalLevelEntityData data) => data.Name == Marker;

        private void Construct(Action<EverestMapEditorState, ISourceEngineContext, OriginalAreaKey, bool> original,
            EverestMapEditorState self, ISourceEngineContext owner, OriginalAreaKey area, bool reload)
        {
            if (owner != engine)
            {
                original(self, owner, area, reload);
                return;
            }

            var session = (owner.Frame.Scenes.CurrentScene as OriginalLevelSceneRoot)?.Session;
            if (session == null)
            {
                var save = ((IOriginalGameplayProcess)owner).SaveContext.CurrentSave;
                session = save == null
                    ? null
                    : ((IOriginalSaveSessionSelectionEnvironment)owner).SaveSessionSelection
                    .GetCurrentSessionSafe(save);
            }

            if (session != null && ((IOriginalAreaKeyEnvironment)owner).AreaKeys.GetSessionArea(session) != area)
                session = null;
            original(self, owner, area, reload);
            var map = (OriginalChapterMapData)MapData.GetValue(self);
            var templates = (List<LevelTemplateState>)Templates.GetValue(self);
            if (templates == null)
            {
                ((IEverestLoggerProcessEnvironment)owner).Logger.Log("VivHelper", "This shouldn't ever happen, uhoh.");
                return;
            }

            ApplyRooms(map.Levels, templates, session);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void ApplyRooms(List<OriginalLevelRoomData> rooms, List<LevelTemplateState> templates,
            ChapterCompletionSession session)
        {
            foreach (var room in rooms)
            {
                var template = templates.FirstOrDefault(item => item.Name == room.Name);
                if (template == null) continue;
                var marker = room.Entities.FirstOrDefault(item => item.Name == Marker);
                if (marker == null) continue;
                string flag = marker.Attr("Flag", "");
                bool expected = true;
                if (!string.IsNullOrWhiteSpace(flag) && flag[0] == '!')
                {
                    expected = false;
                    flag = flag.Substring(1);
                }

                if (string.IsNullOrWhiteSpace(flag) || session == null || session.GetFlag(flag) == expected)
                {
                    if (!marker.Bool("DummyOnly", false)) templates.Remove(template);
                    else
                    {
                        template.Spawns.Clear();
                        template.Dummy = true;
                    }
                }
            }
        }

        private void FilterKeyRooms(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchLdfld<MapEditorState>("mapData"),
                instruction => instruction.MatchLdfld<OriginalChapterMapData>("Levels"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<List<OriginalLevelRoomData>, EverestMapEditorState, List<OriginalLevelRoomData>>>((
                rooms, editor) => editor.EngineContext == engine ? LimitKeyRendering(rooms) : rooms);
        }

        internal static List<OriginalLevelRoomData> LimitKeyRendering(List<OriginalLevelRoomData> rooms) =>
            new List<OriginalLevelRoomData>(rooms).Where(room => !room.Entities.Any(entity => entity.Name == Marker))
                .ToList();

        public void Dispose()
        {
            ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents.OnLoadEntity -= ConsumeMarker;
            keyRendering.Dispose();
            construction.Dispose();
        }
    }
}
