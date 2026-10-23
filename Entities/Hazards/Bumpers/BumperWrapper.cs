// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Reflection;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Gameplay.Hazards.Bumpers;
using Celeste.Graphics.Animation;
using Celeste.Mods.VivHelper.Runtime.Bumpers;
using Celeste.Mods.VivHelper.Runtime.Bumpers.Launching;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.VivHelper.Entities.Hazards.Bumpers
{
    [CustomEntity("VivHelper/BumperWrapper")]
    [RuntimeTypeAlias("VivHelper.Entities.BumperWrapper")]
    public sealed partial class BumperWrapper : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly BumperWrapperContext context;
        private readonly string typeName, anchorName;

        private readonly bool removeBloom,
            removeLight,
            removeWobble,
            attachToSolid,
            useNodes,
            normalStateOnEnd,
            legacyPositionLogic;

        private readonly float scale, newDashCooldown, respawnTime, setStamina;
        private readonly int setDashes;
        private readonly RoomCoreMode coreMode;
        private readonly Vector2[] nodes;
        private readonly BumperLaunchKind launchKind;
        public readonly float ExplodeMultiplier;
        public readonly BoostRestriction BumperBoost;
        private BumperTargetBinding binding;
        private Action<RoomPlayerSession> oldOnCollide;

        public BumperWrapper(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            context = BumperWrapperContext.For(engine);
            Collider = new HitboxColliderGeometry(4, 4, 2, 2);
            typeName = data.Attr("TypeName", "Celeste.Bumper");
            anchorName = data.Attr("AnchorName", "anchor");
            if (string.IsNullOrWhiteSpace(anchorName)) anchorName = "anchor";
            removeBloom = data.Bool("RemoveBloom");
            removeLight = data.Bool("RemoveLight");
            removeWobble = data.Bool("RemoveWobble");
            launchKind = data.Enum("BumperLaunchType", BumperLaunchKind.IgnoreAll);
            ExplodeMultiplier = data.Float("ExplodeStrengthMultiplier", 1);
            if (ExplodeMultiplier == 0) ExplodeMultiplier = 1;
            newDashCooldown = Math.Max(0, data.Float("DashCooldown", .2f));
            normalStateOnEnd = data.Bool("NormalStateOnEnd");
            setDashes = Math.Max(-2, data.Int("SetDashes", -1));
            setStamina = Math.Max(-2, data.Int("SetStamina", -1));
            BumperBoost = ReadBoost(data);
            coreMode = data.Enum("CoreMode", RoomCoreMode.None);
            respawnTime = data.Float("RespawnTime", -1);
            scale = (float)Math.Max(2, data.Int("Scale", 12)) / 12f;
            nodes = data.NodesWithPosition(offset);
            useNodes = data.Bool("useNodes");
            attachToSolid = data.Bool("attachToSolid");
            legacyPositionLogic = data.Bool("oldLaunchLogic", true);
        }

        private static BoostRestriction ReadBoost(EntityData data)
        {
            if (!data.Values.TryGetValue("BumperBoost", out var value)) return BoostRestriction.Default;
            if (value is int number)
                return Enum.IsDefined(typeof(BoostRestriction), number)
                    ? (BoostRestriction)number
                    : BoostRestriction.Default;
            return Enum.TryParse((string)value, true, out BoostRestriction boost) ? boost : BoostRestriction.Default;
        }

        public static SceneEntityState Load(global::Celeste.Game.Execution.Levels.OriginalLevelSceneRoot level,
            global::Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            global::Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data) =>
            new BumperWrapper(level.EngineContext, data, offset);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var components = ((ISourceScene)scene).SourceScene.Components;
            var key = components.Catalog.Find(typeof(PlayerColliderComponentState)).RegistryKey;
            var tracked = components.GetTrackedComponents(key);
            var candidates = new List<PlayerColliderComponentState>();
            for (int i = 0; i < tracked.Count; i++)
            {
                var pc = (PlayerColliderComponentState)tracked[i];
                if (SourceEntityCollision.Check(this, (SceneEntityState)pc.Entity)) candidates.Add(pc);
            }

            foreach (var pc in candidates)
                if (CheckEntity((SceneEntityState)pc.Entity, pc))
                    return;
            RemoveSelf();
        }

        private bool CheckEntity(SceneEntityState entity, PlayerColliderComponentState pc)
        {
            var actual = entity.GetType();
            var members = ((ISourceEverestReflectionEnvironment)engine).Reflection.SourceMembers;
            bool matches =
                ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery
                .ResolveRuntimeTypeAlias(typeName) == actual || actual.FullName == typeName ||
                members.ContainsSourceType(actual) && members.GetOriginalTypeDefinition(actual).Identity == typeName;
            if (!matches) return false;
            if (pc.OnCollide == null || (pc.Collider ?? entity.Collider) is not CircleColliderGeometry circle ||
                circle.Radius != 12) return false;
            var fields = actual;
            if (typeName == "Celeste.Mod.MaxHelpingHand.Entities.RotatingBumper" ||
                typeName == "Celeste.Mod.MaxHelpingHand.Entities.MultiNodeBumper")
                fields =
                    ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery.ResolveRuntimeTypeAlias(
                        "Celeste.Mod.MaxHelpingHand.Entities.BumperNotCoreMode") ??
                    throw new TypeLoadException("Celeste.Mod.MaxHelpingHand.Entities.BumperNotCoreMode");
            binding = new BumperTargetBinding(entity, fields, anchorName);
            var original = (CircleColliderGeometry)entity.Collider;
            var resized = new CircleColliderGeometry(((ISourceVectorEnvironment)engine).VectorArithmetic,
                2 * (int)(6f * scale), original.Position.X * scale, original.Position.Y * scale);
            pc.Collider = resized;
            entity.Collider = resized;
            oldOnCollide = pc.OnCollide;
            entity.Remove(entity.Get<PlayerColliderComponentState>());
            entity.Add(new PlayerColliderComponentState(player => ReplacementOnPlayer(player, entity)));
            entity.Depth = 0;
            var listener = entity.Get<CoreModeListenerState>();
            if (listener != null && (int)coreMode > 0)
            {
                listener.OnChange(coreMode);
                entity.Remove(listener);
            }

            foreach (var component in entity)
                if (component is Sprite sprite)
                    sprite.Scale = Vector2.One * scale;
            if (removeBloom) RemoveAll<BloomPointState>(entity);
            if (removeLight) RemoveAll<VertexLightState>(entity);
            var sine = entity.Get<SineWaveState>();
            if (removeWobble && sine != null)
            {
                sine.Frequency = 0;
                sine.Reset();
            }

            if (!useNodes && attachToSolid && nodes.Length > 1)
            {
                var mover = new SourceStaticMoverState();
                mover.SolidChecker = solid =>
                    SourceEntityCollision.CheckAt((SceneEntityState)mover.Entity, solid, nodes[1]);
                mover.OnMove = amount =>
                {
                    if (ReferenceEquals(entity, mover.Entity)) binding.Anchor += amount;
                };
                entity.Add(mover);
            }

            entity.Add(new BumperWrapperDebugRenderModifier(engine, context, launchKind));
            return true;
        }

        private static void RemoveAll<T>(SceneEntityState entity) where T : SourceComponentState
        {
            var removals = new List<SourceComponentState>();
            foreach (var c in entity)
                if (c is T)
                    removals.Add(c);
            entity.Remove(removals.ToArray());
        }
    }
}
