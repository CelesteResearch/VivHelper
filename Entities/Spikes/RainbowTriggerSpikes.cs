// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Mods.VivHelper.Modules;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Simulation.Gameplay.Hazards.TriggerSpikes;
using Celeste.Simulation.Gameplay.Hazards.TriggerSpikes.Components;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Mods.VivHelper.Spawn;

namespace Celeste.Mods.VivHelper.Entities.Spikes
{
    [CustomEntity("VivHelper/RainbowTriggerSpikesUp=LoadUp", "VivHelper/RainbowTriggerSpikesDown=LoadDown",
        "VivHelper/RainbowTriggerSpikesLeft=LoadLeft", "VivHelper/RainbowTriggerSpikesRight=LoadRight")]
    [RuntimeTypeAlias("VivHelper.Entities.RainbowTriggerSpikes")]
    public partial class RainbowTriggerSpikes : CustomSpike
    {
        protected float Delay = 0.4f;
        protected float delayTimer = 0.05f;
        protected bool Triggered;
        private readonly int size;
        private readonly string overrideType;
        private readonly bool grouped;
        private readonly VivSpikeDependencyState dependencies;
        private readonly Vector2 outwards;
        private Vector2 shakeOffset;
        private SpikeInfo[] spikes;
        private List<AtlasEntry> spikeTextures;
        private List<object> players;
        private ISourceSpikeScene spikeScene;
        public PackedColor oneColor;
        protected bool Grouped => grouped && dependencies.MaxHelpingHandLoaded;

        public static RainbowTriggerSpikes LoadUp(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, SpikeDirection.Up, level.EngineContext);

