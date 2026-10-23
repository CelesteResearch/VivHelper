using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Scheduling;
using Celeste.Scene.Tracking;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Platforms.ZipMovers;

namespace Celeste.Mods.VivHelper.Entities.Platforms.ZipMovers
{
    public partial class CustomCurvedZipMover
    {
        private IEnumerator SequenceLaunch()
        {
            Vector2 initial = Position;
            var input = ((IOriginalGameInputProcessEnvironment)Engine).InputSource;
            var scalar = ((ISourceScalarEnvironment)Engine).ScalarArithmetic;
            while (true)
            {
                if (!HasPlayerRider())
                {
                    yield return null;
                    continue;
                }

                sound.Play(
                    audio == ""
                        ? "event:/game/01_forsaken_city/zip_mover"
                        : "event:/new_content/game/10_farewell/zip_mover", null, 0f);
                input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(.1f);
                yield return .1f;
                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;
                float at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    at = ScalarMath.Approach(at, 1f, 2f * Engine.DeltaTime * speedModF, extrema);
                    percent = reverse ? Math.Abs(easing(at) * 2f - 1f) : 1f - Math.Abs(easing(at) * 2f - 1f);
                    Vector2 next = !moveType ? Vector2Interpolation.Lerp(initial, target, percent, scalar)
                        : !uniform ? curve.bezier.GetPoint(percent)
                        : curve.bezier.GetPointFromLength(percent * curve.bezier.GetBezierLength(25));
                    ZipMoverDrawing.ScrapeParticles(this, Engine, next);
                    if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(.1f, Engine) && !moveType)
                        pathRenderer.CreateSparks();
                    MoveTo(next - new Vector2(Width / 2f, Height / 2f));
                }

                StopPlayerRunIntoAnimation = true;
                StartShaking(.2f);
                streetlight.SetAnimationFrame(1);
                yield return PostLaunchPause;
            }
        }
    }
}
