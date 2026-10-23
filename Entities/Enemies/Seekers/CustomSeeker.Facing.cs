// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        protected virtual void TurnFacing(float dir, string gotoSprite = null)
        {
            if (dir != 0f)
            {
                facing = Math.Sign(dir);
            }

            if (spriteFacing != facing)
            {
                if (State.State == 5)
                {
                    sprite.Play("skid", false, false);
                }
                else if (State.State == 3 || State.State == 2)
                {
                    sprite.Play("flipMouth", false, false);
                }
                else
                {
                    sprite.Play("flipEyes", false, false);
                }

                nextSprite = gotoSprite;
            }
            else if (gotoSprite != null)
            {
                sprite.Play(gotoSprite, false, false);
            }
        }

        protected virtual void SnapFacing(float dir)
        {
            if (dir != 0f)
            {
                spriteFacing = (facing = Math.Sign(dir));
            }
        }
    }
}
