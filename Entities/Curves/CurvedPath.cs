using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Mods.VivHelper.Mathematics.Curves;
using Celeste.Core.Mathematics;
using Celeste.Mods.VivHelper.Presentation.Curves;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.VivHelper.Entities.Curves
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.CurvedPath")]
    public class CurvedPath : SceneEntityState
    {
        public float startPoint;

        public float endPoint;

        public int resolution;

        public BezierSystem bezierObject;

        public VivPathLine[] lines;

        private readonly ISourceVectorArithmetic vectors;

        public CurvedPath(ISourceEngineContext engine, BezierSystem bezier, int numOfLines, int resolution,
            float[] distances, PackedColor[] colors, string[] types, float[] thicknesses, Vector2[] offsets,
            float[] startPoints, float[] endPoints, bool[] addEnds)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, bezier.GetPoint(0.0))
        {
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            this.resolution = resolution;
            lines = new VivPathLine[numOfLines];
            for (int i = 0; i < numOfLines; i++)
            {
                lines[i] = new VivPathLine
                {
                    distance = distances[i],
                    color = colors[i],
                    type = types[i],
                    thickness = thicknesses[i],
                    startT = startPoints[i],
                    endT = endPoints[i],
                    offset = offsets[i],
                    addEnds = addEnds[i],
                    order = i
                };
            }

            bezierObject = bezier;
            Depth = 9001;
        }

        public CurvedPath(ISourceEngineContext engine, BezierSystem bezier, VivPathLine[] paths, int resolution = 20)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, bezier.GetPoint(0.0))
        {
            bezierObject = bezier;
            lines = paths;
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            this.resolution = resolution;
            Depth = 9001;
        }

        public override void Render(ISourceRenderContext context)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                VivPathLine vivPathLine = lines[i];
                if (vivPathLine.distance == 0f)
                {
                    if (bezierObject.single)
                    {
                        bezierObject.curves[0].RenderLine(context, this, vivPathLine.type, vivPathLine.startT,
                            vivPathLine.endT, vivPathLine.offset, resolution, vivPathLine.color, vivPathLine.thickness);
                    }
                    else
                    {
                        bezierObject.RenderLine(context, this, vivPathLine.type, vivPathLine.startT, vivPathLine.endT,
                            vivPathLine.offset, resolution, vivPathLine.color, vivPathLine.thickness);
                    }

                    continue;
                }

                if (bezierObject.single)
                {
                    bezierObject.curves[0].RenderPath(context, this, vivPathLine.distance, vivPathLine.startT,
                        vivPathLine.endT, vivPathLine.type, vivPathLine.offset, resolution, vivPathLine.color,
                        vivPathLine.thickness);
                }
                else
                {
                    bezierObject.RenderPath(context, this, vivPathLine.distance, vivPathLine.startT, vivPathLine.endT,
                        vivPathLine.type, vivPathLine.offset, resolution, vivPathLine.color, vivPathLine.thickness);
                }

                if (vivPathLine.addEnds)
                {
                    bezierObject.GetOffsetPoints(vivPathLine.distance, vivPathLine.startT, out Vector2 first,
                        out Vector2 second);
                    SourcePrimitiveDrawing.Line(context, this, first, second, vivPathLine.color, vectors);
                    bezierObject.GetOffsetPoints(vivPathLine.distance, vivPathLine.endT, out first, out second);
                    SourcePrimitiveDrawing.Line(context, this, first, second, vivPathLine.color, vectors);
                }
            }
        }
    }
}
