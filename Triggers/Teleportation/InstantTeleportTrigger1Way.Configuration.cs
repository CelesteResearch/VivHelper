// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Mathematics.Extrema;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Mods.VivHelper.Runtime.Teleportation;
using Celeste.Mods.VivHelper.Configuration.Errors;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using LevelData = Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using EntityID = Celeste.Core.Maps.MapEntityId;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using Trigger = Celeste.Simulation.Gameplay.Triggers.Foundation.SourceTriggerEntityState;

namespace Celeste.Mods.VivHelper.Triggers.Teleportation
{
    [RuntimeTypeAlias("VivHelper.Triggers.InstantTeleportTrigger1Way")]
    [CustomEntity("VivHelper/ITPT1Way = Load")]
    public partial class InstantTeleportTrigger1Way : SourceTriggerEntityState
    {
        private struct GlitchType
        {
            public float GlitchStrength;

            public Func<float, float> GlitchEaser;

            public float GlitchLength;
        }

        public enum TransitionType
        {
            None,
            ColorFlash,
            Lightning,
            Glitch,
            Wipe
        }

        public int ExitDirection;

        private int EntrySide = 0;

        public string[] flagsNeeded;

        public string[] flagsSet;

        public string roomName;

        public Vector2 startPos;

        public bool resetDashes;

        public TransitionType transition;

        public List<object> transitionInfo;

        public float delay;

        public float customDelay;

        public bool addRespawnDelay;

        public bool ignoreIfDummy;

        public string targetID;

        public string specificRoom;

        public string specificRoomErrorCheckName;

        public bool onlyOnce;

        public bool endCutscene;

        public bool preActive;

        public string setState = null;

        public bool bringHoldableThrough;

        private bool delayAwakeAction;

        public bool freezeGameOnTeleport;

        public TriggerPersistence persistence;

        public EntityID ID;

        public bool transitionListeners;

        public SimulationCoroutine lockRoom;

        private readonly ISourceEngineContext engine;
        private readonly VivTeleportContext context;

        public static Entity Load(Level level, LevelData levelData, Vector2 offset, OriginalLevelEntityData entityData)
        {
            if (TeleportTarget.ReadNonEmpty(entityData, "RoomName") == null && entityData.Has("TargetID"))
            {
                object obj;
                if (level == null)
                {
                    obj = null;
                }
                else
                {
                    Session session = level.Session;
                    obj = ((session != null) ? ((OriginalChapterMapData)session.MapData).Levels : null);
                }

                foreach (LevelData item in (List<LevelData>)obj)
                {
                    foreach (EntityData entity in item.Entities)
                    {
                        if (entity.Name == "VivHelper/TeleportTarget" && entity.Attr("TargetID", (string)null) ==
                            entityData.Attr("TargetID", (string)null))
                        {
                            entityData.Values["RoomName"] = item.Name;
                            break;
                        }
                    }
                }
            }

            if (entityData.Has("RoomName"))
            {
                return (Entity)(object)new InstantTeleportTrigger1Way(level.EngineContext, entityData, offset,
                    new EntityID(levelData.Name, entityData.ID));
            }

            throw new Exception(string.Format(
                "No valid Teleport Target ID matching Instant Teleport Trigger @\nroom {0}, position {1}, ID: {2}",
                levelData.Name, entityData.Position, entityData.Attr("TargetID", "")));
        }

        public InstantTeleportTrigger1Way(ISourceEngineContext engine, OriginalLevelEntityData data, Vector2 offset,
            EntityID id)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            context = VivTeleportContext.For(engine);
            ID = id;
            roomName = data.Level.Name;
            startPos = data.Position + offset;
            targetID = TeleportTarget.ReadNonEmpty(data, "TargetID");
            if (targetID == null)
            {
                throw new InvalidPropertyException(
                    $"Teleport Trigger in room {roomName} at position {data.Position} has an empty targetID property.");
            }

            ExitDirection = ((!data.Has("onExit"))
                ? Clamp(data.Int("ExitDirection", 0), -2, 15)
                : (data.Bool("onExit", false) ? Clamp(data.Int("ExitDirection", 15), -2, 15) : 0));
            string text = data.Attr("RequiredFlags", "");
            if (!string.IsNullOrWhiteSpace(text))
            {
                flagsNeeded = text.Split(',');
            }

