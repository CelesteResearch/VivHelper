// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Mods.VivHelper.Entities.Debris;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;

namespace Celeste.Mods.VivHelper.Entities.Spinners
{
    [SceneTracked]
    [CustomEntity("VivHelper/AnimatedSpinner")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("VivHelper.Entities.AnimatedSpinner")]
    public sealed class AnimatedSpinner : CustomSpinner
    {
        private new readonly string directory;
        private readonly List<string> subdirectories;

        private readonly float timeBetweenFrames;

        // The fixed assembly does not read map attributes into these fields.
        private bool killAnim = false;
        private bool bgRotate = false;
        private Sprite foreground;
        private string path;

        public static new AnimatedSpinner Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public AnimatedSpinner(EntityData data, Vector2 offset, ISourceEngineContext engine) : base(data, offset,
            engine)
        {
            directory = AnimationDirectory == "VivHelper/customSpinner/white"
                ? "VivHelper/customSpinner/animated"
                : AnimationDirectory;
            var value = data.Attr("Subdirectories").Trim().TrimEnd('/');
            subdirectories = value == "" ? new List<string> { "spin" } : new List<string>(value.Split(','));
            timeBetweenFrames = data.Float("TimeBetweenFrames", .1f);
        }

        protected override void CreateSprites()
        {
            if (expanded) return;
            ((ISourceRandomProcessEnvironment)engine).RandomProcess.PushRandom(randomSeed);
            path = directory + "/";
            if (AnimationIsSeeded)
            {
                var computedSeed = AnimationSeed;
                if (computedSeed < 0)
                {
                    var levels = ((OriginalChapterMapData)Level.Session.MapData).Levels;
                    computedSeed = levels.FindIndex(room => room.Name == Level.Session.LevelData.Name) + 977;
                    computedSeed = (unchecked(computedSeed * computedSeed * computedSeed) % 30977 + 30977) % 30977;
                }

                // Original 1.14.10 computes the fallback but passes the original seed.
                path += subdirectories[
                    ChooseAnimationIndex((int)Position.X, (int)Position.Y, AnimationSeed, subdirectories.Count)];
            }
            else path += Random.Choose(subdirectories);

            path += "/";
            DefineSprite(ref foreground, "fg");
            foreground.CenterOrigin();
            Add(foreground);
            foreground.Play("idle");
            foreach (AnimatedSpinner other in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(AnimatedSpinner)))
            {
                if (other.ID <= ID || other.AttachToSolid != AttachToSolid) continue;
                if (!(VectorArithmetic.LengthSquared(other.Position - Position) <
                      (float)Math.Pow(12f * (AnimationScale + other.AnimationScale), 2.0))) continue;
                var toOther = Vector2Geometry.Angle(Position, other.Position);
                var toSelf = Vector2Geometry.Angle(other.Position, Position);
                var start = Position + VectorArithmetic.RotateTowards(Vector2.UnitX, toOther, 6.3f);
                var end = other.Position + VectorArithmetic.RotateTowards(Vector2.UnitX, toSelf, 6.3f);
                var scalar = ((ISourceScalarEnvironment)engine).ScalarArithmetic;
                var at = new Vector2(scalar.Lerp(start.X, end.X, .5f), scalar.Lerp(start.Y, end.Y, .5f)) - Position;
                AddAnimatedConnector(at, (other.AnimationScale + AnimationScale) / 2f,
                    PackedColor.Lerp(other.AnimationTint, AnimationTint, .5f), toOther, AnimationIsSeeded);
            }

            CreateBorder(AnimationBorderColor);
            expanded = true;
            ((ISourceRandomProcessEnvironment)engine).RandomProcess.PopRandom();
        }

        private void DefineSprite(ref Sprite sprite, string subtext)
        {
            sprite = new Sprite(engine, ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas, path);
            sprite.CenterOrigin();
            sprite.Scale = Vector2.One * AnimationScale * AnimationImageScale;
            sprite.AddLoop("idle", "idle_" + subtext, timeBetweenFrames);
            if (killAnim) sprite.Add("kill", "kill_" + subtext, timeBetweenFrames, "idle");
            // The source always sets the foreground field, including when defining a connector.
            foreground.SetColor(AnimationTint);
        }

        private void AddAnimatedConnector(Vector2 at, float connectorScale, PackedColor color, float angle, bool seeded)
        {
            if (filler == null)
            {
                filler = new SceneEntityState(((Celeste.Core.Components.ISourceEntitySourceDataEnvironment)engine)
                    .EntitySourceDataRegistration, Position) { Depth = Depth + 1 };
                ((ISourceScene)Scene).SourceScene.QueueSourceEntity(filler);
            }

            Sprite sprite = null;
            DefineSprite(ref sprite, "bg");
            sprite.Position = at;
            sprite.Rotation = bgRotate ? Random.Choose(0, 1, 2, 3) * ((float)Math.PI / 2f) : angle;
            sprite.CenterOrigin();
            sprite.Scale = Vector2.One * connectorScale * AnimationImageScale;
            sprite.Color = IsRainbow ? GetHue(Position + at) : color;
            sprite.Play("idle");
            filler.Add(sprite);
        }

        protected override void OnPlayer(RoomPlayerSession player)
        {
            player.Die(VectorArithmetic.SafeNormalize(player.Position - Position), false, true);
            if (killAnim) foreground.Play("kill");
        }

        public override void Destroy(bool boss = false)
        {
            if (InView())
            {
                ((Celeste.Core.Audio.ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                    "event:/game/06_reflection/fall_spike_smash", Position);
                CustomCrystalDebris.Burst(engine, Position, AnimationShatterColor, boss, 8,
                    AnimationCustomDebris ? directory + subdirectories?.ToString() + "/debris" : "particles/shard",
                    AnimationDebrisToScale ? AnimationScale : 1f);
            }

            RemoveSelf();
        }
    }
}
