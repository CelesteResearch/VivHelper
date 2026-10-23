// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Scheduling;
using System.Linq;
using Celeste.Game.Entities.Lookouts;
using Celeste.Mods.VivHelper.Entities.Watchtowers;
using Celeste.Simulation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mods.VivHelper.Runtime.Watchtowers
{
    public sealed class PlaybackWatchtowerProcess : IDisposable
    {
        private static int nextResumeState = -174029;
        private readonly int waitingForRelease = System.Threading.Interlocked.Decrement(ref nextResumeState);
        private readonly ISourceEngineContext engine;
        private readonly ILHook hook;

        public PlaybackWatchtowerProcess(ISourceEngineContext engine)
        {
            this.engine = engine;
            hook = new ILHook(typeof(LookoutInteractionRoutine).GetMethod(nameof(LookoutInteractionRoutine.Run),
                new[] { typeof(ILookoutInteractionState), typeof(RoomPlayerSession) }).GetStateMachineTarget(), Modify);
        }

        public void Dispose() => hook.Dispose();

        private bool BeginStep(ILookoutInteractionState state)
            => state is not CustomPlaybackWatchtower tower ||
               !ReferenceEquals(state.Process.EngineContext, engine) || tower.TryBeginViewStep();

        private void Opened(ILookoutInteractionState state)
        {
            if (state is CustomPlaybackWatchtower tower && ReferenceEquals(state.Process.EngineContext, engine))
                tower.OnOpened();
        }

        private void ViewingEnded(ILookoutInteractionState state)
        {
            if (state is CustomPlaybackWatchtower tower && ReferenceEquals(state.Process.EngineContext, engine))
                tower.OnViewingEnded();
        }

        private void Closed(ILookoutInteractionState state)
        {
            if (state is CustomPlaybackWatchtower tower && ReferenceEquals(state.Process.EngineContext, engine))
                tower.OnClosed();
        }

        private void Modify(ILContext il)
        {
            var fields = il.Body.Instructions.Select(instruction => instruction.Operand).OfType<FieldReference>()
                .ToArray();
            var iteratorState = fields.First(field => field.Name == "<>1__state");
            var current = fields.First(field => field.Name == "<>2__current");
            var state = fields.First(field => field.Name == "state");
            var instructions = il.Body.Instructions;
            var opening = instructions.Single(instruction => instruction.Operand is MethodReference method &&
                                                             method.Name == "get_Acceleration").Previous.Previous;
            if (!opening.MatchLdarg(0))
                throw new InvalidOperationException("The actual lookout opening receiver is missing.");
            var aim = instructions.Single(instruction => instruction.Operand is FieldReference field &&
                                                         field.Name == "Aim");
            var step = aim;
            while (!step.MatchLdarg(0)) step = step.Previous;
            // The stack is empty before loading the lookout state for the Aim read.
            if (step.Next.Operand is not FieldReference source || source.Name != state.Name)
                throw new InvalidOperationException("The actual lookout Aim receiver is missing.");
            var spriteReads = instructions.Where(instruction => instruction.Operand is FieldReference field &&
                                                                field.Name == "Sprite" &&
                                                                field.DeclaringType.FullName ==
                                                                typeof(RoomPlayerSession).FullName).ToArray();
            if (spriteReads.Length != 2)
                throw new InvalidOperationException("The actual lookout exit sprite read is missing.");
            var exit = spriteReads[1].Previous;
            var remove = instructions.Single(instruction => instruction.Operand is MethodReference method &&
                                                            method.Name == "RemoveEntity");
            var cursor = new ILCursor(il);
            cursor.Goto(opening, MoveType.AfterLabel);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, state);
            cursor.EmitDelegate<Action<ILookoutInteractionState>>(Opened);
            cursor.Goto(exit, MoveType.AfterLabel);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, state);
            cursor.EmitDelegate<Action<ILookoutInteractionState>>(ViewingEnded);
            cursor.Goto(remove, MoveType.After);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, state);
            cursor.EmitDelegate<Action<ILookoutInteractionState>>(Closed);

            cursor.Goto(step, MoveType.AfterLabel);
            var resume = cursor.MarkLabel();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldc_I4_M1);
            cursor.Emit(OpCodes.Stfld, iteratorState);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, state);
            cursor.EmitDelegate<Func<ILookoutInteractionState, bool>>(BeginStep);
            var proceed = cursor.DefineLabel();
            cursor.Emit(OpCodes.Brtrue, proceed);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldnull);
            cursor.Emit(OpCodes.Stfld, current);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldc_I4, waitingForRelease);
            cursor.Emit(OpCodes.Stfld, iteratorState);
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(proceed);
            cursor.Index = 0;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, iteratorState);
            cursor.Emit(OpCodes.Ldc_I4, waitingForRelease);
            cursor.Emit(OpCodes.Beq, resume);
        }
    }
}
