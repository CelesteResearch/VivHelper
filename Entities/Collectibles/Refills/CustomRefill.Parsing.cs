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
    private void iParser(string[] SMaster)
    {
        string text;
        string text2;
        if (SMaster.Length == 2)
        {
            logic = false;
            text = SMaster[0];
            text2 = SMaster[1];
        }
        else
        {
            logic = SMaster[1].Length != 0 && SMaster[1][0] == '&';
            text = SMaster[0];
            text2 = SMaster[2];
        }

        iStamRef = text[0] == '>';
        if (!FloatParser((text[0] == '>' || text[0] == '<') ? text.Substring(1) : text, _: false, out iStamina))
        {
            iStamina = 20f;
        }

        int num = 0;
        if (text2[0] == '<')
        {
            num++;
            if (text2[1] == '=')
            {
                iDashRef = -2;
                num++;
            }
            else
            {
                iDashRef = -3;
            }
        }
        else if (text2[0] == '>')
        {
            num++;
            if (text2[1] == '=')
            {
                iDashRef = 1;
                num++;
            }
            else
            {
                iDashRef = 2;
            }
        }
        else if (text2[0] == '=')
        {
            num++;
            iDashRef = 0;
        }
        else if (text2.StartsWith("!="))
        {
            num += 2;
            iDashRef = -1;
        }
        else
        {
            iDashRef = -3;
        }

        if (!IntParser(text2.Substring(num), _: true, out iDashes))
        {
            iDashes = -1;
            iDashRef = -1;
        }
    }

    private bool IntParser(string number, bool _, out int k)
    {
        if (string.IsNullOrEmpty(number))
        {
            throw new Exception("Integer was empty.");
        }

        if (number.Contains("+"))
        {
            string[] array = number.Split('+');
            int num = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (!IntParser(array[i], _, out var k2))
                {
                    k = 0;
                    return false;
                }

                num += k2;
            }

            k = num;
            return true;
        }

        if (number.Contains("*"))
        {
            string[] array2 = number.Split('*');
            int num2 = 1;
            for (int j = 0; j < array2.Length; j++)
            {
                if (!IntParser(array2[j], _, out var k3))
                {
                    k = 0;
                    return false;
                }

                num2 *= k3;
            }

            k = num2;
            return true;
        }

        if (number.Contains("/"))
        {
            string[] array3 = number.Split('/');
            int num3 = 1;
            for (int l = 0; l < array3.Length; l++)
            {
                if (!IntParser(array3[l], _, out var k4))
                {
                    k = 0;
                    return false;
                }

                if (k4 == 0)
                {
                    k = 0;
                    return false;
                }

                num3 /= k4;
            }

            k = num3;
            return true;
        }

        if (number.Trim() == "U")
        {
            k = UseNumber;
            return true;
        }

        if (number.Trim() == "u")
        {
            k = count;
            return true;
        }

        if (number.Trim() == "D" && _)
        {
            k = (Scene as OriginalLevelSceneRoot)?.Session?.Inventory.Dashes ?? 1;
            return true;
        }

        return int.TryParse(number.Trim(), out k);
    }

    private bool FloatParser(string number, bool _, out float k)
    {
        if (string.IsNullOrEmpty(number))
        {
            throw new Exception("Float was empty.");
        }

        if (number.Contains("+"))
        {
            string[] array = number.Split('+');
            float num = 0f;
            for (int i = 0; i < array.Length; i++)
            {
                if (!FloatParser(array[i], _, out var k2))
                {
                    k = 0f;
                    return false;
                }

                num += k2;
            }

            k = num;
            return true;
        }

        if (number.Contains("*"))
        {
            string[] array2 = number.Split('*');
            float num2 = 1f;
            for (int j = 0; j < array2.Length; j++)
            {
                if (!FloatParser(array2[j], _, out var k3))
                {
                    k = 0f;
                    return false;
                }

                num2 *= k3;
            }

            k = num2;
            return true;
        }

        if (number.Contains("/"))
        {
            string[] array3 = number.Split('/');
            float num3 = 1f;
            for (int l = 0; l < array3.Length; l++)
            {
                if (!FloatParser(array3[l], _, out var k4))
                {
                    k = 0f;
                    return false;
                }

                if (k4 == 0f)
                {
                    k = 0f;
                    return false;
                }

                num3 /= k4;
            }

            k = num3;
            return true;
        }

        if (number.Trim() == "D")
        {
            k = (_ ? 110f : 20f);
            return true;
        }

        return float.TryParse(number.Trim(), out k);
    }

    private void oParser(string S, string D)
    {
        oStamRef = false;
        if (S[0] == '+')
        {
            oStamRef = true;
            S = S.Substring(1);
        }

        if (!FloatParser(S, _: true, out oStamina))
        {
            oStamina = 110f;
            oStamRef = false;
        }

        oDashRef = false;
        if (D[0] == '+')
        {
            oDashRef = true;
            D = D.Substring(1);
        }

        if (!IntParser(D, _: true, out oDashes))
        {
            oDashes = (Scene as OriginalLevelSceneRoot).Session.Inventory.Dashes;
            oDashRef = false;
        }
    }

    public float OutlineColorLerp()
    {
        float num = Math.Min(1f, (float)(count + 1) / (float)UseNumber);
        return num * num * num;
    }
}
