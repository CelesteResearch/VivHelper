// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Game.Entities.Lookouts;
using Celeste.Mods.VivHelper.Entities.Playback;
using Celeste.Game.Execution.Levels;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Display;
using Celeste.Core.Components;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components.Animation;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Lookout;
using Celeste.Simulation.Gameplay.Player.Presentation.Sprites;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Graphics.Animation;
using Celeste.Game.Components.Talking;

namespace Celeste.Mods.VivHelper.Entities.Watchtowers
{
    [CustomEntity("VivHelper/CustomPlaybackWatchtower")]
    [RuntimeTypeAlias("VivHelper.Entities.CustomPlaybackWatchtower")]
    public partial class CustomPlaybackWatchtower : SceneEntityState, ISourceLookoutInteraction,
        ILookoutInteractionState
    {
        private PlatinumWatchtowerHud hud;
        protected OriginalTalkComponent talk;
        private Sprite sprite;
        private SourceTweenState lightTween;
        private bool interacting;
        private bool onlyY;
        private List<Vector2> nodes;
        private int node;
        private float nodePercent;
        private bool summit;
        private string animPrefix = "";
        private readonly IOriginalLookoutProcess process;
        private readonly Celeste.Mods.VivHelper.Modules.VivHelperModule module;
        private readonly string playbackKey;
        public string tag;
        public bool ignoreBind;
        private readonly bool setOnAwake;
        protected bool toggle, prev, instantStart;
        private bool awaitingRelease;
        private CustomPlayerPlayback playback;
        private readonly float acceleration;
        private readonly float maximumSpeed;
        public bool invertFlag;
        public string flagWhenInHud;

        public static SceneEntityState Load(OriginalLevelSceneRoot level,
            global::Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            global::Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data)
            => new CustomPlaybackWatchtower(data, offset, (IOriginalLookoutProcess)level.EngineContext);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            playback = null;
            foreach (CustomPlayerPlayback entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         playbackKey))
                if (entity.customID == tag)
                {
                    playback = entity;
                    playback.active = false;
                    break;
                }

