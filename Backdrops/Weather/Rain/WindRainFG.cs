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
    [RuntimeTypeAlias("VivHelper.Effects.WindRainFG")]
    public class WindRainFG : BackdropPresentation
    {
        private struct Particle
        {
            public Vector2 Position;
            public float Speed;
            public float Rotation;
            public Vector2 Scale;
        }

        private readonly ISourceEngineContext engine;
        private readonly ISourceVectorArithmetic vectors;
        private readonly ISourceExtremaArithmetic extrema;
        private readonly Particle[] particles = new Particle[240];
        private float visibleFade = 1f;
        private float linearFade = 1f;
        public float Alpha = 1f;
        public PackedColor[] Colors;
        public float windStrength;

        public WindRainFG(ISourceEngineContext engine, Vector2 scroll, string colors, float windStrength)
        {
            this.engine = engine;
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            extrema = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic;
            Scroll = scroll;
            this.windStrength = windStrength;
            if (colors == "") Colors = [SpawnPointRules.ParseColor("161933")];
            else
            {
                var values = colors.Split(',');
                Colors = new PackedColor[values.Length];
                // The installed implementation discards the results of Trim and TrimStart.
                for (var i = 0; i < values.Length; i++) Colors[i] = SpawnPointRules.ParseColor(values[i]);
            }

            var random = ((ISourceRandomEnvironment)engine).Random;
            for (var i = 0; i < particles.Length; i++)
            {
                ref var particle = ref particles[i];
                particle.Position = new Vector2(-32f + SourceRandomMath.NextFloat(random, 384f),
                    -32f + SourceRandomMath.NextFloat(random, 244f));
                particle.Rotation = (float)Math.PI / 2f + SourceRandomMath.Range(random, -.05f, .05f);
                particle.Speed = SourceRandomMath.Range(random, 200f, 600f);
                particle.Scale = new Vector2(4f + (particle.Speed - 200f) / 400f * 12f, 1f);
            }
        }

        public override void Update(ISourceBackdropScene scene)
        {
            base.Update(scene);
            var visible = scene.Raining = IsVisible(scene);
            visibleFade = ScalarMath.Approach(visibleFade, visible ? 1f : 0f,
                engine.Frame.DeltaTime * (visible ? 10f : .25f), extrema);
            if (FadeX != null) linearFade = FadeX.Value(scene.Camera.X + 160f);
            for (var i = 0; i < particles.Length; i++)
                particles[i].Position += (Vector2Geometry.AngleToVector(particles[i].Rotation, particles[i].Speed)
                                          + scene.Wind * windStrength) * engine.Frame.DeltaTime;
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            if (Alpha <= 0f || visibleFade <= 0f || linearFade <= 0f) return;
            // The original computes an alpha multiplier but never applies it to the draw color.
            var camera = scene.Camera;
            for (var i = 0; i < particles.Length; i++)
            {
                ref var particle = ref particles[i];
                var speed = (float)Math.Pow((particle.Speed - 400f) / 400f, 1.1);
                var rotation = Vector2Geometry.Angle(Vector2Geometry.AngleToVector(particle.Rotation,
                    (speed + 1f) * 400f) + scene.Wind * windStrength);
                var position = new Vector2(Mod(particle.Position.X - camera.X * Scroll.X, 320f),
                    Mod(particle.Position.Y - camera.Y * Scroll.Y, 180f));
                var color = Colors[(int)(i * Mod(i * (i + 2) * Math.Abs(i - 7.5f),
                    Math.Abs((2 * i - 1) * i - 3)) * (i + 4.5f)) % Colors.Length];
                Draw(context, scene, position, ParticleColor(i), particle.Scale, rotation);
                var extent = vectors.Rotate(particle.Scale, rotation);
                if (position.Y + extent.Y > 180f)
                    Draw(context, scene, new Vector2(position.X + extent.X > 320f ? position.X - 320f : position.X,
                        position.Y - 180f), ParticleColor(i), particle.Scale, rotation);
                else if (position.X + extent.X > 320f)
                    Draw(context, scene, new Vector2(position.X - 320f, position.Y), ParticleColor(i), particle.Scale,
                        rotation);
            }
        }

        private PackedColor ParticleColor(int i) => Colors[(int)(i * Mod(i * (i + 2) * Math.Abs(i - 7.5f),
            Math.Abs((2 * i - 1) * i - 3)) * (i + 4.5f)) % Colors.Length];

        private static float Mod(float value, float modulus) => (value % modulus + modulus) % modulus;

        private static void Draw(ISourceRenderContext context, ISourceBackdropScene scene, Vector2 position,
            PackedColor color, Vector2 scale, float rotation)
        {
            var pixel = ((ISourceDrawScope)scene.EngineContext).Draw.Pixel;
            var draw = new AtlasTextureDraw(pixel, position, pixel.Center, color, scale, rotation);
            ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, null);
        }
    }
}
