// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Display;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Modules.Updating;
using Celeste.Scene.Scenes;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.Bumpers
{
    public sealed partial class BumperWrapperProcess
    {
        private Hook debugColorPoll;
        private SourceEverestModule tasModule;
        private Func<object, PackedColor> readDebugColor;

        public void LoadContent()
        {
            debugColorPoll?.Dispose();
            debugColorPoll = null;
            tasModule = null;
            readDebugColor = null;
            var dependency = new EverestModuleMetadata { Name = "CelesteTAS", Version = new Version(3, 4, 18) };
            if (!new EverestLoaderProcess(engine).DependencyLoaded(dependency)) return;
            foreach (var module in ((ISourceEverestModulesProcessEnvironment)engine).ModulesProcess.Modules)
                if (module.Metadata.Name == dependency.Name)
                {
                    tasModule = module;
                    break;
                }

            if (tasModule == null) return;
            var getter = tasModule.SettingsType
                .GetProperty("EntityHitboxColor", BindingFlags.Instance | BindingFlags.Public)?.GetGetMethod();
            if (getter != null)
            {
                var method = new DynamicMethod("ReadTasEntityHitboxColor", typeof(PackedColor),
                    new[] { typeof(object) }, getter.DeclaringType, true);
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, getter.DeclaringType);
                il.Emit(OpCodes.Callvirt, getter);
                il.Emit(OpCodes.Ret);
                readDebugColor = (Func<object, PackedColor>)method.CreateDelegate(typeof(Func<object, PackedColor>));
            }

            debugColorPoll = new Hook(typeof(SourceSceneState).GetMethod(nameof(SourceSceneState.BeforeUpdate)),
                (Action<Action<SourceSceneState>, SourceSceneState>)PollDebugColor);
        }

        private void PollDebugColor(Action<SourceSceneState> original, SourceSceneState scene)
        {
            original(scene);
            if (!ReferenceEquals(scene.EngineContext, engine) || !scene.OnRawInterval(scene.Paused ? 3f : 40f)) return;
            try
            {
                context.EntityDebugColor = readDebugColor(tasModule._Settings);
            }
            catch
            {
                context.EntityDebugColor = null;
            }
        }
    }
}
