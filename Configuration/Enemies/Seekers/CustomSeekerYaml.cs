// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Generic;
using Celeste.Core.Reflection.Runtime;
using System.Reflection;
using Celeste.Game.Content.Maps.Entities;
using System.Numerics;

namespace Celeste.Mods.VivHelper.Configuration.Enemies.Seekers;

[RuntimeTypeAlias("VivHelper.Entities.CustomSeekerYaml")]
public class CustomSeekerYaml
{
    public int Order;

    public float Accel;

    public float AttackAccel;

    public float AttackMinXDist;

    public float XDistanceFromWindUp;

    public float AttackStartSpeed;

    public float AttackTargetSpeed;

    public float AttackWindUpSpeed;

    public float AttackWindUpTime;

    public float AttackMaxRotateDegrees;

    public string aggroSFXPath;

    public string boopedSFXPath;

    public string reviveSFXPath;

    public string CustomSpritePath;

    public string CustomShockwavePath;

    public float BounceSpeed;

    public float DirectionDotThreshold;

    public float ExplodeRadius;

    public float SightDistance;

    public float FarDist;

    public float FarDistSpeedMult;

    public float IdleSpeed;

    public float IdleAccel;

    public int MaxNumberOfBounces;

    public int MaxNumberOfDashes;

    public int MaxNumberOfWallCollides;

    public float PatrolSpeed;

    public float PatrolWaitTime;

    public float RegenerationTimerLength;

    public float StrongSkiddingTime;

    public float SkiddingAccel;

    public float StrongSkiddingAccel;

    public float SpottedLosePlayerTime;

    public float SpottedMaxYDist;

    public float SpottedMinAttackTime;

    public float SpottedTargetSpeed;

    public float StunTime;

    public float StunXSpeed;

    public float WallCollideStunThreshold;

    public string DeathEffectColor;

    public string SeekerColorTint;

    public string TrailColor;

    public string FlagOnDeath;

    public bool DisableAllParticles;

    public bool FinalDash;

    public bool RemoveBounceHitbox;

    public float ParticleEmitInterval;

    public float TrailCreateInterval;

    public OriginalLevelEntityData SeekerDataFromYaml(Vector2 position)
    {
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        PropertyInfo[] properties = GetType().GetProperties();
        foreach (PropertyInfo propertyInfo in properties)
        {
            if (propertyInfo.Name != "Order")
            {
                dictionary[propertyInfo.Name] = propertyInfo.GetValue(this);
            }
        }

        return new OriginalLevelEntityData
        {
            Position = position,
            Values = dictionary
        };
    }
}