            toggle = prev = false;
            if (flagWhenInHud != "" && setOnAwake)
                ((OriginalLevelSceneRoot)scene).Session.SetFlag(flagWhenInHud, invertFlag);
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            if (!interacting) return;
            RoomPlayerSession player = ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
            if (player == null) return;
            player.Machine.State = 0;
            player.Sprite.Visible = true;
            player.Hair.Visible = true;
        }

        private global::System.Collections.IEnumerator LookRoutine(RoomPlayerSession player)
            => LookoutInteractionRoutine.Run(this, player);

        public CustomPlaybackWatchtower(EntityData data, Vector2 offset, IOriginalLookoutProcess process)
            : base(((ISourceEntitySourceDataEnvironment)process.EngineContext).EntitySourceDataRegistration,
                data.Position + offset)
        {
            this.process = process;
            module = ((Celeste.Everest.Modules.Runtime.ISourceEverestModulesEnvironment)process.EngineContext).Modules
                .OfType<Celeste.Mods.VivHelper.Modules.VivHelperModule>().First();
            playbackKey = ((ISourceSceneCatalogEnvironment)process.EngineContext).SceneCatalog
                .Find(typeof(CustomPlayerPlayback)).RegistryKey;
            tag = data.Attr("CustomPlaybackTag", "");
            ignoreBind = data.Bool("IgnoreBind", false);
            setOnAwake = data.Bool("SetOnAwake", true);
            instantStart = data.Bool("InstantStart", false);
            acceleration = EntityAttributeReader.Float(data.Values, "Accel", 800f);
            maximumSpeed = EntityAttributeReader.Float(data.Values, "maxSpeed", 240f);
            string flag = EntityAttributeReader.Attr(data.Values, "FlagWhileInHud");
            invertFlag = !string.IsNullOrEmpty(flag) && flag[0] == '!';
            flagWhenInHud = invertFlag ? flag.Substring(1) : flag ?? "";

            Depth = -8500;
            OwnedComponents.Add(talk = new OriginalTalkComponent(process, new IntRect(-24, -8, 48, 8),
                new Vector2(-0.5f, -20f), Interact));
            talk.PlayerMustBeFacing = true;
            summit = EntityAttributeReader.Bool(data.Values, "summit");
            onlyY = EntityAttributeReader.Bool(data.Values, "onlyY");
            Collider = new HitboxColliderGeometry(4f, 4f, -2f, -4f);
            var light = new VertexLightState(process.EngineContext, new Vector2(-1f, -11f),
                global::Celeste.Core.Display.PackedColor.White, 0.8f, 16, 24);
            OwnedComponents.Add(light);
            lightTween = light.CreatePulseTween(process.EngineContext);
            OwnedComponents.Add(lightTween);
            OwnedComponents.Add(sprite = process.SpriteBank.Create(
                string.IsNullOrEmpty(EntityAttributeReader.Attr(data.Values, "XMLName"))
                    ? "lookout"
                    : EntityAttributeReader.Attr(data.Values, "XMLName")));
            sprite.OnFrameChange = animation =>
            {
                switch (animation)
                {
                    case "idle":
                    case "badeline_idle":
                    case "nobackpack_idle":
                        if (sprite.CurrentAnimationFrame == sprite.CurrentAnimationTotalFrames - 1)
                            lightTween.Start();
                        break;
                }
            };
            Vector2[] points = data.NodesOffset(offset);
            if (points != null && points.Length != 0) nodes = new List<Vector2>(points);
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            if (interacting)
            {
                RoomPlayerSession player = ((ISourceScene)scene).SourceScene.Entities
                    .GetTrackedEntity<RoomPlayerSession>();
                if (player != null) player.Machine.State = 0;
            }
        }

        private void Interact(RoomPlayerSession player)
        {
            if (player.DefaultSpriteMode == PlayerSpriteMode.MadelineAsBadeline ||
                process.CurrentSave.Assists.PlayAsBadeline)
                animPrefix = "badeline_";
            else if (player.DefaultSpriteMode == PlayerSpriteMode.MadelineNoBackpack) animPrefix = "nobackpack_";
            else animPrefix = "";
            awaitingRelease = false;
            var coroutine = new SimulationCoroutine(process.EngineContext, LookRoutine(player));
            coroutine.RemoveOnComplete = true;
            OwnedComponents.Add(coroutine);
            interacting = true;
        }

        public void StopInteracting()
        {
            interacting = false;
            sprite.Play(animPrefix + "idle");
        }

        public override void Update()
        {
            if (talk.UI != null) talk.UI.Visible = !CollideCheck("Celeste.Solid");
            UpdateComponentsAndSprite();
        }

        protected void UpdateComponentsAndSprite()
        {
            base.Update();
            RoomPlayerSession player = ((ISourceScene)Scene).SourceScene.Entities
                .GetTrackedEntity<RoomPlayerSession>();
            if (player != null)
            {
                sprite.Active = interacting || player.Machine.State != 11;
                if (!sprite.Active) sprite.SetAnimationFrame(0);
            }
        }

        SceneEntityState ILookoutInteractionState.Entity => this;
        IOriginalLookoutProcess ILookoutInteractionState.Process => process;

        ILookoutHudState ILookoutInteractionState.Hud
        {
            get => hud;
            set => hud = (PlatinumWatchtowerHud)value;
        }

        Sprite ILookoutInteractionState.Sprite => sprite;

        bool ILookoutInteractionState.Interacting
        {
            get => interacting;
            set => interacting = value;
        }

        bool ILookoutInteractionState.OnlyY => onlyY;
        List<Vector2> ILookoutInteractionState.Nodes => nodes;

        int ILookoutInteractionState.Node
        {
            get => node;
            set => node = value;
        }

        float ILookoutInteractionState.NodePercent
        {
            get => nodePercent;
            set => nodePercent = value;
        }

        bool ILookoutInteractionState.Summit => summit;
        string ILookoutInteractionState.AnimationPrefix => animPrefix;
        float ILookoutInteractionState.Acceleration => acceleration;
        float ILookoutInteractionState.MaximumSpeed => maximumSpeed;

        ILookoutHudState ILookoutInteractionState.CreateHud() => new PlatinumWatchtowerHud(process)
        {
            TrackMode = nodes != null, OnlyY = onlyY
        };

        internal void OnOpened()
        {
            if (!string.IsNullOrEmpty(flagWhenInHud))
                ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flagWhenInHud, !invertFlag);
        }

        void ILookoutInteractionState.OnViewStarted()
        {
            if (playback != null && (ignoreBind || instantStart))
            {
                playback.Restart();
                playback.active = true;
            }
        }

        void ILookoutInteractionState.OnViewEnded()
        {
            if (!string.IsNullOrEmpty(flagWhenInHud))
                ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flagWhenInHud, invertFlag);
        }

        internal bool TryBeginViewStep()
        {
            if (playback != null && !ignoreBind)
            {
                if (!awaitingRelease)
                {
                    if (module.Settings.DisplayPlaybacksInLookout.Pressed) toggle = !toggle;
                    awaitingRelease = toggle != prev;
                }

                if (awaitingRelease)
                {
                    if (!module.Settings.DisplayPlaybacksInLookout.Released) return false;
                    if (toggle)
                    {
                        playback.Restart();
                        playback.active = true;
                    }
                    else
                    {
                        playback.active = false;
                        playback.Visible = false;
                    }

                    awaitingRelease = false;
                }
            }

            prev = toggle;
            return true;
        }

        internal void OnViewingEnded()
        {
            if (playback != null)
            {
                playback.active = false;
                playback.Visible = false;
            }
        }

        internal void OnClosed()
        {
            if (playback != null) playback.Visible = false;
        }
    }
}