            text = data.Attr("FlagsOnTeleport", "");
            if (!string.IsNullOrWhiteSpace(text))
            {
                flagsSet = text.Split(',');
            }

            resetDashes = data.Bool("ResetDashes", true);
            specificRoomErrorCheckName = (specificRoom = TeleportTarget.ReadNonEmpty(data, "RoomName"));
            if (specificRoom == null)
            {
                specificRoomErrorCheckName = "No Value Set!";
            }

            transition = data.Enum<TransitionType>("TransitionType", TransitionType.None);
            delay = 0f;
            ignoreIfDummy = !data.Bool("IgnoreNoSpawnpoints", true);
            persistence = data.Enum<TriggerPersistence>("Persistence", TriggerPersistence.Default);
            bringHoldableThrough = data.Bool("BringHoldableThrough", false);
            switch (transition)
            {
                case TransitionType.ColorFlash:
                    transitionInfo = new List<object>
                        { ReadColor(data.Attr("FlashColor", "")) * Clamp(data.Float("FlashAlpha", 1f), 0f, 1f) };
                    break;
                case TransitionType.Lightning:
                {
                    int num = data.Int("LightningCount", 2);
                    string[] array = data.Attr("LightningOffsetRange", "-130,130").Split(',');
                    float[] item;
                    try
                    {
                        item = ((array.Length == 1)
                            ? new float[2]
                            {
                                int.Parse(array[0].Trim()),
                                int.Parse(array[0].Trim())
                            }
                            : ((array.Length != 2)
                                ? new float[2] { -130f, 130f }
                                : new float[2]
                                {
                                    int.Parse(array[0].Trim()),
                                    int.Parse(array[1].Trim())
                                }));
                    }
                    catch (Exception inner)
                    {
                        throw new InvalidPropertyException(
                            $"Lightning Offset Range property in Teleport Trigger in Room {roomName}, Position {startPos}, invalid: needs to be an integer or two integers separated by commas.",
                            inner);
                    }

                    float[] array2 = new float[2]
                    {
                        data.Float("LightningDelay", 0f),
                        0.25f
                    };
                    if (float.TryParse(data.Attr("LightningMaxDelay", ""), out array2[1]))
                    {
                        array2[1] -= array2[0];
                    }

                    transitionInfo = new List<object>
                    {
                        data.Bool("Flash", true),
                        data.Bool("Shake", true),
                        array2,
                        num,
                        item
                    };
                    break;
                }
                case TransitionType.Glitch:
                {
                    Func<float, float> easer;
                    GlitchType glitchType = new GlitchType
                    {
                        GlitchStrength = Clamp(data.Float("GlitchStrength", 0.5f), 0f, 1f),
                        GlitchEaser = (TryGetEaser(data.Attr("StartingGlitchEase", "Linear"), out easer)
                            ? easer
                            : static t => t),
                        GlitchLength = data.Float("StartingGlitchDuration", 0.3f)
                    };
                    Func<float, float> easer2;
                    GlitchType glitchType2 = new GlitchType
                    {
                        GlitchStrength = Clamp(data.Float("GlitchStrength", 0.5f), 0f, 1f),
                        GlitchEaser = (TryGetEaser(data.Attr("EndingGlitchEase", "Linear"), out easer2)
                            ? easer2
                            : static t => t),
                        GlitchLength = data.Float("EndingGlitchDuration", 0.3f)
                    };
                    delay = glitchType.GlitchLength;
                    transitionInfo = new List<object> { glitchType, glitchType2 };
                    break;
                }
                case TransitionType.Wipe:
                    transitionInfo = new List<object>
                    {
                        data.Bool("wipeOnLeave", true),
                        data.Bool("wipeOnEnter", true)
                    };
                    break;
            }

            if (data.Has("freezeOnTeleport"))
            {
                freezeGameOnTeleport = data.Bool("freezeOnTeleport", true);
            }

            if (data.Float("CustomDelay", -1f) >= 0f)
            {
                customDelay = Math.Max(0f, data.Float("CustomDelay", 0f) - delay);
            }

            endCutscene = data.Bool("EndCutsceneOnWarp", true);
            this.Depth = 5000;
            transitionListeners = data.Bool("ActAsTransition", true);
        }
    }
}
