// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.VivHelper.Runtime.Teleportation
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.HelperEntity")]
    public class VivTeleportHelperEntity : SceneEntityState
    {
        public VivTeleportHelperEntity(ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
        }
    }
}
