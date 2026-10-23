// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Graphics.Rendering;
using Celeste.Mods.VivHelper.Runtime.RoomWrapping;

namespace Celeste.Mods.VivHelper.Entities.Controllers.RoomWrapping
{
    public partial class RoomWrapController
    {
        public override void DebugRender(ISourceRenderContext context, object actualCamera)
        {
            if (!RoomWrapFlags.All(level, flag)) return;
            var bounds = level.Bounds;
            var color = PackedColor.LightSeaGreen * .5f;
            if (scrollT)
                SourcePrimitiveDrawing.Line(context, this,
                    new(bounds.Left + 8f + playerOffsets[3], bounds.Top + 8f + playerOffsets[0]),
                    new(bounds.Right - 8f + playerOffsets[1], bounds.Top + 8f + playerOffsets[0]),
                    color, 1f, ((ISourceVectorEnvironment)engine).VectorArithmetic);
            if (scrollR)
                SourcePrimitiveDrawing.Line(context, this,
                    new(bounds.Right - 8f + playerOffsets[1], bounds.Top + 8f + playerOffsets[0]),
                    new(bounds.Right - 8f + playerOffsets[1], bounds.Bottom - 8f + playerOffsets[2]),
                    color, 1f, ((ISourceVectorEnvironment)engine).VectorArithmetic);
            if (scrollB)
                SourcePrimitiveDrawing.Line(context, this,
                    new(bounds.Left + 8f + playerOffsets[3], bounds.Bottom - 8f + playerOffsets[2]),
                    new(bounds.Right - 8f + playerOffsets[1], bounds.Bottom - 8f + playerOffsets[2]),
                    color, 1f, ((ISourceVectorEnvironment)engine).VectorArithmetic);
            if (scrollL)
                SourcePrimitiveDrawing.Line(context, this,
                    new(bounds.Left + 8f + playerOffsets[3], bounds.Top + 8f + playerOffsets[0]),
                    new(bounds.Left + 8f + playerOffsets[3], bounds.Bottom - 8f + playerOffsets[2]),
                    color, 1f, ((ISourceVectorEnvironment)engine).VectorArithmetic);
        }
    }
}
