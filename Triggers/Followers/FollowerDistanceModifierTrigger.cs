// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.VivHelper.Runtime.RefillCancellation;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.VivHelper.Triggers.Followers;

[CustomEntity("VivHelper/FollowerDistModTrigger")]
[RuntimeTypeAlias("VivHelper.Triggers.FollowerDistanceModifierTrigger")]
public class FollowerDistanceModifierTrigger : SourceTriggerEntityState
{
    private readonly RefillCancelContext context;
    private int FFD, FPD;

    public FollowerDistanceModifierTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
        ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset, data.Width,
        data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
    {
        context = RefillCancelContext.For(engine);
        FFD = data.Int("FFDistance", 5);
        FPD = data.Int("FPDistance", 30);
    }

    public override void OnEnter(RoomPlayerSession player)
    {
        base.OnEnter(player);
        context.Session.FFDistance = Math.Min(Math.Max(0, FFD), 30);
        context.Session.FPDistance = Math.Min(Math.Max(1, FPD), 128);
    }

    public override void OnLeave(RoomPlayerSession player)
    {
        base.OnLeave(player);
        RemoveSelf();
    }
}
