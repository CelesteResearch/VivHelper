using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.VivHelper.Entities.Platforms.ZipMovers
{
    [RuntimeTypeAlias("VivHelper.Entities.CurvedStuff.CustomCurvedZipMover+CustomCurvedZipMoverPathRenderer")]
    public sealed class CustomCurvedZipMoverPathRenderer : SceneEntityState
    {
        private readonly CustomCurvedZipMover mover;
        private readonly AtlasEntry cog;
        private readonly ISourceVectorArithmetic vectors;
        private readonly Vector2 from, to, direction, perpendicular, sparkAdd, startA, startB, endA, endB;
        private readonly float length, sparkDirFromB, sparkDirToA, sparkDirToB;
        private readonly ParticleTypePresentation sparks;

        public CustomCurvedZipMoverPathRenderer(CustomCurvedZipMover mover)
            : base(((ISourceEntitySourceDataEnvironment)mover.Engine).EntitySourceDataRegistration)
        {
            this.mover = mover;
            Depth = 5000;
            from = mover.start + new Vector2(mover.Width / 2f, mover.Height / 2f);
            to = mover.target + new Vector2(mover.Width / 2f, mover.Height / 2f);
            vectors = ((ISourceVectorEnvironment)mover.Engine).VectorArithmetic;
            sparkAdd = Vector2Geometry.Perpendicular(vectors.SafeNormalize(from - to, 5f));
            float angle = Vector2Geometry.Angle(from - to);
            sparkDirFromB = angle - (float)Math.PI / 8f;
            sparkDirToA = angle + (float)Math.PI - (float)Math.PI / 8f;
            sparkDirToB = angle + (float)Math.PI + (float)Math.PI / 8f;
            cog = ((ISourceGameGraphicsEnvironment)mover.Engine).GameGraphics.GameAtlas[mover.spritePath + "/cog"];
            direction = vectors.SafeNormalize(to - from);
            perpendicular = Vector2Geometry.Perpendicular(direction);
            var a = perpendicular * 3f;
            var b = -perpendicular * 4f;
            startA = from + a;
            startB = from + b;
            endA = to + a;
            endB = to + b;
            length = vectors.Length(to - from);
            sparks = ((IOriginalGameplayProcess)mover.Engine).ParticleTypes.ZipMover.P_Sparks;
        }

        public void CreateSparks()
            => ZipMoverSparkDrawing.Tail(this, mover.Engine, from, to, sparkAdd, sparkDirFromB, sparkDirToA,
                sparkDirToB, sparks);

        public override void Render(ISourceRenderContext context)
        {
            DrawCogs(context, Vector2.UnitY, PackedColor.Black);
            DrawCogs(context, Vector2.Zero, null);
            if (mover.drawBlackBorder)
                SourcePrimitiveDrawing.Rect((Celeste.Graphics.Rendering.Drawing.ISourceDrawScope)context,
                    new IntRect((int)(mover.X + mover.Shake.X - 1f), (int)(mover.Y + mover.Shake.Y - 1f),
                        (int)mover.Width + 2, (int)mover.Height + 2), PackedColor.Black);
        }

        private void DrawCogs(ISourceRenderContext context, Vector2 offset, PackedColor? colorOverride)
        {
            float phase = mover.percent * (float)Math.PI * 8f;
            ZipMoverDrawing.RopeLines(context, this, vectors, startA, startB, endA, endB, direction, perpendicular,
                length, phase, offset, mover.ropeColor, mover.ropeLightColor, colorOverride);
            float rotation = mover.percent * (float)Math.PI * 2f;
            SourceAtlasDrawing.DrawCentered(context, this, cog, from + offset, colorOverride ?? mover.baseColor, 1f,
                rotation);
            SourceAtlasDrawing.DrawCentered(context, this, cog, to + offset, colorOverride ?? mover.baseColor, 1f,
                rotation);
        }
    }
}
