// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Components.Shaking;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Effects.Mirrors;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        public static CustomSeeker Simple(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(data, offset, (IOriginalSeekerProcess)level.EngineContext);

        public static CustomSeeker Custom(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(data, offset, (IOriginalSeekerProcess)level.EngineContext);

        public CustomSeeker(EntityData data, Vector2 offset, IOriginalSeekerProcess engine) : base(
            new PixelActor(null, data.Position + offset), engine, engine.TargetMotionPolicy)
        {
            this.engine = engine;
            context = Celeste.Mods.VivHelper.Runtime.Enemies.Seekers.CustomSeekerContext.For(engine);
            patrolChoices = context.PatrolChoices;
            Accel = data.Float("Accel", 600f);
            WallCollideStunThreshold = data.Float("WallCollideStunThreshold", 100f);
            StunXSpeed = data.Float("StunXSpeed", 100f);
            BounceSpeed = data.Float("BounceSpeed", 200f);
            SightDistSq = data.Float("SightDistance", 160f) * data.Float("SightDistance", 160f);
            ExplodeRadius = data.Float("ExplodeRadius", 40f);
            StrongSkiddingTime = data.Float("StrongSkiddingTime", 0.08f);
            FarDistSq = data.Float("FarDist", 112f) * data.Float("FarDist", 112f);
            IdleAccel = data.Float("IdleAccel", 200f);
            IdleSpeed = data.Float("IdleSpeed", 50f);
            SkiddingAccel = data.Float("SkiddingAccel", 200f);
            StrongSkiddingAccel = data.Float("StrongSkiddingAccel", 2f * SkiddingAccel);
            PatrolSpeed = data.Float("PatrolSpeed", 25f);
            PatrolWaitTime = data.Float("PatrolWaitTime", 0.4f);
            SpottedTargetSpeed = data.Float("SpottedTargetSpeed", 60f);
            FarSpeedMultiplier = data.Float("FarDistSpeedMult", 2f);
            DirectionDotThreshold = data.Float("DirectionDotThreshold", 0.4f);
            AttackMinXDist = data.Float("AttackMinXDist", 16f);
            SpottedMaxYDist = data.Float("SpottedMaxYDist", 24f);
            SpottedLosePlayerTime = data.Float("SpottedLosePlayerTime", 0.6f);
            SpottedMinAttackTime = data.Float("SpottedMinAttackTime", 0.2f);
            AttackWindUpSpeed = 0f - data.Float("AttackWindUpSpeed", 60f);
            AttackWindUpTime = data.Float("AttackWindUpTime", 0.3f);
            XDistanceFromWindUp = data.Float("XDistanceFromWindUp", 36f);
            AttackStartSpeed = data.Float("AttackStartSpeed", 180f);
            AttackTargetSpeed = data.Float("AttackTargetSpeed", 260f);
            AttackAccel = data.Float("AttackAccel", 300f);
            StunnedAccel = data.Float("StunnedAccel", 150f);
            StunTime = data.Float("StunTime", 0.8f);
            disableAllParticles = data.Bool("DisableAllParticles", false);
            ParticleEmitInterval = (disableAllParticles ? 0f : data.Float("ParticleEmitInterval", 0.04f));
            TrailCreateInterval = data.Float("TrailCreateInterval", 0.06f);
            RegenerateTimerMult = data.Float("RegenerationTimerLength", 1.85f) / 1.85f;
            StrongSkiddingTime = data.Float("StrongSkiddingTime", 0.08f);
            AttackMaxRotateRadians = data.Float("AttackMaxRotateDegrees", 35f) * (float)Math.PI / 180f;
            TrailColor = Celeste.Mods.VivHelper.Spawn.SpawnPointRules.ParseColor(data.Attr("TrailColor", "99e550"));
            numberOfDashes = 0;
            maxNumberOfDashes = data.Int("MaxNumberOfDashes", -1);
            finalDash = data.Bool("FinalDash", false);
            numberOfWallCollides = 0;
            maxNumberOfWallCollides = data.Int("MaxNumberOfWallCollides", -1);
            numberOfBounces = 0;
            maxNumberOfBounces = data.Int("MaxNumberOfBounces", -1);
            legacyPathfindingBehavior = !data.Bool("NewPathfindingBehavior", false);
            AlwaysSeePlayer = data.Bool("AlwaysSeePlayer", false);
            IgnoreCamera = data.Bool("SpottedNoCameraLimit", false);
            deLagValue = Math.Max(1, Math.Min(30, data.Int("aiDelag", 6))) / 60f;
            DisableEffects = data.Bool("DisableEffects", false);
            DeathEffectColor =
                Celeste.Mods.VivHelper.Spawn.SpawnPointRules.ParseColor(data.Attr("DeathEffectColor", "HotPink"));
            RemoveBounceHitbox = data.Bool("RemoveBounceHitbox", false);
            FlagOnDeath = data.Attr("FlagOnDeath", "");
            string text = data.Attr("boopedSFXPath", "");
            boopedSFX = ((text == "") ? "event:/game/05_mirror_temple/seeker_booped" : text);
            string text2 = data.Attr("aggroSFXPath", "");
            aggroSFX = ((text2 == "") ? "event:/game/05_mirror_temple/seeker_aggro" : text2);
            string text3 = data.Attr("reviveSFXPath", "");
            reviveSFX = ((text3 == "") ? "event:/game/05_mirror_temple/seeker_revive" : text3);
            CustomSpritePath = data.Attr("CustomSpritePath", "");
            CustomShockwavePath = data.Attr("CustomShockwavePath", "");
            sprite = ((CustomSpritePath == "")
                ? engine.GameGraphics.SpriteFactory.Create("seeker")
                : engine.GameGraphics.SpriteFactory.Create(CustomSpritePath));
            tint = Celeste.Mods.VivHelper.Spawn.SpawnPointRules.ParseColor(data.Attr("SeekerColorTint", "ffffff"));
            sprite.Color = tint;
            Vector2 position = data.Position + offset;
            Depth = -200;
            patrolPoints = data.NodesOffset(offset);
            lastPosition = position;
            Collider = physicsHitbox = new HitboxColliderGeometry(6f, 6f, -3f, -3f);
            breakWallsHitbox = new HitboxColliderGeometry(6f, 14f, -3f, -7f);
            attackHitbox = RemoveBounceHitbox
                ? new HitboxColliderGeometry(16f, 14f, -8f, -8f)
                : new HitboxColliderGeometry(12f, 8f, -6f, -2f);
            bounceHitbox = RemoveBounceHitbox
                ? new HitboxColliderGeometry(8f, 7f, -4f, -3.5f)
                : new HitboxColliderGeometry(16f, 6f, -8f, -8f);
            pushRadius = new CircleColliderGeometry(engine.VectorArithmetic, ExplodeRadius);
            breakWallsRadius = new CircleColliderGeometry(engine.VectorArithmetic, 16f);
            Add(new PlayerColliderComponentState(OnAttackPlayer, attackHitbox));
            Add(new PlayerColliderComponentState(OnBouncePlayer, bounceHitbox));
            Add(shaker = new ShakerComponentState(engine, on: false));
            Add(State = ((ISourceStateMachineEnvironment)engine).StateMachines.Create(engine, 10));
            State.SetCallbacks(0, IdleUpdate, IdleCoroutine);
            State.SetCallbacks(1, PatrolUpdate, null, PatrolBegin);
            State.SetCallbacks(2, SpottedUpdate, SpottedCoroutine, SpottedBegin);
            State.SetCallbacks(3, AttackUpdate, AttackCoroutine, AttackBegin);
            State.SetCallbacks(4, StunnedUpdate, StunnedCoroutine);
            State.SetCallbacks(5, SkiddingUpdate, SkiddingCoroutine, SkiddingBegin, SkiddingEnd);
            State.SetCallbacks(6, RegenerateUpdate, RegenerateCoroutine, RegenerateBegin, RegenerateEnd);
            State.SetCallbacks(7, null, ReturnedCoroutine);
            State.State = StartingState = data.Int("StartingState", 0);
            onCollideH = OnCollideH;
            onCollideV = OnCollideV;
            Add(idleSineX = new SineWaveState(engine, .5f));
            Add(idleSineY = new SineWaveState(engine, .7f));
            Add(Light = new VertexLightState(engine, Vector2.Zero, PackedColor.White, 1f, 32, 64));
            Add(theo = new HoldableColliderState(OnHoldable, attackHitbox));
            Add(new MirrorReflectionState());
            path = new List<Vector2>();
            IgnoreJumpThrus = true;
            Add(sprite);
            sprite.OnLastFrame = frame =>
            {
                if (flipAnimations.Contains(frame) && spriteFacing != facing)
                {
                    spriteFacing = facing;
                    if (nextSprite != null)
                    {
                        sprite.Play(nextSprite);
                        nextSprite = null;
                    }
                }
            };
            sprite.OnChange = (last, next) =>
            {
                nextSprite = null;
                sprite.OnLastFrame(last);
            };
            SquishCallback = collision =>
            {
                if (!dead && !TrySquishWiggle(collision)) SeekerDeath();
            };
            scaleWiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(.8f, 2f);
            Add(scaleWiggler);
            Add(boopedSfx = engine.SoundSources.Create());
            Add(aggroSfx = engine.SoundSources.Create());
            Add(reviveSfx = engine.SoundSources.Create());
            CustomSeekersList = new List<CustomSeeker>();
        }
    }
}
