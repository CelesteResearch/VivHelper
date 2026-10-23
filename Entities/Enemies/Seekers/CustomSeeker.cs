// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Scene.Components.Shaking;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Graphics.Animation;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.CustomSeeker")]
    [CustomEntity("VivHelper/CustomSeekerS = Simple", "VivHelper/CustomSeeker = Custom",
        "VivHelper/CustomSeekerYaml = Yaml")]
    public partial class CustomSeeker : SourceActorEntityState
    {
        private readonly IOriginalSeekerProcess engine;
        private readonly Celeste.Mods.VivHelper.Runtime.Enemies.Seekers.CustomSeekerContext context;
        public PackedColor TrailColor = new PackedColor(153, 229, 80);
        private const int StIdle = 0;

        private const int StPatrol = 1;

        private const int StSpotted = 2;

        private const int StAttack = 3;

        private const int StStunned = 4;

        private const int StSkidding = 5;

        private const int StRegenerate = 6;

        private const int StReturned = 7;

        private const int size = 12;

        private const int bounceWidth = 16;

        private const int bounceHeight = 4;

        protected float Accel = 600f;

        protected float WallCollideStunThreshold = 100f;

        protected float StunXSpeed = 100f;

        protected float BounceSpeed = 200f;

        protected float SightDistSq = 25600f;

        protected float ExplodeRadius = 40f;

        protected HitboxColliderGeometry physicsHitbox;

        protected HitboxColliderGeometry breakWallsHitbox;

        protected HitboxColliderGeometry attackHitbox;

        protected HitboxColliderGeometry bounceHitbox;

        protected CircleColliderGeometry pushRadius;

        protected CircleColliderGeometry breakWallsRadius;

        protected SimulationStateMachine State;

        protected Vector2 lastSpottedAt;

        protected Vector2 lastPathTo;

        protected bool spotted;

        protected bool canSeePlayer;

        protected SourceCollisionHandler onCollideH;

        protected SourceCollisionHandler onCollideV;

        protected ISourceRandom random;

        protected Vector2 lastPosition;

        protected ShakerComponentState shaker;

        protected OscillationPulseState scaleWiggler;

        protected bool lastPathFound;

        protected List<Vector2> path;

        protected int pathIndex;

        protected Vector2[] patrolPoints;

        protected SineWaveState idleSineX;

        protected SineWaveState idleSineY;

        public VertexLightState Light;

        protected bool dead;

        protected SoundSourceComponentState boopedSfx;

        protected SoundSourceComponentState aggroSfx;

        protected SoundSourceComponentState reviveSfx;

        protected Sprite sprite;

        protected int facing = 1;

        protected int spriteFacing = 1;

        protected string nextSprite;

        protected HoldableColliderState theo;

        protected HashSet<string> flipAnimations = new HashSet<string> { "flipMouth", "flipEyes", "skid" };

        public Vector2 Speed;

        protected float FarDistSq = 12544f;

        protected float IdleAccel = 200f;

        protected float IdleSpeed = 50f;

        protected float PatrolSpeed = 25f;

        protected const int PatrolChoices = 3;

        protected float PatrolWaitTime = 0.4f;

        protected readonly SeekerPatrolPoint[] patrolChoices;

        protected float patrolWaitTimer;

        protected float SpottedTargetSpeed = 60f;

        protected float SpottedMaxYDist = 24f;

        protected float AttackMinXDist = 16f;

        protected float SpottedLosePlayerTime = 0.6f;

        protected float SpottedMinAttackTime = 0.2f;

        protected float spottedLosePlayerTimer;

        protected float spottedTurnDelay;

        protected float AttackWindUpSpeed = -60f;

        protected float AttackWindUpTime = 0.3f;

        protected float AttackStartSpeed = 180f;

        protected float AttackTargetSpeed = 260f;

        protected float AttackAccel = 300f;

        protected float DirectionDotThreshold = 0.4f;

        protected const int AttackTargetUpShift = 2;

        protected float AttackMaxRotateRadians = 0.61086524f;

        protected float attackSpeed;

        protected bool attackWindUp;

        protected float StunnedAccel = 150f;

        protected float StunTime = 0.8f;

        protected float SkiddingAccel = 200f;

        protected float StrongSkiddingAccel = 400f;

        protected float StrongSkiddingTime = 0.08f;

        protected bool strongSkid;

        protected float FarSpeedMultiplier = 2f;

        protected float XDistanceFromWindUp = 36f;

        protected string FlagOnDeath = "";

        public bool legacyPathfindingBehavior;

        public int numberOfDashes;

        public int numberOfBounces;

        public int numberOfWallCollides;

        public int maxNumberOfDashes;

        public int maxNumberOfBounces;

        public int maxNumberOfWallCollides;

        public float skiddingDelay;

        public float attackCooldown;

        public bool finalDash;

        public bool DisableEffects;

        protected string CustomSpritePath;

        protected string CustomShockwavePath;

        protected float ParticleEmitInterval;

        protected float TrailCreateInterval;

        protected float RegenerateTimerMult = 1f;

        protected bool disableAllParticles;

        protected bool IgnoreCamera;

        protected bool AlwaysSeePlayer;

        protected PackedColor tint;

        protected bool RemoveBounceHitbox;

        protected PackedColor DeathEffectColor;

        protected string boopedSFX = "event:/game/05_mirror_temple/seeker_booped";

        protected string aggroSFX = "event:/game/05_mirror_temple/seeker_aggro";

        protected string reviveSFX = "event:/game/05_mirror_temple/seeker_revive";

        protected int StartingState;

        protected float ActorDelay;

        public bool generatorTag;

        public float deLagCounter = 0f;

        public float deLagValue = 0.1f;

        public List<CustomSeeker> CustomSeekersList
        {
            get => context.Seekers;
            set => context.Seekers = value;
        }

        public bool Attacking
        {
            get
            {
                if (State.State == 3)
                {
                    return !attackWindUp;
                }

                return false;
            }
        }

        public bool Spotted
        {
            get
            {
                if (State.State != 3)
                {
                    return State.State == 2;
                }

                return true;
            }
        }

        public bool Regenerating => State.State == 6;

        protected Vector2 FollowTarget => lastSpottedAt - Vector2.UnitY * 2f;
    }
}
