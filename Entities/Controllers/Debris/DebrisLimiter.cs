using Celeste.Core.Components;
using System;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Runtime.Debris;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.VivHelper.Entities.Controllers.Debris
{
    [CustomEntity("VivHelper/DebrisLimiter")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.DebrisLimiter")]
    public class DebrisLimiter : SceneEntityState
    {
        private readonly DebrisLimiterContext context;
        public float limiter;
        public bool random;

        public DebrisLimiter(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            context = DebrisLimiterContext.For(engine);
            limiter = Math.Min(Math.Max(data.Int("limiter", 0), 0), 32) / 32f;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            context.Locker = false;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (context.Locker) RemoveSelf();
            else if (scene is OriginalLevelSceneRoot)
            {
                context.Session.DebrisLimiter = limiter;
                context.Locker = true;
            }
        }
    }
}
