// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components.Timing;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Mods.VivHelper.Runtime.RoomWrapping;
using Celeste.Scene.Components.Shaking;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Entities.Solids.Crushers
{
    [CustomEntity("VivHelper/FlagIntroCrusher")]
    [RuntimeTypeAlias("VivHelper.Entities.FlagIntroCrusher")]
    public class FlagIntroCrusher : SourceSolidEntityState
    {
        private static readonly string PlayerKey = typeof(RoomPlayerSession).FullName;
        private readonly ISourceEngineContext engine;
        private Vector2 shake;
        private Vector2 start;
        private Vector2 end;
        private TileGrid tilegrid;
        private SoundSourceComponentState shakingSfx;
        private readonly Action<Vector2> setShake;
        public string[] flags;
        private bool manualTrigger = false;
        private float delay;
        private bool triggered;
        private float speed;
        private OriginalLevelSceneRoot Level => (OriginalLevelSceneRoot)Scene;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new FlagIntroCrusher(level.EngineContext, data, offset,
            level.SolidWorldFactory.Create(data.Position + offset, true));

        public FlagIntroCrusher(ISourceEngineContext engine, Vector2 position, int width, int height, Vector2 node,
            char tile, ISourceSolidWorld world) : base(position, width, height, true, world)
        {
            this.engine = engine;
            start = position;
            end = node;
            Depth = -10501;
            SurfaceSoundIndex = 4;
            OwnedComponents.Add(tilegrid = ((ISourceAutotilingResources)((ISourceGameGraphicsEnvironment)engine)
                .GameGraphics).ForegroundAutotiler.GenerateBox(tile, width / 8, height / 8).TileGrid);
            OwnedComponents.Add(shakingSfx = new SoundSourceComponentState(engine));
            setShake = value => shake = value;
        }

        public FlagIntroCrusher(ISourceEngineContext engine, EntityData data, Vector2 offset, ISourceSolidWorld world)
            : this(engine, data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset,
                data.Has("tileType") ? data.Char("tileType", '3') : data.Char("tiletype", '3'), world)
        {
            string value = data.Attr("flags", "");
            if (!string.IsNullOrWhiteSpace(value)) flags = value.Split(',');
            delay = data.Float("delay", 1.2f);
            speed = data.Float("speed", 2f);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            triggered = false;
            if (flags != null && RoomWrapFlags.All(scene as OriginalLevelSceneRoot, flags)) Position = end;
            else OwnedComponents.Add(new SimulationCoroutine(engine, Sequence()));
        }

        public override void Update()
        {
            tilegrid.Position = shake;
            base.Update();
        }

        private RoomPlayerSession Player() => ((ISourceScene)Scene).SourceScene.Entities
            .GetTrackedEntity<RoomPlayerSession>(PlayerKey);

        private IEnumerator Sequence()
        {
            RoomPlayerSession player;
            do
            {
                yield return null;
                player = Player();
            } while (!triggered && (flags != null
                         ? !RoomWrapFlags.All(Level, flags)
                         : player == null || !(player.X >= X + 30f) || !(player.X <= Right + 8f)));

            shakingSfx.Play("event:/game/00_prologue/fallblock_first_shake");
            float time = delay;
            var shaker = new ShakerComponentState(engine, time, true, setShake);
            if (!(delay <= 0f)) OwnedComponents.Add(shaker);
            ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                RumbleLength.Medium);
            while (time > 0f)
            {
                player = Player();
                if (!manualTrigger && player != null && (player.X >= X + Width - 8f || player.X < X + 28f))
                {
                    shaker.RemoveSelf();
                    break;
                }

                yield return null;
                time -= engine.DeltaTime;
            }

            var particles = ((IOriginalGameplayProcess)engine).ParticleTypes.FallingBlock;
            for (int i = 2; i < Width; i += 4)
            {
                Level.Particles.Emit(particles.P_FallDustA, 2, new Vector2(X + i, Y), Vector2.One * 4f,
                    (float)Math.PI / 2f);
                Level.Particles.Emit(particles.P_FallDustB, 2, new Vector2(X + i, Y), Vector2.One * 4f);
            }

            shakingSfx.Param("release", 1f);
            time = 0f;
            do
            {
                yield return null;
                time = ScalarMath.Approach(time, 1f, speed * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                MoveTo(Vector2Interpolation.Lerp(start, end,
                    ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeIn(time),
                    ((ISourceScalarEnvironment)engine).ScalarArithmetic));
            } while (!(time >= 1f));

            for (int i = 0; i <= Width; i += 4)
            {
                Level.ParticlesFG.Emit(particles.P_FallDustA, 1, new Vector2(X + i, Bottom), Vector2.One * 4f,
                    -(float)Math.PI / 2f);
                float direction = !(i < Width / 2f) ? 0f : (float)Math.PI;
                Level.ParticlesFG.Emit(particles.P_LandDust, 1, new Vector2(X + i, Bottom), Vector2.One * 4f,
                    direction);
            }

            shakingSfx.Stop(true);
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/00_prologue/fallblock_first_impact",
                Position);
            Level.Shake(.3f);
            ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                RumbleLength.Medium);
            OwnedComponents.Add(new ShakerComponentState(engine, .25f, true, setShake));
        }
    }
}
