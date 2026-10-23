using System;
using System.Numerics;

namespace Celeste.Mods.VivHelper.Mathematics.Curves
{
    public class BezierSystem : BezierObject
    {
        public BezierObject[] curves;

        public new int tStart = 0;

        public new int tEnd;

        public bool single = false;

        public BezierSystem(BezierObject[] b)
        {
            curves = b;
            tEnd = curves.Length;
            single = tEnd == 1;
        }

        public override string ToString()
        {
            string text = "";
            BezierObject[] array = curves;
            foreach (BezierObject bezierObject in array)
            {
                text = text + bezierObject.ToString() + "\n";
            }

            return text;
        }

        public override Vector2 GetPoint(double t)
        {
            if (0.0 <= t && t <= (double)tEnd)
            {
                if (single)
                {
                    return curves[0].GetPoint(t);
                }

                return curves[(int)((t == (double)tEnd) ? (t - 0.009999999776482582) : t)]
                    .GetPoint((t == (double)tEnd) ? 1.0 : (t - (double)(int)t));
            }

            throw new IndexOutOfRangeException("Point Beyond Range");
        }

        public override Vector2 GetDerivative(double t)
        {
            if (0.0 <= t && t <= (double)tEnd)
            {
                if (single)
                {
                    return curves[0].GetDerivative(t);
                }

                return curves[(int)((t == (double)tEnd) ? (t - 0.009999999776482582) : t)]
                    .GetDerivative((t == (double)tEnd) ? 1.0 : (t - (double)(int)t));
            }

            throw new IndexOutOfRangeException("Point Beyond Range");
        }

        public override float GetBezierLength(int resolution = 10)
        {
            float num = 0f;
            BezierObject[] array = curves;
            foreach (BezierObject bezierObject in array)
            {
                num += bezierObject.GetBezierLength(resolution);
            }

            return num;
        }

        public override Vector2 GetPointFromLength(float length)
        {
            if (curves.Length == 1)
            {
                return curves[0].GetPointFromLength(length);
            }

            if (length > GetBezierLength(25))
            {
                return curves[curves.Length - 1].GetPoint(1.0);
            }

            float num = 0f;
            for (int i = 0; i < curves.Length; i++)
            {
                num += curves[i].GetBezierLength(25);
                if (num > length)
                {
                    num -= curves[i].GetBezierLength(25);
                    return curves[i].GetPointFromLength(length - num);
                }
            }

            return curves[curves.Length - 1].GetPoint(1.0);
        }

        public override Vector2[] GetOffsetPoints(float distance, float t)
        {
            if (single)
            {
                return curves[0].GetOffsetPoints(distance, t);
            }

            if (0f <= t && t <= (float)tEnd)
            {
                return curves[(int)((t == (float)tEnd) ? (t - 0.01f) : t)]
                    .GetOffsetPoints(distance, (t == (float)tEnd) ? 1f : (t - (float)(int)t));
            }

            throw new IndexOutOfRangeException("Point Beyond Range");
        }

        public override void GetOffsetPoints(float distance, float t, out Vector2 first, out Vector2 second)
        {
            if (single)
            {
                curves[0].GetOffsetPoints(distance, t, out first, out second);
                return;
            }

            if (0f <= t && t <= (float)tEnd)
            {
                curves[(int)((t == (float)tEnd) ? (t - 0.01f) : t)].GetOffsetPoints(distance,
                    (t == (float)tEnd) ? 1f : (t - (float)(int)t), out first, out second);
                return;
            }

            throw new IndexOutOfRangeException("Point Beyond Range");
        }
    }
}
