// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Mods.VivHelper.Modules;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Formats.Map.Entities.Structure;

namespace Celeste.Mods.VivHelper.Entities.Spikes
{
    [SceneTracked]
    [CustomEntity("VivHelper/CornerSpike")]
    [RuntimeTypeAlias("VivHelper.Entities.SpikeStuff.CornerSpike")]
    public class CornerSpike : CustomSpike
    {
        public new DirectionPlus Direction;
        public readonly bool InnerSpike;
        private readonly SpriteImage image;
        private PackedColor color;
        private readonly bool rainbow;
        private Vector2 imageOffset;
        private readonly bool killFormat;
        private readonly ISourceVectorArithmetic vectors;

        public static CornerSpike Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public CornerSpike(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(data.Position + offset, 0, SpikeDirection.Up, false, false, false, engine)
        {
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            Depth = -1;
            string edge = data.Attr("EdgeDirection", "");
            string direction = edge;
            if (edge.Trim().StartsWith("Inner"))
            {
                InnerSpike = true;
                direction = edge.Substring(5);
            }

            string type = data.Attr("type", "default");
            string prefix = type == "default" ? "danger/spikes/corners/default_" :
                type == "outline" ? "danger/spikes/corners/outline_" : type + "_";
            image = new SpriteImage(((ISourceRandomEnvironment)engine).Random.Choose(
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtextures(
                    prefix + (InnerSpike ? "Inner" + direction : direction))));
            ConfigureDirection(direction);
            color = Runtime.Colors.VivEntityColor.Read(data, "Color");
            rainbow = color == PackedColor.Transparent;
            Add(new PlayerColliderComponentState(OnCollide));
            if (!data.Bool("DoNotAttach", false))
                Add(new SourceStaticMoverState
                {
                    OnShake = OnShake, SolidChecker = IsRiding, JumpThruChecker = IsRiding, OnEnable = OnEnable,
                    OnDisable = OnDisable
                });
            killFormat = data.Bool("KillFormat", false);
            Tags |= OriginalSceneTags.TransitionUpdate;
        }

        private void ConfigureDirection(string value)
        {
            switch (value)
            {
                case "UpLeft":
                    Direction = DirectionPlus.UpLeft;
                    image.JustifyOrigin(Vector2.One);
                    image.RenderPosition = Vector2.One;
                    Collider = InnerSpike
                        ? new ColliderListGeometry(new HitboxColliderGeometry(5, 3, -8, -3),
                            new HitboxColliderGeometry(3, 5, -3, -8))
                        : new HitboxColliderGeometry(3, 3, -3, -3);
                    break;
                case "DownLeft":
                    Direction = DirectionPlus.DownLeft;
                    image.JustifyOrigin(Vector2.UnitX);
                    image.RenderPosition = new Vector2(1, -1);
                    Collider = InnerSpike
                        ? new ColliderListGeometry(new HitboxColliderGeometry(5, 3, -8, 0),
                            new HitboxColliderGeometry(3, 5, -3, 3))
                        : new HitboxColliderGeometry(3, 3, -3, 0);
                    break;
                case "UpRight":
                    Direction = DirectionPlus.UpRight;
                    image.JustifyOrigin(Vector2.UnitY);
                    image.RenderPosition = new Vector2(-1, 1);
                    Collider = InnerSpike
                        ? new ColliderListGeometry(new HitboxColliderGeometry(5, 3, 3, -3),
                            new HitboxColliderGeometry(3, 5, 0, -8))
                        : new HitboxColliderGeometry(3, 3, 0, -3);
                    break;
                case "DownRight":
                    Direction = DirectionPlus.DownRight;
                    image.JustifyOrigin(Vector2.Zero);
                    image.RenderPosition = -Vector2.One;
                    Collider = InnerSpike
                        ? new ColliderListGeometry(new HitboxColliderGeometry(5, 3, 3, 0),
                            new HitboxColliderGeometry(3, 5, 0, 3))
                        : new HitboxColliderGeometry(3, 3);
                    break;
                default: throw new System.Collections.Generic.KeyNotFoundException();
            }
        }

        public override bool BlocksWallJumpSide(SpikeDirection side) => (Direction & (side switch
        {
            SpikeDirection.Up => DirectionPlus.Up, SpikeDirection.Down => DirectionPlus.Down,
            SpikeDirection.Left => DirectionPlus.Left, SpikeDirection.Right => DirectionPlus.Right,
            _ => DirectionPlus.Other
        })) != 0;

        public void SetSpikeColor(Vector2 pos) => color = GetRainbowHue(Position + pos);

        public override void Added(object scene)
        {
            base.Added(scene);
            if (!rainbow) image.Color = color;
            Add(image);
        }

        private void OnEnable() => Active = Visible = Collidable = true;
        private void OnDisable() => Active = Collidable = Visible = false;
        private void OnShake(Vector2 amount) => imageOffset += amount;

        public override void Render(ISourceRenderContext context)
        {
            Vector2 previous = Position;
            Position += imageOffset;
            if (rainbow)
            {
                SetSpikeColor(image.Position);
                image.Color = color;
            }

            image.Render(context);
            base.Render(context);
            Position = previous;
        }

        private bool IsRiding(SourceSolidEntityState solid)
        {
            Vector2 step = Direction switch
            {
                DirectionPlus.UpLeft => Vector2.One, DirectionPlus.DownLeft => new Vector2(1, -1),
                DirectionPlus.UpRight => new Vector2(-1, 1), DirectionPlus.DownRight => -Vector2.One,
                _ => Vector2.Zero
            };
            return step != Vector2.Zero && !SourceEntityCollision.Check(this, solid) &&
                   SourceEntityCollision.CheckAt(this, solid, Position + step);
        }

        private bool IsRiding(ISourceJumpThruEntity jumpThru) => Direction == DirectionPlus.Other &&
                                                                 SourceEntityCollision.CheckAt(this, jumpThru,
                                                                     Position + Vector2.UnitY);

        protected override void OnCollide(RoomPlayerSession player)
        {
            if (killFormat)
            {
                CollideDirectional(player);
                return;
            }

            var speed = player.Data.Motion.Normal.Speed;
            if (InnerSpike)
            {
                bool dream = player.Machine.State == 9;
                var gravity = VivSpikeCapabilities.GetGravity(Engine);
                bool inverted = gravity != null && gravity.IsPlayerInverted();
                var parts = ((ColliderListGeometry)Collider).colliders;
                switch (Direction)
                {
                    case DirectionPlus.UpLeft:
                    case DirectionPlus.UpRight:
                        if (SourceEntityCollision.CheckRect(player, parts[0].Bounds) &&
                            ((!inverted && player.Data.Motion.Normal.Speed.Y >= 0 &&
                              player.Bottom <= parts[0].AbsoluteBottom) ||
                             (inverted && player.Data.Motion.Normal.Speed.Y <= 0 &&
                              (!dream || player.Bottom <= parts[0].AbsoluteBottom)))) player.Die(new Vector2(0, -1));
                        break;
                    case DirectionPlus.DownLeft:
                    case DirectionPlus.DownRight:
                        if (SourceEntityCollision.CheckRect(player, parts[0].Bounds) &&
                            ((!inverted && player.Data.Motion.Normal.Speed.Y <= 0) || (inverted &&
                                player.Data.Motion.Normal.Speed.Y >= 0 &&
                                (!dream || player.Top >= parts[0].AbsoluteTop)))) player.Die(new Vector2(0, -1));
                        break;
                }

                if (Direction is DirectionPlus.UpLeft or DirectionPlus.DownLeft)
                {
                    if (SourceEntityCollision.CheckRect(player, parts[1].Bounds) &&
                        player.Data.Motion.Normal.Speed.X >= 0) player.Die(new Vector2(-1, 0));
                }
                else if (Direction is DirectionPlus.UpRight or DirectionPlus.DownRight)
                {
                    if (SourceEntityCollision.CheckRect(player, parts[1].Bounds) &&
                        player.Data.Motion.Normal.Speed.X <= 0) player.Die(new Vector2(1, 0));
                }
            }
            else
            {
                Vector2 death = Direction switch
                {
                    DirectionPlus.UpLeft => -Vector2.One, DirectionPlus.DownLeft => new Vector2(-1, 1),
                    DirectionPlus.UpRight => new Vector2(1, -1), _ => Vector2.One
                };
                if (vectors.Length(player.Data.Motion.Normal.Speed) <= .05f || vectors.Dot(vectors.Normalize(death),
                        vectors.Normalize(player.Data.Motion.Normal.Speed)) < 0) player.Die(death * 3);
            }
        }

        private void CollideDirectional(RoomPlayerSession player)
        {
            Vector2 death = Vector2.Zero;
            if ((Direction & DirectionPlus.Up) != 0)
            {
                var gravity = VivSpikeCapabilities.GetGravity(Engine);
                if (gravity == null || !gravity.IsPlayerInverted())
                {
                    if (!(player.Data.Motion.Normal.Speed.Y >= 0 &&
                          (Direction > DirectionPlus.Up || player.Bottom <= Bottom))) return;
                }
                else if (!(player.Data.Motion.Normal.Speed.Y <= 0 && (Direction > DirectionPlus.Up ||
                                                                      player.Machine.State == 9 &&
                                                                      player.Bottom <= Bottom))) return;

                death.Y -= 1;
            }

            if ((Direction & DirectionPlus.Down) != 0)
            {
                var gravity = VivSpikeCapabilities.GetGravity(Engine);
                if (gravity == null || !gravity.IsPlayerInverted())
                {
                    if (!(player.Data.Motion.Normal.Speed.Y <= 0)) return;
                }
                else if (!(player.Data.Motion.Normal.Speed.Y >= 0 &&
                           (Direction > DirectionPlus.Down || player.Top >= Top))) return;

                death.Y += 1;
            }

            if ((Direction & DirectionPlus.Left) != 0)
            {
                if (!(player.Data.Motion.Normal.Speed.X >= 0)) return;
                death.X -= 1;
            }

            if ((Direction & DirectionPlus.Right) != 0)
            {
                if (!(player.Data.Motion.Normal.Speed.X <= 0)) return;
                death.X += 1;
            }

            player.Die(death);
        }
    }
}
