using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using LevelData = Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData;
using Color = Celeste.Core.Display.PackedColor;

namespace Celeste.Mods.VivHelper.Triggers
{
    [Celeste.Everest.Tracking.Attributes.EverestTrackedAs(typeof(SourceTriggerEntityState))]
    [RuntimeTypeAlias("VivHelper.Triggers.InstantTeleportTrigger")]
    [CustomEntity("VivHelper/BasicInstantTeleportTrigger", "VivHelper/MainInstantTeleportTrigger",
        "VivHelper/CustomInstantTeleportTrigger")]
    public partial class InstantTeleportTrigger : SourceTriggerEntityState
    {
        public enum TransitionType
        {
            None,
            Lightning,
            GlitchEffect,
            ColorFlash
        }

        public Level level;

        public string[] flags;

        public string currentRoom;

        public string newRoom;

        public Vector2 currentPos;

        public Vector2 newPos;

        public float[] vel;

        public float[] cameraOffset;

        public float rot;

        public float vel2;

        public TransitionType transition;

        public bool velMod;

        public bool rotMod;

        public bool dreaming;

        public bool addTriggerOffset;

        private bool triggered = false;

        private float timeSlowDown;

        private Color flashColor;

        private float delay;

        private int legacyCamera;


        private bool legacySpawnpoint;

        private bool resetDashes;

        private bool forceNormalState;

        private bool onExit;

        private bool DifferentSide;

        private int direction = -1;

        private bool wackyFollowerOverride;

        private bool TransitionListeners;

        private bool bringHoldableThrough;

        private bool doFlash;

        private float flash;

        public InstantTeleportTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            newRoom = data.Attr("WarpRoom", currentRoom).Trim();
            newPos = new Vector2(data.Float("newPosX", -1f), data.Float("newPosY", -1f));
            cameraOffset = new float[2]
            {
                data.Float("CameraOffsetX", 0f),
                data.Float("CameraOffsetY", 0f)
            };
            velMod = data.Bool("VelocityModifier", false);
            vel = new float[2]
            {
                data.Float("ExitVelocityX", (float)(velMod ? 1 : 0)),
                data.Float("ExitVelocityY", (float)(velMod ? 1 : 0))
            };
            vel2 = data.Float("ExitVelocityS", -1f);
            rotMod = data.Bool("RotationType", false);
            rot = 0f - (float)Math.PI * data.Float("RotationActor", 0f) / 180f;
            dreaming = data.Bool("Dreaming", true);
            transition = data.Enum<TransitionType>("TransitionType", TransitionType.None);
            flashColor = ParseColor(data.Attr("Color", "White"));
            flags = data.Attr("ZFlagsData", "").Split(',');
            addTriggerOffset = data.Bool("AddTriggerOffset", true);
            timeSlowDown = data.Float("TimeSlowDown", 0f);
            delay = data.Float("TimeBeforeTeleport", 0f);
            legacyCamera = data.Int("CameraType", -1);
            legacySpawnpoint = data.Bool("LegacySpawnpoint", true);
            resetDashes = data.Bool("ResetDashes", true);
            forceNormalState = data.Bool("ForceNormalState", false);
            if (legacyCamera == -1)
            {
                legacyCamera = 1;
            }

            onExit = data.Bool("OnExit", false);
            DifferentSide = onExit && data.Bool("DifferentSide", false);
            wackyFollowerOverride = data.Bool("followerForcePosition", false);
            TransitionListeners = data.Bool("ActAsTransition", false);
            bringHoldableThrough = data.Bool("bringHoldablesThrough", false);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = (Level)Scene;
            currentRoom = level.Session.Level;
            List<LevelData> levels = ((Celeste.Game.Content.Maps.OriginalChapterMapData)level.Session.MapData).Levels;
            bool flag = false;
            foreach (LevelData item in levels)
            {
                flag |= item.Name == newRoom;
            }

