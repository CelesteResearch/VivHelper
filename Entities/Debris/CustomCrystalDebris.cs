// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Effects.Debris;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Entities.Debris
{
    public sealed class CustomCrystalDebris : CrystalDebrisState
    {
        public CustomCrystalDebris(AtlasData atlas, ISourceEngineContext engine, IPlatformTargetMotionPolicy policy)
            : base(atlas, engine, policy)
        {
        }

        public override void Update()
        {
            Speed.X = ScalarMath.Clamp(Speed.X, -100000f, 100000f);
            Speed.Y = ScalarMath.Clamp(Speed.Y, -100000f, 100000f);
            base.Update();
        }

        public static void Burst(ISourceEngineContext engine, Vector2 position, PackedColor color, bool boss,
            int count, string imagePath, float scale)
        {
            var random = ((ISourceRandomEnvironment)engine).Random;
            for (int i = 0; i < count; i++)
            {
                var piece = engine.EntityPools.Create<CustomCrystalDebris>();
                Vector2 at = position + new Vector2(SourceRandomMath.Range(random, -4, 4),
                    SourceRandomMath.Range(random, -4, 4));
                var image = new SpriteImage(
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.Entries[imagePath]);
                piece.OwnedComponents.Remove(piece.Image);
                piece.Image = image;
                piece.OwnedComponents.Add(image);
                image.CenterOrigin();
                piece.Init(at, color, boss);
                image.Scale = Vector2.One * scale;
                ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene.QueueSourceEntity(piece);
            }
        }
    }
}
