// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Game.Entities.Lookouts;
using Celeste.Game.Execution.Levels;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Display;
using Celeste.Core.Components;
using System;
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
    [CustomEntity("VivHelper/PlatinumWatchtower")]
    [RuntimeTypeAlias("VivHelper.Entities.PlatinumWatchtower")]
    public partial class PlatinumWatchtower : SceneEntityState, ISourceLookoutInteraction, ILookoutInteractionState
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
        private readonly float acceleration;
        private readonly float maximumSpeed;
        private readonly bool invertFlag;
        private readonly string flagWhenInHud;

        public static SceneEntityState Load(OriginalLevelSceneRoot level,
            global::Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            global::Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data)
            => new PlatinumWatchtower(data, offset, (IOriginalLookoutProcess)level.EngineContext);

        public override void Awake(object scene)
        {
            ((OriginalLevelSceneRoot)scene).Session.SetFlag(flagWhenInHud, invertFlag);
            base.Awake(scene);
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            if (!interacting) return;
            RoomPlayerSession player = ((ISourceScene)scene).SourceScene.Entities.FindFirst<RoomPlayerSession>();
            if (player == null) return;
            player.Machine.State = 0;
            player.Sprite.Visible = true;
            player.Hair.Visible = true;
        }

        private global::System.Collections.IEnumerator LookRoutine(RoomPlayerSession player)
            => LookoutInteractionRoutine.Run(this, player);

        public PlatinumWatchtower(EntityData data, Vector2 offset, IOriginalLookoutProcess process)
            : base(((ISourceEntitySourceDataEnvironment)process.EngineContext).EntitySourceDataRegistration,
                data.Position + offset)
        {
            this.process = process;
            if (!string.IsNullOrEmpty(EntityAttributeReader.Attr(data.Values, "Hint1DialogID")) ||
                !string.IsNullOrEmpty(EntityAttributeReader.Attr(data.Values, "Hint1Tag")))
            {
                throw new NotSupportedException(
                    "PlatinumWatchtower hints require Viv CustomPlayerPlayback and dialog capabilities; no SJ watchtower uses hints.");
            }

            acceleration = EntityAttributeReader.Float(data.Values, "Accel", 800f);
            maximumSpeed = EntityAttributeReader.Float(data.Values, "maxSpeed", 240f);
            string flag = EntityAttributeReader.Attr(data.Values, "FlagWhileInHud");
            invertFlag = !string.IsNullOrEmpty(flag) && flag[0] == '!';
            flagWhenInHud = invertFlag ? flag.Substring(1) : flag ?? "";

            Depth = -8500;
            OwnedComponents.Add(talk = new OriginalTalkComponent(process, new IntRect(-24, -8, 48, 8),
                new Vector2(-0.5f, -20f), Interact));
            talk.PlayerMustBeFacing = false;
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
                    .FindFirst<RoomPlayerSession>();
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
                .FindFirst<RoomPlayerSession>();
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
            TrackMode = nodes != null, OnlyY = onlyY,
            hints = new List<global::Celeste.Graphics.Content.Atlas.AtlasEntry>()
        };

        void ILookoutInteractionState.OnViewStarted()
        {
            if (!string.IsNullOrEmpty(flagWhenInHud))
                ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flagWhenInHud, !invertFlag);
        }

        void ILookoutInteractionState.OnViewEnded()
        {
            if (!string.IsNullOrEmpty(flagWhenInHud))
                ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flagWhenInHud, invertFlag);
        }
    }
}