            if (!flag)
            {
                newRoom = currentRoom;
                newPos = new Vector2(-1f, -1f);
                vel = new float[2]
                {
                    velMod ? 1 : 0,
                    velMod ? 1 : 0
                };
                vel2 = -1f;
                dreaming = true;
                cameraOffset = new float[2];
                transition = TransitionType.None;
                flashColor = Color.White;
                rotMod = false;
                rot = 0f;
            }
        }

        public override void Update()
        {
            base.Update();
            if (doFlash)
            {
                flash = Approach(flash, 1f, engine.DeltaTime * 10f);
                if (flash >= 1f)
                {
                    doFlash = false;
                }
            }
            else if (flash > 0f)
            {
                flash = Approach(flash, 0f, engine.DeltaTime * 3f);
            }
        }

        private void TeleportMaster(RoomPlayerSession player)
        {
            if (delay > 0f)
            {
                OwnedComponents.Add(new SimulationCoroutine(engine, OnEntrySequence(player)));
            }
            else if (!triggered && FlagsMatch() && player != null && !player.Dead)
            {
                Teleport(player);
            }
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            if (onExit)
            {
                int[] array = new int[4];
                array[0] = ((player.Data.Motion.Normal.Speed.X > 0f) ? 1 : (-1));
                array[1] = -array[0];
                array[2] = ((player.Data.Motion.Normal.Speed.Y > 0f) ? 1 : (-1));
                array[3] = -array[2];
                Vector2 val = player.TopRight - player.Data.Motion.Normal.Speed;
                Vector2 val2 = player.BottomLeft - player.Data.Motion.Normal.Speed;
                array[0] *= ((this.Left > val.X && this.Left <= player.TopRight.X) ? 1 : 0);
                array[1] *= ((this.Right < val2.X && this.Right >= player.BottomLeft.X) ? 1 : 0);
                array[2] *= ((this.Top > val2.Y && this.Top <= player.BottomLeft.Y) ? 1 : 0);
                array[3] *= ((this.Bottom < val.Y && this.Bottom >= player.TopRight.Y) ? 1 : 0);
                int value = array.Max();
                direction = Array.IndexOf(array, value);
            }
            else
            {
                TeleportMaster(player);
            }
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            if (onExit)
            {
                bool flag = true;
                if (DifferentSide)
                {
                    int[] array = new int[4];
                    array[0] = ((!(player.Data.Motion.Normal.Speed.X > 0f)) ? 1 : (-1));
                    array[1] = -array[0];
                    array[2] = ((!(player.Data.Motion.Normal.Speed.Y > 0f)) ? 1 : (-1));
                    array[3] = -array[2];
                    Vector2 val = player.TopRight - player.Data.Motion.Normal.Speed;
                    Vector2 val2 = player.BottomLeft - player.Data.Motion.Normal.Speed;
                    array[0] *= ((this.Left < val.X && this.Left >= player.TopRight.X) ? 1 : 0);
                    array[1] *= ((this.Right > val2.X && this.Right <= player.BottomLeft.X) ? 1 : 0);
                    array[2] *= ((this.Top < val2.Y && this.Top >= player.BottomLeft.Y) ? 1 : 0);
                    array[3] *= ((this.Bottom > val.Y && this.Bottom <= player.TopRight.Y) ? 1 : 0);
                    int value = array.Max();
                    int num = Array.IndexOf(array, value);
                    flag = num != direction;
                }

                if (flag)
                {
                    TeleportMaster(player);
                }

                direction = -1;
            }
        }

        public IEnumerator OnEntrySequence(RoomPlayerSession player)
        {
            float timer = delay;
            while (timer > 0f)
            {
                if (player.Dead || player == null)
                {
                    yield break;
                }

                timer -= engine.DeltaTime;
                yield return null;
            }

            if (!triggered && FlagsMatch() && player != null && !player.Dead)
            {
                Teleport(player);
            }
        }
    }
}
