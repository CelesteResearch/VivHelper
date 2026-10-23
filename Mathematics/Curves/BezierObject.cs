using System;
using System.Numerics;
using Celeste.Core.Mathematics;

namespace Celeste.Mods.VivHelper.Mathematics.Curves
{
    public abstract class BezierObject
    {
        public int tStart;

        public int tEnd;

        public abstract override string ToString();

        public abstract Vector2 GetPoint(double t);

        public abstract Vector2 GetDerivative(double t);

        public virtual Vector2[] GetOffsetPoints(float distance, float t)
        {
            GetOffsetPoints(distance, t, out Vector2 first, out Vector2 second);
            return new[] { first, second };
        }

        public virtual void GetOffsetPoints(float distance, float t, out Vector2 first, out Vector2 second)
        {
            Vector2 derivative = GetDerivative(t);
            Vector2 point = GetPoint(t);
            float num = (float)Math.Sqrt(derivative.X * derivative.X + derivative.Y * derivative.Y);
            first = new Vector2(point.X + distance * derivative.Y / num, point.Y - distance * derivative.X / num);
            second = new Vector2(point.X + -1f * distance * derivative.Y / num,
                point.Y - -1f * distance * derivative.X / num);
        }


        public virtual float GetBezierLength()
        {
            return GetBezierLength(10);
        }

        public abstract float GetBezierLength(int resolution);

        public virtual Vector2 GetPointFromLength(float length)
        {
            float num = 0f;
            float num2 = -0.04f;
            do
            {
                num2 += 0.04f;
                if (!(num2 > 1f))
                {
                    num += InstalledVectorArithmetic.Shared.Distance(GetPoint(num2 + 0.04f), GetPoint(num2));
                }
            } while (num <= length && !(num2 > 1f));

            if (num2 > 1f)
            {
                num2 = 1f;
            }

            do
            {
                num2 -= 0.0016f;
                num -= InstalledVectorArithmetic.Shared.Distance(GetPoint(num2 + 0.0016f), GetPoint(num2));
            } while (num > length);

            return GetPoint(num2);
        }

        protected static string FormatPoint(Vector2 point)
        {
            return "{X:" + point.X.ToString("G6", System.Globalization.CultureInfo.CurrentCulture)
                         + " Y:" + point.Y.ToString("G6", System.Globalization.CultureInfo.CurrentCulture) + "}";
        }
    }
}
