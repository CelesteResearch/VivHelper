// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Game.Execution.Processes;
using Celeste.Simulation;

namespace Celeste.Mods.VivHelper.Entities.FallingBlocks;

public partial class CustomFallingBlock
{
    private bool CheckAt(string type, Vector2 at) =>
        SourceTrackedEntityCollision.CheckAt(((ISourceScene)Scene).SourceScene.Entities, type, this, at);

    private Vector2 ImpactCenter => angle == 0f ? TopLeft + new Vector2(Width, Height / 2f)
        : angle == (float)Math.PI / 2f ? TopLeft + new Vector2(Width / 2f, Height)
        : angle == (float)Math.PI ? TopLeft + new Vector2(0f, Height / 2f)
        : angle == 4.712389f ? TopLeft + new Vector2(Width / 2f, 0f)
        : TopLeft + new Vector2(Width / 2f, Height);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool PlayerFallCheck()
    {
        if (metaTriggered && !legacy) return true;
        return !flagOnly && (climbFall ? HasPlayerRider() : HasPlayerOnTop());
    }

    protected override bool PlayerFallCheckCore() => PlayerFallCheck();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool PlayerWaitCheck()
    {
        if (legacy ? metaTriggered : Triggered) return true;
        if (PlayerFallCheck()) return true;
        if (climbFall)
        {
            if (!CheckAt(typeof(RoomPlayerSession).FullName, Position - Vector2.UnitX))
                return CheckAt(typeof(RoomPlayerSession).FullName, Position + Vector2.UnitX);
            return true;
        }

        return false;
    }

    protected override bool PlayerWaitCheckCore() => PlayerWaitCheck();

    protected override void ShakeSfxCore() =>
        ((IOriginalGameplayProcess)engine).AudioSession.Play(shakeSfx, Center,
            ((IOriginalGameplayProcess)engine).SaveContext);

    protected override void ImpactSfxCore() =>
        ((IOriginalGameplayProcess)engine).AudioSession.Play(impactSfx, ImpactCenter,
            ((IOriginalGameplayProcess)engine).SaveContext);
}
