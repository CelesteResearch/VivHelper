using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Game.Audio;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Profiles;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Scheduling.ExecutionTags;
using Celeste.Core.Reflection.Runtime;
using Celeste.Simulation;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    [RuntimeTypeAlias("VivHelper.Entities.SeekerStuff.CustomSeekerEffectsController")]
    public class CustomSeekerEffectsController : SceneEntityState, ISourceEngineContextEntity
    {
        private readonly ISourceEngineContext engine;
        private readonly string seekerRegistryKey;
        private readonly string playerRegistryKey;
        private float randomAnxietyOffset;
        public bool enabled = true;
        ISourceEngineContext ISourceEngineContextEntity.EngineContext => engine;

        public CustomSeekerEffectsController(ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            seekerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(CustomSeeker))
                .RegistryKey;
            playerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(RoomPlayerSession))
                .RegistryKey;
            Tags = ((ISourceGlobalFrozenExecutionTagsEnvironment)engine).GlobalFrozenExecutionTags.Global.Value;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            OriginalLevelSceneRoot level = scene as OriginalLevelSceneRoot;
            level.Session.Audio.Music.Layer(3, 0f,
                ((global::Celeste.Game.Audio.State.Globals.IOriginalAudioStateGlobalsEnvironment)engine)
                .AudioStateGlobals.LayerParameters);
            IOriginalGameplayProcess process = (IOriginalGameplayProcess)engine;
            OriginalAudioSession.Apply(level.Session.Audio, process.AudioSession.ChannelOutput,
                process.AudioApplyPolicy, false);
        }

        public override void Update()
        {
            base.Update();
            var level = Scene as OriginalLevelSceneRoot;
            if (enabled)
            {
                if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(.05f, engine))
                    randomAnxietyOffset = SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, -.2f, .2f);
                Vector2 position = level.Camera.Position;
                RoomPlayerSession player =
                    level.SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(
                        playerRegistryKey);

                float target, intensity;
                if (player != null && !player.Dead)
                {
                    float nearest = -1f, attacking = -1f;
                    foreach (CustomSeeker seeker in level.SourceScene.Entities.GetTrackedEntities(
                                 seekerRegistryKey))
                    {
                        if (!seeker.DisableEffects)
                        {
                            float distance =
                                ((ISourceVectorEnvironment)engine).VectorArithmetic.DistanceSquared(player.Center,
                                    seeker.Center);
                            if (!seeker.Regenerating)
                                nearest = !(nearest < 0f) ? Math.Min(nearest, distance) : distance;
                            if (seeker.Attacking)
                                attacking = !(attacking < 0f) ? Math.Min(attacking, distance) : distance;
                        }
                    }

                    target = !(attacking >= 0f) ? 1f : ScalarMath.ClampedMap(attacking, 256f, 4096f, .5f);
                    engine.Effects.AnxietyOrigin = new Vector2((player.Center.X - position.X) / 320f,
                        (player.Center.Y - position.Y) / 180f);
                    intensity = !(nearest >= 0f) ? 0f : ScalarMath.ClampedMap(nearest, 256f, 16384f, 1f, 0f);
                }
                else
                {
                    target = 1f;
                    intensity = 0f;
                }

                engine.Effects.TimeRate = ScalarMath.Approach(engine.Effects.TimeRate, target, 4f * engine.DeltaTime);
                engine.Effects.GameRate = ScalarMath.Approach(engine.Effects.GameRate,
                    ScalarMath.Map(engine.Effects.TimeRate, .5f, 1f), engine.DeltaTime * 2f);
                engine.Effects.Anxiety = ScalarMath.Approach(engine.Effects.Anxiety,
                    (.5f + randomAnxietyOffset) * intensity, 8f * engine.DeltaTime);
                if (engine.Effects.TimeRate == 1f && engine.Effects.GameRate == 1f && engine.Effects.Anxiety == 0f
                    && level.SourceScene.Entities.GetTrackedEntities(seekerRegistryKey).Count == 0) enabled = false;
            }
            else if (level.SourceScene.Entities.GetTrackedEntities(seekerRegistryKey).Count > 0) enabled = true;
        }
    }
}
