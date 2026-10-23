// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Mods.VivHelper.Modules;
using Celeste.Game.Entities.Hazards.Spinners.Crystal;

namespace Celeste.Mods.VivHelper.Entities.Spikes
{
    [SceneTracked(true)]
    [RuntimeTypeAlias("VivHelper.Entities.CustomSpike")]
    public class CustomSpike : SceneEntityState
    {
        protected readonly ISourceEngineContext Engine;
        private OriginalCrystalStaticSpinner hueSource;
        public SpikeDirection Direction;
        public bool CanWallbounce;
        public bool OverrideDirectionParity;
        public bool CanRefillOnGroundWhenNoKill;
        public PlayerColliderComponentState playerCollider;
        public bool NoRefillDash = true;

        protected CustomSpike(Vector2 position, int size, SpikeDirection direction,
            bool canWallbounce, bool canRefillOnGroundIfNoKill, bool allway, ISourceEngineContext engine,
            bool blockLedge = true)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            Position = position;
            Engine = engine;
            Direction = direction;
            CanWallbounce = canWallbounce;
            CanRefillOnGroundWhenNoKill = canRefillOnGroundIfNoKill;
            OverrideDirectionParity = allway;
            if (size <= 0) return;
            bool blocking = true;
            switch (direction)
            {
                case SpikeDirection.Up: Collider = new HitboxColliderGeometry(size, 3f, 0f, -3f); break;
                case SpikeDirection.Down:
                    Collider = new HitboxColliderGeometry(size, 3f);
                    blocking = false;
                    break;
                case SpikeDirection.Left: Collider = new HitboxColliderGeometry(3f, size, -3f); break;
                case SpikeDirection.Right: Collider = new HitboxColliderGeometry(3f, size); break;
                default: return;
            }

            if (blockLedge) Add(new LedgeBlockerState { Blocking = blocking });
            var gravity = VivSpikeCapabilities.GetGravity(engine);
            if (gravity != null)
                Add(gravity.CreatePlayerGravityListener((_, value, _) =>
                {
                    // The original looks up the blocker when called, including a subclass's later component.
                    var ledge = Get<LedgeBlockerState>();
                    if (ledge == null) return;
                    if (Direction == SpikeDirection.Up) ledge.Blocking = value == 0;
                    else if (Direction == SpikeDirection.Down) ledge.Blocking = value == 1;
                }));
        }

        protected PackedColor GetRainbowHue(Vector2 at)
        {
            if (Scene == null) return PackedColor.White;
            hueSource ??= VivHelperSpinnerCapabilities.GetHueSource(Engine);
            ((ISceneEntityAssociation)hueSource).Scene = Scene;
            return hueSource.GetHue(at);
        }

        public virtual bool BlocksWallJumpSide(SpikeDirection side) => Direction == side;

        protected virtual void OnCollide(RoomPlayerSession player)
        {
            if (OverrideDirectionParity)
            {
                player.Die(Vector2.Zero);
                return;
            }

            bool inverted = false;
            if (Direction is SpikeDirection.Up or SpikeDirection.Down)
            {
                var gravity = VivSpikeCapabilities.GetGravity(Engine);
                inverted = gravity != null && gravity.IsPlayerInverted();
            }

            Vector2 death = Vector2.Zero;
            switch (Direction)
            {
                case SpikeDirection.Up:
                    if ((!inverted && player.Data.Motion.Normal.Speed.Y >= 0f && player.Bottom <= Bottom) ||
                        (inverted && player.Data.Motion.Normal.Speed.Y <= 0f && player.Machine.State == 9 &&
                         player.Bottom <= Bottom))
                        death -= Vector2.UnitY;
                    break;
                case SpikeDirection.Down:
                    if ((!inverted && player.Data.Motion.Normal.Speed.Y <= 0f) ||
                        (inverted && player.Data.Motion.Normal.Speed.Y >= 0f && player.Top >= Top))
                        death += Vector2.UnitY;
                    break;
                case SpikeDirection.Left:
                    if (player.Data.Motion.Normal.Speed.X >= 0f) death -= Vector2.UnitX;
                    break;
                case SpikeDirection.Right:
                    if (player.Data.Motion.Normal.Speed.X <= 0f) death += Vector2.UnitX;
                    break;
            }

            if (death != Vector2.Zero) player.Die(death);
        }
    }
}
