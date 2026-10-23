// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Maps;
using Celeste.Game.Controls.Source;

namespace Celeste.Mods.VivHelper.Entities.Solids.FallingBlocks
{
    public partial class CornerBoostFallingBlock
    {
        private IEnumerator Sequence()
        {
            while (!Triggered && (finalBoss || !PlayerFallCheck())) yield return null;
            while (FallDelay > 0f)
            {
                FallDelay -= engine.Frame.DeltaTime;
                yield return null;
            }

            HasStartedFalling = true;
            while (true)
            {
                ShakeSfx();
                StartShaking();
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                    RumbleLength.Medium);
                if (finalBoss) Add(new SimulationCoroutine(engine, HighlightFade(1f)));
                yield return .2f;
                var timer = finalBoss ? .2f : .4f;
                while (timer > 0f && PlayerWaitCheck())
                {
                    yield return null;
                    timer -= engine.Frame.DeltaTime;
                }

                StopShaking();
                FallParticles();
                var speed = 0f;
                var maxSpeed = finalBoss ? 130f : 160f;
                while (true)
                {
                    var level = Level;
                    speed = ScalarMath.Approach(speed, maxSpeed, 500f * engine.Frame.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                    if (MoveVCollideSolids(speed * engine.Frame.DeltaTime, true)) break;
                    if (Top > level.Bounds.Bottom + 16 || Top > level.Bounds.Bottom - 1 &&
                        CheckAt("Celeste.Solid", Position + new Vector2(0f, 1f)))
                    {
                        Visible = false;
                        Collidable = false;
                        yield return .2f;
                        if (((OriginalChapterMapData)level.Session.MapData).CanTransitionTo(level,
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

                ImpactSfx();
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                    RumbleLength.Medium);
                Level.DirectionalShake(Vector2.UnitY, finalBoss ? .2f : .3f);
                if (finalBoss) Add(new SimulationCoroutine(engine, HighlightFade(0f)));
                StartShaking();
                LandParticles();
                yield return .2f;
                StopShaking();
                if (CheckAt("Celeste.SolidTiles", Position + new Vector2(0f, 1f))) break;
                while (CheckAt("Celeste.Platform", Position + new Vector2(0f, 1f))) yield return .1f;
            }

            Safe = true;
        }
    }
}
