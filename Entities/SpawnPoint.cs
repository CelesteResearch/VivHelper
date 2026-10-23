using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.VivHelper.Entities
{
    [SceneTracked]
    [CustomEntity("VivHelper/Spawnpoint")]
    public class SpawnPoint : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly AtlasEntry texture;
        private readonly PackedColor color;
        private readonly PackedColor outlineColor;
        private readonly bool flipX;
        public bool HideFromDebugMap { get; }
        public bool NoResetRespawn { get; }

        public SpawnPoint(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            Position = data.Position + offset;
            var path = data.Attr("Texture", "");
            if (data.Bool("ShowTexture", string.IsNullOrWhiteSpace(path)))
            {
                var atlas = ((ISourceGameGraphicsResources)engine).GameAtlas;
                var defaultTexture = atlas["VivHelper/player_outline"];
                texture = string.IsNullOrWhiteSpace(path) || path.ToLower() == "default"
                    ? defaultTexture
                    : atlas.GetOrDefault(path, defaultTexture);
            }

            NoResetRespawn = data.Bool("NoResetRespawn", false);
            HideFromDebugMap = data.Bool("HideFromDebugMap", false);
            Depth = data.Int("Depth", 5000);
            color = SpawnPointRules.ParseColor(data.Attr("Color", ""));
            outlineColor = SpawnPointRules.ParseColor(data.Attr("OutlineColor", ""));
            flipX = data.Bool("flipX", false);
        }

        public override void Render(ISourceRenderContext context)
        {
            if (texture == null || Scene is not OriginalLevelSceneRoot level) return;
            var camera = level.Camera;
            if (!(Position.X > camera.X - 16f && Position.Y > camera.Y - 16f
                                              && Position.X < camera.X + camera.Viewport.Width + 16f
                                              && Position.Y < camera.Y + camera.Viewport.Height + 16f)) return;
            var at = Position + Vector2.UnitY;
            var justify = new Vector2(0.5f, 1f);
            if (outlineColor != PackedColor.Transparent)
            {
                for (var x = -1; x <= 1; x++)
                for (var y = -1; y <= 1; y++)
                    if (x != 0 || y != 0)
                        SourceAtlasDrawing.DrawJustified(context, this, texture, at + new Vector2(x, y),
                            justify, outlineColor, 1f, 0f, flipX);
            }

            SourceAtlasDrawing.DrawJustified(context, this, texture, at, justify, color, 1f, 0f, flipX);
        }

        public override void DebugRender(ISourceRenderContext context, object actualCamera)
        {
            if (Scene is not OriginalLevelSceneRoot level) return;
            SourcePrimitiveDrawing.Circle(context, this, Position - Vector2.UnitY,
                5f - level.Scheduler.Clock.TimeActive * 5f % 5f,
                NoResetRespawn ? PackedColor.Lavender : PackedColor.Cyan, 8,
                ((ISourceVectorEnvironment)engine).VectorArithmetic);
        }
    }
}
