using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.VivHelper.Persistence;
using Celeste.Mods.VivHelper.Runtime.RefillCancellation;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.VivHelper.Entities.RefillCancellation
{
    [CustomEntity("VivHelper/RefillCancelSpace")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.RefillCancelSpace")]
    public partial class RefillCancelSpace : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly RefillCancelContext context;
        private readonly List<Vector2> particles = new();
        private readonly float[] speeds = { 29f, 12f, 20f, 40f };
        private readonly Vector2[][] directions = new Vector2[8][];
        protected PackedColor color;
        protected float width;
        protected float height;
        protected bool draw;
        public bool DashRestrict;
        public bool DashRefillRestrict;
        public bool StaminaRefillRestrict;
        public int TypeAsInt => (DashRestrict ? 1 : 0) + (DashRefillRestrict ? 2 : 0) + (StaminaRefillRestrict ? 4 : 0);

        public RefillCancelSpace(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            context = RefillCancelContext.For(engine);
            Depth = data.Int("Depth", 1);
            draw = data.Bool("Draw", true);
            DashRestrict = data.Bool("NoDash");
            DashRefillRestrict = data.Bool("NoDashRefill");
            StaminaRefillRestrict = data.Bool("NoStaminaRefill");
            width = data.Width;
            height = data.Height;
            Collider = new HitboxColliderGeometry(width, height);
            var random = ((ISourceRandomEnvironment)engine).Random;
            for (int i = 0; i < width * height / 64f; i++)
                particles.Add(new Vector2(SourceRandomMath.NextFloat(random, width - 1f),
                    SourceRandomMath.NextFloat(random, height - 1f)));
            var arithmetic = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            directions[1] = new[] { Vector2.UnitY };
            directions[2] = new[] { arithmetic.Rotate(Vector2.UnitY, 2.0944057f) };
            directions[3] = new[] { arithmetic.Rotate(Vector2.UnitY, 1.0471871f) };
            directions[4] = new[] { arithmetic.Rotate(Vector2.UnitY, 4.18878f) };
            directions[5] = new[] { arithmetic.Rotate(Vector2.UnitY, 5.235998f) };
            directions[6] = new[] { arithmetic.Rotate(Vector2.UnitY, (float)Math.PI) };
            directions[7] = new Vector2[6];
            for (int i = 0; i < 6; i++) directions[7][i] = arithmetic.Rotate(Vector2.UnitY, (float)Math.PI * i / 3f);
        }

        public override void Update()
        {
            base.Update();
            if (!draw) return;
            var settings = context.Settings;
            color = settings.ColorblindRCS ? BWEval[TypeAsInt] :
                settings.DecreaseParticles != ColorRefillType.None ? ColorEval[TypeAsInt] : ColorEvalNone[TypeAsInt];
            if (settings.DecreaseParticles == ColorRefillType.None) return;
            if (TypeAsInt < 1 || TypeAsInt > 7) throw new Exception("Temp was invalid: " + TypeAsInt);
            Vector2[] list = directions[TypeAsInt];
            for (int i = 0, count = particles.Count; i < count; i += 1 + 2 * (int)settings.DecreaseParticles)
            {
                Vector2 value = particles[i];
                value = TypeAsInt != 7
                    ? value + list[Math.Abs(unchecked((i - 24) * (i + 19) * (i + 21) * (i - 4))) % list.Length] *
                    speeds[i % speeds.Length] * engine.DeltaTime
                    : value + list[((ISourceRandomEnvironment)engine).Random.Next(0, list.Length)] *
                    speeds[i % speeds.Length] * 1.5f * engine.DeltaTime;
                value.Y = Mod(value.Y, height - 1f);
                value.X = Mod(value.X, width - 1f);
                particles[i] = value;
            }

            base.Update();
        }

        protected float Mod(float value, float modulus) => (value % modulus + modulus) % modulus;
    }
}
