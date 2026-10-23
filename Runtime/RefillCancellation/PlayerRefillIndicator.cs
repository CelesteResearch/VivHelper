using Celeste.Core.Components;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering.Drawing;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Graphics.Content.Atlas;
using Celeste.Game.Content;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.VivHelper.Runtime.RefillCancellation
{
    [RuntimeTypeAlias("VivHelper.Entities.RefillCancel+PlayerIndicator")]
    public sealed class PlayerRefillIndicator : SceneEntityState
    {
        private readonly RefillCancelContext context;
        private readonly AtlasEntry dash, dashRefill, staminaRefill;
        private RoomPlayerSession player;

        public PlayerRefillIndicator(RefillCancelContext context)
            : base(((ISourceEntitySourceDataEnvironment)context.Engine).EntitySourceDataRegistration, Vector2.Zero)
        {
            this.context = context;
            var atlas = ((ISourceGameGraphicsEnvironment)context.Engine).GameGraphics.GameAtlas;
            dash = atlas["VivHelper/PlayerIndicator/chevron"];
            dashRefill = atlas["VivHelper/PlayerIndicator/triangle"];
            staminaRefill = atlas["VivHelper/PlayerIndicator/square"];
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            var players = ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(typeof(RoomPlayerSession));
            player = players.Count == 0 ? null : (RoomPlayerSession)players[0];
            Depth = -20000;
        }

        public override void Render(ISourceRenderContext graphics)
        {
            base.Render(graphics);
            if (!context.InSpace || !context.Session.ShowIndicator) return;
            var scope = (ISourceDrawScope)graphics;
            bool monochrome = context.Settings.ColorblindRCS;
            if (context.DashRestrict)
                SourceAtlasDrawing.DrawCentered(scope, dash, player.BottomCenter + new Vector2(-11, 6),
                    monochrome ? PackedColor.White : PackedColor.Red);
            if (context.DashRefillRestrict)
                SourceAtlasDrawing.DrawCentered(scope, dashRefill, player.BottomCenter + new Vector2(0, 6),
                    monochrome ? PackedColor.White : PackedColor.Blue);
            if (context.StaminaRefillRestrict)
                SourceAtlasDrawing.DrawCentered(scope, staminaRefill, player.BottomCenter + new Vector2(12, 6),
                    monochrome ? PackedColor.White : PackedColor.Yellow);
        }
    }
}
