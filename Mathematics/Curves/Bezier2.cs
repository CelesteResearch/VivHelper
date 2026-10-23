using System;
using System.Numerics;
using Celeste.Core.Mathematics;


namespace Celeste.Mods.VivHelper.Mathematics.Curves
{
    public class Bezier2 : BezierObject
    {
        public static Bezier2 end = new Bezier2(Vector2.Zero, Vector2.Zero, Vector2.Zero);

        public Vector2 point1;

        public Vector2 control;

        public Vector2 point2;

        public new int tStart = 0;

        public new int tEnd = 1;

        public static string formula = "(1-t)^2 * P1 + 2*(1-t)*t * C * t^2 * P2";

        public Bezier2(Vector2 start, Vector2 controlPoint, Vector2 end)
        {
            point1 = start;
            point2 = end;
            control = controlPoint;
        }

        public override string ToString()
        {
            return "{ " + FormatPoint(point1) + ", " + FormatPoint(control) + ", " + FormatPoint(point2) + " }";
        }

        public override Vector2 GetPoint(double percent)
        {
            double num = (1.0 - percent) * (1.0 - percent) * (double)point1.X +
                         2.0 * (1.0 - percent) * percent * (double)control.X + percent * percent * (double)point2.X;
            double num2 = (1.0 - percent) * (1.0 - percent) * (double)point1.Y +
                          2.0 * (1.0 - percent) * percent * (double)control.Y + percent * percent * (double)point2.Y;
            return new Vector2((float)num, (float)num2);
        }

        public override Vector2 GetDerivative(double t)
        {
            double num = 2.0 * (1.0 - t) * (double)(control.X - point1.X) + 2.0 * t * (double)(point2.X - control.X);
            double num2 = 2.0 * (1.0 - t) * (double)(control.Y - point1.Y) + 2.0 * t * (double)(point2.Y - control.Y);
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
    }
}
