// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using Celeste.Core.Mathematics;
using Celeste.Physics.Collision.Shapes;

namespace Celeste.Mods.VivHelper.Entities.Spinners
{
    public static class SpinnerHitboxParser
    {
        public static ColliderListGeometry Parse(string text, float scale, ISourceVectorArithmetic vectors)
        {
            string[] entries = text.Split('|');
            if (entries.Length == 0 || entries.Length == 1 && string.IsNullOrWhiteSpace(entries[0])) return null;
            var colliders = new List<ColliderGeometry>();
            foreach (string entry in entries)
            {
                string[] parts = entry.Split(':', ';');
                if (parts[0][0] == 'C')
                {
                    string[] offset = (parts.Length == 2 ? "0,0" : parts[2]).Split(',');
                    colliders.Add(new CircleColliderGeometry(vectors, ParseInt(parts[1], scale),
                        ParseInt(offset[0], scale), ParseInt(offset[1], scale)));
                }
                else if (parts[0][0] == 'R')
                {
                    string[] size = parts[1].Split(',');
                    int width = ParseInt(size[0], scale), height = ParseInt(size[1], scale);
                    int x, y;
                    if (parts.Length == 2)
                    {
                        x = -Math.Abs((int)Math.Round(width / 2f));
                        y = Math.Min(-3, -Math.Abs((int)Math.Round(height / 2f)));
                    }
                    else
                    {
                        string[] offset = parts[2].Split(',');
                        x = ParseInt(offset[0], scale);
                        y = ParseInt(offset[1], scale);
                    }

                    colliders.Add(new HitboxColliderGeometry(width, height, x, y));
                }
            }

            return new ColliderListGeometry(colliders.ToArray());
        }

        public static int ParseInt(string text, float scale)
        {
            if (string.IsNullOrEmpty(text)) throw new Exception("Integer was empty.");
            if (text.Contains("@"))
            {
                int value = 0;
                foreach (string part in text.Split('@')) value += ParseInt(part, scale);
                return value;
            }

            return text[0] == '*' ? int.Parse(text.Substring(1)) : (int)Math.Round(int.Parse(text) * (double)scale);
        }
    }
}
