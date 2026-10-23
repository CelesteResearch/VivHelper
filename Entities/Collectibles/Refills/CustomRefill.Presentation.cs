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

public partial class CustomRefill
{
    public override void Added(object scene)
    {
        base.Added(scene);
        level = Scene as OriginalLevelSceneRoot;
        var keys = ((IOriginalAreaKeyEnvironment)engine).AreaKeys;
        mapName = level.Session != null
            ? keys.GetSID(keys.GetSessionArea(level.Session)) ?? string.Empty
            : string.Empty;
    }

    public override void Update()
    {
        base.Update();
        if (respawnTimer > 0f && (string.IsNullOrEmpty(flagToggle) ||
                                  flagInvert != (Scene as OriginalLevelSceneRoot).Session.GetFlag(flagToggle)))
        {
            respawnTimer = Math.Max(0f, respawnTimer - engine.DeltaTime);
            if (respawnTimer == 0f) Respawn();
        }
        else if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(.1f, engine))
            level.ParticlesFG.Emit(P_Glow, 1, Position, Vector2.One * 5f);

        UpdateWobble();
        light.Alpha = ScalarMath.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * engine.DeltaTime);
        bloom.Alpha = light.Alpha * .8f;
        if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(2f, engine) && sprite.Visible)
        {
            flash.Play("flash", true);
            flash.Visible = true;
        }
    }

    private void Respawn()
    {
        if (!Collidable && count <= UseNumber)
        {
            Collidable = true;
            sprite.Visible = true;
            outline.Visible = false;
            Depth = Depths[0];
            wiggler.Start();
            engine.SpatialAudio.Play(CustomAudio[1], Position);
            level.ParticlesFG.Emit(P_Regen, 16, Position, Vector2.One * 2f);
        }
    }

    private void UpdateWobble()
    {
        if (wobble == WobbleTypes.XOnly || wobble == WobbleTypes.Circle)
        {
            float x = sprite.X = flash.X = sine[0].TwoValue;
            if (bloom != null) bloom.X = x;
        }

        if (wobble == WobbleTypes.YOnly || wobble == WobbleTypes.Circle)
        {
            float y = sprite.Y = flash.Y = sine[1].TwoValue;
            if (bloom != null) bloom.Y = y;
        }
    }

    public override void Render(ISourceRenderContext context)
    {
        if (sprite.Visible)
        {
            if (UseCustomOutline)
            {
                PackedColor color = SourceColorInterpolation.Lerp(PackedColor.White, PackedColor.Red,
                    OutlineColorLerp(), ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                sprite.DrawOutline(context, color, 1);
                outline.Color = color;
            }
            else outline.Color = PackedColor.White;

// The installed Mod draws the ordinary outline after its colored outline.
            sprite.DrawOutline(context, 1);
        }

        base.Render(context);
    }
}
