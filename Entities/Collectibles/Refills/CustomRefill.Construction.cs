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
    public CustomRefill(ISourceEngineContext engine, EntityData e, Vector2 v, int type) : base(
        ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, e.Position + v)
    {
        this.engine = (IOriginalRefillProcess)engine;
        context = RefillCancelContext.For(engine);
        base.Position = e.Position + v;
        string text;
        string text2;
        string text3;
        string s;
        string d;
        switch (type)
        {
            case 0:
                text = e.Float("iStamina", 20f).ToString();
                text2 = (e.Bool("iLogic", false) ? "&" : "|");
                text3 = ((e.Int("iDashes", -1) < 0) ? "D" : e.Int("iDashes", 0).ToString());
                s = e.Float("oStamina", 110f).ToString();
                d = ((e.Int("oDashes", -1) < 0) ? "D" : e.Int("oDashes", 0).ToString());
                UseNumber = e.Int("UseNumber", -1);
                if (UseNumber < 0)
                {
                    UseNumber = int.MaxValue;
                }

                break;
            case 1:
                text = e.Attr("iStam", "20");
                text2 = (string.IsNullOrWhiteSpace(e.Attr("iLog", "")) ? "|" : e.Attr("iLog", ""));
                text3 = e.Attr("iDash", "D");
                s = e.Attr("oStam", "110");
                d = e.Attr("oDash", "D");
                if (!IntParser(e.Attr("UseNum", "-1"), _: false, out UseNumber))
                {
                    UseNumber = int.MaxValue;
                }
                else if (UseNumber < 1)
                {
                    UseNumber = int.MaxValue;
                }

                break;
            default:
                throw new Exception(
                    "How did this happen? Please report what you were doing to Viv when this error occurred.");
        }

        iParser(new string[3] { text, text2, text3 });
        oParser(s, d);
        usecustomoutline = e.Bool("ShowColoredUseOutline", false);
        scale = e.Float("Scale", 1f);
        imageScale = (float)e.Int("ImageSize", 16) * 0.0625f;
        Collider = new HitboxColliderGeometry(16f * scale, 16f * scale, -8f * scale, -8f * scale);
        flagToggle = e.Attr("FlagToggle", "");
        if (!string.IsNullOrWhiteSpace(flagToggle) && flagToggle[0] == '!')
        {
            flagInvert = true;
            flagToggle = flagToggle.Substring(1);
        }

        Add(new PlayerColliderComponentState(OnPlayer));
        string[] sources = ModString(e.Attr("ParticleSources", ""), 0, v);
        string[] array = ModString(e.Attr("ParticleColors", ""), 1, v);
        string[][] array2 = new string[3][];
        for (int i = 0; i < 2; i++)
        {
            array2[i] = array[i].Split(':');
        }

        array2[2] = ((array.Length == 2) ? array2[1] : (array2[2] = array[2].Split(':')));
        PackedColor?[][] array3 = new PackedColor?[3][];
        for (int j = 0; j < 3; j++)
        {
            array3[j] = new PackedColor?[2];
        }

        for (int k = 0; k < 3; k++)
        {
            for (int l = 0; l < 2; l++)
            {
                string text4 = array2[k][l];
                if (string.IsNullOrWhiteSpace(text4))
                {
                    array3[k][l] = null;
                }
                else
                {
                    array3[k][l] = SpawnPointRules.ParseColor(text4);
                }
            }
        }

        ParticleParser(sources, array3, oDashes > 1);

        wobble = EntityAttributeReader.Enum(e.Values, "WobbleType", WobbleTypes.YOnly);
        directory = e.Attr("Directory", "objects/refill/").TrimEnd('/') + "/";
        Add(outline = new SpriteImage(this.engine.GameGraphics.GameAtlas[directory + "outline"]));
        outline.CenterOrigin();
        outline.Visible = false;
        Add(sprite = new Sprite(this.engine, this.engine.GameGraphics.GameAtlas, directory + "idle"));
        sprite.AddLoop("idle", "", .1f);
        sprite.Play("idle");
        sprite.CenterOrigin();
        Add(flash = new Sprite(this.engine, this.engine.GameGraphics.GameAtlas, directory + "flash"));
        flash.Add("flash", "", .05f);
        flash.OnFinish = delegate { flash.Visible = false; };
        flash.CenterOrigin();
        float f = e.Float("ScaleWiggleEffect", .2f);
        if (f > 0f)
            Add(wiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(1f, 4f,
                w => sprite.Scale = flash.Scale = Vector2.One * (1f + w * f)));
        Add(new MirrorReflectionState());
        string b = e.Attr("BloomPoint", "0.8,16").Replace('|', ',');
        if (!string.IsNullOrWhiteSpace(b) && b.Contains(","))
        {
            string[] a = b.Split(',');
            if (float.TryParse(a[0], out float alpha) && float.TryParse(a[1], out float radius))
                Add(bloom = new BloomPointState(alpha, radius));
        }

        string[] vertex = e.Attr("VertexLight", "White,1,16,48").Split(',');
        if (vertex.Length == 4)
        {
            vertex[0].Trim();
            PackedColor color = SpawnPointRules.ParseColor(vertex[0]);
            if (float.TryParse(vertex[1].Trim(), out float alpha) && int.TryParse(vertex[2].Trim(), out int start) &&
                int.TryParse(vertex[3].Trim(), out int end))
                Add(light = new VertexLightState(this.engine, Vector2.Zero, color, alpha, start, end));
        }

        CustomAudio = string.IsNullOrWhiteSpace(e.Attr("CustomAudio", ""))
            ? new string[] { "", "" }
            : e.Attr("CustomAudio", ",").Split(',');
        if (CustomAudio.Length != 2)
            throw new Exception("Invalid CustomAudio parameter for Custom Refill at " + (Position - v).ToString() +
                                ".");
        CustomAudio[0] = !string.IsNullOrWhiteSpace(CustomAudio[0]) ? CustomAudio[0] :
            oDashes > 1 ? "event:/new_content/game/10_farewell/pinkdiamond_touch" : "event:/game/general/diamond_touch";
        CustomAudio[1] = !string.IsNullOrWhiteSpace(CustomAudio[1]) ? CustomAudio[1] :
            oDashes > 1 ? "event:/new_content/game/10_farewell/pinkdiamond_return" :
            "event:/game/general/diamond_return";
        sine = new[] { new SineWaveState(this.engine, .6f), new SineWaveState(this.engine, .6f) };
        Add(sine[0]);
        Add(sine[1]);
        sine[0].Randomize();
        sine[1].Counter = (sine[0].Value + (float)Math.PI / 2f) % ((float)Math.PI * 2f);
        string[] depths = e.Attr("Depths", "-100,8999").Split(',');
        Depths[0] = int.Parse(depths[0]);
        Depths[1] = int.Parse(depths[1]);
        Depth = Depths[0];
        UpdateWobble();
        RespawnTime = e.Float("RespawnTime", 2.5f);
        FreezeTime = e.Float("FreezeTime", .05f);
    }
}
