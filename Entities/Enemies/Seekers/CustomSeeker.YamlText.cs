// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        private static string ColorToHex(PackedColor color) => color.A.ToString("X") + color.R.ToString("X") +
                                                               color.G.ToString("X") + color.B.ToString("X");

        public virtual List<string> GetYAMLText(bool Default)
        {
            List<string> list = new List<string>();
            if (Default)
            {
                return new List<string>
                {
                    "Accel: 600", "AttackAccel: 300", "AttackMinXDist: 16", "AttackStartSpeed: 180",
                    "AttackTargetSpeed: 260", "AttackWindUpSpeed: 60", "AttackWindUpTime: 0.3",
                    "AttackMaxRotateRadians: 35", "", "aggroSFXPath: \"\"",
                    "boopedSFXPath: \"\"", "reviveSFXPath: \"\"", "CustomSpritePath: \"\"", "CustomShockwavePath: \"\"",
                    "", "BounceSpeed: 200", "DirectionDotThreshold: 0.4", "ExplodeRadius: 40", "SightDistance: 160", "",
                    "FarDist: 112", "FarDistSpeedMult: 2", "", "IdleSpeed: 50", "IdleAccel: 200", "",
                    "MaxNumberOfBounces: -1", "MaxNumberOfDashes: -1", "MaxNumberOfWallCollides: -1", "",
                    "PatrolSpeed: 25", "PatrolWaitTime: 0.4", "", "RegenerationTimerLength: 1.85", "",
                    "StrongSkiddingTime: 0.08", "SkiddingAccel: 200", "StrongSkiddingAccel: 400", "",
                    "SpottedLosePlayerTime: 0.6",
                    "SpottedMaxYDist: 24", "SpottedMinAttackTime: 0.2", "SpottedTargetSpeed: 60", "", "StunTime: 0.8",
                    "StunXSpeed: 100", "", "WallCollideStunThreshold: 100", "DeathEffectColor: \"FFFFFF\"",
                    "SeekerColorTint: \"FFFFFF\"",
                    "", "TrailColor: \"FFFFFF\"", "", "DisableAllParticles: False", "FinalDash: True",
                    "RemoveBounceHitbox: False", "", "ParticleEmitInterval: 0.04", "TrailCreateInterval: 0.06",
                    "StartingState: 0",
                    "FlagOnDeath", "\n"
                };
            }

            return new List<string>
            {
                "Accel: " + Accel,
                "AttackAccel: " + AttackAccel,
                "AttackMinXDist: " + AttackMinXDist,
                "AttackStartSpeed: " + AttackStartSpeed,
                "AttackTargetSpeed: " + AttackTargetSpeed,
                "AttackWindUpSpeed: " + (0f - AttackWindUpSpeed),
                "AttackWindUpTime: " + AttackWindUpTime,
                "AttackMaxRotateDegrees: " + (float)Math.Round(AttackMaxRotateRadians * 180f / (float)Math.PI),
                "",
                "aggroSFXPath: \"" + aggroSFX + "\"",
                "boopedSFXPath: \"" + boopedSFX + "\"",
                "reviveSFXPath: \"" + reviveSFX + "\"",
                "CustomSpritePath: \"" + CustomSpritePath + "\"",
                "CustomShockwavePath: \"" + CustomShockwavePath + "\"",
                "",
                "BounceSpeed: " + BounceSpeed,
                "DirectionDotThreshold: " + DirectionDotThreshold,
                "ExplodeRadius: " + ExplodeRadius,
                "SightDistance: " + Math.Sqrt(SightDistSq),
                "",
                "FarDist: " + Math.Sqrt(FarDistSq),
                "FarDistSpeedMult: " + FarSpeedMultiplier,
                "",
                "IdleSpeed: " + IdleSpeed,
                "IdleAccel: " + IdleAccel,
                "",
                "MaxNumberOfBounces: " + maxNumberOfBounces,
                "MaxNumberOfDashes: " + maxNumberOfDashes,
                "MaxNumberOfWallCollides: " + maxNumberOfWallCollides,
                "",
                "PatrolSpeed: " + PatrolSpeed,
                "PatrolWaitTime: " + PatrolWaitTime,
                "",
                "RegenerationTimerLength: " + RegenerateTimerMult * 1.85f,
                "",
                "StrongSkiddingTime: " + StrongSkiddingTime,
                "SkiddingAccel: " + SkiddingAccel,
                "StrongSkiddingAccel: " + StrongSkiddingAccel,
                "",
                "SpottedLosePlayerTime: " + SpottedLosePlayerTime,
                "SpottedMaxYDist: " + SpottedMaxYDist,
                "SpottedMinAttackTime: " + SpottedMinAttackTime,
                "SpottedTargetSpeed: " + SpottedTargetSpeed,
                "",
                "StunTime: " + StunTime,
                "StunXSpeed: " + StunXSpeed,
                "",
                "WallCollideStunThreshold: " + WallCollideStunThreshold,
                "DeathEffectColor: \"" + ColorToHex(DeathEffectColor) + "\"",
                "SeekerColorTint: \"" + ColorToHex(tint) + "\"",
                "",
                "TrailColor: \"" + ColorToHex(TrailColor) + "\"",
                "",
                "DisableAllParticles: " + disableAllParticles,
                "FinalDash: " + finalDash,
                "RemoveBounceHitbox: " + RemoveBounceHitbox,
                "",
                "ParticleEmitInterval: " + ParticleEmitInterval,
                "TrailCreateInterval: " + TrailCreateInterval,
                "StartingState: " + StartingState,
                "\n"
            };
        }
    }
}
