// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Mods.VivHelper.Runtime.Bumpers.Launching;

namespace Celeste.Mods.VivHelper.Entities.Hazards.Bumpers
{
    public sealed partial class BumperWrapper
    {
        private void ReplacementOnPlayer(RoomPlayerSession player, SceneEntityState entity)
        {
            if (binding.Timer > 0) return;
            context.WrapperType = launchKind;
            int dashes = player.Data.Dashes;
            float stamina = player.Data.Stamina;
            context.DisableFreeze = true;
            context.DetectFreeze = false;
            oldOnCollide(player);
            context.DisableFreeze = false;
            if (context.DetectFreeze)
                BumperLaunchMotion.Launch(player, legacyPositionLogic ? Position : entity.Position, false, false,
                    context.WrapperType, BumperBoost, ExplodeMultiplier);
            float cooldown = player.Data.CooldownTimer;
            context.WrapperType = BumperLaunchKind.IgnoreAll;
            if (setDashes == -2) player.Data.Dashes = dashes;
            else if (setDashes != -1) player.Data.Dashes = setDashes;
            if (setStamina == -2) player.Data.Stamina = stamina;
            else if (setStamina != -1) player.Data.Stamina = setStamina;
            player.Speed *= ExplodeMultiplier;
            player.Data.CooldownTimer = newDashCooldown < 0 ? cooldown : newDashCooldown;
            if (normalStateOnEnd) player.Machine.State = 0;
            if (respawnTime >= 0) binding.Timer = respawnTime;
        }
    }
}
