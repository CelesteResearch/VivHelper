// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Triggers.Teleportation
{
    [RuntimeTypeAlias("VivHelper.Triggers.TriggerPersistence")]
    public enum TriggerPersistence
    {
        Default,
        OncePerRetry,
        OncePerMapPlay
    }
}
