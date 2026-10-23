using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Platforms.FloatySpaceBlocks;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Actors.Debris;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.VivHelper.Entities.Platforms.Floating
{
    [CustomEntity("VivHelper/FloatyBreakBlock")]
    [RuntimeTypeAlias("Celeste.Mod.VivHelper.FloatyBreakBlock")]
    public partial class FloatyBreakBlock : FloatySpaceBlockState
    {
        private enum DelayType
        {
            Timer,
            Pressure,
            Sinking
        }

        private float delay;
        private float origDelay;
        private DelayType delayType;
        public bool beginBreak;
        private char tiletype;
        private readonly ISourceEngineContext engine;
        private readonly IOriginalGameplayProcess process;

        public static FloatyBreakBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(data, offset, level.SolidWorldFactory, level.EngineContext);

        public FloatyBreakBlock(EntityData data, Vector2 offset, ISourceSolidWorldFactory worlds,
            ISourceEngineContext engine)
            : base(data, offset, worlds)
        {
            this.engine = engine;
            process = (IOriginalGameplayProcess)engine;
            tiletype = data.Char("tiletype", '3');
            delay = origDelay = data.Float("delay", 0f);
            delayType = data.Enum("delayType", DelayType.Timer);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            beginBreak = false;
            if (delayType == DelayType.Timer) Add(new SimulationCoroutine(engine, DelayTimer()));
            else if (delayType == DelayType.Sinking) Add(new SimulationCoroutine(engine, DelaySinking()));
            else if (delayType == DelayType.Pressure) Add(new SimulationCoroutine(engine, DelayPressure()));
        }

        public void Break()
        {
            if (!Collidable || !(Scene is OriginalLevelSceneRoot)) return;
            process.AudioSession.Play("event:/new_content/game/10_farewell/quake_rockbreak", Position,
                process.SaveContext);
            Collidable = false;
            for (int i = 0; i < Width / 8f; i++)
            for (int j = 0; j < Height / 8f; j++)
                if (!SourceSceneCollision.CheckRect(((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid",
                        new IntRect((int)X + i * 8, (int)Y + j * 8, 8, 8)))
                    ((ISourceScene)Scene).SourceScene.QueueSourceEntity(engine.EntityPools.Create<DebrisState>()
                        .Init(Position + new Vector2(4 + i * 8, 4 + j * 8), tiletype).BlastFrom(TopCenter));
            RemoveSelf();
        }

        public override void OnStaticMoverTrigger(SourceStaticMoverState mover) => beginBreak = true;
    }
}
