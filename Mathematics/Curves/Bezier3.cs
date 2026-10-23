using System;
using System.Numerics;
using Celeste.Core.Mathematics;


namespace Celeste.Mods.VivHelper.Mathematics.Curves
{
    public class Bezier3 : BezierObject
    {
        public static Bezier3 end = new Bezier3(Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero);

        public Vector2 point1;

        public Vector2 point2;

        public Vector2 control1;

        public Vector2 control2;

        public new int tStart = 0;

        public new int tEnd = 1;

        public static string Formula = "(1-t)^3 * P1 + 3*(1-t)^2*t * C1 + 3*t^2*(1-t) * C2 + t^3 * P2";

        public Bezier3(Vector2 start, Vector2 controlStart, Vector2 controlEnd, Vector2 end)
        {
            point1 = start;
            point2 = end;
            control1 = controlStart;
            control2 = controlEnd;
        }

        public override string ToString()
        {
            return "{ " + FormatPoint(point1) + ", " + FormatPoint(control1) + ", " + FormatPoint(control2) + ", " +
                   FormatPoint(point2) + " }";
        }

        public override Vector2 GetPoint(double j)
        {
            double num = 1.0 - j;
            double num2 = num * num * num * (double)point1.X + 3.0 * num * num * j * (double)control1.X +
                          3.0 * num * j * j * (double)control2.X + j * j * j * (double)point2.X;
            double num3 = num * num * num * (double)point1.Y + 3.0 * num * num * j * (double)control1.Y +
                          3.0 * num * j * j * (double)control2.Y + j * j * j * (double)point2.Y;
            return new Vector2((float)num2, (float)num3);
        }

        public static Vector2 GetPoint(float percent, Vector2 start, Vector2 controlStart, Vector2 controlEnd,
            Vector2 end)
        {
            return EvaluatePoint(percent, start, controlStart, controlEnd, end);
        }

        public override Vector2 GetDerivative(double t)
        {
            double num = 3.0 * (1.0 - t) * (1.0 - t) * (double)(control1.X - point1.X) +
                         6.0 * (1.0 - t) * t * (double)(control2.X - control1.X) +
                         3.0 * t * t * (double)(point2.X - control2.X);
            double num2 = 3.0 * (1.0 - t) * (1.0 - t) * (double)(control1.Y - point1.Y) +
                          6.0 * (1.0 - t) * t * (double)(control2.Y - control1.Y) +
                          3.0 * t * t * (double)(point2.Y - control2.Y);
            return new Vector2((float)num, (float)num2);
        }

        public override float GetBezierLength(int resolution = 10)
        {
            if (resolution > 100)
            {
                resolution = 100;
            }

            float num = 0f;
            for (int i = 0; i < resolution; i++)
            {
                num += InstalledVectorArithmetic.Shared.Distance(GetPoint((float)(i + 1) / (float)resolution),
                    GetPoint((float)i / (float)resolution));
            }

            return num;
        }

        public static float GetBezierLength(Vector2 start, Vector2 controlStart, Vector2 controlEnd, Vector2 end,
            int resolution = 10)
        {
            if (resolution > 100) resolution = 100;
            float length = 0f;
            for (int i = 0; i < resolution; i++)
            {
                length += InstalledVectorArithmetic.Shared.Distance(
                    EvaluatePoint((float)(i + 1) / (float)resolution, start, controlStart, controlEnd, end),
                    EvaluatePoint((float)i / (float)resolution, start, controlStart, controlEnd, end));
            }

            return length;
        }


        private static Vector2 EvaluatePoint(double j, Vector2 start, Vector2 firstControl, Vector2 secondControl,
            Vector2 endPoint)
        {
            double num = 1.0 - j;
            double x = num * num * num * (double)start.X + 3.0 * num * num * j * (double)firstControl.X +
                       3.0 * num * j * j * (double)secondControl.X + j * j * j * (double)endPoint.X;
            double y = num * num * num * (double)start.Y + 3.0 * num * num * j * (double)firstControl.Y +
                       3.0 * num * j * j * (double)secondControl.Y + j * j * j * (double)endPoint.Y;
            return new Vector2((float)x, (float)y);
        }
    }
}
