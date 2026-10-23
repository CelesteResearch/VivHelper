// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Scheduling;
using Celeste.Core.Components;
using Celeste.Game.Execution.Startup;
using Celeste.Scene.Entities;
using Celeste.Simulation;

namespace Celeste.Mods.VivHelper.Entities.Hazards.Bumpers
{
    public sealed class BetterBumperBoost : SceneEntityState
    {
        private readonly RoomPlayerSession player;
        private readonly ISourceEngineContext engine;

        public BetterBumperBoost(RoomPlayerSession player)
            : base(((ISourceEntitySourceDataEnvironment)player.EngineContext).EntitySourceDataRegistration)
        {
            this.player = player;
            engine = player.EngineContext;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.FrozenUpdate.Value;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            player.Freeze(.05f);
        }

        public override void Update()
        {
            base.Update();
            // The original flag is local to Update, so each frozen frame can apply the boost.
            if (engine.Effects.FreezeTimer > 0f)
            {
                if (player.Input.MoveX.Value == Math.Sign(player.Speed.X))
                {
                    player.Data.ExplodeLaunchBoostTimer = 0;
                    player.Data.Motion.Normal.Speed.X *= 1.2f;
                }
            }
            else
            {
                player.Freeze(.05f);
                RemoveSelf();
            }
        }
    }
}
