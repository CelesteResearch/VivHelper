using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Graphics.Content;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content.Atlas;
using Celeste.Core.Display;
using Celeste.Graphics.Rendering;
using Celeste.Simulation;
using Celeste.Mods.VivHelper.Runtime.Blackout;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.VivHelper.Entities.Lighting
{
    [SceneTracked(false)]
    [CustomEntity("VivHelper/BlackoutController")]
    [RuntimeTypeAlias("VivHelper.Entities.BlackoutController")]
    public sealed class BlackoutController : SceneEntityState
    {
        public enum States
        {
            On,
            Flashing,
            Flag,
            Off
        }

        public States state;
        public float delay;
        private float timer;
        private readonly ISourceEngineContext engine;
        private readonly BlackoutProcess process;
        private readonly AtlasEntry blackout;
        private int checkCount = -1;

        public BlackoutController(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            process = BlackoutProcess.For(engine);
            state = data.Enum("StartingState", States.Off);
            if (state == States.Flashing)
            {
                delay = data.Float("Delay", 3f);
                timer = delay;
            }

            Depth = -249900;
            blackout = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["VivHelper/entities/Blackout"];
        }

        private int CountControllers() => ((ISourceScene)Scene).SourceScene.Entities
            .GetTrackedEntities(typeof(BlackoutController)).Count;

        public override void Awake(object scene)
        {
            if (((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(typeof(BlackoutController)).Count > 1)
                checkCount = 0;
            base.Awake(scene);
            process.Blackout = state == States.On;
            OwnedComponents.Add(new RoomTransitionListener { OnOutBegin = () => process.Blackout = false });
        }

        public override void Update()
        {
            if (checkCount > -1 && checkCount < 10 && CountControllers() > 1) checkCount++;
            if (checkCount > 9) throw new Exception("Multiple Blackout Controllers found within the scene.");
            base.Update();
            if (state == States.Flashing)
            {
                if (timer > 0f)
                {
                    timer -= engine.DeltaTime;
                    return;
                }

                process.Blackout = !process.Blackout;
                timer = delay;
            }
            else if (state == States.Flag)
                process.Blackout = ((OriginalLevelSceneRoot)Scene).Session.GetFlag("VH_Blackout");
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            SourceAtlasDrawing.DrawCentered(context, this, blackout,
                ((OriginalLevelSceneRoot)Scene).Camera.Position + new Vector2(160f, 90f),
                PackedColor.White * (1f - process.AlphaFade));
        }
    }
}
