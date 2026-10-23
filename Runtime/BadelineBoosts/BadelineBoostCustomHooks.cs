// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Modules.Updating;
using Celeste.Game.Entities.BadelineBoosts;
using Celeste.Mods.VivHelper.Entities.BadelineBoosts;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Dummy;
using Celeste.Simulation.Gameplay.Player.State;
using Celeste.Game.Execution.Levels;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mods.VivHelper.Runtime.BadelineBoosts
{
    public sealed class BadelineBoostCustomHooks : IDisposable
    {
        private static readonly MethodInfo Routine =
            typeof(BadelineBoostState).GetMethod("BoostRoutine", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Func<BadelineBoostState, RoomPlayerSession> Holding =
            Getter<RoomPlayerSession>("holding");

        private static readonly Func<BadelineBoostState, int> NodeIndex = Getter<int>("nodeIndex");
        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> hooks = new List<IDisposable>();

        private static readonly ConditionalWeakTable<RoomPlayerSession, LaunchCache> launchCache =
            new ConditionalWeakTable<RoomPlayerSession, LaunchCache>();

        private bool extendedVariantsLoaded;

        private sealed class LaunchCache
        {
            public float? Value;
        }

        private static Func<BadelineBoostState, T> Getter<T>(string name)
        {
            var self = Expression.Parameter(typeof(BadelineBoostState));
            return Expression.Lambda<Func<BadelineBoostState, T>>(Expression.Field(self,
                    typeof(BadelineBoostState).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)), self)
                .Compile();
        }

        public BadelineBoostCustomHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                using (new DetourConfigContext(new DetourConfig("VivHelper.BadelineBoostCustom", after: new[] { "*" }))
                           .Use())
                {
                    hooks.Add(new ILHook(Routine.GetStateMachineTarget(), PatchRoutine));
                    hooks.Add(new Hook(Routine,
                        (Func<Func<BadelineBoostState, RoomPlayerSession, IEnumerator>, BadelineBoostState,
                            RoomPlayerSession, IEnumerator>)WrapRoutine));
                    hooks.Add(new ILHook(
                        typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.BadelineBoostLaunch)),
                        PatchLaunch));
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Initialize()
        {
            extendedVariantsLoaded = ((IEverestLoaderProcessEnvironment)engine).Loader.DependencyLoaded(
                new EverestModuleMetadata { Name = "ExtendedVariantMode", VersionString = "0.21.0" });
        }

        private bool Owns(BadelineBoostState self, out BadelineBoostCustom custom)
        {
            custom = self as BadelineBoostCustom;
            return custom != null && ReferenceEquals(custom.EngineContext, engine);
        }

        private IEnumerator WrapRoutine(Func<BadelineBoostState, RoomPlayerSession, IEnumerator> orig,
            BadelineBoostState self, RoomPlayerSession player) =>
            Owns(self, out var custom) && ReferenceEquals(player.EngineContext, engine)
                ? CustomRoutine(orig, custom, player)
                : orig(self, player);

        private IEnumerator CustomRoutine(Func<BadelineBoostState, RoomPlayerSession, IEnumerator> orig,
            BadelineBoostCustom custom, RoomPlayerSession player)
        {
            int oldDash = player.Data.Dashes;
            float oldStam = player.Data.Stamina;
            yield return new SourceSwapImmediately(orig(custom, player));
            Refill(custom, player, oldDash, oldStam);
            if (extendedVariantsLoaded)
                player.OwnedComponents.Add(new SimulationCoroutine(engine,
                    RepeatRefill(custom, player, oldDash, oldStam)));
        }

        internal static void Refill(BadelineBoostCustom custom, RoomPlayerSession player, int dash, float stamina)
        {
            player.Data.Dashes = custom.replaceDashes(dash, player.Inventory.Dashes);
            player.Data.Stamina = custom.replaceStamina(stamina, 110f);
        }

        internal static IEnumerator RepeatRefill(BadelineBoostCustom custom, RoomPlayerSession player, int dash,
            float stamina)
        {
            Refill(custom, player, dash, stamina);
            yield return null;
            Refill(custom, player, dash, stamina);
            yield return null;
            Refill(custom, player, dash, stamina);
        }

        private bool InitialRefill(BadelineBoostState self)
        {
            if (!Owns(self, out var custom)) return false;
            var player = Holding(self);
            if (!ReferenceEquals(player.EngineContext, engine)) return false;
            Refill(custom, player, player.Data.Dashes, player.Data.Stamina);
            return true;
        }

        private void DiscardedFloor(BadelineBoostState self)
        {
            if (Owns(self, out var custom) && custom.FloorPositionOnLaunch)
            {
                var position = Holding(self).Position;
                _ = new Vector2((float)Math.Floor(position.X), (float)Math.Floor(position.Y));
            }
        }

        private RoomPlayerSession CacheLaunchPlayer(RoomPlayerSession player, BadelineBoostState self)
        {
            CacheLaunch(0f, self, player);
            return player;
        }

        private float CacheLaunch(float atX, BadelineBoostState self, RoomPlayerSession player)
        {
            if (!Owns(self, out var custom) || !ReferenceEquals(player.EngineContext, engine)) return atX;
            float? value = custom.LaunchStrengthMultiplier;
            if (!value.HasValue)
            {
                int index = NodeIndex(self);
                if (index > custom.LaunchStrengthMultiplierSet.Length)
                    ((IEverestLoggerProcessEnvironment)engine).Logger.Log("VivHelper",
                        "LaunchStrengthMultiplier was not set for this index.");
                else value = custom.LaunchStrengthMultiplierSet[index];
            }

            if (value.HasValue) launchCache.GetValue(player, _ => new LaunchCache()).Value = value;
            return atX;
        }

        private float ApplyLaunch(float value, RoomPlayerSession player)
        {
            if (ReferenceEquals(player.EngineContext, engine) && launchCache.TryGetValue(player, out var cache) &&
                cache.Value.HasValue)
            {
                value *= cache.Value.Value;
                cache.Value = null;
            }

            return value;
        }

        private Action ModifiedAlarm(Action orig, BadelineBoostState self)
        {
            if (!Owns(self, out var custom) || custom.DashLogic == "D") return orig;
            var scene = engine.Frame.Scenes.CurrentScene as ISourceScene;
            var player =
                scene?.SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession).FullName);
            if (player == null || !ReferenceEquals(player.EngineContext, engine)) return orig;
            return () =>
            {
                int dashes = player.Data.Dashes;
                orig();
                player.Data.Dashes = dashes;
            };
        }

        private void PatchRoutine(ILContext il)
        {
            var stateType = Routine.GetStateMachineTarget().DeclaringType;
            var self = stateType.GetField("<>4__this",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var player = stateType.GetField("player",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    i => i.MatchStfld<PlayerDummyController>(nameof(PlayerDummyController.Gravity))))
                throw new InvalidOperationException("BadelineBoost Dummy.Gravity anchor not found.");
            var refillEnd = cursor.Clone();
            if (!refillEnd.TryGotoNext(MoveType.After, i => i.MatchStfld<PlayerState>(nameof(PlayerState.Stamina))))
                throw new InvalidOperationException("BadelineBoost initial stamina anchor not found.");
            var label = refillEnd.MarkLabel();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, self);
            cursor.EmitDelegate<Func<BadelineBoostState, bool>>(InitialRefill);
            cursor.Emit(OpCodes.Brtrue, label);

            cursor.Index = 0;
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStloc(8)))
                throw new InvalidOperationException("BadelineBoost non-final launch display local 8 anchor not found.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, self);
            cursor.EmitDelegate<Action<BadelineBoostState>>(DiscardedFloor);

            cursor.Index = 0;
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStloc(8)) ||
                !cursor.TryGotoNext(MoveType.Before, i => i.MatchLdloc(1)))
                throw new InvalidOperationException("BadelineBoost pre-CenterX launch player anchor not found.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, self);
            cursor.EmitDelegate<Func<RoomPlayerSession, BadelineBoostState, RoomPlayerSession>>(CacheLaunchPlayer);

            cursor.Index = 0;
            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcR4(.15f)))
                throw new InvalidOperationException("BadelineBoost alarm duration anchor not found.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, self);
            cursor.EmitDelegate<Func<Action, BadelineBoostState, Action>>(ModifiedAlarm);
        }

        private void PatchLaunch(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After, i => i.MatchLdcR4(-330f)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, RoomPlayerSession, float>>(ApplyLaunch);
                count++;
            }

            if (count == 0) throw new InvalidOperationException("BadelineBoostLaunch -330 anchor not found.");
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
