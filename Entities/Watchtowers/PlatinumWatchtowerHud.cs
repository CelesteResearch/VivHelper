// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Game.Entities.Lookouts;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Game.Controls.Source;
using System;
using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Input.Gameplay;
using Celeste.Graphics.Content.Atlas;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Collision;
using Celeste.Simulation;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;

namespace Celeste.Mods.VivHelper.Entities.Watchtowers
{
    [RuntimeTypeAlias("VivHelper.Entities.Watchtowers.Hud")]
    public class PlatinumWatchtowerHud : SceneEntityState, ILookoutHudState
    {
        public PackedColor paddingColor = PackedColor.White;
        public global::System.Collections.Generic.List<AtlasEntry> hints;
        public bool TrackMode;

        public float TrackPercent;

        public bool OnlyY;

        public float Easer;

        private float timerUp;

        private float timerDown;

        private float timerLeft;

        private float timerRight;

        private float multUp;

        private float multDown;

        private float multLeft;

        private float multRight;

        private float left;

        private float right;

        private float up;

        private float down;

        private Vector2 aim;

        private AtlasEntry halfDot;
        private readonly IOriginalLookoutProcess process;

        public PlatinumWatchtowerHud(IOriginalLookoutProcess process)
            : base(((ISourceEntitySourceDataEnvironment)process.EngineContext).EntitySourceDataRegistration,
                Vector2.Zero)
        {
            this.process = process;
            halfDot = process.GuiAtlas.Entries["dot"].GetSubtexture(0, 0, 64, 32);
            AddTag(((global::Celeste.Game.Execution.Startup.IOriginalGameTagsEnvironment)process.EngineContext).Tags
                .HUD.Value);
        }

        public override void Update() => LookoutHudPresentation.Update(this);

        public override void Render(ISourceRenderContext context) =>
            LookoutHudPresentation.Render(this, context, paddingColor);

        private bool Blocked(IntRect rectangle)
            => SourceSceneCollisionQueries.First<object>(rectangle,
                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities("Celeste.LookoutBlocker")) != null;

        private void DrawCentered(ISourceRenderContext context, AtlasEntry texture, Vector2 position,
            PackedColor color, float scale = 1f, float rotation = 0f)
            => SourceAtlasDrawing.DrawCentered(context, this, texture, position, color, scale, rotation);

        private void DrawJustified(ISourceRenderContext context, AtlasEntry texture, Vector2 position,
            Vector2 justify, PackedColor color)
            => SourceAtlasDrawing.DrawJustified(context, this, texture, position, justify, color);

        private void DrawJustified(ISourceRenderContext context, AtlasEntry texture, Vector2 position,
            Vector2 justify, PackedColor color, Vector2 scale)
            => SourceAtlasDrawing.DrawJustified(context, this, texture, position, justify, color, scale);

        SceneEntityState ILookoutHudState.Entity => this;
        IOriginalLookoutProcess ILookoutHudState.Process => process;
        void ILookoutHudState.UpdateComponents() => base.Update();

        bool ILookoutHudState.TrackMode
        {
            get => TrackMode;
            set => TrackMode = value;
        }

        float ILookoutHudState.TrackPercent
        {
            get => TrackPercent;
            set => TrackPercent = value;
        }

        bool ILookoutHudState.OnlyY
        {
            get => OnlyY;
            set => OnlyY = value;
        }

        float ILookoutHudState.Easer
        {
            get => Easer;
            set => Easer = value;
        }

        float ILookoutHudState.TimerUp
        {
            get => timerUp;
            set => timerUp = value;
        }

        float ILookoutHudState.TimerDown
        {
            get => timerDown;
            set => timerDown = value;
        }

        float ILookoutHudState.TimerLeft
        {
            get => timerLeft;
            set => timerLeft = value;
        }

        float ILookoutHudState.TimerRight
        {
            get => timerRight;
            set => timerRight = value;
        }

        float ILookoutHudState.MultUp
        {
            get => multUp;
            set => multUp = value;
        }

        float ILookoutHudState.MultDown
        {
            get => multDown;
            set => multDown = value;
        }

        float ILookoutHudState.MultLeft
        {
            get => multLeft;
            set => multLeft = value;
        }

        float ILookoutHudState.MultRight
        {
            get => multRight;
            set => multRight = value;
        }

        float ILookoutHudState.Left
        {
            get => left;
            set => left = value;
        }

        float ILookoutHudState.Right
        {
            get => right;
            set => right = value;
        }

        float ILookoutHudState.Up
        {
            get => up;
            set => up = value;
        }

        float ILookoutHudState.Down
        {
            get => down;
            set => down = value;
        }

        Vector2 ILookoutHudState.Aim
        {
            get => aim;
            set => aim = value;
        }

        AtlasEntry ILookoutHudState.HalfDot
        {
            get => halfDot;
            set => halfDot = value;
        }
    }
}
