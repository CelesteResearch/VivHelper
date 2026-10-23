// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using Celeste.Core.Scheduling;
using System.Linq;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Riding;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Maps;
using Celeste.Simulation;
using Celeste.Everest.Content.Maps.Processing;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        public static List<object> GetList(ISourceEngineContext engine) =>
            (engine.Frame.Scenes.CurrentScene as OriginalLevelSceneRoot).SourceScene.Entities.GetTrackedEntities(
                typeof(CustomSeeker));

        private RoomPlayerSession FirstPlayer() =>
            ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(context.PlayerRegistryKey);

        public override void Added(object scene)
        {
            base.Added(scene);
            colliderRegistryKey = ((ISourceScene)scene).SourceScene.Components.Catalog
                .Find(typeof(CustomSeekerCollider)).RegistryKey;
            var level = (OriginalLevelSceneRoot)Scene;
            random = engine.RandomProcess.Factory.CreateSeeded(
                ((OriginalLevelRoomData)level.Session.LevelData).LoadSeed);
            var meta = ((IEverestChapterMapProcessingEnvironment)engine).MapProcessing.GetModeMeta(
                (OriginalChapterMapData)level.Session.MapData);
            if (meta?.SeekerSlowdown == true &&
                ((ISourceScene)Scene).SourceScene.Entities.Entities.Count(entity =>
                    entity is CustomSeekerEffectsController) == 0)
                ((ISourceScene)Scene).SourceScene.QueueSourceEntity(new CustomSeekerEffectsController(engine));
            CustomSeekersList.Add(this);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var player = FirstPlayer();
            if (player == null || X == player.X) SnapFacing(1f);
            else SnapFacing(Math.Sign(player.X - X));
            if (FlagOnDeath != "") ((OriginalLevelSceneRoot)scene).Session.SetFlag(FlagOnDeath, false);
        }

        public override bool IsRiding(ISourceEntityCollider solid) => false;
        public override bool IsRiding(ISourceJumpThruCollider jumpthru) => false;

        public override void Removed(object scene)
        {
            CustomSeekersList.Remove(this);
            base.Removed(scene);
        }
    }
}
