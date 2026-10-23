// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Physics.Collision;
using Celeste.Graphics.Content;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using Celeste.Graphics.Animation;
using Celeste.Mods.VivHelper.Modules;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.VivHelper.Entities.Spikes
{
    [CustomEntity("VivHelper/RainbowSpikesUp=LoadUp", "VivHelper/RainbowSpikesDown=LoadDown",
        "VivHelper/RainbowSpikesLeft=LoadLeft", "VivHelper/RainbowSpikesRight=LoadRight")]
    [RuntimeTypeAlias("VivHelper.Entities.RainbowSpikes")]
    public class RainbowSpikes : CustomSpike
    {
        public const string TentacleType = "tentacles";
        public PackedColor OneColor;
        private float timer;
        private readonly int size;
        private readonly string overrideType;
        private Vector2 imageOffset;
        private readonly Runtime.Spikes.RainbowSpikeRidingBounds ridingBounds;
        public PackedColor EnabledColor = PackedColor.White;
        public PackedColor DisabledColor = PackedColor.White;
        public bool VisibleWhenDisabled;

        public static RainbowSpikes LoadUp(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new RainbowSpikes(data, offset, SpikeDirection.Up, level.EngineContext);

        public static RainbowSpikes LoadDown(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new RainbowSpikes(data, offset, SpikeDirection.Down, level.EngineContext);

        public static RainbowSpikes LoadLeft(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new RainbowSpikes(data, offset, SpikeDirection.Left, level.EngineContext);

        public static RainbowSpikes LoadRight(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new RainbowSpikes(data, offset, SpikeDirection.Right, level.EngineContext);

        public RainbowSpikes(EntityData data, Vector2 offset, SpikeDirection direction, ISourceEngineContext engine)
            : base(data.Position + offset, direction >= SpikeDirection.Left ? data.Height : data.Width,
                direction, data.Bool("OverrideWallBounce"), data.Bool("groundRefill"),
                data.Bool("KillFromAnyDirection"), engine)
        {
            ridingBounds = Runtime.Spikes.RainbowSpikeRidingBounds.For(engine);
            Depth = -1;
            size = direction >= SpikeDirection.Left ? data.Height : data.Width;
            overrideType = data.Attr("type", "default");
            Add(new Celeste.Simulation.Gameplay.Player.Collision.Components.PlayerColliderComponentState(OnCollide));
            if (!data.Bool("DoNotAttach"))
                Add(new SourceStaticMoverState
                {
                    OnShake = amount => imageOffset += amount, SolidChecker = IsRiding,
                    JumpThruChecker = _ => false, OnEnable = OnEnable, OnDisable = OnDisable
                });
            Tags |= OriginalSceneTags.TransitionUpdate;
            string color = data.Attr("Color", "");
            OneColor = color == "" ? PackedColor.Transparent : SpawnPointRules.ParseColor(color);
        }

        private string ResolveSpikeType(string areaType)
        {
            string type = !string.IsNullOrEmpty(overrideType) && overrideType != "default" ? overrideType : areaType;
            return type == TentacleType ? "default" : type;
        }

        private string GetSpikeTexturePrefix(string type)
        {
            switch (type)
            {
                case "default": return "danger/spikes/default";
                case "outline": return "danger/spikes/outline";
                case "cliffside":
                case "reflection":
                case "whitereflection": return "danger/spikes/whitereflection";
                default: return type;
            }
        }

        private void OnImageAdded(SpriteImage image)
        {
            // Installed 1.14.10 recolors all images after each image is added, before the next RNG choice.
            try
            {
                SetSpikeColor();
            }
            catch
            {
            }
        }

        public void SetSpikeColor()
        {
            if (Runtime.Spikes.RainbowSpikeComponentEnumeration.CanUseValueEnumerator)
            {
                using (var cursor = Components.GetValueEnumerator())
                    while (cursor.MoveNext())
                        if (cursor.Current is SpriteImage image)
                            image.Color = OneColor != PackedColor.Transparent
                                ? OneColor
                                : GetRainbowHue(Position + image.Position);
                return;
            }

            foreach (var component in Components)
                if (component is SpriteImage image)
                    image.Color = OneColor != PackedColor.Transparent
                        ? OneColor
                        : GetRainbowHue(Position + image.Position);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            timer = Engine.DeltaTime;
        }

        public override void Update()
        {
            base.Update();
            if (Scene == null) return;
            if (timer > 0f) timer -= Engine.DeltaTime;
            else if (OneColor == PackedColor.Transparent) SetSpikeColor();
        }

        private void OnEnable()
        {
            Active = Visible = Collidable = true;
            SetSpikeColor();
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            var spikeScene = (ISourceSpikeScene)scene;
            SpikeDecorationBuilder.Add(Engine, this, size, Direction, ResolveSpikeType(spikeScene.SpikeType),
                GetSpikeTexturePrefix, spikeScene.SpikeRandom, OnImageAdded);
        }

        private void OnDisable()
        {
            Active = Collidable = false;
            if (VisibleWhenDisabled)
            {
                foreach (var component in Components)
                    if (component is SpriteImage image)
                        image.Color = DisabledColor;
                return;
            }

            Visible = false;
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 previous = Position;
            Position += imageOffset;
            base.Render(context);
            Position = previous;
        }

        public void SetOrigins(Vector2 origin)
        {
            foreach (var component in Components)
                if (component is SpriteImage image)
                {
                    Vector2 relative = origin - Position;
                    image.Origin = image.Origin + relative - image.Position;
                    image.Position = relative;
                }
        }

        private bool IsRiding(SourceSolidEntityState solid)
        {
            if (ridingBounds.TryReject(this, solid)) return false;
            Vector2 step = Direction switch
            {
                SpikeDirection.Up => Vector2.UnitY, SpikeDirection.Down => -Vector2.UnitY,
                SpikeDirection.Left => Vector2.UnitX, SpikeDirection.Right => -Vector2.UnitX,
                _ => Vector2.Zero
            };
            return step != Vector2.Zero && !SourceEntityCollision.Check(this, solid)
                                        && SourceEntityCollision.CheckAt(this, solid, Position + step);
        }
    }
}
