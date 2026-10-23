// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Events.LevelLoading;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Runtime.RefillCancellation;
using Celeste.Scene.Following;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.Following;

public sealed class FollowerDistanceProcess : IDisposable
{
    private readonly RefillCancelContext context;
    private readonly Func<LeaderState, ISourceEngineContext> getEngine;
    private readonly Hook update;

    public FollowerDistanceProcess(RefillCancelContext context)
    {
        this.context = context;
        var leader = Expression.Parameter(typeof(LeaderState), "leader");
        getEngine = Expression.Lambda<Func<LeaderState, ISourceEngineContext>>(
                Expression.Field(leader,
                    typeof(LeaderState).GetField("engine", BindingFlags.Instance | BindingFlags.NonPublic)), leader)
            .Compile();
        update = new Hook(typeof(LeaderState).GetMethod(nameof(LeaderState.Update)),
            (Action<Action<LeaderState>, LeaderState>)Update);
        EverestLevelLoaderSourceEvents.OnLoadingThread += Loading;
    }

    private void Loading(OriginalLevelSceneRoot level)
    {
        if (!ReferenceEquals(level.EngineContext, context.Engine)) return;
        context.Session.FFDistance = context.Settings.FFDistance;
        context.Session.FPDistance = context.Settings.FPDistance;
        context.Session.MakeClose = context.Settings.MakeClose;
    }

    private float GetFPDistance() => context.Session.FPDistance < 1 ? .1f : context.Session.FPDistance / 10f;
    private int GetFFDistance() => context.Session.FFDistance < 1 ? 1 : context.Session.FFDistance;

    private void Update(Action<LeaderState> original, LeaderState leader)
    {
        if (!ReferenceEquals(getEngine(leader), context.Engine))
        {
            original(leader);
            return;
        }

        Vector2 point = ((ISourceEntityPosition)leader.Entity).Position + leader.Position;
        if (leader.PastPoints.Count == 0 ||
            ((ISourceVectorEnvironment)context.Engine).VectorArithmetic.Length(point - leader.PastPoints[0]) >=
            GetFPDistance())
        {
            leader.PastPoints.Insert(0, point);
            if (leader.PastPoints.Count > 350) leader.PastPoints.RemoveAt(leader.PastPoints.Count - 1);
        }

        int index = (int)GetFPDistance();
        foreach (var follower in leader.Followers)
        {
            if (index >= leader.PastPoints.Count) break;
            Vector2 target = leader.PastPoints[index];
            if (follower.DelayTimer <= 0f && follower.MoveTowardsLeader)
            {
                var actual = (ISourceMutableEntityState)follower.Entity;
                actual.Position = actual.Position + (target - actual.Position) *
                    (1f - (float)Math.Pow(0.009999999776482582,
                        context.Engine.DeltaTime / (!context.Settings.MakeClose ? 1f : index == 0 ? .05f : .25f)));
            }

            index += GetFFDistance();
        }
    }

    public void Dispose()
    {
        EverestLevelLoaderSourceEvents.OnLoadingThread -= Loading;
        update.Dispose();
    }
}
