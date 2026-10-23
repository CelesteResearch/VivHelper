using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Entities.Birds;
using Celeste.Game.Controls.Source;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Gameplay.Environment.Wind;
using Celeste.Simulation.Gameplay.Enemies.Seeker;
using Celeste.Simulation.Presentation.Hud.Tutorials.Birds;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.VivHelper.Entities.Holdables.Gliders
{
    [CustomEntity("VivHelper/ReskinnableJelly")]
    [RuntimeTypeAlias("VivHelper.Entities.CustomGlider")]
    public partial class CustomGlider : SourceActorEntityState
    {
        public ParticleTypePresentation P_Glide, P_GlideUp, P_Platform, P_Glow, P_Expand;
        public Vector2 Speed;
        public HoldableState Hold;
        private readonly ISourceEngineContext engine;
        private OriginalLevelSceneRoot level;
        private readonly SourceCollisionHandler onCollideH, onCollideV;
        private Vector2 prevLiftSpeed;
        private readonly Vector2 startPos;
        private float noGravityTimer, highFrictionTimer;
        private bool bubble, tutorial, destroyed;
        private readonly Sprite sprite;
        private readonly WigglerComponent wiggler;
        private readonly SineWaveState platformSine;
        private readonly SoundSourceComponentState fallingSfx;
        private BirdTutorialGuiState tutorialGui;

        public static CustomGlider Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public CustomGlider(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(new PixelActor(null, data.Position + offset), engine,
                ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy)
        {
            this.engine = engine;
            bubble = data.Bool("bubble");
            tutorial = data.Bool("tutorial");
            startPos = Position;
            Collider = new HitboxColliderGeometry(8, 10, -4, -10);
            onCollideH = OnCollideH;
            onCollideV = OnCollideV;
            sprite = new Sprite(engine, ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas,
                data.Attr("Directory", "objects/glider").Trim('/') + "/") { Justify = new Vector2(.5f, .58f) };
            sprite.AddLoop("idle", "idle", .1f);
            sprite.AddLoop("held", "held", .1f);
            sprite.AddLoop("fallLoop", "fallLoop", .06f);
            sprite.Add("fall", "fall", .06f, "fallLoop");
            sprite.Add("death", "death", .06f);
            Add(sprite);
            sprite.Play("idle");
            Add(wiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(.25f, 4));
            Depth = data.Int("Depth", -5);
            Add(Hold = new HoldableState(engine, .3f));
            Hold.PickupCollider = new HitboxColliderGeometry(20, 22, -10, -16);
            Hold.SlowFall = true;
            Hold.SlowRun = false;
            Hold.OnPickup = OnPickup;
            Hold.OnRelease = OnRelease;
            Hold.SpeedGetter = () => Speed;
            Hold.OnHitSpring = value => HitSpring((ISourceSpringEntity)value);
            Add(platformSine = new SineWaveState(engine, .3f));
            Add(fallingSfx = new SoundSourceComponentState(engine));
            Add(new WindMoverState(WindMode));
            InitializeParticles(data);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = (OriginalLevelSceneRoot)Scene;
            if (tutorial)
            {
                var dialog = ((IOriginalGameTextEnvironment)engine).Text.Dialog;
                tutorialGui = new BirdTutorialGuiState(engine, ((IOriginalGameTagsEnvironment)engine).Tags.HUD.Value,
                    ((IOriginalBirdProcess)engine).BirdTutorialResources, this, new Vector2(0, -24),
                    dialog.Clean("tutorial_carry"),
                    dialog.Clean("tutorial_hold"), BirdTutorialButton.Grab) { Open = true };
                ((ISourceScene)Scene).SourceScene.QueueSourceEntity(tutorialGui);
            }
        }

        private void PlaySound(string sound, Vector2 position)
        {
            var process = (IOriginalGameplayProcess)engine;
            process.AudioSession.Play(sound, position, process.SaveContext);
        }
    }
}
