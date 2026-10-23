// SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Celeste.Core.Display;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.VivHelper.Spawn;

namespace Celeste.Mods.VivHelper.Runtime.Colors
{
    internal static class VivEntityColor
    {
        public static PackedColor Read(EntityData data, string key)
        {
            string value = data.Attr(key, "");
            if (string.IsNullOrWhiteSpace(value)) return PackedColor.White;
            if (value == "Transparent" || value.Length == 8 && value.Substring(0, 2) == "00" &&
                int.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                return PackedColor.Transparent;
            var color = SpawnPointRules.ParseColor(value);
            return color == PackedColor.Transparent ? PackedColor.White : color;
        }
    }
}
