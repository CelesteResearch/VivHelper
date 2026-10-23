// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Mods.VivHelper.Components.Solids;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Entities.Solids
{
    [SceneTracked(true)]
    [EverestTrackedAs(typeof(CornerBoostSolid))]
    [RuntimeTypeAlias("VivHelper.Entities.CornerBoostSolid")]
    public class CornerBoostSolid : SourceSolidEntityState
    {
        internal readonly ISourceEngineContext Engine;

        public CornerBoostSolid(Vector2 position, float width, float height, bool safe, bool perfectCB,
            ISourceSolidWorld world) : base(position, width, height, safe, world)
        {
            Engine = world.EngineContext;
            Add(new SolidModifierComponent(perfectCB ? 2 : 1, false, false));
        }
    }
}
