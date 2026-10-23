// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.Jumpthrus;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Entities.Platforms.JumpThrus
{
    [CustomEntity("VivHelper/FallThru = Load")]
    [RuntimeTypeAlias("VivHelper.Entities.DuckThru")]
    public class DuckThru : JumpthruPlatformState
    {
        private float playerDuckTimer;
        private float FallTime = .35f;
        private bool IgnoreOnlyThis;
        private readonly ISourceEngineContext engine;

        public DuckThru(EntityData data, Vector2 offset, ISourceSolidWorld world) : base(data, offset, world)
        {
            engine = world.EngineContext;
            FallTime = data.Float("FallTime", .35f);
            IgnoreOnlyThis = data.Bool("IgnoreOnlyThis", false);
        }

        public static DuckThru Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
            => new(data, offset, level.SolidWorldFactory.CreateJumpThru(data.Position + offset, true));

        public override void Update()
        {
            base.Update();
            var player = GetPlayer();
            if (player != null && player.Data.Motion.Ducking && HasPlayerRider())
            {
                playerDuckTimer += engine.DeltaTime;
                if (playerDuckTimer >= FallTime)
                    player.Add(new SimulationCoroutine(engine, FallThru(player)));
            }
            else playerDuckTimer = 0f;
        }

        public IEnumerator FallThru(RoomPlayerSession player)
        {
            if (player != null && !player.Dead) GoThrough(player, true);
            player.Speed = new Vector2(0f, 60f);
            yield return .25f;
            while ((player = GetPlayer()) == null || player.Dead) yield return null;
            GoThrough(player, false);
        }

        public void GoThrough(RoomPlayerSession player, bool on)
        {
            if (IgnoreOnlyThis) Collidable = !on;
            else player.IgnoreJumpThrus = on;
        }

        private RoomPlayerSession GetPlayer()
            => (engine.Frame.Scenes.CurrentScene as ISourceScene)?.SourceScene.Entities
                .GetTrackedEntity<RoomPlayerSession>();
    }
}
