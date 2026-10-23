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
        private class ZipMoverPathRenderer : SceneEntityState
        {
            public CornerBoostZipMover ZipMover;

            private AtlasEntry cog;

            private Vector2 from;

            private Vector2 to;

            private Vector2 sparkAdd;

            private float sparkDirFromA;

            private float sparkDirFromB;

            private float sparkDirToA;

            private float sparkDirToB;

            public ZipMoverPathRenderer(CornerBoostZipMover zipMover)
                : base(((ISourceEntitySourceDataEnvironment)zipMover.engine).EntitySourceDataRegistration)
            {
                base.Depth = 5000;
                ZipMover = zipMover;
                from = SourceVector2Math.Add(ZipMover.start, new Vector2(ZipMover.Width / 2f, ZipMover.Height / 2f));
                to = SourceVector2Math.Add(ZipMover.target, new Vector2(ZipMover.Width / 2f, ZipMover.Height / 2f));
                sparkAdd = Vector2Geometry.Perpendicular(
                    ((ISourceVectorEnvironment)ZipMover.engine).VectorArithmetic.SafeNormalize(
                        SourceVector2Math.Subtract(from, to), 5f));
                float num = Vector2Geometry.Angle(SourceVector2Math.Subtract(from, to));
                sparkDirFromA = num + (float)Math.PI / 8f;
                sparkDirFromB = num - (float)Math.PI / 8f;
                sparkDirToA = num + (float)Math.PI - (float)Math.PI / 8f;
                sparkDirToB = num + (float)Math.PI + (float)Math.PI / 8f;
                if (zipMover.theme == Themes.Moon)
                {
                    cog = ((ISourceGameGraphicsEnvironment)ZipMover.engine).GameGraphics.GameAtlas[
                        "objects/zipmover/moon/cog"];
                }
                else
                {
                    cog = ((ISourceGameGraphicsEnvironment)ZipMover.engine).GameGraphics.GameAtlas[
                        "objects/zipmover/cog"];
                }
            }

            public void CreateSparks()
            {
                ZipMoverDrawing.Sparks(this, ZipMover.engine, from, to, sparkAdd,
                    sparkDirFromA, sparkDirFromB, sparkDirToA, sparkDirToB, ZipMover.particleTypes.P_Sparks);
            }

            public override void Render(ISourceRenderContext context)
            {
                DrawCogs(context, Vector2.UnitY, PackedColor.Black);
                DrawCogs(context, Vector2.Zero);
                if (ZipMover.drawBlackBorder)
                {
                    SourcePrimitiveDrawing.Rect((ISourceDrawScope)context,
                        new IntRect((int)(ZipMover.X + ZipMover.Shake.X - 1f),
                            (int)(ZipMover.Y + ZipMover.Shake.Y - 1f),
                            (int)ZipMover.Width + 2, (int)ZipMover.Height + 2), PackedColor.Black);
                }
            }

            private void DrawCogs(ISourceRenderContext context, Vector2 offset, PackedColor? colorOverride = null)
            {
                ZipMoverDrawing.Ropes(context, this, ZipMover.engine, from, to, cog, ZipMover.percent,
                    ropeColor, ropeLightColor, offset, colorOverride);
            }
        }
    }
}
