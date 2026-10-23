// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Rendering;
using Celeste.Core.Mathematics.LinearAlgebra;

namespace Celeste.Mods.VivHelper.Entities.Collectibles.RefillWalls
{
    public partial class WrapperRefillWall
    {
        public override void Render(ISourceRenderContext context)
        {
            var camera = ((OriginalLevelSceneRoot)Scene).Camera;
            if (Right < camera.Left || Left > camera.Right || Bottom < camera.Top || Top > camera.Bottom) return;
            if (respawnTimer > 0f)
            {
                for (int i = 0; i < Width; i += 8)
                {
                    SourcePrimitiveDrawing.Line(context, this, TopLeft + Vector2.UnitX * (i + 2),
                        TopLeft + Vector2.UnitX * (i + 6), color2, vectors);
                    SourcePrimitiveDrawing.Line(context, this, BottomLeft + Vector2.UnitX * (i + 2),
                        BottomLeft + Vector2.UnitX * (i + 6), color2, vectors);
                }

                for (int i = 0; i < Height; i += 8)
                {
                    SourcePrimitiveDrawing.Line(context, this, TopLeft + Vector2.UnitY * (i + 2),
                        TopLeft + Vector2.UnitY * (i + 6), color2, vectors);
                    SourcePrimitiveDrawing.Line(context, this, TopRight + Vector2.UnitY * (i + 2),
                        TopRight + Vector2.UnitY * (i + 6), color2, vectors);
                }

                if (texture != null) texture.Color = PackedColor.White * .25f;
            }
            else
            {
                SourcePrimitiveDrawing.HollowRect(context, this, X - 1f, Y - 1f, Width + 2f, Height + 2f, color2);
                SourcePrimitiveDrawing.Rect(context, this, X + 1f, Y + 1f, Width - 2f, Height - 2f, color1);
                if (texture != null) texture.Color = PackedColor.White;
            }

            if (forceRender)
            {
                tiedEntity.Render(context);
                return;
            }

            if (drawWithOutline) texture.DrawOutline(context, 1);
            texture.Render(context);
        }
    }
}
