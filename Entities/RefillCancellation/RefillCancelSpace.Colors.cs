using System.Collections.Generic;
using Celeste.Core.Display;
using Celeste.Graphics.Display.Colors;

namespace Celeste.Mods.VivHelper.Entities.RefillCancellation
{
    public partial class RefillCancelSpace
    {
        public static Dictionary<int, PackedColor> ColorEval = new()
        {
            { 1, SourceHexColor.HexToColor("8b0000") }, { 2, SourceHexColor.HexToColor("6f81f4") },
            { 3, SourceHexColor.HexToColor("a06ff4") }, { 4, SourceHexColor.HexToColor("8b8b00") },
            { 5, SourceHexColor.HexToColor("8b4500") }, { 6, SourceHexColor.HexToColor("00b900") },
            { 7, PackedColor.LightGray }
        };

        public static Dictionary<int, PackedColor> ColorEvalNone = new()
        {
            { 1, SourceHexColor.HexToColor("ff0000") }, { 2, SourceHexColor.HexToColor("87ceeb") },
            { 3, SourceHexColor.HexToColor("9370db") }, { 4, SourceHexColor.HexToColor("dcdc00") },
            { 5, SourceHexColor.HexToColor("ff9500") }, { 6, SourceHexColor.HexToColor("00dc00") },
            { 7, PackedColor.LightGray }
        };

        public static Dictionary<int, PackedColor> BWEval = new()
        {
            { 1, PackedColor.Gray }, { 2, PackedColor.Gray }, { 3, PackedColor.LightGray },
            { 4, PackedColor.Gray }, { 5, PackedColor.LightGray }, { 6, PackedColor.LightGray },
            { 7, PackedColor.White }
        };
    }
}
