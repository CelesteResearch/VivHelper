// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Geometry;
using Celeste.Scene.Tracking;
using Celeste.Core.Scheduling;
using Celeste.Simulation;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.VivHelper.Entities.Solids.Growing
{
    public partial class GrowBlock
    {
        private static readonly Action<LightOccludeState, IntRect?> SetLightBounds = BindLightBounds();

        private static Action<LightOccludeState, IntRect?> BindLightBounds()
        {
            var field = typeof(LightOccludeState).GetField("bounds", BindingFlags.Instance | BindingFlags.NonPublic);
            var method = new DynamicMethod("SetGrowLightBounds", typeof(void),
                new[] { typeof(LightOccludeState), typeof(IntRect?) }, typeof(GrowBlock), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<LightOccludeState, IntRect?>)method.CreateDelegate(
                typeof(Action<LightOccludeState, IntRect?>));
        }

        public void Resize()
        {
            var player = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(PlayerKey);
            if (player == null) return;
            var delta = Position + new Vector2(width * .5f, height * .5f) - player.Body.ExactPosition;
            var distance = vectors.LengthSquared(new Vector2(Math.Abs(delta.X), Math.Abs(delta.Y)));
            float threshold = Math.Max(width, height);
            threshold *= threshold;
            float progress = Math.Max(0f, distance - threshold) / factor;
            progress = Math.Min(1f, Math.Max(progress, 0f));
            progress = 1f - progress;
            alphaA = easing.QuadOut(progress);
            alphaB = easing.SineInOut(progress);
            alphaC = Math.Min(1f, 2f - 2f * easing.SineOut(1f - progress));
            offset = move * (1f - alphaA) * .25f * new Vector2(width, height);
            lightOcclude.Alpha = progress;
            SetLightBounds(lightOcclude, new IntRect((int)((1f - alphaB) * .5f * width + offset.X),
                (int)((1f - alphaB) * .5f * height + offset.Y), (int)(width * alphaB), (int)(height * alphaB)));
            Hitbox.Width = width * alphaB;
            Hitbox.Height = height * alphaB;
            Hitbox.Position = (1f - alphaB) * .5f * new Vector2(width, height) + offset;
        }
    }
}
