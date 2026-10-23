// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Game.Editor;
using Celeste.Graphics.Geometry.TwoD.Camera;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.Spawning;

public sealed partial class InterRoomSpawnProcess
{
    private ILHook editorSpawns, editorColors;
    private Func<MapEditorState, List<LevelTemplateState>> getTemplates;

    private void LoadEditorHooks()
    {
        var editor = Expression.Parameter(typeof(MapEditorState), "editor");
        getTemplates = Expression.Lambda<Func<MapEditorState, List<LevelTemplateState>>>(
                Expression.Field(editor,
                    typeof(MapEditorState).GetField("levels", BindingFlags.NonPublic | BindingFlags.Instance)), editor)
            .Compile();
        editorSpawns = new ILHook(typeof(LevelTemplateState).GetMethod(nameof(LevelTemplateState.RenderContents)),
            ModifyEditorSpawns);
        editorColors = new ILHook(typeof(MapEditorState).GetMethod(nameof(MapEditorState.Render)), ModifyEditorColors);
    }

    private void ModifyEditorSpawns(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(MoveType.After,
            i => i.MatchCall<PackedColor>("get_Red") &&
                 i.Next.MatchCall(typeof(SourcePrimitiveDrawing), nameof(SourcePrimitiveDrawing.Rect)));
        cursor.GotoNext(MoveType.Before,
            i => i.MatchCall(typeof(SourcePrimitiveDrawing), nameof(SourcePrimitiveDrawing.Rect)));
        cursor.Remove();
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_3);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld,
            typeof(LevelTemplateState).GetField("EngineContext", BindingFlags.Instance | BindingFlags.NonPublic));
        cursor
            .EmitDelegate<
                Action<ISourceDrawScope, float, float, float, float, PackedColor, LevelTemplateState,
                    List<LevelTemplateState>, ISourceEngineContext>>(DrawSpawn);
    }

    private void DrawSpawn(ISourceDrawScope scope, float x, float y, float width, float height, PackedColor color,
        LevelTemplateState template, List<LevelTemplateState> levels, ISourceEngineContext owner)
    {
        if (ReferenceEquals(owner, engine))
        {
            var cache = getCache();
            if (cache != null && cache.TryGetValue(template.Name, out var info))
            {
                var at = new Vector2(x, y + 1f);
                if (info.InterRoomRespawnSet.TryGetValue(at * 8f, out var target))
                {
                    foreach (var other in levels)
                        if (other.Name == target.roomName)
                        {
                            SourcePrimitiveDrawing.Line(scope, target.position / 8f - Vector2.UnitY, at - Vector2.UnitY,
                                PackedColor.PowderBlue * .5f, ((ISourceVectorEnvironment)engine).VectorArithmetic);
                            break;
                        }

                    return;
                }

                // Native SpawnChecker uses the unscaled editor position for this second check.
                if (info.NoResetRespawns.ContainsKey(at)) return;
            }
        }

        SourcePrimitiveDrawing.Rect(scope, x, y, width, height, color);
    }

    private void ModifyEditorColors(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(MoveType.Before,
            i => i.MatchCallOrCallvirt<LevelTemplateState>(nameof(LevelTemplateState.RenderOutline)));
        cursor.Remove();
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<LevelTemplateState, ISourceRenderContext, CameraState, MapEditorState>>(DrawOutline);
    }

    private void DrawOutline(LevelTemplateState template, ISourceRenderContext context, CameraState camera,
        MapEditorState editor)
    {
        if (ReferenceEquals(editor.EngineContext, engine))
            PostRenderContents(template, getTemplates(editor), (ISourceDrawScope)context, getCache());
        template.RenderOutline(context, camera);
    }

    internal static void PostRenderContents(LevelTemplateState template, List<LevelTemplateState> levels,
        ISourceDrawScope scope, Dictionary<string, LevelInfo> cache)
    {
        if (cache == null || !cache.TryGetValue(template.Name, out var info) || template.Spawns == null) return;
        foreach (var spawn in template.Spawns)
        {
            var at = new Vector2(template.X + spawn.X, template.Y + spawn.Y);
            if (info.NoResetRespawns.ContainsKey(at * 8f))
                SourcePrimitiveDrawing.Rect(scope, at.X, at.Y - 1f, 1f, 1f, PackedColor.FromHex("ae8cfc"));
            else if (info.InterRoomRespawnSet.TryGetValue(at * 8f, out var target))
                foreach (var other in levels)
                    if (other.Name == target.roomName)
                    {
                        SourcePrimitiveDrawing.Rect(scope, at.X, at.Y - 1f, 1f, 1f, PackedColor.Orange);
                        break;
                    }
        }
    }

    private void UnloadEditorHooks()
    {
        editorColors.Dispose();
        editorSpawns.Dispose();
    }
}
