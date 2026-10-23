using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Game.Entities.Platforms.ZipMovers.Motion;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Core.Display;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Timing;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Audio.Sources;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Controls.Source;

namespace Celeste.Mods.VivHelper.Entities.Solids.ZipMovers
{
    public partial class CornerBoostZipMover
    {
        private IEnumerator Sequence()
        {
            Vector2 start = Position;
            while (true)
            {
                if (!HasPlayerRider())
                {
                    yield return null;
                    continue;
                }

                sfx.Play((theme == Themes.Normal)
                    ? "event:/game/01_forsaken_city/zip_mover"
                    : "event:/new_content/game/10_farewell/zip_mover");
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                    RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;
                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;
                float at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    Vector2 vector = ZipMoverMotion.Forward(engine, start, target, ref at, ref percent);
                    ScrapeParticlesCheck(vector);
                    if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(0.1f, engine))
                    {
                        pathRenderer.CreateSparks();
                    }

                    MoveTo(vector);
                }

                StartShaking(0.2f);
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                    RumbleLength.Medium);
                (Scene as OriginalLevelSceneRoot).Shake(0.3f);
                StopPlayerRunIntoAnimation = true;
                yield return 0.5f;
                StopPlayerRunIntoAnimation = false;
                streetlight.SetAnimationFrame(2);
                at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    Vector2 position = ZipMoverMotion.Backward(engine, target, start, ref at, ref percent);
                    MoveTo(position);
                }

                StopPlayerRunIntoAnimation = true;
                StartShaking(0.2f);
                streetlight.SetAnimationFrame(1);
                yield return 0.5f;
            }
        }
    }
}
