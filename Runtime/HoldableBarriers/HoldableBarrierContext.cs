// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Simulation.Runtime.Session;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Mods.VivHelper.Entities.HoldableBarriers;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.VivHelper.Runtime.HoldableBarriers
{
    public sealed class HoldableBarrierContext : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, HoldableBarrierContext> contexts = new();
        private readonly ConditionalWeakTable<ChapterCompletionSession, SavedBarrierController> sessions = new();
        private readonly ISourceEngineContext engine;

        public HoldableBarrierContext(ISourceEngineContext engine)
        {
            this.engine = engine;
            contexts.Add(engine, this);
        }

        public static HoldableBarrierContext For(ISourceEngineContext engine) => contexts.GetValue(engine,
            static _ => throw new InvalidOperationException("VivHelper HoldableBarrier module is not loaded."));

        public SavedBarrierController Saved(OriginalLevelSceneRoot level)
            => sessions.GetValue(level.Session, static _ => new SavedBarrierController());

        public HoldableBarrierColorController Controller(object scene)
            => ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntity<HoldableBarrierColorController>();

        public void Save(OriginalLevelSceneRoot level, HoldableBarrierColorController controller)
        {
            var saved = Saved(level);
            // The installed helper deliberately serializes unpadded ARGB channels; retain its round-trip.
            saved.ParticleColorHex = ColorToHex(controller.ParticleColor);
            saved.BaseColorHex = ColorToHex(controller.BaseColor);
            saved.ParticleDirection = controller.ParticleDirection;
            saved.SolidOnRelease = controller.SolidOnRelease;
        }

        private static string ColorToHex(Celeste.Core.Display.PackedColor color)
            => color.A.ToString("X") + color.R.ToString("X") + color.G.ToString("X") + color.B.ToString("X");

        public void Dispose() => contexts.Remove(engine);
    }
}
