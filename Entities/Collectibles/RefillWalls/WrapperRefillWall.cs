// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Mods.VivHelper.Runtime.RefillWalls;

namespace Celeste.Mods.VivHelper.Entities.Collectibles.RefillWalls
{
    [CustomEntity("VivHelper/RefillWallWrapper")]
    [RuntimeTypeAlias("VivHelper.Entities.WrapperRefillWall")]
    public partial class WrapperRefillWall : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly ISourceVectorArithmetic vectors;
        private readonly string typeName, spriteVarname, respawnMethodName;
        private readonly float respawnTime;
        private readonly int oneUse;
        private readonly PackedColor color1, color2;
        private readonly bool drawWithOutline;
        private SceneEntityState tiedEntity;
        private SpriteImage texture;
        private bool forceRender;
        private Action<RoomPlayerSession> refillUse;
        private RefillWallTargetBinding binding;
        private float respawnTimer;
        public bool sprite_or_image => texture == null;

        public WrapperRefillWall(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            Collider = new HitboxColliderGeometry(data.Width, data.Height);
            typeName = data.Attr("TypeName", "Refill");
            spriteVarname = data.Attr("ImageVariableName", "sprite").Trim();
            if (string.IsNullOrWhiteSpace(spriteVarname)) spriteVarname = "sprite";
            respawnMethodName = data.Attr("RespawnMethodName", "Respawn");
            if (string.IsNullOrWhiteSpace(respawnMethodName)) respawnMethodName = "Respawn";
            color1 = SpawnPointRules.ParseColor(data.Attr("InnerColor", "888888"));
            color2 = SpawnPointRules.ParseColor(data.Attr("OuterColor", "d0d0d0"));
            ScalarMath.Clamp(data.Float("Alpha", 1f), 0f, 1f);
            respawnTime = data.Float("RespawnTime", -1f);
            Depth = data.Int("Depth", 100);
            drawWithOutline = data.Bool("textureOutline", false);
            oneUse = -1;
            if (data.Has("oneUse"))
            {
                string value = data.Values["oneUse"].ToString();
                if (!int.TryParse(value, out oneUse))
                    oneUse = bool.TryParse(value, out bool parsed) ? parsed ? 1 : -1 : -1;
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var tracker = ((ISourceScene)scene).SourceScene.Components;
            var key = tracker.Catalog.Find(typeof(PlayerColliderComponentState)).RegistryKey;
            var colliders = tracker.GetTrackedComponents(key);
            var candidates = new List<PlayerColliderComponentState>();
            for (int i = 0; i < colliders.Count; i++)
            {
                var pc = (PlayerColliderComponentState)colliders[i];
                if (SourceEntityCollision.Check(this, (SceneEntityState)pc.Entity)) candidates.Add(pc);
            }

            foreach (var pc in candidates)
                if (CheckEntity((SceneEntityState)pc.Entity, pc))
                    return;
            RemoveSelf();
        }

        private bool CheckEntity(SceneEntityState entity, PlayerColliderComponentState pc)
        {
            Type actual = entity.GetType();
            Type requested = ((Celeste.Everest.Reflection.Runtime.ISourceRuntimeTypeDiscoveryEnvironment)engine)
                .RuntimeTypeDiscovery.ResolveRuntimeTypeAlias(typeName);
            var members = ((Celeste.Everest.Reflection.ISourceEverestReflectionEnvironment)engine).Reflection
                .SourceMembers;
            bool sourceNameMatches = members.ContainsSourceType(actual) &&
                                     members.GetOriginalTypeDefinition(actual).Identity == typeName;
            if (actual != requested && actual.FullName != typeName && !sourceNameMatches) return false;
            if (pc.OnCollide == null) return false;
            var candidate = RefillWallTargetBinding.Create(entity, respawnMethodName);
            object image = null;
            if (spriteVarname.StartsWith("$"))
            {
                string mode = spriteVarname.ToLowerInvariant();
                if (mode == "$render") forceRender = true;
                else image = mode == "$sprite" ? entity.Get<Sprite>() : RefillWallTargetBinding.FirstImage(entity);
            }
            else image = candidate.ReadImage(spriteVarname);

            if (!forceRender)
            {
                if (image == null) image = RefillWallTargetBinding.FirstImage(entity);
                texture = image switch
                {
                    Sprite sprite => new SpriteImage(sprite.Animations["idle"].Frames[0]),
                    SpriteImage value => value,
                    Celeste.Graphics.Content.Atlas.AtlasEntry atlas => new SpriteImage(atlas),
                    _ => null
                };
                if (texture == null) return false;
                texture.CenterOrigin();
                texture.RenderPosition = Center;
            }

            if (!candidate.HasTimer) return false;
            if (oneUse > -1) candidate.SetOneUse(oneUse == 1);
            binding = candidate;
            tiedEntity = entity;
            tiedEntity.Position = Center;
            tiedEntity.Collidable = tiedEntity.Visible = false;
            refillUse = pc.OnCollide;
            Add(new PlayerColliderComponentState(OnPlayer));
            return true;
        }

        public override void Update()
        {
            base.Update();
            if (respawnTimer > 0f)
            {
                respawnTimer -= engine.DeltaTime;
                if (respawnTimer <= 0f) Respawn();
            }
        }

        private void Respawn()
        {
            if (!Collidable) Collidable = true;
            tiedEntity.Collidable = false;
            binding.Respawn();
            tiedEntity.Collidable = false;
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            tiedEntity.Collidable = true;
            refillUse(player);
            if (tiedEntity.Collidable) tiedEntity.Collidable = false;
            else
            {
                Collidable = false;
                respawnTimer = respawnTime > 0f ? respawnTime : binding.Timer;
                binding.Timer = 0f;
            }

            if (binding.OneUse) RemoveSelf();
        }
    }
}
