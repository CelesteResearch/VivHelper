// SPDX-License-Identifier: MIT

using System.Collections;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Geometry;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.Jumpthrus;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Actors.Debris;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Entities.Platforms.JumpThrus
{
    [CustomEntity("VivHelper/CrumbleJumpThru")]
    [RuntimeTypeAlias("VivHelper.Entities.CrumbleJumpThruOnTouch")]
    public class CrumbleJumpThruOnTouch : JumpthruPlatformState
    {
        public bool permanent;
        public float delay;
        public bool triggered;
        private readonly ISourceEngineContext engine;

        public CrumbleJumpThruOnTouch(EntityData data, Vector2 offset, ISourceSolidWorld world) : base(data, offset,
            world)
        {
            engine = world.EngineContext;
            delay = data.Float("Delay", .1f);
            permanent = data.Bool("Permanent", false);
            Add(new SimulationCoroutine(engine, Sequence()));
        }

        public static CrumbleJumpThruOnTouch Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(data, offset, level.SolidWorldFactory.CreateJumpThru(data.Position + offset, true));

        public void Break()
        {
            if (!Collidable || Scene == null) return;
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                "event:/new_content/game/10_farewell/quake_rockbreak", Position);
            Collidable = false;
            var sourceScene = ((ISourceScene)Scene).SourceScene;
            for (int i = 0; i < Width / 8f; i++)
                if (!SourceSceneCollision.CheckRect(sourceScene.Entities, "Celeste.Solid",
                        new IntRect((int)X + i * 8, (int)Y, 8, 8)))
                    sourceScene.QueueSourceEntity(engine.EntityPools.Create<DebrisState>()
                        .Init(Position + new Vector2(4 + i * 8, 4f), '9', true).BlastFrom(TopCenter));
            // Published VivHelper's Permanent branch only performs SceneAs<Level>; it does not persist an ID.
            if (permanent) _ = Scene as OriginalLevelSceneRoot;
            RemoveSelf();
        }

        private IEnumerator Sequence()
        {
            while (!triggered && !HasPlayerRider()) yield return null;
            while (delay > 0f)
            {
                delay -= engine.DeltaTime;
                yield return null;
            }

            Break();
        }
    }
}
