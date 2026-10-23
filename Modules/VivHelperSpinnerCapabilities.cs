// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Mods.VivHelper.Entities.Debris;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Modules
{
    public static class VivHelperSpinnerCapabilities
    {
        private sealed class OwnerState
        {
            public readonly HashSet<int> DashStates = [];
            public Celeste.Game.Entities.Hazards.Spinners.Crystal.OriginalCrystalStaticSpinner HueSource;
        }

        private static readonly ConditionalWeakTable<ISourceEngineContext, OwnerState> Owners = new();

        internal static HashSet<int> GetDashStatesForOwner(ISourceEngineContext engine)
            => Owners.GetValue(engine, static _ => new()).DashStates;

        public static bool IsDashState(ISourceEngineContext engine, int state)
            => state is 2 or 5 || Owners.TryGetValue(engine, out var owner) && owner.DashStates.Contains(state);

        public static void RegisterDashState(ISourceEngineContext engine, int state)
            => Owners.GetValue(engine, static _ => new()).DashStates.Add(state);

        public static void UnregisterDashState(ISourceEngineContext engine, int state)
        {
            if (Owners.TryGetValue(engine, out var owner)) owner.DashStates.Remove(state);
        }

        public static void InitializeHue(ISourceEngineContext engine)
        {
            var owner = Owners.GetValue(engine, static _ => new());
            if (owner.HueSource != null) return;
            // Installed Viv lazily constructs one detached original spinner for its shared hue function.
            owner.HueSource = new Celeste.Game.Entities.Hazards.Spinners.Crystal.OriginalCrystalStaticSpinner(
                System.Numerics.Vector2.Zero, false,
                Celeste.Simulation.Gameplay.Hazards.Spinners.Crystal.CrystalSpinnerColor.Rainbow, engine);
        }

        public static Celeste.Game.Entities.Hazards.Spinners.Crystal.OriginalCrystalStaticSpinner GetHueSource(
            ISourceEngineContext engine)
        {
            InitializeHue(engine);
            return Owners.GetValue(engine, static _ => new()).HueSource;
        }

        public static Celeste.Core.Display.PackedColor GetHue(ISourceEngineContext engine, object scene,
            System.Numerics.Vector2 position)
        {
            if (scene == null) return Celeste.Core.Display.PackedColor.White;
            InitializeHue(engine);
            var spinner = Owners.GetValue(engine, static _ => new()).HueSource;
            ((Celeste.Scene.Tracking.ISceneEntityAssociation)spinner).Scene = scene;
            return spinner.GetHue(position);
        }

        public static void RegisterDebrisPool(ISourceEngineContext engine)
        {
            foreach (var pool in engine.EntityPools.EnumeratePools())
                if (pool.Key == typeof(CustomCrystalDebris))
                    return;
            engine.EntityPools.Register(typeof(CustomCrystalDebris).AssemblyQualifiedName,
                () => new CustomCrystalDebris(((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas,
                    engine, ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy));
        }
    }
}
