// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Hazards.Lightning;

namespace Celeste.Mods.VivHelper.Runtime.Lighting
{
    public sealed class FloatyLightContacts
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, FloatyLightContacts> Contexts = new();
        private readonly ISourceEngineContext engine;
        private Type[] collidingTypes = { typeof(LightningState) };

        private FloatyLightContacts(ISourceEngineContext engine) => this.engine = engine;

        public static FloatyLightContacts For(ISourceEngineContext engine)
            => Contexts.GetValue(engine, static owner => new FloatyLightContacts(owner));

        public void Initialize()
        {
            var discovery = ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery;
            Type jackal = discovery.ResolveRuntimeTypeAlias("Celeste.Mod.JackalCollabHelper.Entities.DarkMatter");
            Type strawberryJam = discovery.ResolveRuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DarkMatter");
            int count = 1 + (jackal != null ? 1 : 0) + (strawberryJam != null ? 1 : 0);
            var types = new Type[count];
            types[0] = typeof(LightningState);
            int index = 1;
            if (jackal != null) types[index++] = jackal;
            if (strawberryJam != null) types[index] = strawberryJam;
            collidingTypes = types;
        }

        public bool Check(SceneEntityRegistry entities, Vector2 position, Vector2 tip, bool broken)
        {
            for (int i = 0; i < collidingTypes.Length; i++)
            {
                if (!entities.TryGetTrackedEntities(collidingTypes[i], out var candidates)) continue;
                for (int j = 0; j < candidates.Count; j++)
                {
                    var entity = (ISourceEntityCollider)candidates[j];
                    if (SourceEntityGeometryCollision.Point(entity, position + tip)
                        || (!broken && SourceEntityGeometryCollision.Point(entity, position - tip))) return true;
                }
            }

            return false;
        }
    }
}
