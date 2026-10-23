// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Platforms.CrushBlocks;
using Celeste.Game.Gameplay.Platforms.FallingBlocks;
using Celeste.Mods.VivHelper.Entities.FallingBlocks;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.FallingBlocks;

internal sealed class CustomFallingBlockProcess : IDisposable
{
    private readonly ISourceEngineContext engine;
    private readonly ILHook hook;
    private readonly FieldInfo owner;
    private string trackerKey;

    public CustomFallingBlockProcess(ISourceEngineContext engine)
    {
        this.engine = engine;
        var original =
            typeof(OriginalCrushBlockState).GetMethod("AttackSequence", BindingFlags.Instance | BindingFlags.NonPublic);
        var target = original.GetCustomAttribute<IteratorStateMachineAttribute>().StateMachineType
            .GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        owner = target.DeclaringType.GetField("<>4__this",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        hook = new ILHook(target, ModifyAttack);
    }

    private void ModifyAttack(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Original published Viv hook stloc5 is the result of CollideFirst<FallingBlock>,
        // after the moving loop. Shared has different local numbering: retain the semantic call/store boundary.
        cursor.GotoNext(MoveType.After, i => i.Operand is GenericInstanceMethod method
                                             && method.ElementMethod.DeclaringType.FullName ==
                                             typeof(SourceTrackedEntityCollision).FullName
                                             && method.ElementMethod.Name ==
                                             nameof(SourceTrackedEntityCollision.FirstAt)
                                             && method.GenericArguments.Count == 1
                                             && method.GenericArguments[0].FullName ==
                                             typeof(OriginalFallingBlockState).FullName);
        cursor.GotoNext(MoveType.After, i => i.MatchStloc(out _));
        var direction =
            typeof(OriginalCrushBlockState).GetField("crushDir", BindingFlags.Instance | BindingFlags.NonPublic);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, owner);
        cursor.Emit(OpCodes.Dup);
        cursor.Emit(OpCodes.Ldfld, direction);
        cursor.EmitDelegate<Action<OriginalCrushBlockState, Vector2>>(Trigger);
    }

    private void Trigger(OriginalCrushBlockState crush, Vector2 direction)
    {
        if (crush.Scene is not ISourceEngineContextScene scene || !ReferenceEquals(scene.EngineContext, engine)) return;
        trackerKey ??= ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(CustomFallingBlock))
            .RegistryKey;
        var block = SourceTrackedEntityCollision.FirstAt<CustomFallingBlock>(
            ((ISourceScene)crush.Scene).SourceScene.Entities, trackerKey, crush, crush.Position + direction);
        if (block != null && block.IsTriggeredByCrushBlock) block.Triggered = true;
    }

    public void Dispose() => hook.Dispose();
}
