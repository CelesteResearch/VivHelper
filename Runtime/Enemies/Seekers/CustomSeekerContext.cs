// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Generic;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Mods.VivHelper.Entities.Enemies.Seekers;

namespace Celeste.Mods.VivHelper.Runtime.Enemies.Seekers
{
    public sealed class CustomSeekerContext
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, CustomSeekerContext> owners = new();

        public static CustomSeekerContext For(ISourceEngineContext engine) =>
            owners.GetValue(engine, e => new CustomSeekerContext(e));

        private CustomSeekerContext(ISourceEngineContext engine)
        {
            PlayerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(RoomPlayerSession))
                .RegistryKey;
        }

        public readonly string PlayerRegistryKey;
        public List<CustomSeeker> Seekers;
        public readonly SeekerPatrolPoint[] PatrolChoices = new SeekerPatrolPoint[3];
    }
}
