// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Game.Controls.Source;
using Celeste.Game.Content.Maps;

namespace Celeste.Mods.VivHelper.Entities.FallingBlocks;

public partial class CustomFallingBlock
{
    private IEnumerator Sequence()
    {
        while (!Triggered && !EndShaking && (finalBoss || !PlayerFallCheck())) yield return null;
        Triggered = true;
        while (!EndShaking && FallDelay > 0f)
        {
            FallDelay -= engine.Frame.DeltaTime;
            yield return null;
        }

        HasStartedFalling = true;
        while (true)
        {
            if (!EndShaking)
            {
                ShakeSfxCore();
                StartShaking();
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                    RumbleLength.Medium);
                if (finalBoss) OwnedComponents.Add(new SimulationCoroutine(engine, HighlightFadeCore(1f)));
                yield return .2f;
                float timer = finalBoss ? .2f : .4f;
                while (timer > 0f && PlayerWaitCheck())
                {
                    yield return null;
                    timer -= engine.Frame.DeltaTime;
                }

                StopShaking();
                EndShaking = true;
            }

            EmitFallParticlesCore();
            float speed = 0f;
            while (true)
            {
                speed = ScalarMath.Approach(speed, maxSpeed, accel * engine.Frame.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                if (MoveVCollideSolids((float)Math.Sin(angle) * speed * engine.Frame.DeltaTime, dashBlock))
                {
                    Level.Session.SetFlag(flagOnGround, true);
                    break;
                }

                if (MoveHCollideSolids((float)Math.Cos(angle) * speed * engine.Frame.DeltaTime, dashBlock))
                {
                    Level.Session.SetFlag(flagOnGround, true);
                    break;
                }

                if (Top > Level.Bounds.Bottom + 16 ||
                    (Top > Level.Bounds.Bottom - 1 && CheckAt("Celeste.Solid", ImpactCenter)))
                {
                    Visible = false;
                    Collidable = false;
                    yield return .2f;
                    if (((OriginalChapterMapData)Level.Session.MapData).CanTransitionTo(Level,
                            new Vector2(Center.X, Bottom + 12f)))
                    {
                        yield return .2f;
                        Level.Shake(.3f);
                        ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                            RumbleLength.Medium);
                    }

                    RemoveSelf();
                    DestroyStaticMovers();
                    yield break;
                }

                yield return null;
            }

            ImpactSfxCore();
            ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                RumbleLength.Medium);
            var direction = ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitX, angle);
            Level.DirectionalShake(direction, finalBoss ? .2f : .3f);
            if (finalBoss) OwnedComponents.Add(new SimulationCoroutine(engine, HighlightFadeCore(0f)));
            StartShaking();
            LandParticlesCore();
            yield return .2f;
            StopShaking();
            if (!DisableEndShake) EndShaking = false;
            if (CheckAt("Celeste.SolidTiles",
                    Position + ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitX, angle))) break;
            while (CheckAt("Celeste.Platform",
                       Position + ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitX, angle)))
                yield return .1f;
        }

        Safe = true;
    }
}
