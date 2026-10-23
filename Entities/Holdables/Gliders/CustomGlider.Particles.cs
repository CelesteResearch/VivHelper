using System;
using System.Reflection;
using Celeste.Core.Display;
using System.Numerics;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Graphics.Content;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.VivHelper.Entities.Holdables.Gliders
{
    public partial class CustomGlider
    {
        private static PackedColor OldColor(string name)
        {
            var result = SpawnPointRules.ParseColor(name);
            if (typeof(PackedColor).GetProperty(name,
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
                    is { PropertyType: var type } && type == typeof(PackedColor)) return result;
            return new PackedColor(result.R, result.G, result.B, 255);
        }

        private void InitializeParticles(EntityData data)
        {
            string glidePath = data.Attr("GlidePath", "particles/rect");
            if (string.IsNullOrWhiteSpace(glidePath)) glidePath = "particles/rect";
            string glowPath = data.Attr("GlowPath");
            if (string.IsNullOrWhiteSpace(glowPath)) glowPath = "";
            var original = ((IOriginalGameplayProcess)engine).ParticleTypes.Glider;
            P_Glide = new ParticleTypePresentation
            {
                Acceleration = Vector2.UnitY * 60, SpeedMin = 30, SpeedMax = 40, Direction = -(float)Math.PI / 2,
                DirectionRange = (float)Math.PI / 2, LifeMin = .6f, LifeMax = 1.2f,
                ColorMode = ParticleTypePresentation.ColorModes.Blink,
                FadeMode = ParticleTypePresentation.FadeModes.Late,
                Color = OldColor(data.Attr("GlideColor1", "4FFFF3")),
                Color2 = OldColor(data.Attr("GlideColor2", "FFF899")), Size = .5f, SizeRange = .2f,
                RotationMode = ParticleTypePresentation.RotationModes.SameAsDirection
            };
            if (!string.IsNullOrWhiteSpace(glidePath))
                P_Glide.Source = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[glidePath];
            P_GlideUp = new ParticleTypePresentation(original.P_Glide)
                { Acceleration = Vector2.UnitY * -10, SpeedMin = 50, SpeedMax = 60 };
            P_Glow = new ParticleTypePresentation
            {
                SpeedMin = 8, SpeedMax = 16, DirectionRange = (float)Math.PI * 2, LifeMin = .4f, LifeMax = .8f,
                Size = 1,
                FadeMode = ParticleTypePresentation.FadeModes.Late, Color = OldColor(data.Attr("GlowColor1", "B7F3FF")),
                Color2 = OldColor(data.Attr("GlowColor2", "F4FDFF")),
                ColorMode = ParticleTypePresentation.ColorModes.Blink
            };
            // The installed original writes GlowPath into P_Glide, not P_Glow.
            if (!string.IsNullOrWhiteSpace(glowPath))
                P_Glide.Source = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[glowPath];
            P_Expand = new ParticleTypePresentation(original.P_Glow)
            {
                SpeedMin = 40, SpeedMax = 80, SpeedMultiplier = .2f, LifeMin = .6f,
                LifeMax = 1.2f, DirectionRange = (float)Math.PI * 3 / 4
            };
            P_Platform = new ParticleTypePresentation
            {
                Acceleration = Vector2.UnitY * 60, SpeedMin = 5, SpeedMax = 20, Direction = -(float)Math.PI / 2,
                LifeMin = .6f, LifeMax = 1.4f, FadeMode = ParticleTypePresentation.FadeModes.Late, Size = 1
            };
        }
    }
}
