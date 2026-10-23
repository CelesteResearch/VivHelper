// SPDX-License-Identifier: AGPL-3.0-only

using System.Collections;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.FallingBlocks;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.VivHelper.Entities.Solids.FallingBlocks
{
    [CustomEntity("VivHelper/CornerBoostFallingBlock")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.CornerBoostFallingBlock")]
    public partial class CornerBoostFallingBlock : CornerBoostSolid
    {
        private static readonly string PlayerKey = typeof(RoomPlayerSession).FullName;
        private readonly ISourceEngineContext engine;
        private readonly char TileType;
        private readonly TileGrid tiles;
        private readonly TileGrid highlight;
        private readonly bool finalBoss;
        private readonly int climbFall;
        public bool Triggered;
        public float FallDelay;
        public bool HasStartedFalling { get; private set; }
        private OriginalLevelSceneRoot Level => (OriginalLevelSceneRoot)Scene;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new CornerBoostFallingBlock(data.Position + offset,
            data.Char("tiletype", '3'), data.Width, data.Height, false, data.Bool("behind", false),
            data.Bool("bufferClimbFall", false) ? 2 : data.Bool("climbFall", true) ? 1 : 0,
            data.Bool("PerfectCornerBoost", false), level.SolidWorldFactory.Create(data.Position + offset, false));

        public CornerBoostFallingBlock(Vector2 position, char tile, int width, int height, bool finalBoss,
            bool behind, int climbFall, bool perfectCB, ISourceSolidWorld world)
            : base(position, width, height, false, perfectCB, world)
        {
            engine = world.EngineContext;
            this.finalBoss = finalBoss;
            this.climbFall = climbFall;
            var random = ((ISourceRandomProcessEnvironment)engine).RandomProcess;
            var seed = ((ISourceRandomEnvironment)engine).Random.Next();
            var autotiler = ((ISourceAutotilingResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                .ForegroundAutotiler;
            random.PushRandom(seed);
            Add(tiles = autotiler.GenerateBox(tile, width / 8, height / 8).TileGrid);
            random.PopRandom();
            if (finalBoss)
            {
                random.PushRandom(seed);
                Add(highlight = autotiler.GenerateBox('G', width / 8, height / 8).TileGrid);
                random.PopRandom();
                highlight.Alpha = 0f;
            }

            Add(new SimulationCoroutine(engine, Sequence()));
            Add(new LightOccludeState(1f));
            Add(new TileInterceptorState(tiles, false));
            TileType = tile;
            SurfaceSoundIndex = ((IOriginalTileSurfaceEnvironment)engine).SurfaceIndices.TileToIndex[tile];
            if (behind) Depth = 5000;
        }

        public static OriginalFallingBlockState CreateFinalBossBlock(OriginalLevelSceneRoot level,
            OriginalLevelEntityData data, Vector2 offset) => new(data.Position + offset, 'g', data.Width, data.Height,
            true, false, false, level.SolidWorldFactory.Create(data.Position + offset, false), level.EngineContext);

        public override void OnShake(Vector2 amount)
        {
            base.OnShake(amount);
            tiles.Position += amount;
            if (highlight != null) highlight.Position += amount;
        }

        public override void OnStaticMoverTrigger(SourceStaticMoverState mover)
        {
            if (!finalBoss) Triggered = true;
        }

        private bool CheckAt(string key, Vector2 position) =>
            SourceTrackedEntityCollision.CheckAt(((ISourceScene)Scene).SourceScene.Entities, key, this, position);

        private bool PlayerFallCheck()
        {
            switch (climbFall)
            {
                case 2:
                    var scene = engine.Frame.Scenes.CurrentScene as ISourceScene;
                    var player = scene?.SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(PlayerKey);
                    return player != null && !player.Dead && (HasPlayerRider() || player.Input.Jump.Pressed &&
                        (player.World.WallJumpCheck(1) || player.World.WallJumpCheck(-1)));
                case 1: return HasPlayerRider();
                case 0: return HasPlayerOnTop();
                default: return HasPlayerRider();
            }
        }

        private bool PlayerWaitCheck()
        {
            if (Triggered || PlayerFallCheck()) return true;
            return climbFall == 1 && (CheckAt(PlayerKey, Position - Vector2.UnitX) ||
                                      CheckAt(PlayerKey, Position + Vector2.UnitX));
        }

        private IEnumerator HighlightFade(float to)
        {
            var from = highlight.Alpha;
            for (var p = 0f; p < 1f; p += engine.Frame.DeltaTime / .5f)
            {
                highlight.Alpha = ((ISourceScalarEnvironment)engine).ScalarArithmetic.Lerp(from, to,
                    ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeInOut(p));
                tiles.Alpha = 1f - highlight.Alpha;
                yield return null;
            }

            highlight.Alpha = to;
            tiles.Alpha = 1f - to;
        }
    }
}
