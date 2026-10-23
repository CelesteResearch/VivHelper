// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Entities.HoldableBarriers;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Holdables;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.HoldableBarriers
{
    public sealed class HoldableBarrierHooks : IDisposable
    {
        private delegate bool MoveOrig(SourceActorEntityState actor, float amount, SourceCollisionHandler collision,
            ISourceEntityCollider pusher);

        private delegate bool MoveHook(MoveOrig orig, SourceActorEntityState actor, float amount,
            SourceCollisionHandler collision, ISourceEntityCollider pusher);

        private delegate bool GroundOrig(SourceActorEntityState actor, int down);

        private delegate bool GroundHook(GroundOrig orig, SourceActorEntityState actor, int down);

        private delegate void ReleaseOrig(HoldableState holdable, Vector2 force);

        private delegate void ReleaseHook(ReleaseOrig orig, HoldableState holdable, Vector2 force);

        private readonly ISourceEngineContext engine;
        private readonly HoldableBarrierContext context;
        private readonly List<IDisposable> hooks = [];

        public HoldableBarrierHooks(ISourceEngineContext engine, HoldableBarrierContext context)
        {
            this.engine = engine;
            this.context = context;
            hooks.Add(new Hook(typeof(SourceActorEntityState).GetMethod(nameof(SourceActorEntityState.MoveH),
                [typeof(float), typeof(SourceCollisionHandler), typeof(ISourceEntityCollider)]), (MoveHook)MoveH));
            hooks.Add(new Hook(typeof(SourceActorEntityState).GetMethod(nameof(SourceActorEntityState.MoveV),
                [typeof(float), typeof(SourceCollisionHandler), typeof(ISourceEntityCollider)]), (MoveHook)MoveV));
            hooks.Add(new Hook(typeof(SourceActorEntityState).GetMethod(nameof(SourceActorEntityState.OnGround),
                [typeof(int)]), (GroundHook)OnGround));
            hooks.Add(new Hook(typeof(HoldableState).GetMethod(nameof(HoldableState.Release)), (ReleaseHook)Release));
        }

        private bool Owns(object scene) =>
            scene is ISourceEngineContextScene owner && ReferenceEquals(owner.EngineContext, engine);

        private bool MoveH(MoveOrig orig, SourceActorEntityState actor, float amount, SourceCollisionHandler collision,
            ISourceEntityCollider pusher)
        {
            var hold = actor.Get<HoldableState>();
            if (!Owns(actor.Scene) || actor is RoomPlayerSession || hold == null || hold.IsHeld)
                return orig(actor, amount, collision, pusher);
            var barriers = ((ISourceScene)actor.Scene).SourceScene.Entities.GetTrackedEntities(typeof(HoldableBarrier));
            SetCollidable(barriers, true);
            try
            {
                return orig(actor, amount, collision, pusher);
            }
            finally
            {
                SetCollidable(barriers, false);
            }
        }

        private static bool TryGroups(object scene, out List<object> barriers, out List<object> jumpThrus)
        {
            var registry = ((ISourceScene)scene).SourceScene.Entities;
            jumpThrus = null;
            return registry.TryGetTrackedEntities(typeof(HoldableBarrier), out barriers)
                   && registry.TryGetTrackedEntities(typeof(HoldableBarrierJumpThru), out jumpThrus);
        }

        private bool MoveV(MoveOrig orig, SourceActorEntityState actor, float amount, SourceCollisionHandler collision,
            ISourceEntityCollider pusher)
        {
            var hold = actor.Get<HoldableState>();
            if (!Owns(actor.Scene) || actor is RoomPlayerSession || hold == null || hold.IsHeld
                || !TryGroups(actor.Scene, out var barriers, out var jumpThrus))
                return orig(actor, amount, collision, pusher);
            SetCollidable(barriers, true);
            SetCollidable(jumpThrus, true);
            try
            {
                return orig(actor, amount, collision, pusher);
            }
            finally
            {
                SetCollidable(barriers, false);
                SetCollidable(jumpThrus, false);
            }
        }

        private bool OnGround(GroundOrig orig, SourceActorEntityState actor, int down)
        {
            var hold = actor.Get<HoldableState>();
            if (!Owns(actor.Scene) || hold == null || hold.IsHeld
                || !TryGroups(actor.Scene, out var barriers, out var jumpThrus)) return orig(actor, down);
            SetCollidable(barriers, true);
            SetCollidable(jumpThrus, true);
            try
            {
                return orig(actor, down);
            }
            finally
            {
                SetCollidable(barriers, false);
                SetCollidable(jumpThrus, false);
            }
        }

        private void Release(ReleaseOrig orig, HoldableState hold, Vector2 force)
        {
            if (!Owns(hold.Scene) || !TryGroups(hold.Scene, out var barriers, out var jumpThrus)
                                  || barriers.Count + jumpThrus.Count == 0)
            {
                orig(hold, force);
                return;
            }

            SetCollidable(barriers, true);
            SetCollidable(jumpThrus, true);
            try
            {
                bool solid = context.Controller(hold.Scene)?.SolidOnRelease ??
                             context.Saved((OriginalLevelSceneRoot)hold.Scene).SolidOnRelease;
                if (!solid && hold.Entity is SceneEntityState entity)
                {
                    HoldableBarrier colliding = null;
                    foreach (HoldableBarrier barrier in barriers)
                        if (SourceEntityCollision.Check(barrier, entity))
                        {
                            colliding = barrier;
                            break;
                        }

                    if (colliding != null)
                        MoveHQuickCollision(entity, colliding, 3 * Math.Sign(colliding.CenterX - entity.CenterX));
                    SetCollidable(barriers, false);
                    SetCollidable(jumpThrus, false);
                }

                orig(hold, force);
            }
            finally
            {
                SetCollidable(barriers, false);
                SetCollidable(jumpThrus, false);
            }
        }

        internal static int MoveHQuickCollision(SceneEntityState entity, HoldableBarrier barrier, int value)
        {
            for (int i = 1; i <= Math.Abs(value); i++)
            {
                int amount = i * Math.Sign(value);
                var delta = new Vector2(amount, 0f);
                if (!SourceEntityCollision.CheckAt(entity, barrier, entity.Position + delta))
                {
                    entity.Position += delta;
                    return amount;
                }

                if (!SourceEntityCollision.CheckAt(entity, barrier, entity.Position - delta))
                {
                    entity.Position -= delta;
                    return amount;
                }
            }

            return 0;
        }

        internal static void SetCollidable(List<object> entities, bool value)
        {
            foreach (SceneEntityState entity in entities) entity.Collidable = value;
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
