// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Components.Solids;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Simulation.Gameplay.Player.Collision.Contacts;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Runtime.CornerBoosts
{
    public sealed class CornerBoostRuntime
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, CornerBoostRuntime> owners = new();

        private static readonly string[] legacyMapSIDs =
        {
            "MOCE/issy/MOCE_CPCL", "MOCE/issy/MOCE_LXVI", "Tardigrade/WaterbearMountain/WaterbearMountain",
            "Cabob/AmberTerminus/1-AmberTerminus", "Cabob/AmberTerminus/1-AmberTerminus-B",
            "AnarchyCollab2022/1-Submissions/02_ellatas", "AnarchyCollab2022/1-Submissions/22_pogseal",
            "AnarchyCollab2022/1-Submissions/ZZ_heartside", "WinterCollab2021/1-Maps/Jackal",
            "WinterCollab2021/1-Maps/Jackal_KAERRA"
        };

        internal readonly ISourceEngineContext Engine;
        internal SolidModifierComponent CurrentActive;
        internal bool Initialized;
        private CornerBoostRuntime(ISourceEngineContext engine) => Engine = engine;

        public static CornerBoostRuntime For(ISourceEngineContext engine)
            => owners.GetValue(engine, e => new CornerBoostRuntime(e));

        internal static List<SourceComponentState> Modifiers(RoomPlayerSession player)
        {
            var tracker = ((ISourceScene)player.Scene).SourceScene.Components;
            var definition = tracker.Catalog.Find(typeof(SolidModifierComponent));
            return definition == null ? null : tracker.GetTrackedComponents(definition.RegistryKey);
        }

        public bool ModifyWallJumpCheck(bool previous, PlayerContacts contacts, int direction, Vector2 offset)
        {
            if (contacts.ActualOwner is not RoomPlayerSession player ||
                !ReferenceEquals(player.EngineContext, Engine)) return previous;
            var modifiers = Modifiers(player);
            if (modifiers == null || modifiers.Count == 0) return previous;
            var level = (OriginalLevelSceneRoot)player.Scene;
            var keys = ((IOriginalAreaKeyEnvironment)Engine).AreaKeys;
            string sid = keys.GetSID(keys.GetSessionArea(level.Session));
            if (Array.IndexOf(legacyMapSIDs, sid) >= 0)
            {
                if (previous) return true;
                bool hit = false;
                Vector2 position = player.Position;
                player.Position = position + offset;
                foreach (var component in modifiers)
                    if (component.Entity is SourceSolidEntityState solid)
                    {
                        hit |= LegacyCollideCheck(solid, player, ((SolidModifierComponent)component).CornerBoostBlock);
                        if (hit) break;
                    }

                player.Position = position;
                return hit;
            }

            return WallJumpCollision(modifiers, player, direction, false) || previous;
        }

        public bool WallJumpCollision(List<SourceComponentState> modifiers, RoomPlayerSession player, int dir,
            bool includeOriginal)
        {
            int reach = Math.Max(Initialized ? GetWallJumpReach(player, dir) : 3,
                (int)Math.Ceiling(Math.Abs(player.Speed.X) * Engine.DeltaTime) + 1);
            int first = includeOriginal ? 4 : 0;
            if (reach < first) return false;
            foreach (SolidModifierComponent modifier in modifiers)
            {
                if (modifier.Entity is not SourceSolidEntityState solid) continue;
                int max = modifier.CornerBoostBlock > 0 ? modifier.CornerBoostBlock : reach;
                for (int i = first; i <= max; i++)
                    if (player.ClimbBoundsCheck(dir) && !ClimbBlockerState.EdgeCheck(player.Scene, player, dir * i) &&
                        SourceEntityCollision.CheckAt(player, solid, player.Position + Vector2.UnitX * (dir * i)))
                    {
                        CurrentActive = modifier;
                        return true;
                    }
            }

            return false;
        }

        private bool LegacyCollideCheck(SourceSolidEntityState solid, RoomPlayerSession player, int count)
        {
            bool hit = false;
            int max = count < 0 ? Math.Max(3, (int)Math.Ceiling(Math.Abs(player.Speed.X) * Engine.DeltaTime)) : 3;
            for (int i = 0; i <= max; i++)
            {
                hit |= count > 1
                    ? SourceEntityCollision.CheckAt(solid, player,
                        solid.Position - Vector2.UnitX * (player.Data.Facing * i))
                    : SourceEntityCollision.CheckAt(player, solid,
                        player.Position + Vector2.UnitX * (player.Data.Facing * i));
                if (hit) break;
            }

            return hit;
        }

        private static int GetWallJumpReach(RoomPlayerSession player, int dir)
        {
            bool extended = player.DashAttacking && player.Data.Direction.X == 0f && player.Data.Direction.Y == -1f;
            if (extended)
                foreach (SpikesState spike in ((ISourceScene)player.Scene).SourceScene.Entities.GetTrackedEntities(
                             "Celeste.Spikes"))
                    if (spike.Direction == (dir <= 0 ? SpikeDirection.Right : SpikeDirection.Left) &&
                        SourceEntityCollision.CheckAt(player, spike, player.Position + Vector2.UnitX * dir * 5f))
                    {
                        extended = false;
                        break;
                    }

            return extended ? 5 : 3;
        }
    }
}
