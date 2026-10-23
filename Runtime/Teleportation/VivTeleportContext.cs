// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics.Easing;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Mods.VivHelper.Persistence;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.VivHelper.Runtime.Teleportation
{
    public sealed class VivTeleportContext
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, VivTeleportContext> contexts = new();
        private SourceEverestModule owner;
        private VivHelperSession session = new();
        public ISourceEngineContext Engine { get; }
        public VivHelperSession Session => owner != null ? (VivHelperSession)owner._Session : session;
        public VivTeleportHelperEntity HelperEntity;
        public System.Numerics.Vector2? HackedFocusPoint;
        public Type[] UnloadTypes;

        private static readonly string[] manualStates =
        {
            "Normal", "Climb", "Dash", "Swim", "Boost", "RedDash", "HitSquash", "Launch", "Pickup", "DreamDash",
            "SummitLaunch", "Dummy", "IntroWalk", "IntroJump", "IntroRespawn", "IntroWakeUp", "BirdDashTutorial",
            "Frozen", "ReflectionFall", "StarFly", "TempleFall", "CassetteFly", "Attract", "IntroMoonJump", "FlingBird",
            "IntroThinkForABit"
        };

        private static readonly HashSet<int> emptyDashStates = new();
        private HashSet<int> spinnerDashStates = emptyDashStates;
        private HashSet<int> legacyDashStates = emptyDashStates;
        private VivTeleportContext(ISourceEngineContext engine) => Engine = engine;

        public static VivTeleportContext For(ISourceEngineContext engine) =>
            contexts.GetValue(engine, static e => new(e));

        public IDisposable Bind(SourceEverestModule module, HashSet<int> spinnerStates, HashSet<int> legacyStates)
        {
            owner = module;
            spinnerDashStates = spinnerStates;
            legacyDashStates = legacyStates;
            return new Binding(this, module);
        }

        private Dictionary<string, Func<float, float>> easers;

        public bool TryGetEaser(string name, out Func<float, float> easer)
        {
            easers ??= ((ISourceNamedEasingArithmetic)((ISourceEasingEnvironment)Engine).EasingArithmetic)
                .NamedEasers.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            return easers.TryGetValue(name, out easer);
        }

        public SceneEntityState GetHelperEntity(object scene)
            => scene == null
                ? null
                : ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntity<VivTeleportHelperEntity>();

        internal void LoadingThread(OriginalLevelSceneRoot level)
        {
            if (!ReferenceEquals(level.EngineContext, Engine)) return;
            if (UnloadTypes == null)
                UnloadTypes = new[]
                {
                    ResolveType("Celeste.FlingBird"), ResolveType("Celeste.Mod.JackalHelper.Entities.BraveBird"),
                    ResolveType("Celeste.Mod.JackalHelper.Entities.AltBraveBird")
                };
            if (!level.Scheduler.GetToAdd().Contains(HelperEntity) && !level.Scheduler.Entities.Contains(HelperEntity))
            {
                HelperEntity = new VivTeleportHelperEntity(Engine);
                var tags = ((IOriginalGameTagsEnvironment)Engine).Tags;
                HelperEntity.Tags = tags.FrozenUpdate.Value | tags.Persistent.Value | tags.PauseUpdate.Value |
                                    tags.Global.Value | tags.PauseUpdate.Value;
                level.Scheduler.QueueSourceEntity(HelperEntity);
            }
        }

        private Type ResolveType(string name)
        {
            var discovery = ((ISourceRuntimeTypeDiscoveryEnvironment)Engine).RuntimeTypeDiscovery;
            var type = discovery.ResolveRuntimeTypeAlias(name);
            if (type != null) return type;
            var catalog = ((ISourceSceneCatalogEnvironment)Engine).SceneCatalog;
            foreach (var registration in catalog.RuntimeRegistrations)
                if (catalog.Find(registration.SourceType)?.LogicalTypeName == name)
                    return registration.RequireActualType();
            return discovery.ResolveRuntimeTypeAlias(name) ?? discovery.GetTypes()
                .FirstOrDefault(t => t.RequireActualType().FullName == name)?.RequireActualType();
        }

        public bool MatchDashState(int state) => state == 2 || state == 5 || spinnerDashStates.Contains(state) ||
                                                 legacyDashStates.Contains(state);

        public void RemoveTeleportEntities(OriginalLevelSceneRoot level)
        {
            var types = UnloadTypes.ToList();
            var matches = new List<SceneEntityState>();
            foreach (var entity in level.Scheduler.Entities)
                if (types.Contains(entity.GetType()))
                    matches.Add((SceneEntityState)entity);
            foreach (var entity in matches) level.RemoveEntity(entity);
        }

        public bool DefineSetState(string input, out int output)
        {
            if (int.TryParse(input, out output))
            {
                if (output < 0) return false;
                if (output > 25)
                    throw new Celeste.Mods.VivHelper.Configuration.Errors.InvalidPropertyException(
                        "You tried to put in a custom state value over 25. I recommend retrieving the classname or using the default values provided in the dropdown.");
                return true;
            }

            output = Array.IndexOf(manualStates, input);
            if (output >= 0) return true;
            output = Array.IndexOf(manualStates, "St" + input);
            if (output >= 0) return true;
            var names = input.Split('.').ToList();
            if (names.Count < 2)
                throw new Celeste.Mods.VivHelper.Configuration.Errors.InvalidPropertyException(
                    "You input an invalid custom state.");
            string name = names.Last();
            names.RemoveAt(names.Count - 1);
            var type = ResolveType(string.Join(".", names));
            if (type == null)
                throw new Celeste.Mods.VivHelper.Configuration.Errors.InvalidPropertyException(
                    "The custom state class path was invalid!");
            var field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                throw new Celeste.Mods.VivHelper.Configuration.Errors.InvalidPropertyException(
                    "The custom state field name was invalid!");
            if (field.FieldType != typeof(int))
                throw new Celeste.Mods.VivHelper.Configuration.Errors.InvalidPropertyException(
                    "The custom state field was found but not shown to be a valid field (it is not an integer)");
            output = (int)field.GetValue(null);
            return true;
        }

        private sealed class Binding(VivTeleportContext context, SourceEverestModule module) : IDisposable
        {
            public void Dispose()
            {
                if (!ReferenceEquals(context.owner, module)) return;
                context.session = context.Session;
                context.owner = null;
            }
        }
    }
}
