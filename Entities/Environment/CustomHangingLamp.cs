// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Environment.HangingLamps;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.VivHelper.Entities.Environment
{
    [CustomEntity("VivHelper/CustomHangingLamp")]
    [RuntimeTypeAlias("VivHelper.Entities.CustomHangingLamp")]
    public class CustomHangingLamp : SceneEntityState, ISourceEngineContextEntity
    {
        private readonly ISourceEngineContext engine;
        ISourceEngineContext ISourceEngineContextEntity.EngineContext => engine;
        public readonly int Length;
        private readonly List<Sprite> sprites = new();
        private readonly BloomPointState bloom;
        private readonly VertexLightState light;
        private float speed;
        private float rotation;
        private float soundDelay;
        private readonly SoundSourceComponentState sfx;
        private readonly string audioPath;
        private readonly float inverseWeight;
        private readonly float lightDistance;
        private readonly bool drawOutline;
        private readonly Action playSwingSound;

        public static CustomHangingLamp Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            return new CustomHangingLamp(data, offset, level.EngineContext);
        }

        public CustomHangingLamp(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            Position = data.Position + offset + Vector2.UnitX * 4f;
            Length = Math.Max(16, data.Height);
            Depth = 2000;
            var directory = data.Attr("directory", "VivHelper/customHangingLamp/").Trim().TrimEnd('/') + "/";
            var animationSpeed = Math.Max(data.Float("AnimationSpeed", .2f), 0f);
            var suffix = "";
            if (data.Has("Suffix"))
                suffix = "/" + Chooser<string>.FromString<string>(data.Attr("Suffix"))
                    .Choose(((ISourceRandomEnvironment)engine).Random);
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            atlas.PushFallback(null);
            var topTexture = atlas.GetAtlasSubtextures(directory + "base" + suffix)[0];
            if (topTexture == null)
                throw new Exception("Missing file at Graphics/Atlases/Gameplay/" + directory + "base" + suffix + "00");
            var chainTexture = atlas.GetAtlasSubtextures(directory + "chain" + suffix)[0];
            if (chainTexture == null)
                throw new Exception("Missing file at Graphics/Atlases/Gameplay/" + directory + "chain" + suffix + "00");
            var lampTexture = atlas.GetAtlasSubtextures(directory + "lamp" + suffix)[0];
            if (lampTexture == null)
                throw new Exception("Missing file at Graphics/Atlases/Gameplay/" + directory + "lamp" + suffix + "00");
            atlas.PopFallback();
            var topWidth = topTexture.FrameWidth;
            var chainWidth = chainTexture.FrameWidth;
            var chainHeight = chainTexture.FrameHeight;
            var lampWidth = lampTexture.FrameWidth;
            var lampHeight = lampTexture.FrameHeight;
            var top = new Sprite(engine, atlas, directory + "base") { Position = Position };
            top.AddLoop("main", suffix, animationSpeed);
            top.Origin.X = topWidth / 2;
            top.Play("main");
            for (var i = 0; i < Length - 8; i += chainHeight)
            {
                var chain = new Sprite(engine, atlas, directory + "chain") { Position = Position };
                chain.AddLoop("main", suffix, animationSpeed);
                chain.Origin = new Vector2(chainWidth / 2f, -i);
                chain.Play("main");
                sprites.Add(chain);
            }

            var lamp = new Sprite(engine, atlas, directory + "lamp") { Position = Position };
            lamp.AddLoop("main", "", animationSpeed);
            lamp.Origin.X = lampWidth / 2;
            lamp.Origin.Y = -(Length - lampHeight);
            lamp.Play("main");
            sprites.Add(top);
            sprites.Add(lamp);
            var extrema = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic;
            Add(bloom = new BloomPointState(Vector2.UnitY * (Length - lampHeight),
                ScalarMath.Clamp(data.Float("BloomAlpha", 1f), 0f, 1f, extrema),
                ScalarMath.Clamp(data.Float("BloomRadius", 48f), 0f, 128f, extrema)));
            Add(light = new VertexLightState(engine, Vector2.UnitY * (Length - lampHeight),
                SpawnPointRules.ParseColor(data.Attr("LightColor", "White")),
                ScalarMath.Clamp(data.Float("LightAlpha", 1f), 0f, 1f, extrema),
                ScalarMath.Clamp(data.Int("LightFadeIn", 24), 0, 120),
                ScalarMath.Clamp(data.Int("LightFadeOut", 48), 0, 120)));
            audioPath = data.Attr("AudioPath", "event:/game/02_old_site/lantern_hit");
            inverseWeight = 1f / Math.Max(data.Float("WeightMultiplier", 1f), .025f);
            Add(sfx = new SoundSourceComponentState(engine));
            if (chainHeight == lampHeight)
                Collider = new HitboxColliderGeometry(chainWidth, Length, -chainWidth / 2f);
            else
                Collider = new ColliderListGeometry(
                    new HitboxColliderGeometry(chainWidth, Length - lampHeight, -chainWidth / 2f),
                    new HitboxColliderGeometry(lampWidth, lampHeight, -lampWidth / 2f, Length - lampHeight));
            lightDistance = Length - lampHeight / 2f;
            light.Position = Vector2.UnitY * lightDistance;
            drawOutline = data.Bool("DrawOutline", true);
            playSwingSound = PlaySwingSound;
        }

        public override void Update()
        {
            base.Update();
            soundDelay -= engine.DeltaTime;
            var player =
                ((OriginalLevelSceneRoot)Scene).Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(
                    typeof(RoomPlayerSession).FullName);
            var touchingPlayer = player != null && Collider.Collide(player.Collider);
            HangingLampPhysics.Advance(ref speed, ref rotation, ref soundDelay, engine, touchingPlayer,
                touchingPlayer ? player.Data.Motion.Normal.Speed.X : 0f, touchingPlayer ? player.Y - Y : 0f,
                Length, inverseWeight, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic, playSwingSound);
            if (sprites.Count > 1)
                for (var i = 1; i < sprites.Count; i++)
                    sprites[i].Rotation = rotation;
            var at = Vector2Geometry.AngleToVector(rotation + (float)Math.PI / 2f, lightDistance);
            bloom.Position = light.Position = at + Vector2Geometry.Round(Position) - Position;
            sfx.Position = at;
        }

        private void PlaySwingSound()
        {
            sfx.Play(audioPath);
        }

        public override void Render(ISourceRenderContext context)
        {
            if (sprites.Count <= 0) return;
            if (drawOutline)
                foreach (var sprite in sprites)
                    sprite.DrawOutline(context, 1);
            foreach (var sprite in sprites) sprite.Render(context);
        }
    }
}
