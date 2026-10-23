// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Mods.VivHelper.Runtime.HoldableBarriers;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Entities.HoldableBarriers
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.HoldableBarrier")]
    [CustomEntity("VivHelper/HoldableBarrier")]
    public class HoldableBarrier : SourceSolidEntityState
    {
        public float Flash;
        public float Solidify;
        public bool Flashing;
        private float solidifyDelay;
        protected readonly List<Vector2> particles = new();
        private readonly List<HoldableBarrier> adjacent = new();
        private readonly float[] speeds = [12f, 20f, 40f];
        public HoldableBarrierColorController ColorController;
        private readonly ISourceEngineContext engine;
        private readonly HoldableBarrierContext context;
        private readonly string barrierRegistryKey;

        public static HoldableBarrier Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.SolidWorldFactory);

        public HoldableBarrier(EntityData data, Vector2 offset, ISourceSolidWorldFactory worlds)
            : this(data.Position + offset, data.Width, data.Height, worlds.Create(data.Position + offset, false))
        {
        }

        public HoldableBarrier(Vector2 position, float width, float height, ISourceSolidWorld world)
            : base(position, width, height, false, world)
        {
            engine = world.EngineContext;
            barrierRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(HoldableBarrier))
                .RegistryKey;
            context = HoldableBarrierContext.For(engine);
            for (int i = 0; i < Width * Height / 16f; i++)
                particles.Add(new Vector2(
                    SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, Width - 1f),
                    SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, Height - 1f)));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntity<HoldableBarrierRenderer>().Track(this);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            ColorController = context.Controller(scene);
            Collidable = false;
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntity<HoldableBarrierRenderer>().Untrack(this);
        }

        public void OnHoldable()
        {
        }

        private void OnReflect()
        {
            Flash = 1f;
            Flashing = true;
            var registry = ((ISourceScene)Scene).SourceScene.Entities;
            SourceSceneCollision.IntoRect(registry, barrierRegistryKey,
                new IntRect((int)X, (int)Y - 2, (int)Width, (int)Height + 4), adjacent);
            SourceSceneCollision.IntoRect(registry, barrierRegistryKey,
                new IntRect((int)X - 2, (int)Y, (int)Width + 4, (int)Height), adjacent);
            foreach (var barrier in adjacent)
                if (!barrier.Flashing)
                    barrier.OnReflect();
            adjacent.Clear();
        }

        public override void Update()
        {
            ColorController ??= context.Controller(Scene);
            if (Flashing)
            {
                Flash = ScalarMath.Approach(Flash, 0f, engine.DeltaTime * 4f,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                if (Flash <= 0f) Flashing = false;
            }
            else if (solidifyDelay > 0f) solidifyDelay -= engine.DeltaTime;
            else if (Solidify > 0f)
                Solidify = ScalarMath.Approach(Solidify, 0f, engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);

            var saved = ColorController == null ? context.Saved((OriginalLevelSceneRoot)Scene) : null;
            for (int i = 0; i < particles.Count; i++)
            {
                var point = particles[i] + (ColorController?.ParticleDirection ?? saved.ParticleDirection)
                    * speeds[i % speeds.Length] * engine.DeltaTime;
                point.Y = Mod(point.Y, Height - 1f);
                point.X = Mod(point.X, Width - 1f);
                particles[i] = point;
            }

            base.Update();
        }

        private static float Mod(float value, float modulus) => (value % modulus + modulus) % modulus;

        public override void Render(ISourceRenderContext render)
        {
            var color = (ColorController?.ParticleColor ?? SpawnPointRules.ParseColor(
                context.Saved((OriginalLevelSceneRoot)Scene).ParticleColorHex)) * .5f;
            foreach (var point in particles)
            {
                var draw = new AtlasTextureDraw(
                    ((Celeste.Graphics.Rendering.Drawing.ISourceDrawScope)render).Draw.Pixel,
                    Position + point, Vector2.Zero, color, Vector2.One);
                ((ISourceRenderCommandSink<AtlasTextureDraw>)render).Draw(in draw, this);
            }

            if (Flashing)
                SourcePrimitiveDrawing.Rect(render, this, Collider.AbsoluteLeft, Collider.AbsoluteTop,
                    Collider.Width, Collider.Height, color * Flash);
        }
    }
}
