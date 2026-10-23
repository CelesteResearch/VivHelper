// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Presentation.Barriers;
using Celeste.Mods.VivHelper.Runtime.HoldableBarriers;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Game.Execution.Levels;
using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Components;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Scene.Scheduling.ExecutionTags;
using Celeste.Graphics.Display;
using Celeste.Core.Components.Rendering;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Rendering;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Geometry.TwoD.Camera;
using Celeste.Graphics.Rendering;

namespace Celeste.Mods.VivHelper.Entities.HoldableBarriers
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.HoldableBarrierRenderer")]
    public class HoldableBarrierRenderer : SceneEntityState
    {
        public sealed class Edge
        {
            public HoldableBarrier Parent;
            public bool Visible;
            public Vector2 A, B, Min, Max, Normal, Perpendicular;
            public float[] Wave;
            public float Length;

            private readonly ISourceEngineContext engine;

            public Edge(HoldableBarrier parent, Vector2 a, Vector2 b, ISourceEngineContext engine)
            {
                this.engine = engine;
                Parent = parent;
                Visible = true;
                A = a;
                B = b;
                Min = new Vector2(((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Min(a.X, b.X),
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Min(a.Y, b.Y));
                Max = new Vector2(((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Max(a.X, b.X),
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Max(a.Y, b.Y));
                Normal = ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(b - a);
                Perpendicular = new Vector2(Normal.Y, -Normal.X);
                Length = ((ISourceVectorEnvironment)engine).VectorArithmetic.Length(a - b);
            }

            public void UpdateWave(float time)
            {
                if (Wave == null || Wave.Length <= Length) Wave = new float[(int)Length + 2];
                for (int i = 0; i <= Length; i++) Wave[i] = GetWaveAt(time, i, Length);
            }

            private float GetWaveAt(float offset, float along, float length)
            {
                if (along <= 1f || along >= length - 1f) return 0f;
                if (Parent.Solidify >= 1f) return 0f;
                return BarrierTileGeometry.Wave(offset, along, length, Parent.Solidify, engine);
            }

            public bool InView(ref IntRect view)
                => view.Left < Parent.Position.X + Max.X && view.Right > Parent.Position.X + Min.X
                                                         && view.Top < Parent.Position.Y + Max.Y &&
                                                         view.Bottom > Parent.Position.Y + Min.Y;
        }

        private readonly List<HoldableBarrier> list = new();
        private readonly List<Edge> edges = new();
        private readonly Action<HoldableBarrier, Vector2, Vector2> addEdge;
        private VirtualMap<bool> tiles;
        private IntRect levelTileBounds;
        private bool dirty;
        private readonly HoldableBarrierContext owner;
        private HoldableBarrierColorController colorController;
        private readonly ISourceRenderEnvironment environment;
        private readonly ISourceEngineContext engine;
        public IReadOnlyList<HoldableBarrier> Blocks => list;
        public IReadOnlyList<Edge> Edges => edges;
        public bool Dirty => dirty;
        public IntRect LevelTileBounds => levelTileBounds;

        public HoldableBarrierRenderer(ISourceEngineContext engine, ISourceRenderEnvironment environment)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            addEdge = AddEdge;
            owner = HoldableBarrierContext.For(engine);
            this.engine = engine;
            this.environment = environment;
            Tags = ((ISourceGlobalFrozenExecutionTagsEnvironment)engine).GlobalFrozenExecutionTags.Global.Value
                   | ((ISourcePersistentTransitionExecutionTagsEnvironment)engine).PersistentTransitionExecutionTags
                   .TransitionUpdate.Value;
            Depth = 0;
            var bloom = new CustomBloomCallbackComponentState(OnRenderBloom);
            OwnedComponents.Add(bloom);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            colorController = owner.Controller(scene);
        }

        public void Track(HoldableBarrier block)
        {
            list.Add(block);
            if (tiles == null)
            {
                levelTileBounds = ((ISourceLevelTileBoundsScene)Scene).TileBounds;
                tiles = new VirtualMap<bool>(levelTileBounds.Width, levelTileBounds.Height, false);
            }

            WriteTiles(block, true);
            dirty = true;
        }

        public void Untrack(HoldableBarrier block)
        {
            list.Remove(block);
            if (list.Count <= 0) tiles = null;
            else WriteTiles(block, false);
            dirty = true;
        }

        private void WriteTiles(HoldableBarrier block, bool value)
        {
            BarrierTileGeometry.WriteTiles(block, tiles, levelTileBounds, value);
        }

        public override void Update()
        {
            colorController ??= owner.Controller(Scene);
            if (dirty) RebuildEdges();
            UpdateEdges();
        }

        public void UpdateEdges()
        {
            var camera = ((ISourceCameraScene)Scene).Camera;
            var view = new IntRect((int)camera.Left - 4, (int)camera.Top - 4,
                (int)(camera.Right - camera.Left) + 8, (int)(camera.Bottom - camera.Top) + 8);
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].Visible)
                {
                    if (OnInterval(.25f, i * .01f) && !edges[i].InView(ref view)) edges[i].Visible = false;
                }
                else if (OnInterval(.05f, i * .01f) && edges[i].InView(ref view)) edges[i].Visible = true;

                if (edges[i].Visible && (OnInterval(.05f, i * .01f) || edges[i].Wave == null))
                    edges[i].UpdateWave(((ISourceScene)Scene).SourceScene.Clock.TimeActive * 3f);
            }
        }

        private bool OnInterval(float interval, float offset)
            => ((ISourceScene)Scene).SourceScene.Clock.OnInterval(interval, offset, engine);

        public void RebuildEdges()
        {
            dirty = false;
            edges.Clear();
            if (list.Count <= 0) return;
            var level = (ISourceLevelTileBoundsScene)Scene;
            _ = level.TileBounds.Left;
            _ = level.TileBounds.Top;
            _ = level.TileBounds.Right;
            _ = level.TileBounds.Bottom;
            BarrierTileGeometry.RebuildEdges(list, tiles, levelTileBounds, addEdge);
        }

        private void AddEdge(HoldableBarrier block, Vector2 a, Vector2 b)
            => edges.Add(new Edge(block, a, b, engine));

        private bool Inside(int x, int y) => tiles[x - levelTileBounds.X, y - levelTileBounds.Y];

        private void OnRenderBloom()
        {
            if (!(colorController?.ToggleBloomRendering ?? true)) return;
            var context = environment.CurrentContext;
            var camera = ((ISourceCameraScene)Scene).Camera;
            _ = new IntRect((int)camera.Left, (int)camera.Top,
                (int)(camera.Right - camera.Left), (int)(camera.Bottom - camera.Top));
            foreach (HoldableBarrier block in list)
                if (block.Visible)
                    SourcePrimitiveDrawing.Rect(context, this, block.Position.X, block.Position.Y, block.Width,
                        block.Height, PackedColor.White);
            foreach (Edge edge in edges)
                if (edge.Visible)
                {
                    Vector2 start = edge.Parent.Position + edge.A;
                    _ = edge.Parent.Position + edge.B;
                    for (int i = 0; i <= edge.Length; i++)
                    {
                        Vector2 at = start + edge.Normal * i;
                        SourcePrimitiveDrawing.Line(context, this, at, at + edge.Perpendicular * edge.Wave[i],
                            PackedColor.White, ((ISourceVectorEnvironment)engine).VectorArithmetic);
                    }
                }
        }

        public override void Render(ISourceRenderContext context)
        {
            if (list.Count <= 0) return;
            PackedColor tint = colorController?.BaseColor ??
                               SpawnPointRules.ParseColor(owner.Saved((OriginalLevelSceneRoot)Scene).BaseColorHex);
            PackedColor color = tint * .15f;
            PackedColor edgeColor = tint * .25f;
            foreach (HoldableBarrier block in list)
                if (block.Visible)
                    SourcePrimitiveDrawing.Rect(context, this, block.Collider.AbsoluteLeft, block.Collider.AbsoluteTop,
                        block.Collider.Width, block.Collider.Height, color);
            if (edges.Count <= 0) return;
            foreach (Edge edge in edges)
                if (edge.Visible)
                {
                    Vector2 start = edge.Parent.Position + edge.A;
                    _ = edge.Parent.Position + edge.B;
                    for (int i = 0; i <= edge.Length; i++)
                    {
                        Vector2 at = start + edge.Normal * i;
                        SourcePrimitiveDrawing.Line(context, this, at, at + edge.Perpendicular * edge.Wave[i],
                            edgeColor,
                            ((ISourceVectorEnvironment)engine).VectorArithmetic);
                    }
                }
        }
    }
}
