using Celeste.Game.Execution.Processes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Core.Audio;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Content;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.VivHelper.Entities.Teleportation
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.InLevelTeleporter")]
    [CustomEntity("VivHelper/InLevelTeleporter")]
    public partial class InLevelTeleporter : SceneEntityState
    {
        public enum Directions
        {
            Up = 3,
            Down = 1,
            Left = 2,
            Right = 0
        }

        private readonly ISourceEngineContext engine;
        private readonly InLevelTeleporterParticleTypes particles;
        private readonly List<SourceActorEntityState> actorCollisions = new();
        private bool updatingActors;
        private ParticleTypePresentation P_Glow => particles.Glow;

        public InLevelTeleporter pair;

        public string[] Flags;

        public float speedOut;

        public int length;

        public bool enabled;

        public bool cameraWarp;

        public int numOfUses;

        public int useCount;

        public string audio;

        private bool allActors;

        private bool AlwaysRetainSpeed;

        private bool legacy;

        private bool center;

        private bool OHMod;

        private float cooldown;

        private float CooldownTime = 0.05f;

        private Vector2 particlePosition;

        private Vector2 particleScope;

        private float particleDir;

        private PackedColor particleColor;

        public Directions Direction { get; private set; }

        public InLevelTeleporter(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Enum<Directions>("dir1", Directions.Up), data.Attr("flags1", ""), data.Float("sO1", 0f),
                data.Int("l", 16), data.Bool("eO1", false), data.Bool("allActors", false), data.Bool("cW", false),
                data.Int("NumberOfUses1", int.MaxValue), data.Attr("Audio", ""), data.Position + offset,
                data.Bool("legacy", true), data.Bool("center", false), data.Bool("OutbackHelperMod", false),
                data.Attr("Path", ""), ReadColor(data), data.Float("CooldownTime", 0f),
                data.Bool("AlwaysRetainSpeed", true))
        {
            Vector2 position = data.Nodes[0] + offset;
            pair = new InLevelTeleporter(engine, data.Enum<Directions>("dir2", Directions.Up), data.Attr("flags2", ""),
                data.Float("sO2", 0f), data.Int("l", 8), data.Bool("eO2", false), data.Bool("allActors", false),
                data.Bool("cW", false), data.Int("NumberOfUses2", int.MaxValue), data.Attr("Audio", ""), position,
                data.Bool("legacy", true), data.Bool("center", false), data.Bool("OutbackHelperMod", false),
                data.Attr("Path", ""), ReadColor(data), data.Float("CooldownTime", 0f),
                data.Bool("AlwaysRetainSpeed", true));
        }

        public InLevelTeleporter(ISourceEngineContext engine, Directions dir, string flags, float sOut, int length,
            bool eO, bool aA, bool cW, int use, string audio, Vector2 position, bool legacyTP, bool center, bool OOHMod,
            string spritePath, PackedColor particleColor, float cooldowntime, bool alwaysRetainSpeed)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            particles = InLevelTeleporterParticleTypes.For(engine);
            Direction = dir;
            Flags = flags.Split(',');
            speedOut = sOut;
            this.length = length;
            this.audio = audio;
            AlwaysRetainSpeed = alwaysRetainSpeed;
            cameraWarp = cW;
            numOfUses = use;
            if (numOfUses == -1)
            {
                numOfUses = int.MaxValue;
            }
            else if (numOfUses < 1)
            {
                numOfUses = 1;
            }

            switch (Direction)
            {
                case Directions.Left:
                    this.Collider = new HitboxColliderGeometry(16f, (float)length, -16f, 0f);
                    break;
                case Directions.Down:
                    this.Collider = new HitboxColliderGeometry((float)length, 16f, 0f, 0f);
                    break;
                case Directions.Right:
                    this.Collider = new HitboxColliderGeometry(16f, (float)length, 0f, 0f);
                    break;
                default:
                    this.Collider = new HitboxColliderGeometry((float)length, 16f, 0f, -16f);
                    break;
            }

            if (!eO)
            {
                this.Add(new PlayerColliderComponentState(OnPlayer, this.Collider, null));
            }

            enabled = false;
            useCount = 0;
            allActors = aA;
            legacy = legacyTP;
            this.center = center;
            OHMod = OOHMod;
            CooldownTime = cooldowntime;
            if (string.IsNullOrWhiteSpace(spritePath) || !(particleColor != PackedColor.Transparent))
            {
                return;
            }

            float num = (float)Math.PI / 2f * (float)dir;
            particlePosition = base.Position +
                               ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitX, num) * 15f +
                               ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitY, num) *
                               (float)(length - 1) / (float)(((int)dir % 3 == 0) ? 2 : (-2));
            particleScope = ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitY, num) *
                (float)(length - 2) / 2f;
            particleDir = 0f - num;
            this.particleColor = particleColor;
            ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.PushFallback(null);
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            AtlasEntry val = atlas.GetOrDefault(spritePath, atlas.GetFallback());
            int num2 = length / 8;
            if (val != null)
            {
                int num3 = val.FrameWidth / 8;
                for (int i = 0; i < num2; i++)
                {
                    int num4 = ((i != 0) ? ((i != num2 - 1) ? 1 : 2) : 0);
                    SpriteImage val2 = new SpriteImage(val.GetSubtexture(0, num4 * 8, 8, 8, null));
                    val2.Position =
                        ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitX, num) * 16f +
                        ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitY, num) *
                        (float)(-8 * i + (((int)dir % 3 == 0) ? length : 0));
                    val2.Rotation = num + (float)Math.PI;
                    val2.Color = particleColor;
                    Add(val2);
                }
            }
            else
            {
                List<AtlasEntry> atlasSubtextures =
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtextures(spritePath);
                if (atlasSubtextures.Count != 0)
                {
                    List<AtlasEntry>[] array = new List<AtlasEntry>[3]
                    {
                        new List<AtlasEntry>(),
                        new List<AtlasEntry>(),
                        new List<AtlasEntry>()
                    };
                    foreach (AtlasEntry item in atlasSubtextures)
                    {
                        array[0].Add(item.GetSubtexture(0, 0, 8, 8, null));
                        array[1].Add(item.GetSubtexture(0, 8, 8, 8, null));
                        array[2].Add(item.GetSubtexture(0, 16, 8, 8, null));
                    }

                    AtlasEntry[][] array2 = new AtlasEntry[3][]
                    {
                        array[0].ToArray(),
                        array[1].ToArray(),
                        array[2].ToArray()
                    };
                    for (int j = 0; j < num2; j++)
                    {
                        int num5 = ((j != 0) ? ((j != num2 - 1) ? 1 : 2) : 0);
                        Sprite val3 = new Sprite(engine,
                            ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas, "");
                        val3.Justify = Vector2.Zero;
                        val3.AddLoop("idle", 0.06f, array2[num5]);
                        val3.Play("idle");
                        val3.Position =
                            ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitX, num) * 16f +
                            ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitY, num) *
                            (float)(-8 * j + (((int)dir % 3 == 0) ? length : 0));
                        val3.Rotation = num + (float)Math.PI;
                        val3.Color = particleColor;
                        Add(val3);
                    }
                }
            }

            ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.PopFallback();
        }
    }
}
