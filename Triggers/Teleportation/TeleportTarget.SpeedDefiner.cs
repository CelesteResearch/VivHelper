// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Triggers.Teleportation
{
    public partial class TeleportTarget
    {
        [RuntimeTypeAlias("VivHelper.Triggers.TeleportTarget+SpeedModifier")]
        public enum SpeedModifier
        {
            NoChange,
            Add,
            Multiply,
            Set
        }

        [RuntimeTypeAlias("VivHelper.Triggers.TeleportTarget+SpeedDefiner")]
        public class SpeedDefiner
        {
            private SpeedModifier mod;
            private Vector2 dv;
            private bool setByLength;
            private readonly ISourceVectorArithmetic arithmetic;

            public SpeedDefiner(ISourceVectorArithmetic arithmetic, SpeedModifier modifier, float strength)
            {
                this.arithmetic = arithmetic;
                mod = modifier;
                dv = new Vector2(modifier == SpeedModifier.Add ? strength * .7071067f : strength);
                if (mod == SpeedModifier.Set) setByLength = true;
            }

            public SpeedDefiner(ISourceVectorArithmetic arithmetic, SpeedModifier modifier, float x, float y)
            {
                this.arithmetic = arithmetic;
                mod = modifier;
                dv = new Vector2(x, y);
            }

            public Vector2 ModifySpeed(Vector2 input)
                => mod switch
                {
                    SpeedModifier.Multiply => new Vector2(input.X * dv.X, input.Y * dv.Y),
                    SpeedModifier.Set => setByLength ? arithmetic.SafeNormalize(input, Vector2.Zero) * dv.X : dv,
                    SpeedModifier.Add => input + dv,
                    _ => input
                };
        }
    }
}
