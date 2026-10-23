// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using Celeste.Core.Display;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Scene.Scheduling;

namespace Celeste.Mods.VivHelper.Triggers.Teleportation
{
    public partial class InstantTeleportTrigger1Way
    {
        private SceneTagSet GlobalTag => ((IOriginalGameTagsEnvironment)engine).Tags.Global.Value;

        private float Clamp(float value, float min, float max) => ScalarMath.Clamp(value, min, max,
            ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);

        private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

        private float Range(float min, float max) =>
            SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, min, max);

        private static Vector2 Floor(Vector2 value) => new((float)Math.Floor(value.X), (float)Math.Floor(value.Y));
        private static bool SameEntityID(MapEntityId left, MapEntityId right) => left == right;

        private static void DoScreenWipe(OriginalLevelSceneRoot level, bool wipeIn, Action completed)
        {
            if (level.AreaData.Wipe == null)
                _ = new Celeste.Simulation.Presentation.Scene.Transitions.WindWipePresentation(level, wipeIn,
                    completed);
            else level.AreaData.Wipe(level, wipeIn, completed);
        }

        private static bool IsValidRoomName(string name, List<OriginalLevelRoomData> rooms) =>
            rooms.Any(room => room.Name == name);

        private static bool FlagsMatch(OriginalLevelSceneRoot level, string[] flags, string mode)
        {
            if (level == null) return false;
            bool result = mode == "and";
            if (flags == null || flags.Length == 0 || (flags.Length == 1 && flags[0] == "")) return true;
            foreach (string flag in flags)
                result = mode == "or"
                    ? result | (flag[0] != '!'
                        ? level.Session.GetFlag(flag)
                        : !level.Session.GetFlag(flag.TrimStart('!')))
                    : result & (flag[0] != '!'
                        ? level.Session.GetFlag(flag)
                        : !level.Session.GetFlag(flag.TrimStart('!')));
            return result;
        }

        private bool TryGetEaser(string name, out Func<float, float> ease)
            => context.TryGetEaser(name, out ease);

        private static readonly Dictionary<string, PackedColor> colors = typeof(PackedColor)
            .GetProperties(BindingFlags.Static | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(PackedColor))
            .ToDictionary(property => property.Name.ToLower(), property => (PackedColor)property.GetValue(null));

        private static PackedColor ReadColor(string value)
        {
            if (colors.TryGetValue(value.ToLower(), out var color)) return color;
            // Published OldHexToColor accepts XNA packed ABGR and its original 6 digit byte swap.
            string text = value.Trim('#');
            if (text.StartsWith("0x")) text = text.Substring(2);
            if (text.Length == 6 &&
                uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
                return new PackedColor((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
            if (text.Length == 8 && text.Substring(0, 2) == "00" && Regex.IsMatch(text.Substring(2), "[^0-9a-f]"))
                return PackedColor.Transparent;
            return uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint packed)
                ? new PackedColor { PackedValue = packed }
                : PackedColor.Transparent;
        }
    }
}
