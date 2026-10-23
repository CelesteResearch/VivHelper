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
        private void ScrapeParticlesCheck(Vector2 to)
        {
            if (!((ISourceScene)Scene).SourceScene.Clock.OnInterval(0.03f, engine))
            {
                return;
            }

            bool flag = to.Y != ExactPosition.Y;
            bool flag2 = to.X != ExactPosition.X;
            if (flag && !flag2)
            {
                int num = Math.Sign(to.Y - ExactPosition.Y);
                Vector2 vector = ((num != 1) ? TopLeft : BottomLeft);
                int num2 = 4;
                if (num == 1)
                {
                    num2 = Math.Min((int)Height - 12, 20);
                }

                int num3 = (int)Height;
                if (num == -1)
                {
                    num3 = Math.Max(16, (int)Height - 16);
                }

                if (SourceSceneCollision.CheckPoint(((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid",
                        SourceVector2Math.Add(vector, new Vector2(-2f, num * -2))))
                {
                    for (int i = num2; i < num3; i += 8)
                    {
                        (Scene as OriginalLevelSceneRoot).ParticlesFG.Emit(
                            particleTypes.P_Scrape,
                            SourceVector2Math.Add(TopLeft, new Vector2(0f, (float)i + (float)num * 2f)),
                            (num == 1) ? (-(float)Math.PI / 4f) : ((float)Math.PI / 4f));
                    }
                }

                if (SourceSceneCollision.CheckPoint(((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid",
                        SourceVector2Math.Add(vector, new Vector2(Width + 2f, num * -2))))
                {
                    for (int j = num2; j < num3; j += 8)
                    {
                        (Scene as OriginalLevelSceneRoot).ParticlesFG.Emit(
                            particleTypes.P_Scrape,
                            SourceVector2Math.Add(TopRight, new Vector2(-1f, (float)j + (float)num * 2f)),
                            (num == 1) ? ((float)Math.PI * -3f / 4f) : ((float)Math.PI * 3f / 4f));
                    }
                }
            }
            else
            {
                if (!flag2 || flag)
                {
                    return;
                }

                int num4 = Math.Sign(to.X - ExactPosition.X);
                Vector2 vector2 = ((num4 != 1) ? TopLeft : TopRight);
                int num5 = 4;
                if (num4 == 1)
                {
                    num5 = Math.Min((int)Width - 12, 20);
                }

                int num6 = (int)Width;
                if (num4 == -1)
                {
                    num6 = Math.Max(16, (int)Width - 16);
                }

                if (SourceSceneCollision.CheckPoint(((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid",
                        SourceVector2Math.Add(vector2, new Vector2(num4 * -2, -2f))))
                {
                    for (int k = num5; k < num6; k += 8)
                    {
                        (Scene as OriginalLevelSceneRoot).ParticlesFG.Emit(
                            particleTypes.P_Scrape,
                            SourceVector2Math.Add(TopLeft, new Vector2((float)k + (float)num4 * 2f, -1f)),
                            (num4 == 1) ? ((float)Math.PI * 3f / 4f) : ((float)Math.PI / 4f));
                    }
                }

                if (SourceSceneCollision.CheckPoint(((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid",
                        SourceVector2Math.Add(vector2, new Vector2(num4 * -2, Height + 2f))))
                {
                    for (int l = num5; l < num6; l += 8)
                    {
                        (Scene as OriginalLevelSceneRoot).ParticlesFG.Emit(
                            particleTypes.P_Scrape,
                            SourceVector2Math.Add(BottomLeft, new Vector2((float)l + (float)num4 * 2f, 0f)),
                            (num4 == 1) ? ((float)Math.PI * -3f / 4f) : (-(float)Math.PI / 4f));
                    }
                }
            }
        }
    }
}
