using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Identifiers;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.LightSources.Torches;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Core.Display;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Mods.VivHelper.Runtime.Graphics;
using Celeste.Mods.VivHelper.Spawn;

namespace Celeste.Mods.VivHelper.Entities.Lighting.Torches
{
    [CustomEntity("VivHelper/CustomTorch")]
    [RuntimeTypeAlias("VivHelper.Entities.CustomTorch2")]
    public sealed class CustomTorch2 : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly ParticleTypePresentation P_OnLight2;
        public PackedColor color;
        public float alpha;
        public int startFade;
        public int endFade;
        public string FlagName;
        public bool lit;
        public bool startLit;
        public bool unlightOnDeath;
        public VertexLightState light;
        public BloomPointState bloom;
        public Sprite sprite;
        public SpriteImage image;

        public CustomTorch2(ISourceEngineContext engine, OriginalLevelEntityData data, Vector2 offset, MapEntityId id)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            startLit = data.Bool("startLit");
            unlightOnDeath = data.Bool("unlightOnDeath");
            color = SpawnPointRules.ParseColor(data.Attr("Color", "Cyan"));
            alpha = data.Float("Alpha", 1f);
            FlagName = "VivHelperTorch_" + EntityIdCodec.Format(id);
            if (alpha < 0f || alpha > 1f) alpha = 1f;
            startFade = Math.Abs(data.Int("startFade", 48));
            endFade = Math.Abs(data.Int("endFade", 64));
            Collider = new CircleColliderGeometry(
                ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic,
                data.Float("RegisterRadius", 4f));
            sprite = VivHelperGraphicsResources.For(engine).SpriteBank.Create("CustomTorch");
            if (sprite == null) throw new Exception("CustomTorch Sprite is missing!");
            sprite.Color = SpawnPointRules.ParseColor(data.Attr("spriteColor", "White"));
            OwnedComponents.Add(sprite);
            OwnedComponents.Add(light = new VertexLightState(engine, color, 1f, startFade, endFade));
            OwnedComponents.Add(bloom = new BloomPointState(alpha / 2f, 8f));
            bloom.Visible = false;
            light.Visible = false;
            OwnedComponents.Add(new PlayerColliderComponentState(OnPlayer));
            P_OnLight2 = new ParticleTypePresentation(((IOriginalGameplayProcess)engine).ParticleTypes.Torch.P_OnLight);
            P_OnLight2.Color = color;
            Depth = 2000;
        }

        public override void Added(object scene)
        {
            var level = Scene as OriginalLevelSceneRoot;
            if (unlightOnDeath) level?.Session?.SetFlag(FlagName, false);
            if (startLit || level?.Session?.GetFlag(FlagName) == true)
            {
                bloom.Visible = light.Visible = true;
                lit = true;
                Collidable = false;
                sprite.Play("on");
            }

            base.Added(scene);
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            if (lit) return;
            ((Celeste.Core.Audio.ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                "event:/game/05_mirror_temple/torch_activate", Position);
            lit = true;
            bloom.Visible = true;
            light.Visible = true;
            Collidable = false;
            sprite.Play("turnOn");
            SourceTweenState tween = TorchLightingTransition.Create(engine, light, bloom, startFade, endFade, alpha);
            OwnedComponents.Add(tween);
            if (!unlightOnDeath) ((OriginalLevelSceneRoot)Scene).Session.SetFlag(FlagName);
            ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(P_OnLight2, 12, Position, new Vector2(3f, 3f));
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) =>
            new CustomTorch2(level.EngineContext, data, offset, new MapEntityId(room.Name, data.ID));
    }
}
