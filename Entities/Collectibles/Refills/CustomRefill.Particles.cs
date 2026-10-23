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

public partial class CustomRefill : SceneEntityState
{
    private string[] ModString(string s, int t, Vector2 v)
    {
        switch (t)
        {
            case 0:
            {
                if (string.IsNullOrWhiteSpace(s))
                {
                    s = "||";
                }

                string[] array = s.Trim().Split('|');
                if (array.Length != 3)
                {
                    throw new Exception("Invalid ParticleSources parameter for Custom Refill at " +
                                        (Position - v).ToString() + ".");
                }

                return array;
            }
            case 1:
            {
                if (string.IsNullOrWhiteSpace(s))
                {
                    s = ((oDashes > 1) ? "FFD3F9:EF94E3|FFA5AA:DD6CCA" : "d3ffd4:85fc87|a5fff7:6de081");
                }

                string[] array = s.Trim().Split('|');
                if (array.Length < 2 || array.Length > 3)
                {
                    throw new Exception("Invalid ParticleColors parameter for Custom Refill at " +
                                        (Position - v).ToString() + ".");
                }

                return array;
            }
            default:
                throw new Exception("HOW");
        }
    }

    private void ParticleParser(string[] sources, PackedColor?[][] colors, bool twodash)
    {
        P_Shatter = new ParticleTypePresentation(engine.RefillParticles.P_Shatter);
        P_Glow = new ParticleTypePresentation(engine.RefillParticles.P_Glow);
// The installed Mod deliberately clones the glow template for regeneration.
        P_Regen = new ParticleTypePresentation(engine.RefillParticles.P_Glow);
        if (!string.IsNullOrWhiteSpace(sources[0])) P_Shatter.Source = engine.GameGraphics.GameAtlas[sources[0]];
        if (!string.IsNullOrWhiteSpace(sources[1])) P_Glow.Source = engine.GameGraphics.GameAtlas[sources[1]];
        if (!string.IsNullOrWhiteSpace(sources[2])) P_Regen.Source = engine.GameGraphics.GameAtlas[sources[2]];
        if (colors[0][0].HasValue) P_Shatter.Color = colors[0][0].Value;
        if (colors[0][1].HasValue) P_Shatter.Color2 = colors[0][1].Value;
        if (colors[1][0].HasValue) P_Glow.Color = colors[1][0].Value;
        if (colors[1][1].HasValue) P_Glow.Color2 = colors[1][1].Value;
        if (colors[2][0].HasValue) P_Regen.Color = colors[2][0].Value;
        else if (colors[1][0].HasValue) P_Regen.Color = colors[1][0].Value;
        if (colors[2][1].HasValue) P_Regen.Color2 = colors[2][1].Value;
        else if (colors[1][1].HasValue) P_Regen.Color2 = colors[1][1].Value;
    }
}
