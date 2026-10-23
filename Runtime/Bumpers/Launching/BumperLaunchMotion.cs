// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Scene.Tracking;
using Celeste.Mods.VivHelper.Entities.Hazards.Bumpers;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Scene.Effects;

namespace Celeste.Mods.VivHelper.Runtime.Bumpers.Launching
{
    public static class BumperLaunchMotion
    {
        public static Vector2 Launch(RoomPlayerSession player, Vector2 from, bool snapUp, bool sidesOnly,
            BumperLaunchKind kind, BoostRestriction boost, float multiplier = 1f)
        {
            if (sidesOnly ||
                kind is not (BumperLaunchKind.Cardinal or BumperLaunchKind.Diagonal or BumperLaunchKind.EightWay
                    or BumperLaunchKind.Alt4way) && boost == BoostRestriction.Default)
                return player.ExplodeLaunch(from, snapUp, sidesOnly);
            var engine = player.EngineContext;
            ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(2, 1);
            bool directionalBoost = kind is BumperLaunchKind.Cardinal or BumperLaunchKind.EightWay;
            bool restrictable = directionalBoost || kind is not (BumperLaunchKind.Diagonal or BumperLaunchKind.Alt4way);
            if (restrictable && boost == BoostRestriction.BetterBoost)
                ((ISourceScene)player.Scene).SourceScene.AddEntity(new BetterBumperBoost(player));
            else player.Freeze(.1f);
            player.Special.LaunchApproachX = null;
            var vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            Vector2 direction = player.Center - from;
            direction = vectors.SafeNormalize(direction, -Vector2.UnitY);
            switch (kind)
            {
                case BumperLaunchKind.Cardinal: player.Speed = Vector2Geometry.FourWayNormal(direction) * 280f; break;
                case BumperLaunchKind.EightWay: player.Speed = Vector2Geometry.EightWayNormal(direction) * 280f; break;
                case BumperLaunchKind.Alt4way: player.Speed = AlternateDirection(direction, multiplier) * 280f; break;
                // The released mod computes Diagonal4Way but never assigns its result to Speed.
                case BumperLaunchKind.Diagonal: break;
                default:
                    float dot = vectors.Dot(direction, Vector2.UnitY);
                    if (snapUp && dot <= -.7f) direction = -Vector2.UnitY;
                    else if (dot <= .65f && dot >= -.55f)
                    {
                        direction.Y = 0;
                        direction.X = Math.Sign(direction.X);
                    }

                    player.Speed = 280f * direction;
                    break;
            }

            bool lift = kind switch
            {
                BumperLaunchKind.Cardinal or BumperLaunchKind.EightWay => player.Speed.Y == 0f,
                BumperLaunchKind.Alt4way => player.Speed.Y <= 0f,
                BumperLaunchKind.Diagonal => false,
                _ => player.Speed.Y <= 50f
            };
            if (lift)
            {
                player.Data.Motion.Normal.Speed.Y = Math.Min(-150f, player.Speed.Y);
                player.Data.Motion.AutoJump = true;
            }
            else if (kind == BumperLaunchKind.Diagonal && player.Speed.Y <= -50f) player.Data.Motion.AutoJump = true;

            if (restrictable && player.Speed.X != 0 &&
                boost is not (BoostRestriction.NoBoost or BoostRestriction.BetterBoost))
            {
                bool immediate = player.Input.MoveX.Value == Math.Sign(player.Speed.X) ||
                                 !directionalBoost && boost == BoostRestriction.AlwaysBoost;
                if (immediate)
                {
                    player.Data.ExplodeLaunchBoostTimer = 0;
                    player.Data.Motion.Normal.Speed.X *= 1.2f;
                }
                else if (directionalBoost
                             ? boost == BoostRestriction.OldBehavior
                             : boost != BoostRestriction.OldBehavior)
                {
                    player.Data.ExplodeLaunchBoostTimer = .01f;
                    player.Data.ExplodeLaunchBoostSpeed = player.Speed.X * 1.2f;
                }
            }

            ((ISourcePlayerDashEffectsScene)player.Level).CreatePlayerDashSlash(player.Center,
                Vector2Geometry.Angle(player.Speed));
            if (!player.World.NoRefills) player.World.RefillDash();
            player.RefillStamina();
            player.Data.CooldownTimer = .2f;
            player.Machine.State = 7;
            return kind is BumperLaunchKind.Cardinal or BumperLaunchKind.Diagonal or BumperLaunchKind.EightWay
                or BumperLaunchKind.Alt4way
                ? player.Speed
                : direction;
        }

        private static Vector2 AlternateDirection(Vector2 direction, float multiplier)
        {
            float angle = Vector2Geometry.Angle(direction);
            if (angle >= 1.0472f && angle <= 2.0944f) return Vector2Geometry.AngleToVector(1.5708f, 1f);
            if (-.6545f <= angle && angle < 1.0472f)
            {
                var value = Vector2Geometry.AngleToVector(-.436332f, 1f);
                value.Y /= multiplier;
                return value;
            }

            if (angle > 2.0944f && angle <= 3.141593f || angle > -3.1416f && angle < -2.4871f)
            {
                var value = Vector2Geometry.AngleToVector(-2.70526f, 1f);
                value.Y /= multiplier;
                return value;
            }

            return Vector2Geometry.AngleToVector(-1.5708f, 1f);
        }
    }
}
