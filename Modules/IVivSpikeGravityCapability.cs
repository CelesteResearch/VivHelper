// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Components;
using Celeste.Scene.Entities;

namespace Celeste.Mods.VivHelper.Modules
{
    public interface IVivSpikeGravityCapability
    {
        bool IsPlayerInverted();
        SourceComponentState CreatePlayerGravityListener(Action<SceneEntityState, int, float> callback);
    }
}
