// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Mods.VivHelper.Runtime.HoldableBarriers;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Entities.HoldableBarriers
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.HoldableBarrierJumpThru")]
    [CustomEntity("VivHelper/HoldableBarrierJumpThru")]
    public class HoldableBarrierJumpThru : SourceJumpThruEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly HoldableBarrierContext context;
        private readonly int columns;
        public HoldableBarrierColorController ColorController;
        public PackedColor InnerColor;
        public PackedColor OuterColor;

        public static HoldableBarrierJumpThru Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.SolidWorldFactory);

        public HoldableBarrierJumpThru(EntityData data, Vector2 offset, ISourceSolidWorldFactory worlds)
            : this(data.Position + offset, data.Width, worlds.CreateJumpThru(data.Position + offset, false))
        {
        }

        private HoldableBarrierJumpThru(Vector2 position, int width, ISourceSolidWorld world)
            : base(position, width, false, world)
        {
            engine = world.EngineContext;
            context = HoldableBarrierContext.For(engine);
            SurfaceSoundIndex = 32;
            columns = (int)Width / 8;
            Visible = true;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            ColorController = context.Controller(scene);
            Collidable = true;
            InnerColor = (ColorController?.ParticleColor ??
                          SpawnPointRules.ParseColor(context.Saved((OriginalLevelSceneRoot)scene).ParticleColorHex)) *
                         .5f;
            OuterColor = (ColorController?.BaseColor ??
                          SpawnPointRules.ParseColor(context.Saved((OriginalLevelSceneRoot)scene).BaseColorHex)) * .5f;
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            var inner = atlas["VivHelper/holdableJumpThru/00"];
            var outer = atlas["VivHelper/holdableJumpThru/01"];
            int count = inner.FrameWidth / 8;
            var barriers = ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(typeof(HoldableBarrier));
            HoldableBarrierHooks.SetCollidable(barriers, true);
            try
            {
                var random = ((ISourceRandomEnvironment)engine).Random;
                for (int column = 0; column < columns; column++)
                {
                    int x;
                    int y;
                    if (column == 0)
                    {
                        x = 0;
                        y = SolidAt(Position - Vector2.UnitX) ? 0 : 1;
                    }
                    else if (column == columns - 1)
                    {
                        x = count - 1;
                        y = SolidAt(Position + Vector2.UnitX) ? 0 : 1;
                    }
                    else
                    {
                        x = 1 + random.Next(count - 2);
                        y = RandomSelection.Choose(random, 0, 1);
                    }

                    Add(new SpriteImage(inner.GetSubtexture(x * 8, y * 8, 8, 8))
                        { X = column * 8, Color = InnerColor });
                    Add(new SpriteImage(outer.GetSubtexture(x * 8, y * 8, 8, 8))
                        { X = column * 8, Color = OuterColor });
                }
            }
            finally
            {
                HoldableBarrierHooks.SetCollidable(barriers, false);
                Collidable = false;
            }
        }

        private bool SolidAt(Vector2 at)
        {
            var registry = ((ISourceScene)Scene).SourceScene.Entities;
            return SourceTrackedEntityCollision.CheckAt(registry, "Celeste.Solid", this, at)
                   || SourceTrackedEntityCollision.CheckAt(registry, "Celeste.SwapBlock", this, at)
                   || SourceTrackedEntityCollision.CheckAt(registry, "Celeste.ExitBlock", this, at);
        }
    }
}
