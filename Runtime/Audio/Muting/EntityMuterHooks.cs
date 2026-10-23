using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Audio.Fmod.Events;
using Celeste.Audio.Fmod.Interop;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Audio.Runtime;
using Celeste.Game.Gameplay.Interaction.Springs;
using Celeste.Game.Save.Runtime;
using Celeste.Mods.VivHelper.Components.Audio;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling;
using Celeste.Simulation.Audio.Sources;
using Mono.Cecil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using OpCode = Mono.Cecil.Cil.OpCode;

namespace Celeste.Mods.VivHelper.Runtime.Audio.Muting
{
    public sealed class EntityMuterHooks : IDisposable
    {
        private delegate FmodEventReference CreateOriginal(OriginalGameAudioSession self, string path,
            Vector2? position, IOriginalGameSaveContext saveContext);

        private delegate void SoundCreateOriginal(OriginalGameAudioSession self, string path,
            ref ISourceAudioEventReference instance, ref bool is3D, ref bool isOneshot);

        private delegate void SoundUpdateOriginal(SoundSourceComponentState self);

        private delegate FmodResult GetVolumeDelegate(IntPtr instance, out float volume, out float finalVolume);

        private static readonly Func<SoundSourceComponentState, ISourceEngineContext> SoundEngine =
            ReadField<ISourceEngineContext>("engine");

        private static readonly Func<SoundSourceComponentState, ISourceAudioEventReference> SoundInstance =
            ReadField<ISourceAudioEventReference>("instance");

        private static readonly GetVolumeDelegate GetVolume = (GetVolumeDelegate)Delegate.CreateDelegate(
            typeof(GetVolumeDelegate),
            typeof(FmodEventReference).Assembly.GetType("Celeste.Audio.Fmod.Interop.NativeMethods")
                .GetMethod("FMOD_Studio_EventInstance_GetVolume", BindingFlags.Static | BindingFlags.NonPublic));

        private readonly EntityMuterContext context;
        private readonly List<IDisposable> hooks = new();

