using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.VivHelper.Backdrops.Weather.Rain
{
    [RuntimeTypeAlias("VivHelper.Effects.CustomRain")]
    public class CustomRain : BackdropPresentation
    {
        public struct Particle
        {
            public Vector2 Position;
            public Vector2 Speed;
            public float Rotation;
            public Vector2 Scale;
            public PackedColor color;

            public void Init(ISourceEngineContext engine, float rot, float speedMult, PackedColor color)
            {
                Position = new Vector2(
                    -32f + SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, 384f),
                    -32f + SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, 244f));
                Rotation = rot;
                Speed = Vector2Geometry.AngleToVector(Rotation,
                    SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, 200f * speedMult,
                        600f * speedMult));
                Scale = new Vector2(
                    4f + (((ISourceVectorEnvironment)engine).VectorArithmetic.Length(Speed) - 200f) * speedMult /
                    33.33333f, 1f);
                this.color = color;
            }
        }

        private readonly ISourceEngineContext engine;
        public float speedMult;
        public float alpha;
        public Particle[] particles;
        public int count;
        private float visibleFade = 1f;
        private float linearFade = 1f;
        private ISourceRandom Random => ((ISourceRandomEnvironment)engine).Random;

        public CustomRain(ISourceEngineContext engine, Vector2 scroll, float angle, float angleDiff, float speedMult,
            int count, string colors, float alpha)
        {
            this.engine = engine;
            Scroll = scroll;
            this.count = count;
            particles = new Particle[count];
            string[] names = colors.Split(',');
            var palette = new PackedColor[names.Length];
            for (int i = 0; i < names.Length; i++) palette[i] = SpawnPointRules.ParseColor(names[i].Trim());
            float rotation = angle * (-(float)Math.PI / 180f);
            float difference = Math.Abs(angleDiff * ((float)Math.PI / 180f));
            for (int i = 0; i < count; i++)
                particles[i].Init(engine, rotation + SourceRandomMath.Range(Random, -difference, difference),
                    speedMult, RandomSelection.Choose(Random, palette));
            this.alpha = alpha;
        }

        public override void Update(ISourceBackdropScene scene)
        {
            base.Update(scene);
            bool raining = scene.Raining = IsVisible(scene);
            visibleFade = ScalarMath.Approach(visibleFade, raining ? 1f : 0f, engine.DeltaTime * (raining ? 10f : .25f),
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            if (FadeX != null) linearFade = FadeX.Value(scene.Camera.X + 160f);
            for (int i = 0; i < count; i++) particles[i].Position += particles[i].Speed * engine.DeltaTime;
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            if (!(alpha > 0f) || !(visibleFade > 0f) || !(linearFade > 0f)) return;
            var camera = scene.Camera;
            for (int i = 0; i < particles.Length; i++)
            {
                var position = new Vector2(ScalarMath.Mod(particles[i].Position.X - camera.X, 320f),
                    ScalarMath.Mod(particles[i].Position.Y - camera.Y, 180f));
                DrawParticle(context, i, position);
                var extent =
                    ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(particles[i].Scale,
                        particles[i].Rotation);
                if (position.Y + extent.Y > 180f)
                {
                    if (position.X + extent.X > 320f)
                        DrawParticle(context, i, new Vector2(position.X - 320f, position.Y - 180f));
                    else DrawParticle(context, i, new Vector2(position.X, position.Y - 180f));
                }
                else if (position.X + extent.X > 320f)
                    DrawParticle(context, i, new Vector2(position.X - 320f, position.Y));
            }
        }

        private void DrawParticle(ISourceRenderContext context, int index, Vector2 position)
        {
            var pixel = ((ISourceDrawScope)engine).Draw.Pixel;
            var draw = new AtlasTextureDraw(pixel, position, pixel.Center,
                particles[index].color * alpha * linearFade * visibleFade,
                particles[index].Scale, particles[index].Rotation);
            ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, null);
        }
    }
}
