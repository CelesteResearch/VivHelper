using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation.Gameplay.Player.Intro;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.Blackout
{
    public sealed class BlackoutProcess : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, BlackoutProcess> Owners = new();
        private static readonly ConditionalWeakTable<ISourceEngineContext, BlackoutVisualState> VisualStates = new();
        private readonly BlackoutVisualState visualState;
        private readonly ISourceEngineContext engine;
        private readonly Func<bool> getBlackout;
        private readonly Action<bool> setBlackout;
        private readonly Hook update;
        private readonly Hook load;

        public float AlphaFade
        {
            get => visualState.AlphaFade;
            set => visualState.AlphaFade = value;
        }

        public bool Blackout
        {
            get => getBlackout();
            set => setBlackout(value);
        }

        public static BlackoutProcess For(ISourceEngineContext engine)
        {
            Owners.TryGetValue(engine, out var owner);
            return owner;
        }

        public BlackoutProcess(ISourceEngineContext engine, Func<bool> getBlackout, Action<bool> setBlackout)
        {
            this.engine = engine;
            visualState = VisualStates.GetValue(engine, static _ => new BlackoutVisualState());
            this.getBlackout = getBlackout;
            this.setBlackout = setBlackout;
            Owners.Add(engine, this);
            update = new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.Update)),
                (Action<Action<OriginalLevelSceneRoot>, OriginalLevelSceneRoot>)UpdateLevel);
            load = new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot, PlayerIntroType,
                    bool>)LoadLevel);
        }

        private void UpdateLevel(Action<OriginalLevelSceneRoot> orig, OriginalLevelSceneRoot level)
        {
            orig(level);
            if (ReferenceEquals(level.EngineContext, engine) && !level.FrozenOrPaused)
                AlphaFade = ScalarMath.Approach(AlphaFade, Blackout ? 0f : 1f, engine.DeltaTime * 2f,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
        }

        private void LoadLevel(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> orig, OriginalLevelSceneRoot level,
            PlayerIntroType intro, bool fromLoader)
        {
            orig(level, intro, fromLoader);
            if (ReferenceEquals(level.EngineContext, engine)) Blackout = false;
        }

        public void Dispose()
        {
            load.Dispose();
            update.Dispose();
            Owners.Remove(engine);
        }
    }
}
