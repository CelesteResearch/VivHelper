using Celeste.Scene.Tracking.Attributes;
using System;
using System.Numerics;
using System.Reflection;
using System.Collections.Generic;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Presentation.Hud.CoreMessages;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.VivHelper.Entities.Text.CoreMessages
{
    [SceneTracked(false)]
    [CustomEntity("VivHelper/CustomCoreMessage = Load0", "VivHelper/CustomCoreMessage2 = Load1")]
    [RuntimeTypeAlias("VivHelper.Entities.ColoredCustomCoreMessage")]
    public sealed class ColoredCustomCoreMessage : SceneEntityState
    {
        public enum PauseRenderTypes
        {
            Hidden,
            Shown,
            Fade
        }

        private readonly ISourceEngineContext engine;
        private readonly string text;
        private readonly bool outline;
        private readonly bool alwaysRender;
        private readonly bool lockPosition;
        private readonly float renderDistance;
        private readonly float alphaMult;
        private readonly Vector2 scale;
        private readonly Func<float, float> ease;
        private readonly PackedColor color;
        private readonly PackedColor outlineColor;
        private readonly Vector2[] nodes;
        private readonly bool customPositionRange;
        private readonly PauseRenderTypes pauseType;
        public float alpha;
        public float defaultFadedValue;

        public static ColoredCustomCoreMessage Load0(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset, 0);

        public static ColoredCustomCoreMessage Load1(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset, 1);

        public ColoredCustomCoreMessage(ISourceEngineContext engine, EntityData data, Vector2 offset, int legacy)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            var tags = ((IOriginalGameTagsEnvironment)engine).Tags;
            Tags = tags.HUD.Value | tags.PauseUpdate.Value |
                   (data.Bool("ShowInTransition") ? tags.TransitionUpdate.Value : 0);
            string key = data.Attr("dialog", "app_ending");
            bool literal = key.StartsWith("*§");
            text = literal ? key.Substring(2) : ((IOriginalGameTextEnvironment)engine).Text.Dialog.Clean(key);
            if (text.Contains("\n") || text.Contains("\r"))
            {
                var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length != 0) text = lines[data.Int("line")];
                else if (!literal) text = "{" + key + "}";
            }

            outlineColor = OldColor(data.Attr("OutlineColor", "Black"));
            outline = data.Has("outline") ? data.Bool("outline") : outlineColor != PackedColor.Transparent;
            pauseType = data.Enum("PauseType", PauseRenderTypes.Hidden);
            scale = Vector2.One * data.Float("Scale", 1.25f);
            renderDistance = data.Float("RenderDistance", 128);
            ease = GetEaser(engine, data.Attr("EaseType", "CubeInOut"));
            color = OldColor(data.Attr("TextColor1", "White"));
            alwaysRender = data.Bool("AlwaysRender");
            lockPosition = data.Bool("LockPosition");
            nodes = data.NodesOffset(offset);
            customPositionRange = legacy == 1 && nodes.Length > 1 && nodes.Length % 2 == 0;
            defaultFadedValue = data.Float("DefaultFadedValue");
            alphaMult = ScalarMath.Clamp(data.Float("AlphaMultiplier", 1), 0, 1);
        }

        private static PackedColor OldColor(string name)
        {
            var result = SpawnPointRules.ParseColor(name);
            if (typeof(PackedColor).GetProperty(name,
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
                    is { PropertyType: var type } && type == typeof(PackedColor)) return result;
            return new PackedColor(result.R, result.G, result.B, 255);
        }

        private static Func<float, float> GetEaser(ISourceEngineContext engine, string name)
        {
            var arithmetic = ((ISourceEasingEnvironment)engine).EasingArithmetic;
            foreach (var pair in ((ISourceNamedEasingArithmetic)arithmetic).NamedEasers)
                if (pair.Key == name)
                    return pair.Value;
            return arithmetic.CubeInOut;
        }

        public override void Update()
        {
            var scheduler = ((ISourceScene)Scene).SourceScene;
            var player = scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession).FullName);
            if (scheduler.Paused)
            {
                switch (pauseType)
                {
                    case PauseRenderTypes.Hidden: alpha = 0; break;
                    case PauseRenderTypes.Fade: alpha = ScalarMath.Approach(alpha, 0, .05f); break;
                }
            }
            else
            {
                float target;
                if (alwaysRender) target = alphaMult;
                else if (!customPositionRange)
                    target = player == null
                        ? alpha
                        : alphaMult * (defaultFadedValue + (1 - defaultFadedValue) *
                            ease(ScalarMath.ClampedMap(Math.Abs(X - player.X), 0, renderDistance, 1, 0)));
                else
                {
                    var values = new List<float>
                        { ScalarMath.ClampedMap(Math.Abs(X - player.X), 0, renderDistance, 1, 0) };
                    var scalar = ((ISourceScalarEnvironment)engine).ScalarArithmetic;
                    for (int i = 0; i < nodes.Length; i += 2)
                    {
                        var center = Vector2Interpolation.Lerp(nodes[i], nodes[i + 1], .5f, scalar);
                        values.Add(ScalarMath.ClampedMap(Math.Abs(center.X - player.X), 0, nodes[i + 1].X - center.X, 1,
                            0));
                    }

                    target = alphaMult * (defaultFadedValue +
                                          (1 - defaultFadedValue) * ease(SourceArrayExtrema.Max(values.ToArray())));
                }

                alpha = pauseType == PauseRenderTypes.Fade ? ScalarMath.Approach(alpha, target, .05f) : target;
            }

            base.Update();
        }

        public override void Render(ISourceRenderContext context)
        {
            var camera = ((OriginalLevelSceneRoot)Scene).Camera.Position;
            var position = lockPosition
                ? CoreMessageLayout.LockedPosition(Position, camera)
                : CoreMessageLayout.UnlockedPosition(Position, camera);
            var save = ((IOriginalGameplayProcess)engine).SaveContext.CurrentSave;
            if (save != null && save.Assists.MirrorMode) position.X = 1920 - position.X;
            var font = ((IOriginalGameTextEnvironment)engine).Text.ActiveFont;
            if (outline)
                font.DrawOutline(context, this, text, position, new Vector2(.5f), scale, color * alpha, 2,
                    outlineColor * alpha);
            else font.Draw(context, this, text, position, new Vector2(.5f), scale, color * alpha);
        }
    }
}
