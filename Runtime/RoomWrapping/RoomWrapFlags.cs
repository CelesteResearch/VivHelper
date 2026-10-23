// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Game.Execution.Levels;

namespace Celeste.Mods.VivHelper.Runtime.RoomWrapping
{
    public static class RoomWrapFlags
    {
        public static bool All(OriginalLevelSceneRoot level, string[] flags)
        {
            if (level == null) return false;
            if (flags == null || flags.Length == 0 || flags.Length == 1 && flags[0] == "") return true;
            bool result = true;
            foreach (string flag in flags)
                result &= flag[0] != '!' ? level.Session.GetFlag(flag) : !level.Session.GetFlag(flag.TrimStart('!'));
            return result;
        }
    }
}
