// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Gameplay.Platforms.FallingBlocks;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Entities.FallingBlocks;

[SceneTracked(false)]
[RuntimeTypeAlias("VivHelper.Entities.CustomFallingBlock")]
public partial class CustomFallingBlock : FallingBlockState
{
    private static readonly Dictionary<string, float> angles = new()
    {
        ["right"] = 0f, ["down"] = (float)Math.PI / 2f,
        ["left"] = (float)Math.PI, ["up"] = 4.712389f
    };

    private readonly float accel;
    private readonly float maxSpeed;
    private readonly bool dashBlock;
    private readonly string flagTrigger;
    private readonly string flagOnFall;
    private readonly string flagOnGround;
    private readonly bool flagOnly;
    private readonly string shakeSfx;
    private readonly string impactSfx;

    private readonly float angle;

    // Original 1.14.10 stores these two respawn values but never reads them.
    private readonly bool respawn;
    private readonly float respawnTime;
    public bool IsTriggeredByCrushBlock;
    public bool metaTriggered;
    public bool legacy;
    public bool DisableEndShake;
    public int RoomWrap;
    public bool EndShaking { get; private set; }
    private OriginalLevelSceneRoot Level => (OriginalLevelSceneRoot)Scene;

    public CustomFallingBlock(EntityData data, Vector2 offset, ISourceEngineContext engine,
        ISourceSolidWorldFactory worlds)
        : base(data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height, false,
            data.Bool("behind", false), data.Bool("bufferClimbFall", false) || data.Bool("climbFall", true),
            worlds.Create(data.Position + offset, false), engine)
    {
        if (data.Bool("bufferClimbFall", false))
            throw new NotSupportedException("VivHelper SolidModifier bufferClimbFall is not ported.");
        RoomWrap = data.Int("RoomWrapMaxRev", 0);
        IsTriggeredByCrushBlock = data.Bool("CrushBlockTrigger", false);
        float respawnT = data.Float("RespawnTime", -1f);
        respawn = respawnT >= 0f;
        respawnTime = respawn ? respawnT : 0f;
        accel = data.Float("Accel", 500f);
        maxSpeed = data.Float("MaxSpeed", 160f);
        shakeSfx = data.Attr("ShakeSFX", "event:/game/general/fallblock_shake");
        impactSfx = data.Attr("ImpactSFX", "event:/game/general/fallblock_impact");
        angle = angles[data.Attr("Direction", "Down").ToLower()];
        flagOnFall = data.Attr("FlagOnFall", "");
        flagTrigger = data.Attr("FlagTrigger", "");
        flagOnGround = data.Attr("FlagOnGround", "");
        flagOnly = data.Bool("flagOnly", false);
        legacy = !data.Bool("Legacy", false);
        dashBlock = data.Bool("BreakDashBlocks", true);
        DisableEndShake = data.Bool("ShakeForAllFalls", false);
    }

    public override void Awake(object scene)
    {
        base.Awake(scene);
        ResetFlag(flagOnGround);
        ResetFlag(flagOnFall);
        ResetFlag(flagTrigger);
    }

    private void ResetFlag(string flag)
    {
        if (flag != "" && Level.Session.GetFlag(flag)) Level.Session.SetFlag(flag, false);
    }

    public override void Update()
    {
        base.Update();
        if (flagTrigger != "" && Level.Session.GetFlag(flagTrigger) && !Triggered && !metaTriggered)
        {
            if (legacy) Triggered = true;
            else metaTriggered = true;
        }

        if (Triggered && flagOnFall != "" && !Level.Session.GetFlag(flagOnFall))
            Level.Session.SetFlag(flagOnFall, true);
        global::Celeste.Mods.VivHelper.Entities.Controllers.RoomWrapping.RoomWrapController.WrapFallingBlock(this,
            ref RoomWrap);
    }

    public override void OnStaticMoverTrigger(
        Celeste.Simulation.Gameplay.Platforms.StaticMovers.SourceStaticMoverState mover)
    {
        if (!finalBoss)
        {
            Triggered = true;
            metaTriggered = true;
        }
    }

    protected override IEnumerator CreateSequence() => Sequence();
}
