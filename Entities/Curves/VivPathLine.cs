using System.Numerics;
using Celeste.Core.Display;

namespace Celeste.Mods.VivHelper.Entities.Curves
{
    public struct VivPathLine
    {
        public int order;

        public PackedColor color;

        public string type;

        public float startT;

        public float endT;

        public float thickness;

        public float distance;

        public Vector2 offset;

        public bool addEnds;
    }
}
