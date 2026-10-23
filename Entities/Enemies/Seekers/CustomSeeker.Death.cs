// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Core.Components;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation.Presentation.Player.Death;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        protected virtual void SeekerDeath()
        {
            var entity = new SceneEntityState(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                Position);
            var effect = new DeathEffect(DeathEffectColor, engine, Center - Position)
                { OnEnd = () => entity.RemoveSelf() };
            entity.OwnedComponents.Add(effect);
            entity.Depth = -1000000;
            ((ISourceScene)Scene).SourceScene.QueueSourceEntity(entity);
            engine.SpatialAudio.Play("event:/game/05_mirror_temple/seeker_death", Position);
            if (FlagOnDeath != "") ((OriginalLevelSceneRoot)Scene).Session.SetFlag(FlagOnDeath, true);
            RemoveSelf();
            dead = true;
        }
    }
}
