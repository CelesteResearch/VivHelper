// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Scheduling;
using Celeste.Simulation.Gameplay.Player.Dash;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.Boosters
{
    internal sealed partial class VivBoosterStateProcess
    {
        private static readonly Func<DashSequence, SimulationStateMachine> GetMachine = BindMachine();
        private Hook dashStartHook;

        private void InstallDashStartHook() => dashStartHook = new Hook(
            typeof(DashSequence).GetMethod(nameof(DashSequence.Start)),
            (Func<Func<DashSequence, int>, DashSequence, int>)StartDash);

        private int StartDash(Func<DashSequence, int> original, DashSequence dash)
        {
            if (ReferenceEquals(dash.EngineContext, Engine))
            {
                int state = GetMachine(dash).State;
                // The released module tests WindBoost.timer for Pink too; retain that exact condition.
                if (state == States.Orange || (state == States.Wind && WindTimer.Value > 0f) ||
                    (state == States.Pink && WindTimer.Value > 0f)) return 5;
            }

            return original(dash);
        }

        private static Func<DashSequence, SimulationStateMachine> BindMachine()
        {
            var method = new DynamicMethod("VivBoosterDashMachine", typeof(SimulationStateMachine),
                new[] { typeof(DashSequence) }, typeof(VivBoosterStateProcess), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld,
                typeof(DashSequence).GetField("machine", BindingFlags.NonPublic | BindingFlags.Instance));
            il.Emit(OpCodes.Ret);
            return (Func<DashSequence, SimulationStateMachine>)method.CreateDelegate(
                typeof(Func<DashSequence, SimulationStateMachine>));
        }
    }
}
