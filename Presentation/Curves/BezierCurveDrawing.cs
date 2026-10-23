using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Mods.VivHelper.Mathematics.Curves;
using Celeste.Core.Rendering;
using Celeste.Graphics.Rendering;

namespace Celeste.Mods.VivHelper.Presentation.Curves
{
    public static class BezierCurveDrawing
    {
        public static void RenderLine(this Bezier2 curve, ISourceRenderContext context, object owner,
            Vector2 offset = default, int resolution = 20, PackedColor color = default, float thiccness = 1.2f)
        {
            if (offset == default(Vector2))
            {
                offset = Vector2.Zero;
            }

            for (int i = 0; i < resolution; i++)
            {
                DrawLine(context, owner, curve.GetPoint(i / resolution) + offset,
                    curve.GetPoint((i + 1) / resolution) + offset,
                    (color != default(PackedColor)) ? color : PackedColor.White, thiccness);
            }
        }

        public static void RenderLine(this Bezier2 curve, ISourceRenderContext context, object owner, string type,
            float startPoint = 0f, float endPoint = 1f, Vector2 offset = default, int resolution = 20,
            PackedColor color = default, float thiccness = 0f)
        {
            if (!(thiccness > 0f))
            {
                return;
            }

            if (offset == default(Vector2))
            {
                offset = Vector2.Zero;
            }

            int num = 0;
            while (num < resolution)
            {
                if (type != "dotted")
                {
                    DrawLine(context, owner,
                        curve.GetPoint(startPoint + (endPoint - startPoint) * (float)num / (float)resolution) + offset,
                        curve.GetPoint(startPoint + (endPoint - startPoint) * (float)(num + 1) / (float)resolution) +
                        offset, (color != default(PackedColor)) ? color : PackedColor.White, thiccness);
                    num += ((!(type == "dashed")) ? 1 : 2);
                }
                else
                {
                    SourcePrimitiveDrawing.Circle(context, owner, curve.GetPoint(num / resolution) + offset, thiccness,
                        (color != default(PackedColor)) ? color : PackedColor.White, resolution,
                        InstalledVectorArithmetic.Shared);
                    num += 2;
                }
            }
        }

        public static void RenderPath(this Bezier2 curve, ISourceRenderContext context, object owner, float distance,
            float startPoint = 0f, float endPoint = 1f, string type = "line", Vector2 offset = default,
            int resolution = 20, PackedColor color = default, float thiccness = 0f)
        {
            if (thiccness > 0f)
            {
                if (offset == default(Vector2))
                {
                    offset = Vector2.Zero;
                }

                for (int i = 0; i < resolution; i += ((!(type == "dashed")) ? 1 : 2))
                {
                    curve.GetOffsetPoints(distance, startPoint + (endPoint - startPoint) * (float)i / (float)resolution,
                        out Vector2 first, out Vector2 second);
                    curve.GetOffsetPoints(distance,
                        startPoint + (endPoint - startPoint) * (float)(i + 1) / (float)resolution,
                        out Vector2 nextFirst, out Vector2 nextSecond);
                    DrawLine(context, owner, first + offset, nextFirst + offset,
                        (color != default(PackedColor)) ? color : PackedColor.White, thiccness);
                    DrawLine(context, owner, second + offset, nextSecond + offset,
                        (color != default(PackedColor)) ? color : PackedColor.White, thiccness);
                }
            }
        }

        public static void RenderLine(this Bezier3 curve, ISourceRenderContext context, object owner,
            Vector2 offset = default, int resolution = 20, PackedColor color = default, float thiccness = 1.2f)
        {
            if (thiccness > 0f)
            {
                if (offset == default(Vector2))
                {
                    offset = Vector2.Zero;
                }

                for (int i = 0; i < resolution; i++)
                {
                    DrawLine(context, owner, curve.GetPoint(i / resolution) + offset,
                        curve.GetPoint((i + 1) / resolution) + offset,
                        (color != default(PackedColor)) ? color : PackedColor.White, thiccness);
                }
            }
        }

        public static void RenderLine(this Bezier3 curve, ISourceRenderContext context, object owner, string type,
            float startPoint = 0f, float endPoint = 1f, Vector2 offset = default, int resolution = 20,
            PackedColor color = default, float thiccness = 1.2f)
        {
            if (!(thiccness > 0f))
            {
                return;
            }

            if (offset == default(Vector2))
            {
                offset = Vector2.Zero;
            }

            int num = 0;
            while (num < resolution)
            {
                if (type != "dotted")
                {
                    DrawLine(context, owner,
                        curve.GetPoint(startPoint + (endPoint - startPoint) * (float)num / (float)resolution) + offset,
                        curve.GetPoint(startPoint + (endPoint - startPoint) * (float)(num + 1) / (float)resolution) +
                        offset, (color != default(PackedColor)) ? color : PackedColor.White, thiccness);
                    num += ((!(type == "dashed")) ? 1 : 2);
                }
                else
                {
                    SourcePrimitiveDrawing.Circle(context, owner, curve.GetPoint(num / resolution) + offset, thiccness,
                        (color != default(PackedColor)) ? color : PackedColor.White, resolution,
                        InstalledVectorArithmetic.Shared);
                    num += 2;
                }
            }
        }

