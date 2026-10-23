using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Celeste.Core.Display;

namespace Celeste.Mods.VivHelper.Spawn
{
    public static class SpawnPointRules
    {
        public static bool IsSpawn(string name) => name == "player" || name == "VivHelper/Spawnpoint"
                                                                    || name == "VivHelper/InterRoomSpawner" ||
                                                                    name == "VivHelper/InterRoomSpawnTarget";

        private static readonly Dictionary<string, PackedColor> NamedColors = CreateNamedColors();

        private static Dictionary<string, PackedColor> CreateNamedColors()
        {
            var colors = new Dictionary<string, PackedColor>();
            foreach (var property in typeof(PackedColor).GetProperties(BindingFlags.Public | BindingFlags.Static))
                if (property.PropertyType == typeof(PackedColor))
                    colors[property.Name.ToLower()] = (PackedColor)property.GetValue(null);
            return colors;
        }

        public static PackedColor ParseColor(string text)
        {
            if (NamedColors.TryGetValue(text.ToLower(), out var named)) return named;
            var hex = text.Trim('#');
            if (hex.StartsWith("0x")) hex = hex.Substring(2);
            if (hex.Length == 8 && hex.StartsWith("00") &&
                System.Text.RegularExpressions.Regex.IsMatch(hex.Substring(2), "[^0-9a-f]"))
                return PackedColor.Transparent;
            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                return PackedColor.Transparent;
            if (hex.Length == 6) return new PackedColor((byte)(value >> 16), (byte)(value >> 8), (byte)value);
            return new PackedColor { PackedValue = value };
        }

        public static bool ShouldSkipReload(bool noResetRespawn, bool noResetOnRetry, bool pausedRetry)
            => noResetRespawn && (noResetOnRetry || !pausedRetry);
    }
}
