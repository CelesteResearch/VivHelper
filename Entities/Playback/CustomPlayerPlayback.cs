// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Framework.Diagnostics.Commands;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Playback;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation.Replay;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Presentation.Sprites;
using Celeste.Simulation.Gameplay.Tutorial.Replay;
using Celeste.Simulation.Presentation.Effects.Trails.Lifecycle;

namespace Celeste.Mods.VivHelper.Entities.Playback
{
    [CustomEntity("VivHelper/CPP")]
    [SceneTracked]
    [RuntimeTypeAlias("VivHelper.Entities.CustomPlayerPlayback")]
    public sealed class CustomPlayerPlayback : PlayerPlaybackState
    {
        public bool active;
        public string customID;
        private readonly float speedMult;
        private readonly string breaker;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new CustomPlayerPlayback(((ISourceEngineContextScene)level).EngineContext, data, offset);

        public CustomPlayerPlayback(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset, PlayerSpriteMode.Playback, data.Attr("tutorial", ""))
        {
            if (data.Nodes != null && data.Nodes.Length != 0)
            {
                rangeMinX = Position.X;
                rangeMaxX = Position.X;
                var nodes = data.NodesOffset(offset);
                foreach (var node in nodes)
                {
                    rangeMinX = Math.Min(rangeMinX, node.X);
                    rangeMaxX = Math.Max(rangeMaxX, node.X);
                }
            }

            startDelay = data.Float("Delay", 1f);
            active = data.Bool("StartActive", true);
            speedMult = data.Float("SpeedMultiplier", 1f);
            customID = data.Attr("CustomStringID", "");
            if (data.Attr("Color", "") != "") PlaybackColor = SpawnPointRules.ParseColor(data.Attr("Color", ""));
            Depth = data.Int("Depth", 9008);
            if (Sprite != null)
            {
                Sprite.Color = PlaybackColor ?? Hair.Color;
                OwnedComponents.Add(Sprite);
            }
        }

        public CustomPlayerPlayback(ISourceEngineContext engine, Vector2 start, PlayerSpriteMode sprite,
            string tutorial)
            : base(engine, start, sprite, FindTimeline(engine, tutorial),
                new OriginalPlayerPlaybackAudio(((IOriginalGameplayProcess)engine).AudioSession,
                    ((IOriginalGameplayProcess)engine).SaveContext), true, true)
        {
            if (Timeline == null)
                breaker = "PlayerPlayback at " + start + " errors due to no Tutorial \"" + tutorial +
                          ".\" You may need to restart or reload Assets manually to resolve this change.";
        }

        private static List<CharacterReplayFrame> FindTimeline(ISourceEngineContext engine, string tutorial)
            => ((ISourcePlaybackTimelineEnvironment)engine).PlaybackTimelines.TryGetValue(tutorial, out var timeline)
                ? timeline
                : null;

        public void PreAwake(object scene)
        {
            if (breaker == null) return;
            var commands = ((ISourceCommandsProcessEnvironment)engine).Commands;
            commands.Open = true;
            commands.Log(breaker);
            Active = false;
            RemoveSelf();
        }

        public new void Restart() => base.Restart();

        public override void Update()
        {
            if (startDelay > 0f) startDelay -= engine.DeltaTime;
            LastPosition = Position;
            OwnedComponents.Update();
            if (!active) return;
            if (index >= Timeline.Count - 1 || Time >= TrimEnd)
            {
                if (Visible) audio.Play(DisappearSound, Position);
                Visible = false;
                Position = start;
                loopDelay -= engine.DeltaTime;
                if (loopDelay <= 0f)
                {
                    var player = Scene == null
                        ? null
                        : ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
                    if (player == null || player.Position.X > rangeMinX && player.Position.X < rangeMaxX) Restart();
                }
            }
            else if (startDelay <= 0f &&
                     ((ISourceScene)Scene).SourceScene.Clock.OnInterval(engine.DeltaTime / Math.Min(1f, speedMult),
                         engine))
            {
                SetFrame(index);
                time += engine.DeltaTime;
                while (index < Timeline.Count - 1 && time >= Timeline[index + 1].TimeStamp)
                    index += speedMult > 1f ? (int)Math.Round(speedMult) : 1;
            }

            if (Visible && ShowTrail && Scene != null &&
                ((ISourceScene)Scene).SourceScene.Clock.OnInterval(.1f, engine))
                TrailManagerState.Add(engine, (ISourceRenderEnvironment)engine, Position, Sprite, Hair, Sprite.Scale,
                    Hair.Color, Depth + 1);
        }

        public override void Render(ISourceRenderContext context)
        {
            if (active) base.Render(context);
        }
    }
}
