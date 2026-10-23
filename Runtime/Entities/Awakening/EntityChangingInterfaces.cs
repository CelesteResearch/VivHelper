using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Scene.Scheduling;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.Entities.Awakening
{
    public sealed class EntityChangingInterfaces : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly ILHook hook;

        public EntityChangingInterfaces(ISourceEngineContext engine)
        {
            this.engine = engine;
            hook = new ILHook(
                typeof(SceneUpdateScheduler).GetMethod(nameof(SceneUpdateScheduler.CommitQueuedEntityLists)), Inject);
        }

        private void Inject(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, i => i.MatchLdfld<SceneUpdateScheduler>("adding"),
                i => i.MatchCallvirt<HashSet<object>>(nameof(HashSet<object>.Clear)));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld,
                typeof(SceneUpdateScheduler).GetField("toAwake", BindingFlags.NonPublic | BindingFlags.Instance));
            cursor.EmitDelegate<Action<SceneUpdateScheduler, List<object>>>(PreAwakeCall);
            cursor.GotoNext(MoveType.After, i => i.MatchLdfld<SceneUpdateScheduler>("toAwake"),
                i => i.MatchCallvirt<List<object>>(nameof(List<object>.Clear)));
            cursor.Index--;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<List<object>, SceneUpdateScheduler, List<object>>>(PostAwakeCall);
        }

        private bool Owns(SceneUpdateScheduler list) => list.ActualSceneIdentity is ISourceEngineContextScene scene &&
                                                        ReferenceEquals(scene.EngineContext, engine);

        private void PreAwakeCall(SceneUpdateScheduler list, List<object> entities)
        {
            if (!Owns(list)) return;
            foreach (object entity in entities)
            {
                if (entity is PreAwakeHolder holder) holder.PreAwake(list.ActualSceneIdentity);
                foreach (var component in ((ISourceOwnedComponentList)entity).OwnedComponents)
                    if (component is PreAwakeHolder child)
                        child.PreAwake(list.ActualSceneIdentity);
            }
        }

        private List<object> PostAwakeCall(List<object> entities, SceneUpdateScheduler list)
        {
            if (!Owns(list)) return entities;
            foreach (object entity in entities)
            {
                if (entity is PostAwakeHolder holder) holder.PostAwake(list.ActualSceneIdentity);
                foreach (var component in ((ISourceOwnedComponentList)entity).OwnedComponents)
                    if (component is PostAwakeHolder child)
                        child.PostAwake(list.ActualSceneIdentity);
            }

            return entities;
        }

        public void Dispose() => hook.Dispose();
    }
}
