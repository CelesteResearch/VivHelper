using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Events;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Presentation.Environment;
using Celeste.Mods.VivHelper.Entities.RefillCancellation;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Runtime.Session;
using Celeste.Simulation.Presentation.Player.Death;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.RefillCancellation
{
    public sealed class RefillCancelProcess : IDisposable
    {
        private readonly RefillCancelContext context;
        private readonly EverestGameEventsProcess events;
        private readonly List<IDisposable> hooks = new();
        private readonly List<RefillCancelSpace> updateSpaces = new();

        private PlayerRefillIndicator indicator
        {
            get => context.Indicator;
            set => context.Indicator = value;
        }

        public RefillCancelProcess(RefillCancelContext context)
        {
            this.context = context;
            events = ((IEverestGameEventsEnvironment)context.Engine).GameEvents;
            try
            {
                using (new DetourConfigContext(new DetourConfig("VivHelper.RefillCancel", after: new[] { "*" })).Use())
                {
                    hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.UseRefill)),
                        (Func<Func<RoomPlayerSession, bool, bool>, RoomPlayerSession, bool, bool>)UseRefill));
                    hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Update)),
                        (Action<Action<RoomPlayerSession>, RoomPlayerSession>)Update));
                    hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Die)),
                        (Func<Func<RoomPlayerSession, Vector2, bool, bool, PlayerDeathPresentation>, RoomPlayerSession,
                            Vector2, bool, bool, PlayerDeathPresentation>)Die));
                }

                events.OnExit += OnExit;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool Owns(RoomPlayerSession player) => ReferenceEquals(player.EngineContext, context.Engine);

        private void OnExit(OriginalLevelSceneRoot level, LevelExitSequence exit, LevelExitMode mode,
            ChapterCompletionSession session, HiresSnowState snow)
            => context.InSpace = context.DashRefillRestrict =
                context.DashRestrict = context.StaminaRefillRestrict = false;

        private static bool TrySpaces(RoomPlayerSession player, out List<object> spaces)
        {
            spaces = null;
            return player.Scene is ISourceScene scene &&
                   scene.SourceScene.Entities.TryGetTrackedEntities(typeof(RefillCancelSpace), out spaces);
        }

        private bool UseRefill(Func<RoomPlayerSession, bool, bool> orig, RoomPlayerSession player, bool twoDashes)
        {
            if (!Owns(player) || !TrySpaces(player, out var spaces)) return orig(player, twoDashes);
            bool dash = false, stamina = false;
            foreach (RefillCancelSpace space in spaces)
            {
                if (!SourceEntityCollision.Check(player, space)) continue;
                dash |= space.DashRefillRestrict;
                stamina |= space.StaminaRefillRestrict;
                if (dash && stamina) break;
            }

            if (dash && stamina) return false;
            if (!dash) return orig(player, twoDashes);
            if (player.Data.Stamina >= 20f) return false;
            player.RefillStamina();
            return true;
        }

        private void CapDashes(RoomPlayerSession player)
        {
            if (player.Data.Dashes > context.Session.DashCount) player.Data.Dashes = context.Session.DashCount;
            else if (player.Data.Dashes < context.Session.DashCount) context.Session.DashCount = player.Data.Dashes;
        }

        private void Update(Action<RoomPlayerSession> orig, RoomPlayerSession player)
        {
            if (!Owns(player) || !TrySpaces(player, out var spaces))
            {
                orig(player);
                return;
            }

            updateSpaces.Clear();
            foreach (RefillCancelSpace space in spaces)
                if (SourceEntityCollision.Check(player, space))
                    updateSpaces.Add(space);
            if (context.InSpace)
            {
                if (indicator == null || indicator.Scene == null)
                    ((ISourceScene)player.Scene).SourceScene.QueueSourceEntity(indicator =
                        new PlayerRefillIndicator(context));
                if (context.DashRefillRestrict) CapDashes(player);
            }

            if (updateSpaces.Count == 0 && context.InSpace)
            {
                context.InSpace = false;
                if (indicator != null)
                {
                    ((ISourceScene)player.Scene).SourceScene.RemoveEntity(indicator);
                    indicator = null;
                }

                player.World.NoRefills = false;
            }
            else if (updateSpaces.Count > 0 && !context.InSpace)
            {
                context.InSpace = true;
                context.Session.DashCount = player.Data.Dashes;
                context.Session.StaminaCount = player.Data.Stamina;
            }

            orig(player);
            if (!context.InSpace) return;
            context.DashRestrict = context.DashRefillRestrict = context.StaminaRefillRestrict = false;
            foreach (var space in updateSpaces)
            {
                context.DashRestrict |= space.DashRestrict;
                context.DashRefillRestrict |= space.DashRefillRestrict;
                context.StaminaRefillRestrict |= space.StaminaRefillRestrict;
                if (context.DashRestrict && context.DashRefillRestrict && context.StaminaRefillRestrict) break;
            }

            if (context.DashRestrict) player.Data.CooldownTimer = context.Engine.DeltaTime + .02f;
            if (context.DashRefillRestrict)
            {
                if (!player.World.NoRefills) player.World.NoRefills = true;
                CapDashes(player);
            }
            else
            {
                context.Session.DashCount = player.Data.Dashes;
                if (player.World.NoRefills) player.World.NoRefills = false;
            }

            if (context.StaminaRefillRestrict && player.Data.Stamina > context.Session.StaminaCount)
                player.Data.Stamina = context.Session.StaminaCount;
            else context.Session.StaminaCount = player.Data.Stamina;
        }

        private PlayerDeathPresentation Die(Func<RoomPlayerSession, Vector2, bool, bool, PlayerDeathPresentation> orig,
            RoomPlayerSession player, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            var result = orig(player, direction, evenIfInvincible, registerDeathInStats);
            if (Owns(player) && result != null)
            {
                context.Session.StaminaCount =
                    player.Data.Stamina = Math.Max(player.Data.Stamina, context.Session.StaminaCount);
                if (indicator != null && indicator.Scene != null) indicator.RemoveSelf();
            }

            return result;
        }

        public void Dispose()
        {
            events.OnExit -= OnExit;
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
