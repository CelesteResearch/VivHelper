using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Graphics.Rendering;

namespace Celeste.Mods.VivHelper.Entities.Collectibles.RefillWalls
{
    public partial class RefillWall
    {
        public override void Render(ISourceRenderContext context)
        {
            var camera = ((Celeste.Game.Execution.Levels.OriginalLevelSceneRoot)Scene).Camera;
            if (Right < camera.Left || Left > camera.Right || Bottom < camera.Top || Top > camera.Bottom) return;
            var fill = ColorDetermine(true);
            var border = ColorDetermine(false);
            if (respawnTimer > 0f)
            {
                for (int i = 0; i < Width; i += 8)
                {
                    SourcePrimitiveDrawing.Line(context, this, TopLeft + Vector2.UnitX * (i + 2),
                        TopLeft + Vector2.UnitX * (i + 6), border,
                        ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic);
                    SourcePrimitiveDrawing.Line(context, this, BottomLeft + Vector2.UnitX * (i + 2),
                        BottomLeft + Vector2.UnitX * (i + 6), border,
                        ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic);
                }

                for (int i = 0; i < Height; i += 8)
                {
                    SourcePrimitiveDrawing.Line(context, this, TopLeft + Vector2.UnitY * (i + 2),
                        TopLeft + Vector2.UnitY * (i + 6), border,
                        ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic);
                    SourcePrimitiveDrawing.Line(context, this, TopRight + Vector2.UnitY * (i + 2),
                        TopRight + Vector2.UnitY * (i + 6), border,
                        ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic);
                }

                if (!alt) sprite.Color = PackedColor.White * 0.25f;
            }
            else if (oneUse)
            {
                SourcePrimitiveDrawing.HollowRect(context, this, X - 1f, Y - 1f, Width + 2f, Height + 2f,
                    border * 0.6f);
                for (int i = 0; i < Width; i += 8)
                {
                    SourcePrimitiveDrawing.Line(context, this, TopLeft - Vector2.UnitY + Vector2.UnitX * (i + 2),
                        TopLeft - Vector2.UnitY + Vector2.UnitX * (i + 6), border,
                        ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic);
                    SourcePrimitiveDrawing.Line(context, this, BottomLeft + Vector2.UnitX * (i + 2),
                        BottomLeft + Vector2.UnitX * (i + 6), border,
                        ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic);
                }

                for (int i = 0; i < Height; i += 8)
                {
                    SourcePrimitiveDrawing.Line(context, this, TopLeft + Vector2.UnitY * (i + 2),
                        TopLeft + Vector2.UnitY * (i + 6), border,
                        ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic);
                    SourcePrimitiveDrawing.Line(context, this, TopRight + Vector2.UnitX + Vector2.UnitY * (i + 2),
                        TopRight + Vector2.UnitX + Vector2.UnitY * (i + 6), border,
                        ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic);
                }

                SourcePrimitiveDrawing.Rect(context, this, X + 1f, Y + 1f, Width - 2f, Height - 2f, fill);
                if (alt) altSprite.Render(context);
                else sprite.Render(context);
            }
            else
            {
                SourcePrimitiveDrawing.HollowRect(context, this, X - 1f, Y - 1f, Width + 2f, Height + 2f, border);
                SourcePrimitiveDrawing.Rect(context, this, X + 1f, Y + 1f, Width - 2f, Height - 2f, fill);
                if (!alt) sprite.Color = PackedColor.White;
            }

            if (alt) altSprite.Render(context);
            else sprite.Render(context);
        }

        private PackedColor ColorDetermine(bool fill)
        {
            switch (colorState)
            {
                case RefillWallColorState.Normal:
                    if (fill)
                        return (twoDashes ? new PackedColor(189, 64, 148, 255) : new PackedColor(32, 128, 32, 255)) *
                               alpha;
                    return (twoDashes ? new PackedColor(226, 104, 209, 255) : new PackedColor(147, 189, 64, 255)) *
                           alpha;
                case RefillWallColorState.WhiteFlash: return PackedColor.White * 0f;
                case RefillWallColorState.Outline:
                    return fill ? PackedColor.Transparent : new PackedColor(128, 128, 128, 255) * (alpha * 0.25f);
                default: throw new Exception("InvalidColorState");
            }
        }
    }
}
