// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Game.Gameplay.Effects.Slashes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Display.Colors;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Oscillation;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Mods.VivHelper.Runtime.RefillCancellation;

namespace Celeste.Mods.VivHelper.Entities.Collectibles.Refills;

public partial class CustomRefill
{
    private void OnPlayer(RoomPlayerSession player)
    {
        if (GetFlags(Scene as OriginalLevelSceneRoot, flagToggle.Split(',')) && AnalyzeRefill(player))
        {
            engine.SpatialAudio.Play(CustomAudio[0], Center);
            engine.RumbleSession.Rumble(1, 1);
            Collidable = false;
            Add(new SimulationCoroutine(engine, RefillRoutine(player), true));
        }
    }

    private static bool GetFlags(OriginalLevelSceneRoot level, string[] flags)
    {
        if (level == null) return false;
        if (flags == null || flags.Length == 0 || (flags.Length == 1 && flags[0] == "")) return true;
        bool value = true;
        foreach (string flag in flags)
            value &= flag[0] == '!' ? !level.Session.GetFlag(flag.TrimStart('!')) : level.Session.GetFlag(flag);
        return value;
    }

    public bool AnalyzeRefill(RoomPlayerSession P)
    {
        bool flag = (iStamRef ? (P.Data.Stamina > iStamina) : (P.Data.Stamina <= iStamina));
        bool flag2 = iDashRef switch
        {
            -2 => P.Data.Dashes <= iDashes,
            -1 => P.Data.Dashes != iDashes,
            0 => P.Data.Dashes == iDashes,
            1 => P.Data.Dashes >= iDashes,
            2 => P.Data.Dashes > iDashes,
            _ => P.Data.Dashes < iDashes,
        };
        bool flag3 = (logic ? (flag && flag2) : (flag || flag2));
        if (flag3)
        {
            P.Data.Stamina = (oStamRef ? (P.Data.Stamina + oStamina) : ((oStamina < 0f) ? 110f : oStamina));
            P.Data.Dashes = ((!oDashRef)
                ? ((oDashes < 0) ? (Scene as OriginalLevelSceneRoot).Session.Inventory.Dashes : oDashes)
                : ((mapName == "KAERRA/FurryWeek/day3")
                    ? (P.Data.Dashes + oDashes)
                    : Math.Max(0, P.Data.Dashes + oDashes)));
        }

        return flag3;
    }


    private IEnumerator RefillRoutine(RoomPlayerSession player)
    {
        if (FreezeTime > 0f) engine.Freeze(FreezeTime);
        yield return null;
        level.Shake(.3f);
        sprite.Visible = flash.Visible = false;
        if (count++ <= UseNumber)
        {
            outline.Visible = true;
            respawnTimer = RespawnTime;
        }

        Depth = Depths[1];
        yield return .05f;
        float angle = (float)Math.Atan2(player.Speed.Y, player.Speed.X);
        level.ParticlesFG.Emit(P_Shatter, 5, Position, Vector2.One * 4f, angle - (float)Math.PI / 2f);
        level.ParticlesFG.Emit(P_Shatter, 5, Position, Vector2.One * 4f, angle + (float)Math.PI / 2f);
        OriginalSlashFxState.Burst(engine, Position, angle);
        if (count > UseNumber) RemoveSelf();
    }
}
