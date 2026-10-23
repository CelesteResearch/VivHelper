// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    [RuntimeTypeAlias("VivHelper.Entities.CustomSeeker+RecoverBlast")]
    public class CustomSeekerRecoverBlast : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private Sprite sprite;
        private string CustomShockwavePath;

        public CustomSeekerRecoverBlast(ISourceEngineContext engine) : base(((ISourceEntitySourceDataEnvironment)engine)
            .EntitySourceDataRegistration) => this.engine = engine;

        public override void Added(object scene)
        {
            base.Added(scene);
            Depth = -199;
            if (sprite == null)
                sprite = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.SpriteFactory
                    .Create(CustomShockwavePath);
            Add(sprite);
            sprite.OnLastFrame = frame => RemoveSelf();
            sprite.Play("shockwave", restart: true);
        }

        public static void Spawn(ISourceEngineContext engine, Vector2 position, string path)
        {
            var effect = engine.EntityPools.Create<CustomSeekerRecoverBlast>();
            effect.CustomShockwavePath = path != null && path != "" ? path : "seekerShockWave";
            effect.Position = position;
            ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene.QueueSourceEntity(effect);
        }
    }
}
