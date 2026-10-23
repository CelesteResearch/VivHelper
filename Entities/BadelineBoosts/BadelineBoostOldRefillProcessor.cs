// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;

namespace Celeste.Mods.VivHelper.Entities.BadelineBoosts
{
    internal static class BadelineBoostOldRefillProcessor
    {
        public static Dictionary<string, Func<int>> IntParserEntityValues_Default(this SceneEntityState e)
        {
            if (e.Scene == null)
            {
                return null;
            }

            return new Dictionary<string, Func<int>>
            {
                {
                    "D",
                    () => (e.Scene as OriginalLevelSceneRoot).Session?.Inventory.Dashes ?? 1
                },
                {
                    "Default",
                    () => (e.Scene as OriginalLevelSceneRoot).Session?.Inventory.Dashes ?? 1
                }
            };
        }

        public static Dictionary<string, Func<float>> FloatParserEntityValues_Default(this SceneEntityState e)
        {
            if (e.Scene == null)
            {
                return null;
            }

            return new Dictionary<string, Func<float>>
            {
                {
                    "D",
                    () => 110f
                },
                {
                    "Default",
                    () => 110f
                }
            };
        }

        public static bool IntParserV1(string number, Dictionary<string, Func<int>> parseSpecialValues, out int value)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                value = 0;
                return false;
            }

            if (parseSpecialValues.ContainsKey(number))
            {
                value = parseSpecialValues[number]();
                return true;
            }

            return IntParserV1(number, parseSpecialValues, _: false, out value);
        }

        private static bool IntParserV1(string number, Dictionary<string, Func<int>> parseSpecialValues, bool _,
            out int value)
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
                    if (!IntParserV1(array[i], parseSpecialValues, _: false, out var value2))
                    {
                        value = 0;
                        return false;
                    }

                    num += value2;
                }

                value = num;
                return true;
            }

            if (number.Contains("*"))
            {
                string[] array2 = number.Split('*');
                int num2 = 1;
                for (int j = 0; j < array2.Length; j++)
                {
                    if (!IntParserV1(array2[j], parseSpecialValues, _: false, out var value3))
                    {
                        value = 0;
                        return false;
                    }

                    num2 *= value3;
                }

                value = num2;
                return true;
            }

            if (number.Contains("/"))
            {
                string[] array3 = number.Split('/');
                int num3 = 1;
                for (int k = 0; k < array3.Length; k++)
                {
                    if (!IntParserV1(array3[k], parseSpecialValues, _: false, out var value4))
                    {
                        value = 0;
                        return false;
                    }

                    if (value4 == 0)
                    {
                        value = 0;
                        return false;
                    }

                    num3 /= value4;
                }

                value = num3;
                return true;
            }

            if (parseSpecialValues.ContainsKey(number.Trim()))
            {
                value = parseSpecialValues[number.Trim()]();
                return true;
            }

            return int.TryParse(number.Trim(), out value);
        }

        public static bool FloatParserV1(string number, Dictionary<string, Func<float>> parseSpecialValues,
            out float value)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                value = 0f;
                return false;
            }

            if (parseSpecialValues.ContainsKey(number))
            {
                value = parseSpecialValues[number]();
                return true;
            }

            return FloatParserV1(number, parseSpecialValues, _: false, out value);
        }

        private static bool FloatParserV1(string number, Dictionary<string, Func<float>> parseSpecialValues, bool _,
            out float value)
        {
            if (string.IsNullOrEmpty(number))
            {
                throw new Exception("Floating-point number was empty.");
            }

            if (number.Contains("+"))
            {
                string[] array = number.Split('+');
                float num = 0f;
                for (int i = 0; i < array.Length; i++)
                {
                    if (!FloatParserV1(array[i], parseSpecialValues, _: false, out var value2))
                    {
                        value = 0f;
                        return false;
                    }

                    num += value2;
                }

                value = num;
                return true;
            }

            if (number.Contains("*"))
            {
                string[] array2 = number.Split('*');
                float num2 = 1f;
                for (int j = 0; j < array2.Length; j++)
                {
                    if (!FloatParserV1(array2[j], parseSpecialValues, _: false, out var value3))
                    {
                        value = 0f;
                        return false;
                    }

                    num2 *= value3;
                }

                value = num2;
                return true;
            }

            if (number.Contains("/"))
            {
                string[] array3 = number.Split('/');
                float num3 = 1f;
                for (int k = 0; k < array3.Length; k++)
                {
                    if (!FloatParserV1(array3[k], parseSpecialValues, _: false, out var value4))
                    {
                        value = 0f;
                        return false;
                    }

                    if (value4 == 0f)
                    {
                        value = 0f;
                        return false;
                    }

                    num3 /= value4;
                }

                value = num3;
                return true;
            }

            if (parseSpecialValues.ContainsKey(number.Trim()))
            {
                value = parseSpecialValues[number.Trim()]();
                return true;
            }

            return float.TryParse(number.Trim(), out value);
        }
    }
}