        public static void RenderPath(this Bezier3 curve, ISourceRenderContext context, object owner, float distance,
            float startPoint = 0f, float endPoint = 1f, string type = "line", Vector2 offset = default,
            int resolution = 20, PackedColor color = default, float thiccness = 1.2f)
        {
            if (thiccness > 0f)
            {
                if (offset == default(Vector2))
                {
                    offset = Vector2.Zero;
                }

                for (int i = 0; i < resolution; i += ((!(type == "dashed")) ? 1 : 3))
                {
                    curve.GetOffsetPoints(distance,
                        startPoint + (endPoint - 1E-05f - startPoint) * (float)i / (float)resolution, out Vector2 first,
                        out Vector2 second);
                    curve.GetOffsetPoints(distance,
                        startPoint + (endPoint - 1E-05f - startPoint) * (float)(i + 1) / (float)resolution,
                        out Vector2 nextFirst, out Vector2 nextSecond);
                    DrawLine(context, owner, first + offset, nextFirst + offset,
                        (color != default(PackedColor)) ? color : PackedColor.White, thiccness);
                    DrawLine(context, owner, second + offset, nextSecond + offset,
                        (color != default(PackedColor)) ? color : PackedColor.White, thiccness);
                }
            }
        }

        public static void RenderLine(this BezierSystem curve, ISourceRenderContext context, object owner, string type,
            float startPoint = 0f, float endPoint = -1f, Vector2 offset = default, int resolution = 20,
            PackedColor color = default, float thiccness = 1.2f)
        {
            if (!(thiccness > 0f))
            {
                return;
            }

            if (offset == default(Vector2))
            {
                offset = Vector2.Zero;
            }

            if (color == default(PackedColor))
            {
                color = PackedColor.White;
            }

            if (endPoint == -1f)
            {
                endPoint = curve.tEnd;
            }

            if (curve.tEnd == 1)
            {
                curve.curves[0].RenderLine(context, owner, type, startPoint, (endPoint < 1f) ? endPoint : 1f, offset,
                    resolution, color, thiccness);
                return;
            }

            for (int i = (int)startPoint; (float)i < endPoint; i++)
            {
                curve.curves[i].RenderLine(context, owner, type,
                    (startPoint - (float)i > 0f) ? (startPoint - (float)i) : 0f,
                    (endPoint - (float)i < 1f) ? (endPoint - (float)i) : 1f, offset, resolution, color, thiccness);
            }
        }

        public static void RenderPath(this BezierSystem curve, ISourceRenderContext context, object owner,
            float distance, float startPoint = 0f, float endPoint = -1f, string type = "line", Vector2 offset = default,
            int resolution = 20, PackedColor color = default, float thiccness = 1.2f)
        {
            if (thiccness > 0f)
            {
                if (offset == default(Vector2))
                {
                    offset = Vector2.Zero;
                }

                if (color == default(PackedColor))
                {
                    color = PackedColor.White;
                }

                if (endPoint == -1f)
                {
                    endPoint = curve.tEnd;
                }

                if (curve.tEnd == 1)
                {
                    curve.curves[0].RenderPath(context, owner, distance, startPoint, (endPoint < 1f) ? endPoint : 1f,
                        type, offset, resolution, color, thiccness);
                }

                for (int i = (int)startPoint; (float)i < endPoint; i++)
                {
                    curve.curves[i].RenderPath(context, owner, distance,
                        (startPoint - (float)i > 0f) ? (startPoint - (float)i) : 0f,
                        (endPoint - (float)i < 1f) ? (endPoint - (float)i) : 1f, type, offset, resolution, color,
                        thiccness);
                }
            }
        }

        public static void RenderLine(this BezierObject curve, ISourceRenderContext context, object owner, string type,
            float startPoint, float endPoint, Vector2 offset, int resolution, PackedColor color, float thiccness)
        {
            if (curve is Bezier2 bezier2)
            {
                bezier2.RenderLine(context, owner, type, startPoint, endPoint, offset, resolution, color, thiccness);
            }
            else if (curve is Bezier3 bezier3)
            {
                bezier3.RenderLine(context, owner, type, startPoint, endPoint, offset, resolution, color, thiccness);
            }
            else if (curve is BezierSystem beziersystem)
            {
                beziersystem.RenderLine(context, owner, type, startPoint, endPoint, offset, resolution, color,
                    thiccness);
            }
            else
            {
                throw new NotSupportedException("Unknown Bezier curve drawing implementation.");
            }
        }

        public static void RenderPath(this BezierObject curve, ISourceRenderContext context, object owner,
            float distance, float startPoint, float endPoint, string type, Vector2 offset, int resolution,
            PackedColor color, float thiccness)
        {
            if (curve is Bezier2 bezier2)
            {
                bezier2.RenderPath(context, owner, distance, startPoint, endPoint, type, offset, resolution, color,
                    thiccness);
            }
            else if (curve is Bezier3 bezier3)
            {
                bezier3.RenderPath(context, owner, distance, startPoint, endPoint, type, offset, resolution, color,
                    thiccness);
            }
            else if (curve is BezierSystem beziersystem)
            {
                beziersystem.RenderPath(context, owner, distance, startPoint, endPoint, type, offset, resolution, color,
                    thiccness);
            }
            else
            {
                throw new NotSupportedException("Unknown Bezier curve drawing implementation.");
            }
        }

        private static void DrawLine(ISourceRenderContext context, object owner, Vector2 first, Vector2 second,
            PackedColor color, float thickness)
        {
            float angle = (float)Math.Atan2(second.Y - first.Y, second.X - first.X);
            SourcePrimitiveDrawing.LineAngle(context, owner, first, angle,
                InstalledVectorArithmetic.Shared.Distance(first, second), color, thickness);
        }
    }
}