        public EntityMuterHooks(EntityMuterContext context)
        {
            this.context = context;
            try
            {
                hooks.Add(new Hook(
                    typeof(SoundSourceComponentState).GetMethod(nameof(SoundSourceComponentState.Update)),
                    (Action<SoundUpdateOriginal, SoundSourceComponentState>)SoundUpdate));
                hooks.Add(new Hook(
                    typeof(OriginalGameAudioSession).GetMethod(nameof(OriginalGameAudioSession.CreateInstance)),
                    (Func<CreateOriginal, OriginalGameAudioSession, string, Vector2?, IOriginalGameSaveContext,
                        FmodEventReference>)CreateInstance));
                hooks.Add(new Hook(SoundCreateMethod(), (SoundCreateHook)CreateSoundInstance));
                HookMethodInfoWithAudioMute(
                    typeof(OriginalSpringState).GetMethod("BounceAnimate",
                        BindingFlags.Instance | BindingFlags.NonPublic), false, null);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static MethodInfo SoundCreateMethod()
        {
            var map = typeof(OriginalGameAudioSession).GetInterfaceMap(typeof(ISourceSoundSourceAudio));
            for (int i = 0; i < map.InterfaceMethods.Length; i++)
                if (map.InterfaceMethods[i].Name == nameof(ISourceSoundSourceAudio.CreateInstance))
                    return map.TargetMethods[i];
            throw new MissingMethodException(nameof(ISourceSoundSourceAudio.CreateInstance));
        }

        private delegate void SoundCreateHook(SoundCreateOriginal original, OriginalGameAudioSession self, string path,
            ref ISourceAudioEventReference instance, ref bool is3D, ref bool isOneshot);

        private FmodEventReference CreateInstance(CreateOriginal original, OriginalGameAudioSession self, string path,
            Vector2? position, IOriginalGameSaveContext saveContext)
        {
            if (!ReferenceEquals(self, context.Audio)) return original(self, path, position, saveContext);
            string resolved = context.ResolvePath(path, out var changed);
            var instance = original(self, resolved, position, saveContext);
            changed?.SetParamsToEvent(instance, context.Engine);
            return instance;
        }

        private void CreateSoundInstance(SoundCreateOriginal original, OriginalGameAudioSession self, string path,
            ref ISourceAudioEventReference instance, ref bool is3D, ref bool isOneshot)
        {
            if (!ReferenceEquals(self, context.Audio))
            {
                original(self, path, ref instance, ref is3D, ref isOneshot);
                return;
            }

            string resolved = context.ResolvePath(path, out var changed);
            original(self, resolved, ref instance, ref is3D, ref isOneshot);
            changed?.SetParamsToEvent(instance, context.Engine);
        }

        private void SoundUpdate(SoundUpdateOriginal original, SoundSourceComponentState self)
        {
            if (ReferenceEquals(SoundEngine(self), context.Engine))
            {
                if (ReferenceEquals(self.Entity, context.ObjPlayingAudio))
                {
                    if (self.Playing && SoundInstance(self) is FmodEventReference instance &&
                        GetVolume(instance.GetRaw(), out _, out float finalVolume) == FmodResult.OK &&
                        finalVolume != 0f)
                        instance.SetVolume(0f);
                }
                else if (!self.Playing && SoundInstance(self) is FmodEventReference instance &&
                         GetVolume(instance.GetRaw(), out _, out float finalVolume) == FmodResult.OK &&
                         finalVolume != 1f)
                    instance.SetVolume(1f);
            }

            original(self);
        }

        private static Func<SoundSourceComponentState, T> ReadField<T>(string name)
        {
            var method = new DynamicMethod("ReadSoundSource" + name, typeof(T),
                new[] { typeof(SoundSourceComponentState) }, typeof(SoundSourceComponentState), true);
            var il = method.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Ldfld,
                typeof(SoundSourceComponentState).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            return (Func<SoundSourceComponentState, T>)method.CreateDelegate(
                typeof(Func<SoundSourceComponentState, T>));
        }

        private bool Owns(object caller) => caller is ISourceEntityScene entity &&
                                            entity.Scene is ISourceEngineContextScene scene &&
                                            ReferenceEquals(scene.EngineContext, context.Engine);

        private bool BeginAudio(object caller)
        {
            bool owned = Owns(caller);
            if (owned) context.ObjPlayingAudio = caller;
            return owned;
        }

        private void EndAudio(bool owned)
        {
            if (owned) context.ObjPlayingAudio = null;
        }

        public void HookMethodInfoWithAudioMute(MethodInfo info, bool isCoroutine, Tuple<OpCode, object>[] instructions)
        {
            MethodInfo method = info;
            if (instructions == null || instructions.Length == 0)
            {
                if (isCoroutine)
                {
                    method = info.GetStateMachineTarget();
                    instructions = new[]
                    {
                        Tuple.Create(OpCodes.Ldarg_0, (object)null),
                        Tuple.Create(OpCodes.Ldfld, (object)method.DeclaringType.GetField("<>4__this"))
                    };
                }
                else instructions = new[] { Tuple.Create(OpCodes.Ldarg_0, (object)null) };
            }

            // The published VivHelper hook targets info even when its caller instructions were obtained from the iterator.
            hooks.Add(new ILHook(info, il => InjectAudio(il, instructions)));
        }

        private void InjectAudio(ILContext il, Tuple<OpCode, object>[] instructions)
        {
            var cursor = new ILCursor(il);
            var owned = new Mono.Cecil.Cil.VariableDefinition(il.Import(typeof(bool)));
            il.Body.Variables.Add(owned);
            while (cursor.TryGotoNext(i => (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
                                           i.Operand is MethodReference method &&
                                           method.DeclaringType.FullName == typeof(OriginalGameAudioSession).FullName &&
                                           method.ReturnType.FullName == typeof(FmodEventReference).FullName &&
                                           !method.Name.StartsWith("get_")))
            {
                EmitCaller(cursor, instructions);
                cursor.EmitDelegate<Func<object, bool>>(BeginAudio);
                cursor.Emit(OpCodes.Stloc, owned);
                cursor.Index++;
                cursor.Emit(OpCodes.Ldloc, owned);
                cursor.EmitDelegate<Action<bool>>(EndAudio);
            }
        }

        private static void EmitCaller(ILCursor cursor, Tuple<OpCode, object>[] instructions)
        {
            foreach (var instruction in instructions)
                if (instruction.Item2 == null) cursor.Emit(instruction.Item1);
                else cursor.Emit(instruction.Item1, instruction.Item2);
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
