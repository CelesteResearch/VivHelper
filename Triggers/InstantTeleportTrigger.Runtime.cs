using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Effects.Liquids;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling;
using MonoMod.Utils;

namespace Celeste.Mods.VivHelper.Triggers
{
    public partial class InstantTeleportTrigger
    {
        private readonly ISourceEngineContext engine;
        private ISourceVectorArithmetic VectorArithmetic => ((ISourceVectorEnvironment)engine).VectorArithmetic;
        private SceneTagSet GlobalTag => ((IOriginalGameTagsEnvironment)engine).Tags.Global.Value;

        private float Approach(float value, float target, float amount) => ScalarMath.Approach(value, target, amount,
            ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);

        private float Clamp(float value, float min, float max) => ScalarMath.Clamp(value, min, max,
            ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);

        private bool FlagsMatch()
        {
            if (level == null) return false;
            bool result = true;
            if (flags == null || flags.Length == 0 || (flags.Length == 1 && flags[0] == "")) return true;
            foreach (string flag in flags)
                result &= flag[0] != '!' ? level.Session.GetFlag(flag) : !level.Session.GetFlag(flag.TrimStart('!'));
            return result;
        }

        // The four additional states are allocated by VivHelper's player extensions.
        // The owning extension registers its actual state indices on the same engine.
        private static readonly ConditionalWeakTable<ISourceEngineContext, HashSet<int>> dashStates = new();

        internal static HashSet<int> GetDashStatesForOwner(ISourceEngineContext engine)
            => dashStates.GetOrCreateValue(engine);

        public static void RegisterDashState(ISourceEngineContext engine, int state) =>
            dashStates.GetOrCreateValue(engine).Add(state);

        private bool MatchDashState(int state) => state == 2 || state == 5 ||
                                                  (dashStates.TryGetValue(engine, out var states) &&
                                                   states.Contains(state));

        private IEnumerator TimeSlowDown(float duration, float speedInit)
        {
            for (float i = 0f; i < duration; i += 0.05f)
            {
                engine.Effects.TimeRate = speedInit + (1f - speedInit) * i;
                yield return 0.05f;
            }

            engine.Effects.TimeRate = 1f;
        }

        private static readonly Dictionary<string, PackedColor> colors = typeof(PackedColor)
            .GetProperties(BindingFlags.Static | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(PackedColor))
            .ToDictionary(property => property.Name.ToLower(), property => (PackedColor)property.GetValue(null));

        private static PackedColor ParseColor(string text)
        {
            if (colors.TryGetValue(text.ToLower(), out var color)) return color;
            text = text.Trim('#');
            if (text.StartsWith("0x")) text = text.Substring(2);
            if (text.Length == 6 &&
                uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                return new PackedColor(SourceColorParsing.HexToColor((int)value));
            if (text.Length == 8 && text.Substring(0, 2) == "00" && Regex.IsMatch(text.Substring(2), "[^0-9a-f]"))
                return PackedColor.Transparent;
            return uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
                ? new PackedColor { PackedValue = value }
                : PackedColor.Transparent;
        }

        private Type FindActualType(string sourceType)
        {
            var external = ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery
                .ResolveRuntimeTypeAlias(sourceType);
            if (external != null) return external;
            foreach (var registration in level.Scheduler.Catalog.RuntimeRegistrations)
            {
                var definition = level.Scheduler.Catalog.Find(registration.SourceType);
                if (definition?.LogicalTypeName == sourceType) return registration.RequireActualType();
            }

            return null;
        }

        private void RemoveTeleportEntities()
        {
            var types = new[]
            {
                FindActualType("Celeste.FlingBird"),
                FindActualType("Celeste.Mod.JackalHelper.Entities.BraveBird"),
                FindActualType("Celeste.Mod.JackalHelper.Entities.AltBraveBird")
            };
            foreach (object entity in level.Scheduler.Entities)
            foreach (Type type in types)
                if (type != null && type.IsInstanceOfType(entity))
                {
                    level.RemoveEntity(entity);
                    break;
                }
        }

        private void ResetLavaPosition()
        {
            foreach (string name in new[]
                         { "Celeste.SandwichLava", "Celeste.Mod.MaxHelpingHand.Entities.CustomSandwichLava" })
            {
                Type type = FindActualType(name);
                if (type == null) continue;
                var entity = level.Scheduler.Entities.FirstOrDefault(type.IsInstanceOfType) as SceneEntityState;
                if (entity == null) continue;
                entity.Y = level.Camera.Bottom - 10f;
                var data = new DynamicData(type, entity);
                var top = data.Get<LavaRectangleComponentState>("topRect");
                var bottom = data.Get<LavaRectangleComponentState>("bottomRect");
                float displacement = name == "Celeste.SandwichLava" ? 0f : data.Get<float>("sandwichDisplacement");
                top.Position.Y = data.Get<float>("TopOffset") - top.Height + displacement;
                bottom.Position.Y = -displacement;
            }
        }
    }
}
