// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Events.LevelLoading;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Rendering;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Rendering;
using Celeste.Scene.Scenes;
using Celeste.Simulation;
using Celeste.Simulation.Presentation.Scene.Transitions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.Teleportation
{
    public sealed class VivTeleportHooks : IDisposable
    {
        private readonly VivTeleportContext context;
        private readonly List<IDisposable> hooks = new();

        public VivTeleportHooks(VivTeleportContext context)
        {
            this.context = context;
            try
            {
                hooks.Add(new Hook(
                    typeof(SpotlightWipePresentation).GetConstructor(new[]
                        { typeof(ISourceRendererScene), typeof(bool), typeof(Action) }),
                    (Action<Action<SpotlightWipePresentation, ISourceRendererScene, bool, Action>,
                        SpotlightWipePresentation, ISourceRendererScene, bool, Action>)SpotlightConstructed));
                hooks.Add(new ILHook(
                    typeof(SpotlightWipePresentation).GetMethod(nameof(SpotlightWipePresentation.Render)),
                    SpotlightRender));
                hooks.Add(new Hook(typeof(SourceSceneState).GetMethod(nameof(SourceSceneState.BeforeUpdate)),
                    (Action<Action<SourceSceneState>, SourceSceneState>)BeforeUpdate));
                hooks.Add(new ILHook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.Render)),
                    LevelRender));
                EverestLevelLoaderSourceEvents.OnLoadingThread += context.LoadingThread;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool Owns(OriginalLevelSceneRoot level) =>
            level != null && ReferenceEquals(level.EngineContext, context.Engine);

        private void BeforeUpdate(Action<SourceSceneState> original, SourceSceneState scene)
        {
            original(scene);
            if (scene is not OriginalLevelSceneRoot level || !Owns(level)) return;
            var player = level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>();
            if (player != null && context.Session.TeleportState)
            {
                context.Session.TeleportAction?.Invoke(player, level);
                context.Session.TeleportAction = null;
            }
        }

        private void SpotlightConstructed(
            Action<SpotlightWipePresentation, ISourceRendererScene, bool, Action> original,
            SpotlightWipePresentation wipe, ISourceRendererScene scene, bool wipeIn, Action completed)
        {
            original(wipe, scene, wipeIn, completed);
            if (scene is not OriginalLevelSceneRoot level || !Owns(level) || !context.Session.TeleportState) return;
            wipe.Linear = true;
            wipe.Duration = .5f;
        }

        private void SpotlightRender(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction =>
                        instruction.MatchCallOrCallvirt<SpotlightWipePresentation>("get_FocusPoint"))) return;
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Vector2, object, Vector2>>(ModifyFocusPoint);
        }

        private Vector2 ModifyFocusPoint(Vector2 original, object scene)
        {
            if (scene is not OriginalLevelSceneRoot level || !Owns(level) || !context.Session.TeleportState)
                return original;
            var player = level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>();
            if (player != null) return player.Position - level.Camera.Position - new Vector2(0f, 8f);
            return context.HackedFocusPoint ??
                   ((ISourceScreenWipeEnvironment)context.Engine).ProcessWipe.SpotlightFocusPoint;
        }

        private void LevelRender(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                    instruction => instruction.MatchLdarg(0) &&
                                   instruction.Next.MatchLdfld<OriginalLevelSceneRoot>(
                                       nameof(OriginalLevelSceneRoot.HiresSnow)) &&
                                   instruction.Next.Next.MatchBrfalse(out _))) return;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalLevelSceneRoot>>(RenderStalledWipe);
        }

        private void RenderStalledWipe(OriginalLevelSceneRoot level)
        {
            if (!Owns(level) || !context.Session.StallScreenWipe) return;
            context.Session.StallScreenWipe = false;
            var render = level.SourceRenderContext;
            GameplayRendererState.Begin(render);
            SourcePrimitiveDrawing.Rect((ISourceDrawScope)render, level.Camera.X - 2f, level.Camera.Y - 2f, 1924f,
                1084f, PackedColor.Black);
            GameplayRendererState.End(render);
        }

        public void Dispose()
        {
            EverestLevelLoaderSourceEvents.OnLoadingThread -= context.LoadingThread;
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
