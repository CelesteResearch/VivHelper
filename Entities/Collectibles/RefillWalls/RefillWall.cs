using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Graphics.Animation;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.VivHelper.Entities.Collectibles.RefillWalls
{
    [RuntimeTypeAlias("VivHelper.Entities.RefillWall")]
    [CustomEntity("VivHelper/RefillWall")]
    public partial class RefillWall : SceneEntityState
    {
        private readonly IOriginalRefillProcess engine;
        private readonly bool twoDashes;
        private readonly bool oneUse;
        private readonly ParticleTypePresentation pRegen;
        private readonly ParticleTypePresentation pGlow;
        private OriginalLevelSceneRoot level;
        private SpriteImage sprite;
        private Sprite altSprite;
        private bool alt;
        private float alpha;
        private float respawnTime;
        private float respawnTimer;
        private RefillWallColorState colorState;

        public RefillWall(ISourceEngineContext engine, Vector2 position, bool twoDashes, bool oneUse)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = (IOriginalRefillProcess)engine;
            Add(new PlayerColliderComponentState(OnPlayer));
            this.twoDashes = twoDashes;
            this.oneUse = oneUse;
            pRegen = twoDashes ? this.engine.RefillParticles.P_RegenTwo : this.engine.RefillParticles.P_Regen;
            pGlow = twoDashes ? this.engine.RefillParticles.P_GlowTwo : this.engine.RefillParticles.P_Glow;
            Add(new MirrorReflectionState());
            colorState = RefillWallColorState.Normal;
            Depth = 100;
        }

        public RefillWall(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset, data.Bool("twoDashes"), data.Bool("oneUse"))
        {
            Collider = new HitboxColliderGeometry(data.Width, data.Height);
            alpha = data.Float("Alpha", 0f);
            alt = data.Bool("UseFullSprite");
            respawnTime = data.Float("RespawnTime", 2.5f);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = (OriginalLevelSceneRoot)Scene;
            if (alt)
            {
                string path = twoDashes ? "objects/refillTwo/" : "objects/refill/";
                altSprite = new Sprite(engine, engine.GameGraphics.GameAtlas, path);
                altSprite.AddLoop("idle", "idle", 0.1f);
                altSprite.AddLoop("outline", 0.05f, new[] { engine.GameGraphics.GameAtlas[path + "outline"] });
                altSprite.Add("flash", "flash", 0.05f, "outline");
                altSprite.CenterOrigin();
                Add(new SineWaveState(engine, 0.6f));
                altSprite.Position = Position + new Vector2(Width / 2f - 10f, Height / 2f - 10f);
            }
            else
            {
                sprite = new SpriteImage(
                    engine.GameGraphics.GameAtlas[twoDashes ? "objects/refillTwo/idle00" : "objects/refill/idle00"]);
                sprite.Position = Position + new Vector2(Width / 2f - sprite.Texture.FrameWidth / 2,
                    Height / 2f - sprite.Texture.FrameHeight / 2);
            }
        }

        public override void Update()
        {
            base.Update();
            if (respawnTimer > 0f)
            {
                respawnTimer -= engine.DeltaTime;
                if (respawnTimer <= 0f) Respawn();
            }
            else if (((ISourceScene)Scene).SourceScene.Clock.OnRawInterval(0f, engine))
                level.ParticlesFG.Emit(pGlow, 1, Center, new Vector2(Width / 2f, Height / 2f));
        }

        private void Respawn()
        {
            if (!Collidable)
            {
                Collidable = true;
                colorState = RefillWallColorState.Normal;
                Depth = 100;
                engine.SpatialAudio.Play(
                    twoDashes
                        ? "event:/new_content/game/10_farewell/pinkdiamond_return"
                        : "event:/game/general/diamond_return", Position);
                level.ParticlesFG.Emit(pRegen, 16, Center, Vector2.One * 2f);
            }
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            if (player.UseRefill(twoDashes))
            {
                engine.SpatialAudio.Play(
                    twoDashes
                        ? "event:/new_content/game/10_farewell/pinkdiamond_touch"
                        : "event:/game/general/diamond_touch", Position);
                engine.RumbleSession.Rumble(1, 1);
                Collidable = false;
                Add(new SimulationCoroutine(engine, RefillRoutine(player)));
                colorState = RefillWallColorState.Outline;
                respawnTimer = respawnTime;
            }
        }

        private IEnumerator RefillRoutine(RoomPlayerSession player)
        {
            engine.Freeze(0.05f);
            level.Shake(0.3f);
            Depth = 8999;
            yield return 0.05f;
            if (oneUse) RemoveSelf();
        }
    }
}
