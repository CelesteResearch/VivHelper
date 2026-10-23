// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Engine.Actions;
using Celeste.Mods.VivHelper.Entities.Hazards.Bumpers;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mods.VivHelper.Runtime.Bumpers
{
    public sealed partial class BumperWrapperProcess : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly BumperWrapperContext context;
        private readonly ILHook freeze;
        private readonly Hook debug;

        public BumperWrapperProcess(ISourceEngineContext engine)
        {
            this.engine = engine;
            context = BumperWrapperContext.For(engine);
            // The original After={"*"} runs after every On hook. A body prefix retains
            // that position without relying on priority ordering, which changed in MonoMod 25.
            freeze = new ILHook(typeof(OriginalCelesteFreeze).GetMethod(nameof(OriginalCelesteFreeze.Freeze)), il =>
            {
                var cursor = new ILCursor(il);
                var originalBody = cursor.DefineLabel();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<ISourceEngineContext, bool>>(SuppressFreeze);
                cursor.Emit(OpCodes.Brfalse, originalBody);
                cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(originalBody);
            });
            try
            {
                debug = new Hook(typeof(SceneEntityState).GetMethod(nameof(SceneEntityState.DebugRender)),
                    (Action<Action<SceneEntityState, ISourceRenderContext, object>, SceneEntityState,
                        ISourceRenderContext, object>)DebugRender);
            }
            catch
            {
                freeze.Dispose();
                throw;
            }
        }

        private bool SuppressFreeze(ISourceEngineContext owner)
        {
            if (!ReferenceEquals(owner, engine) || !context.DisableFreeze) return false;
            context.DetectFreeze = true;
            return true;
        }

        private void DebugRender(Action<SceneEntityState, ISourceRenderContext, object> original,
            SceneEntityState entity, ISourceRenderContext render, object camera)
        {
            if (entity.Scene is ISourceEngineContextScene scene && ReferenceEquals(scene.EngineContext, engine))
            {
                var modifier = entity.Get<BumperWrapperDebugRenderModifier>();
                if (modifier != null && modifier.RenderNewDebug(render, camera)) return;
            }

            original(entity, render, camera);
        }

        public void Dispose()
        {
            debugColorPoll?.Dispose();
            debug.Dispose();
            freeze.Dispose();
            context.Reset();
        }
    }
}
