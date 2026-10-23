using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Walls.Breakable;
using Celeste.Game.Execution.Engine.Actions;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.VivHelper.Entities.Walls.DashBlocks
{
    [CustomEntity("VivHelper/ExitDashBlock")]
    [RuntimeTypeAlias("VivHelper.Entities.ExitDashBlock")]
    public sealed class ExitDashBlock : SourceSolidEntityState
    {
        private char tileType;
        private readonly MapEntityId id;
        private readonly bool permanent;
        private readonly bool blendIn;
        private readonly bool canDash;
        private readonly RoomTransitionListener tl;
        private readonly EffectCutoutState cutout;
        private TileGrid tiles;
        private float startAlpha;

        public static ExitDashBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(data, offset, level.CreateEntityId(room, data), level.SolidWorldFactory);

        public ExitDashBlock(EntityData data, Vector2 offset, MapEntityId id, ISourceSolidWorldFactory worlds)
            : base(data.Position + offset, data.Width, data.Height, true, worlds.Create(data.Position + offset, true))
        {
            Depth = -13000;
            this.id = id;
            permanent = data.Bool("permanent", false);
            tileType = data.Char("tiletype", '3');
            tl = new RoomTransitionListener { OnOutBegin = OnTransitionOutBegin, OnInBegin = OnTransitionInBegin };
            OwnedComponents.Add(tl);
            cutout = new EffectCutoutState();
            OwnedComponents.Add(cutout);
            SurfaceSoundIndex = ((Celeste.Game.Audio.Surfaces.IOriginalTileSurfaceEnvironment)SourceWorld.EngineContext)
                .SurfaceIndices.TileToIndex[tileType];
            EnableAssistModeChecks = false;
            blendIn = data.Bool("blendin", false);
            canDash = data.Bool("canDash", false);
            OnDashCollide = OnDashed;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var autotiler = ((ISourceAutotilingResources)SourceWorld.EngineContext).ForegroundAutotiler;
            if (!blendIn)
            {
                tiles = autotiler.GenerateBox(tileType, (int)Width / 8, (int)Height / 8).TileGrid;
                OwnedComponents.Add(new LightOccludeState());
            }
            else
            {
                var level = (OriginalLevelSceneRoot)Scene;
                var bounds = ((OriginalChapterMapData)level.Session.MapData).TileBounds;
                tiles = autotiler.GenerateOverlay(tileType, (int)(X / 8f) - bounds.Left,
                    (int)(Y / 8f) - bounds.Top, (int)Width / 8, (int)Height / 8, level.SolidsData).TileGrid;
            }

            OwnedComponents.Add(tiles);
            OwnedComponents.Add(new TileInterceptorState(tiles, true));
            if (HasPlayer())
            {
                cutout.Alpha = tiles.Alpha = 0f;
                Collidable = false;
            }
        }

        public override void Update()
        {
            base.Update();
            if (Collidable)
                cutout.Alpha = tiles.Alpha = ScalarMath.Approach(tiles.Alpha, 1f, SourceWorld.EngineContext.DeltaTime,
                    ((ISourceExtremaEnvironment)SourceWorld.EngineContext).ExtremaArithmetic);
            else if (!HasPlayer())
            {
                Collidable = true;
                ((ISourceSpatialAudioEnvironment)SourceWorld.EngineContext).SpatialAudio.Play(
                    "event:/game/general/passage_closed_behind", Center);
            }
        }

        private bool HasPlayer() => SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities,
            typeof(RoomPlayerSession).FullName, this);

        private void OnTransitionOutBegin()
        {
            if (Collider.Collide(((OriginalLevelSceneRoot)Scene).Bounds))
            {
                tl.OnOut = OnTransitionOut;
                startAlpha = tiles.Alpha;
            }
        }

        private void OnTransitionOut(float percent)
        {
            cutout.Alpha = tiles.Alpha = ScalarMath.Lerp(startAlpha, 0f, percent,
                ((ISourceScalarEnvironment)SourceWorld.EngineContext).ScalarArithmetic);
            cutout.Update();
        }

        private void OnTransitionInBegin()
        {
            if (Collider.Collide(((OriginalLevelSceneRoot)Scene).PreviousBounds.Value) && !HasPlayer())
            {
                cutout.Alpha = tiles.Alpha = 0f;
                tl.OnIn = OnTransitionIn;
            }
        }

        private void OnTransitionIn(float percent)
        {
            cutout.Alpha = tiles.Alpha = percent;
            cutout.Update();
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            OriginalCelesteFreeze.Freeze(SourceWorld.EngineContext, .05f);
        }

        public void Break(Vector2 from, Vector2 direction, bool playSound = true, bool playDebrisSound = true)
        {
            if (playSound) BreakableTileBlockEffects.PlayBreakSound(SourceWorld, tileType, this);
            BreakableTileBlockEffects.EmitDebris(this, SourceWorld, ref tileType, playDebrisSound, from);
            Collidable = false;
            if (permanent) RemoveAndFlagAsGone();
            else RemoveSelf();
        }

        public void RemoveAndFlagAsGone()
        {
            RemoveSelf();
            ((OriginalLevelSceneRoot)Scene).Session.DoNotLoad.Add(id);
        }

        private DashCollisionResult OnDashed(RoomPlayerSession player, Vector2 direction)
        {
            if (tiles.Alpha != 1f) return DashCollisionResult.Ignore;
            if (!canDash && player.Machine.State != 5 && player.Machine.State != 10)
                return DashCollisionResult.NormalCollision;
            Break(player.Center, direction);
            return DashCollisionResult.Rebound;
        }
    }
}
