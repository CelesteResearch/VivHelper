using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Runtime.Events.Player;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Mods.VivHelper.Runtime.HoldableBarriers;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Gameplay.Player.Presentation.Sprites;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Modules
{
    public sealed partial class VivHelperModule : SourceEverestModule
    {
        private readonly List<IDisposable> hooks = [];
        private global::Celeste.Mods.VivHelper.Runtime.BadelineBoosts.BadelineBoostCustomHooks badelineBoostCustomHooks;
        private ConditionalWeakTable<OriginalLevelSceneRoot, RetryState> retryStates = new();

        public VivHelperModule(ISourceEngineContext engine) : base(engine)
        {
        }

        public override void Load()
        {
            hooks.Add(Runtime.Spikes.RainbowSpikeRidingBounds.For(SourceEngine));
            badelineBoostCustomHooks =
                new global::Celeste.Mods.VivHelper.Runtime.BadelineBoosts.BadelineBoostCustomHooks(SourceEngine);
            hooks.Add(badelineBoostCustomHooks);
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.Debris.DebrisLimiterHooks(SourceEngine, this));
            LoadCustomDashBlockHooks();
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.Blackout.BlackoutProcess(
                SourceEngine, () => Session.Blackout, value => Session.Blackout = value));
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.Backdrops.Rain.WindRainRegistration(SourceEngine));
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.Backdrops.Rain.CustomRainRegistration(SourceEngine));
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.Watchtowers.PlaybackWatchtowerProcess(SourceEngine));
            LoadHideRoomInMap();
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.Spawning.InterRoomSpawnProcess(
                SourceEngine, () => Session.LevelInfoCache));
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.Collectibles.PreviousBerriesToFlagProcess(
                SourceEngine));
            LoadFallingBlocks();
            LoadBumperWrappers();
            LoadRoomWrap();
            LoadInstantTeleport();
            var refillCancellation =
                global::Celeste.Mods.VivHelper.Runtime.RefillCancellation.RefillCancelContext.For(SourceEngine);
            hooks.Add(refillCancellation.Bind(this));
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.Following.FollowerDistanceProcess(refillCancellation));
            hooks.Add(
                new global::Celeste.Mods.VivHelper.Runtime.RefillCancellation.RefillCancelProcess(refillCancellation));
            LoadEntityMuters();
            LoadCustomSeekers();
            VivHelperSpinnerCapabilities.RegisterDebrisPool(SourceEngine);
            hooks.Add(new VivSpikeHooks(SourceEngine));
            hooks.Add(new HoldableBarrierProcess(SourceEngine));
            hooks.Add(new global::Celeste.Mods.VivHelper.Runtime.CornerBoosts.CornerBoostProcess(SourceEngine));
            hooks.Add(new ILHook(typeof(OriginalLevelRoomDataReader).GetMethod(nameof(OriginalLevelRoomDataReader.Read),
                [typeof(BinaryPackerElement), typeof(IOriginalLevelRoomDataExtensions)]), ModifyRoomReader));
            EverestPlayerSourceEvents.OnSpawn += PlayerSpawned;
            hooks.Add(new ILHook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.Reload)),
                ModifyReload));
            hooks.Add(new ILHook(
                typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.PauseOriginalGame)),
                ModifyPause));
        }

        public override void Initialize()
        {
            VivSpikeDependencies.Initialize(SourceEngine);
            badelineBoostCustomHooks.Initialize();
            global::Celeste.Mods.VivHelper.Runtime.Lighting.FloatyLightContacts.For(SourceEngine).Initialize();
        }

        public override void Unload()
        {
            global::Celeste.Mods.VivHelper.Runtime.Curves.VivCurveContext.Release(SourceEngine);
            UnloadFallingBlocks();
            for (var i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            badelineBoostCustomHooks = null;
            bumperWrappers = null;
            EverestPlayerSourceEvents.OnSpawn -= PlayerSpawned;
            retryStates = new();
        }

        private static void ModifyRoomReader(ILContext il)
        {
            // Only widen the first player test. Custom spawn entities remain in Entities for rendering and options.
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.Before, i => i.MatchLdstr("player"));
            cursor.Emit(OpCodes.Dup);
            cursor.GotoNext(MoveType.After, i => i.MatchCall<string>("op_Equality"));
            cursor.EmitDelegate<Func<string, bool, bool>>((name, vanilla) => vanilla || SpawnPointRules.IsSpawn(name));
        }

        private void PlayerSpawned(RoomPlayerSession player)
        {
            if (player.Scene is not OriginalLevelSceneRoot level
                || !ReferenceEquals(((ISourceEngineContextScene)level).EngineContext, SourceEngine)) return;
            foreach (var entity in ((OriginalLevelRoomData)level.Session.LevelData).Entities)
            {
                // The shipped 1.14.10 compares relative entity coordinates directly, without room offset.
                if ((entity.Name != "VivHelper/Spawnpoint" && entity.Name != "VivHelper/InterRoomSpawnTarget")
                    || entity.Position != player.Position) continue;
                if (entity.Bool("forceFacing", true)) player.Data.Facing = entity.Bool("flipX", false) ? -1 : 1;
                break;
            }
        }

        private void ModifyPause(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, i => i.MatchLdstr("menu_pause_retry"));
            cursor.GotoNext(MoveType.After, i => i.MatchNewobj<Action>());
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Action, OriginalLevelSceneRoot, Action>>((action, level) => () =>
            {
                if (ReferenceEquals(((ISourceEngineContextScene)level).EngineContext, SourceEngine))
                    retryStates.GetValue(level, static _ => new RetryState()).PausedRetry = true;
                action();
            });
        }

        private void ModifyReload(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.Before, i => i.MatchLdarg(0),
                i => i.MatchCallOrCallvirt<OriginalLevelSceneRoot>(nameof(OriginalLevelSceneRoot.UnloadLevel)));
            var before = cursor.Clone();
            cursor.GotoNext(MoveType.After,
                i => i.MatchCallOrCallvirt<OriginalLevelSceneRoot>(nameof(OriginalLevelSceneRoot.LoadLevel)));
            var after = cursor.MarkLabel();
            before.Emit(OpCodes.Ldarg_0);
            before.EmitDelegate<Func<OriginalLevelSceneRoot, bool>>(SkipReload);
            before.Emit(OpCodes.Brtrue, after);
        }

        private bool SkipReload(OriginalLevelSceneRoot level)
        {
            if (!ReferenceEquals(((ISourceEngineContextScene)level).EngineContext, SourceEngine)) return false;
            var state = retryStates.GetValue(level, static _ => new RetryState());
            var pausedRetry = state.PausedRetry;
            state.PausedRetry = false;
            if (!level.Session.RespawnPoint.HasValue) return false;
            var point = level.Session.RespawnPoint.Value;
            foreach (var entity in ((OriginalLevelRoomData)level.Session.LevelData).Entities)
            {
                if (entity.Name != "VivHelper/Spawnpoint" ||
                    entity.Position + ((OriginalLevelRoomData)level.Session.LevelData).Position != point)
                    continue;
                // The source stores Flag but does not apply it to NoResetRespawn.
                if (!SpawnPointRules.ShouldSkipReload(entity.Bool("NoResetRespawn", false),
                        entity.Bool("NoResetOnRetry", false), pausedRetry)) return false;
                var mode = level.Session.Inventory.Backpack
                    ? PlayerSpriteMode.Madeline
                    : PlayerSpriteMode.MadelineNoBackpack;
                var player = level.CreatePlayer(point, mode);
                player.IntroType = PlayerIntroType.Respawn;
                level.Scheduler.QueueSourceEntity(player);
                level.DoScreenWipe(true);
                return true;
            }

            return false;
        }

        private sealed class RetryState
        {
            public bool PausedRetry;
        }
    }
}
