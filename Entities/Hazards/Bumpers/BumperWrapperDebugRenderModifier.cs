// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Rendering;
using Celeste.Mods.VivHelper.Runtime.Bumpers;
using Celeste.Mods.VivHelper.Runtime.Bumpers.Launching;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;

namespace Celeste.Mods.VivHelper.Entities.Hazards.Bumpers
{
    public sealed class BumperWrapperDebugRenderModifier : SourceComponentState
    {
        private readonly ISourceVectorArithmetic vectors;
        private readonly BumperWrapperContext context;
        public readonly BumperLaunchKind BumperModifier;

        public BumperWrapperDebugRenderModifier(ISourceEngineContext engine, BumperWrapperContext context,
            BumperLaunchKind modifier) : base(true, false)
        {
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            this.context = context;
            BumperModifier = modifier;
        }

        public bool RenderNewDebug(ISourceRenderContext render, object camera)
        {
            var entity = (SceneEntityState)Entity;
            var circle = (CircleColliderGeometry)entity.Collider;
            var center = circle.AbsolutePosition;
            float radius = circle.Radius;
            var color = entity.Collidable
                ? context.EntityDebugColor ?? PackedColor.Red
                : context.EntityDebugColor * .5f ?? PackedColor.DarkRed;
            SourcePrimitiveDrawing.Circle(render, entity, center, radius, color, 4, vectors);
            if (BumperModifier is BumperLaunchKind.Cardinal or BumperLaunchKind.EightWay)
            {
                SourcePrimitiveDrawing.Line(render, entity, center - new Vector2(.70711f, .70711f) * radius,
                    center + new Vector2(.70711f, .70711f) * radius, color, vectors);
                SourcePrimitiveDrawing.Line(render, entity, center - new Vector2(.70711f, -.70711f) * radius,
                    center + new Vector2(.70711f, -.70711f) * radius, color, vectors);
            }

            if (BumperModifier is BumperLaunchKind.Diagonal or BumperLaunchKind.EightWay)
            {
                SourcePrimitiveDrawing.Line(render, entity, center - Vector2.UnitX * radius,
                    center + Vector2.UnitX * radius, color, vectors);
                SourcePrimitiveDrawing.Line(render, entity, center - Vector2.UnitY * radius,
                    center + Vector2.UnitY * radius, color, vectors);
            }

            if (BumperModifier == BumperLaunchKind.Alt4way)
            {
                SourcePrimitiveDrawing.Line(render, entity, center + vectors.Rotate(Vector2.UnitX, 1.0472f) * radius,
                    center, color, vectors);
                SourcePrimitiveDrawing.Line(render, entity, center + vectors.Rotate(Vector2.UnitX, -.6545f) * radius,
                    center, color, vectors);
                SourcePrimitiveDrawing.Line(render, entity, center + vectors.Rotate(Vector2.UnitX, 2.0944f) * radius,
                    center, color, vectors);
                SourcePrimitiveDrawing.Line(render, entity, center + vectors.Rotate(Vector2.UnitX, -2.4871f) * radius,
                    center, color, vectors);
                SourcePrimitiveDrawing.Point(render, entity, center, new PackedColor(color.R, color.G, color.B));
            }

            return true;
        }
    }
}
