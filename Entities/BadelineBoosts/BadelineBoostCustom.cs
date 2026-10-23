// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Entities.BadelineBoosts;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.VivHelper.Entities.BadelineBoosts
{
    [CustomEntity("VivHelper/BadelineBoostNoRefill = BaddyBoostNoRefill")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.BadelineBoostCustom")]
    public class BadelineBoostCustom : BadelineBoostState
    {
        internal readonly ISourceEngineContext EngineContext;

        private static Vector2[] Nodes(EntityData data, Vector2 offset)
        {
            var nodes = new Vector2[data.Nodes.Length + 1];
            nodes[0] = data.Position + offset;
            for (int i = 0; i < data.Nodes.Length; i++) nodes[i + 1] = data.Nodes[i] + offset;
            return nodes;
        }

        public static SceneEntityState BaddyBoostNoRefill(OriginalLevelSceneRoot level,
            OriginalLevelRoomData room, Vector2 offset, OriginalLevelEntityData data) =>
            new BadelineBoostCustom(level.EngineContext, data, offset, true);

        public const string BadelineBoostMultCacheName = "VH_BBLM";

        public Func<int, int, int> replaceDashes;

        public Func<float, float, float> replaceStamina;

        public bool FloorPositionOnLaunch;

        public bool ignoreCameraLock;

        public float? LaunchStrengthMultiplier;

        public float[] LaunchStrengthMultiplierSet;

        public string DashLogic;

        public string StamLogic;

        public BadelineBoostCustom(ISourceEngineContext engine, EntityData data, Vector2 offset, bool noRefillOverride)
            : base(engine, Nodes(data, offset), data.Bool("lockCamera", true), data.Bool("canSkip", false),
                data.Bool("finalCh9Boost", false), data.Bool("finalCh9GoldenBoost", false),
                data.Bool("finalCh9Dialog", false))
        {
            EngineContext = engine;
            replaceDashes = (int i, int j) => i;
            replaceStamina = (float i, float j) => i;
            if (noRefillOverride || (data.Has("NoDashRefill") && !data.Has("DashesLogic")))
            {
                DashLogic = ((data.Bool("NoDashRefill", false) || noRefillOverride) ? "+0" : "D");
            }
            else
            {
                DashLogic = data.Attr("DashesLogic", "");
            }

            if (data.Has("NoStamRefill") && !data.Has("StaminaLogic"))
            {
                DashLogic = (data.Bool("NoStamRefill", false) ? "+0" : "D");
            }
            else
            {
                StamLogic = data.Attr("StaminaLogic", "");
            }

            FloorPositionOnLaunch = data.Bool("FloorPositionOnLaunch", false);
            LaunchStrengthMultiplier = 0f;
            string text = data.Attr("LaunchStrengthMultiplier", "1");
            if (text.Contains(','))
            {
                string[] array = text.Split(',');
                int num = data.Nodes.Length;
                LaunchStrengthMultiplierSet = new float[num];
                for (int num2 = 0; num2 < Math.Min(num, array.Length); num2++)
                {
                    LaunchStrengthMultiplierSet[num2] = float.Parse(array[num2]);
                }

                for (int num3 = Math.Min(num, array.Length); num3 < num; num3++)
                {
                    LaunchStrengthMultiplierSet[num3] = 1f;
                }

                LaunchStrengthMultiplier = null;
            }
            else
            {
                LaunchStrengthMultiplier = Math.Max(0.1f, float.Parse(text));
            }

            ignoreCameraLock = data.Bool("IgnoreCameraLock", false);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            SetRefillActions(DashLogic, StamLogic, scene);
        }

        public void SetRefillActions(string dashes, string stamina, object scene)
        {
            if (string.IsNullOrWhiteSpace(dashes) || dashes == "D")
            {
                replaceDashes = (int i, int j) => (j > 1) ? 1 : ((i < j) ? j : i);
            }
            else
            {
                int num = 0;
                if (dashes[0] == '+' || dashes[0] == '-')
                {
                    num = ((dashes[0] == '+') ? 1 : (-1));
                    dashes = dashes.Substring(1);
                }

                if (!BadelineBoostOldRefillProcessor.IntParserV1(dashes, this.IntParserEntityValues_Default(),
                        out var outDash))
                {
                    replaceDashes = (int i, int j) => (j > 1) ? 1 : ((i < j) ? j : i);
                }
                else
                {
                    switch (num)
                    {
                        case -1:
                            replaceDashes = (int i, int j) => Math.Max(0, i - outDash);
                            break;
                        case 1:
                            replaceDashes = (int i, int j) => Math.Max(0, i + outDash);
                            break;
                        default:
                            replaceDashes = (int i, int j) => Math.Max(0, outDash);
                            break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(stamina) || stamina == "D")
            {
                replaceStamina = (float i, float j) => j;
                return;
            }

            int num2 = 0;
            if (stamina[0] == '+' || stamina[0] == '-')
            {
                num2 = ((stamina[0] == '+') ? 1 : (-1));
                stamina = stamina.Substring(1);
            }

            if (!BadelineBoostOldRefillProcessor.FloatParserV1(stamina, this.FloatParserEntityValues_Default(),
                    out var outStam))
            {
                replaceStamina = (float i, float j) => j;
                return;
            }

            switch (num2)
            {
                case -1:
                    replaceStamina = (float i, float j) => Math.Max(0f, i - outStam);
                    break;
                case 1:
                    replaceStamina = (float i, float j) => Math.Max(0f, i + outStam);
                    break;
                default:
                    replaceStamina = (float i, float j) => Math.Max(0f, outStam);
                    break;
            }
        }
    }
}