        public static RainbowTriggerSpikes LoadDown(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new(data, offset, SpikeDirection.Down, level.EngineContext);

        public static RainbowTriggerSpikes LoadLeft(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new(data, offset, SpikeDirection.Left, level.EngineContext);

        public static RainbowTriggerSpikes LoadRight(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new(data, offset, SpikeDirection.Right, level.EngineContext);

        public RainbowTriggerSpikes(EntityData data, Vector2 offset, SpikeDirection direction,
            ISourceEngineContext engine)
            : base(data.Position + offset, direction >= SpikeDirection.Left ? data.Height : data.Width,
                direction, false, false, false, engine, false)
        {
            size = direction >= SpikeDirection.Left ? data.Height : data.Width;
            grouped = data.Bool("Grouped");
            dependencies = VivSpikeDependencies.For(engine);
            if (grouped && !dependencies.MaxHelpingHandLoaded)
                throw new Exception(
                    "Grouped Rainbow Trigger Spikes attempted to load without Maddie's Helping Hand as a dependency.");
            overrideType = data.Attr("type", "default");
            switch (direction)
            {
                case SpikeDirection.Up:
                    outwards = -Vector2.UnitY;
                    Collider = new HitboxColliderGeometry(size, 3f, 0f, -3f);
                    Add(new SafeGroundBlockerComponentState());
                    Add(new LedgeBlockerState(player => UpSafeBlockCheck((RoomPlayerSession)player)));
                    break;
                case SpikeDirection.Down:
                    outwards = Vector2.UnitY;
                    Collider = new HitboxColliderGeometry(size, 3f);
                    break;
                case SpikeDirection.Left:
                    outwards = -Vector2.UnitX;
                    Collider = new HitboxColliderGeometry(3f, size, -3f);
                    Add(new SafeGroundBlockerComponentState());
                    Add(new LedgeBlockerState(player => SideSafeBlockCheck((RoomPlayerSession)player)));
                    break;
                case SpikeDirection.Right:
                    outwards = Vector2.UnitX;
                    Collider = new HitboxColliderGeometry(3f, size);
                    Add(new SafeGroundBlockerComponentState());
                    Add(new LedgeBlockerState(player => SideSafeBlockCheck((RoomPlayerSession)player)));
                    break;
            }

            Add(new PlayerColliderComponentState(OnCollide));
            if (!data.Bool("DoNotAttach"))
                Add(new SourceStaticMoverState
                {
                    OnShake = amount => shakeOffset += amount, SolidChecker = IsRiding, JumpThruChecker = IsRiding
                });
            Depth = -50;
            string color = data.Attr("Color", "");
            oneColor = color == "" ? PackedColor.Transparent : SpawnPointRules.ParseColor(color);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            NoRefillDash = false;
            spikeScene = (ISourceSpikeScene)scene;
            // The registry owns this list throughout the scene; additions/removals mutate it in place.
            players = spikeScene.SourceScene.Entities.GetTrackedEntities(typeof(RoomPlayerSession));
            string type = spikeScene.SpikeType;
            if (!string.IsNullOrEmpty(overrideType) && overrideType != "default")
                type = overrideType is "cliffside" or "reflection" ? "whitereflection" : overrideType;
            string direction = Direction.ToString().ToLower();
            var atlas = ((ISourceGameGraphicsEnvironment)Engine).GameGraphics.GameAtlas;
            string prefix = "danger/spikes/" + type + "_" + direction;
            spikeTextures =
                AtlasLookup.Subtextures(atlas, atlas[prefix + "00"] != null ? prefix : type + "_" + direction);
            spikes = new SpikeInfo[size / 8];
            for (int i = 0; i < spikes.Length; i++)
            {
                spikes[i].Parent = this;
                spikes[i].Index = i;
                spikes[i].Position = Direction switch
                {
                    SpikeDirection.Up => Vector2.UnitX * ((float)i + 0.5f) * 8f + Vector2.UnitY,
                    SpikeDirection.Down => Vector2.UnitX * ((float)i + 0.5f) * 8f - Vector2.UnitY,
                    SpikeDirection.Left => Vector2.UnitY * ((float)i + 0.5f) * 8f + Vector2.UnitX,
                    _ => Vector2.UnitY * ((float)i + 0.5f) * 8f - Vector2.UnitX
                };
                spikes[i].color = PackedColor.White;
            }
        }

        public override void Update()
        {
            base.Update();
            for (int i = 0; i < spikes.Length; i++) spikes[i].Update();
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            Vector2 justify = Direction switch
            {
                SpikeDirection.Up => new Vector2(0.5f, 1f), SpikeDirection.Down => new Vector2(0.5f, 0f),
                SpikeDirection.Left => new Vector2(1f, 0.5f), SpikeDirection.Right => new Vector2(0f, 0.5f),
                _ => Vector2.One * 0.5f
            };
            for (int i = 0; i < spikes.Length; i++)
                SourceAtlasDrawing.DrawJustified(context, this, spikeTextures[spikes[i].TextureIndex],
                    Position + shakeOffset + spikes[i].Position + outwards * (-4f + spikes[i].Lerp * 4f),
                    justify, spikes[i].color);
        }

        private void GetPlayerCollideIndex(RoomPlayerSession player, out int minIndex, out int maxIndex)
            => TriggerSpikeCollision.GetPlayerCollideIndices(player, this, (TriggerSpikesDirection)Direction,
                8f, out minIndex, out maxIndex);

        protected override void OnCollide(RoomPlayerSession player)
        {
            GetPlayerCollideIndex(player, out int min, out int max);
            if (max >= 0 && min < spikes.Length)
            {
                min = Math.Max(min, 0);
                max = Math.Min(max, spikes.Length - 1);
                for (int i = min; i <= max && !spikes[i].OnPlayer(player, outwards); i++)
                {
                }
            }
        }

        private bool PlayerCheck(int index)
        {
            RoomPlayerSession player = null;
            for (int i = 0; i < players.Count; i++)
                if (SourceEntityCollision.Check(this, (ISourceEntityCollider)players[i]))
                {
                    player = (RoomPlayerSession)players[i];
                    break;
                }

            if (player == null) return false;
            GetPlayerCollideIndex(player, out int min, out int max);
            return min <= index + 1 && max >= index - 1;
        }

        private bool PlayerCheck()
        {
            bool result = false;
            for (int i = 0; i < spikes.Length; i++)
            {
                result |= spikes[i].PlayerCheck();
                if (result) break;
            }

            return result;
        }

        private bool UpSafeBlockCheck(RoomPlayerSession player)
        {
            int shift = 8 * player.Data.Facing;
            return SafeBlockRange((int)((player.Left + shift - Left) / 4f),
                (int)((player.Right + shift - Left) / 4f));
        }

        private bool SideSafeBlockCheck(RoomPlayerSession player)
            => SafeBlockRange((int)((player.Top - Top) / 4f), (int)((player.Bottom - Top) / 4f));

        private bool SafeBlockRange(int min, int max)
        {
            if (max < 0 || min >= spikes.Length) return false;
            min = Math.Max(min, 0);
            max = Math.Min(max, spikes.Length - 1);
            for (int i = min; i <= max; i++)
                if (spikes[i].Lerp >= 1f)
                    return true;
            return false;
        }

        private bool IsRiding(SourceSolidEntityState solid)
        {
            Vector2 step = Direction switch
            {
                SpikeDirection.Up => Vector2.UnitY, SpikeDirection.Down => -Vector2.UnitY,
                SpikeDirection.Left => Vector2.UnitX, SpikeDirection.Right => -Vector2.UnitX,
                _ => Vector2.Zero
            };
            return step != Vector2.Zero && !SourceEntityCollision.Check(this, solid)
                                        && SourceEntityCollision.CheckAt(this, solid, Position + step);
        }

        private bool IsRiding(ISourceJumpThruEntity jumpThru)
            => Direction == SpikeDirection.Up &&
               SourceEntityCollision.CheckAt(this, jumpThru, Position + Vector2.UnitY);

        private void PlaySound(string path, Vector2 at) => spikeScene.PlaySpikeSound(path, at);
    }
}
