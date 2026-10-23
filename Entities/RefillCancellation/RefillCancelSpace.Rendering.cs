using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Rendering;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Rendering;
using Celeste.Mods.VivHelper.Persistence;

namespace Celeste.Mods.VivHelper.Entities.RefillCancellation
{
    public partial class RefillCancelSpace
    {
        public override void Render(ISourceRenderContext graphics)
        {
            var camera = (Scene as OriginalLevelSceneRoot)?.Camera;
            if (camera == null) return;
            if (!(Right > camera.Left - 8f) || !(Left < camera.Right + 8f) || !(Top < camera.Bottom + 16f) ||
                !(Bottom > camera.Top - 8f)) return;
            base.Render(graphics);
            if (!draw) return;
            if (context.Settings.RCSLines) AltDraw(graphics);
            else BaseDraw(graphics);
        }

        private PackedColor Mix(PackedColor first, PackedColor second, float amount)
        {
            var arithmetic = ((ISourceScalarEnvironment)engine).ScalarArithmetic;
            amount = ScalarMath.Clamp(amount, 0f, 1f, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            return new PackedColor((int)arithmetic.Lerp(first.R, second.R, amount),
                (int)arithmetic.Lerp(first.G, second.G, amount),
                (int)arithmetic.Lerp(first.B, second.B, amount), (int)arithmetic.Lerp(first.A, second.A, amount));
        }

        private PackedColor ParticleTint()
        {
            var settings = context.Settings;
            if (settings.ColorblindRCS) return color;
            var result = color;
            result.A = (byte)ScalarMath.Clamp(255f * (0.25f + ((int)settings.DecreaseParticles - 1) * 0.125f), 0f, 255f,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            return result;
        }

        private void Fill(ISourceRenderContext graphics, bool lines)
        {
            var amount = context.Settings.DecreaseParticles;
            var fill = amount == ColorRefillType.None
                ? color * 0.35f
                : (lines
                    ? Mix(color, PackedColor.White, 0.133f * ((int)amount + 1))
                    : Mix(PackedColor.White, color, 0.133f * ((int)amount + 1))) * 0.1f;
            SourcePrimitiveDrawing.Rect(graphics, this, Collider.AbsoluteLeft, Collider.AbsoluteTop, Collider.Width,
                Collider.Height, fill);
        }

        private void BaseDraw(ISourceRenderContext graphics)
        {
            Fill(graphics, false);
            if (context.Settings.DecreaseParticles == ColorRefillType.None) return;
            for (int i = 0; i < particles.Count; i += 1 + 2 * (int)context.Settings.DecreaseParticles)
                SourcePrimitiveDrawing.Point(graphics, this, Position + particles[i], ParticleTint());
        }

        private void AltDraw(ISourceRenderContext graphics)
        {
            Fill(graphics, true);
            if (context.Settings.DecreaseParticles == ColorRefillType.None) return;
            var arithmetic = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            Vector2 direction = TypeAsInt switch
            {
                1 => Vector2.UnitY,
                2 => arithmetic.Rotate(Vector2.UnitY, (float)Math.PI * 3f / 4f) * 1.414f,
                3 => arithmetic.Rotate(Vector2.UnitY, (float)Math.PI / 4f) * 1.414f,
                4 => arithmetic.Rotate(Vector2.UnitY, 3.926991f) * 1.414f,
                5 => arithmetic.Rotate(Vector2.UnitY, 5.4977875f) * 1.414f,
                6 => arithmetic.Rotate(Vector2.UnitY, (float)Math.PI),
                _ => Vector2.Zero
            };
            for (int i = 0; i < particles.Count; i += 1 + (int)(2f * (int)context.Settings.DecreaseParticles))
            {
                Vector2 position = Position + particles[i];
                if (TypeAsInt == 7) SourcePrimitiveDrawing.Point(graphics, this, position, ParticleTint());
                else
                    SourcePrimitiveDrawing.Line(graphics, this, position - direction, position + direction,
                        ParticleTint(), arithmetic);
            }
        }
    }
}
