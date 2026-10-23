using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        protected virtual int StunnedUpdate()
        {
            Speed = Approach(Speed, Vector2.Zero, 150f * engine.DeltaTime);
            return 4;
        }

        protected virtual IEnumerator StunnedCoroutine()
        {
            if (finalDash && numberOfDashes == maxNumberOfDashes)
            {
                Add(new SimulationCoroutine(engine, seekerDeath(0.15f)));
            }

            yield return 0.8f;
            State.State = 0;
        }

        protected virtual void SkiddingBegin()
        {
            engine.SpatialAudio.Play("event:/game/05_mirror_temple/seeker_dash_turn", this.Position);
            strongSkid = false;
            TurnFacing(-facing);
        }

        protected virtual int SkiddingUpdate()
        {
            Speed = Approach(Speed, Vector2.Zero,
                (strongSkid ? StrongSkiddingAccel : SkiddingAccel) * engine.DeltaTime);
            if (engine.VectorArithmetic.LengthSquared(Speed) < 400f)
            {
                if (canSeePlayer)
                {
                    return 2;
                }

                return 0;
            }

            return 5;
        }

        protected virtual IEnumerator SkiddingCoroutine()
        {
            yield return StrongSkiddingTime;
            strongSkid = true;
        }

        protected virtual void SkiddingEnd()
        {
            spriteFacing = facing;
        }
    }
}
