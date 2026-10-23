// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Hazards.Spinners.Crystal;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Engine.Actions;
using Celeste.Mods.VivHelper.Entities.Debris;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Hazards.Spinners.Crystal;

namespace Celeste.Mods.VivHelper.Entities.Spinners
{
    [CustomEntity("VivHelper/CustomSpinner")]
    [SceneTracked]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("VivHelper.Entities.CustomSpinner")]
    public partial class CustomSpinner : OriginalCrystalStaticSpinner
    {
        protected SpinnerColorMode type;
        private PackedColor tint;
        private readonly PackedColor borderColor;
        private readonly PackedColor shatterColor;
        private readonly float scale;
        private readonly float imageScale;
        private readonly bool debrisToScale;
        private readonly bool customDebris;
        private readonly bool shatterDash;
        private readonly string shatterFlag;
        private readonly string flagToggle;
        private readonly bool flagToggleInvert;
        private readonly bool createConnectors;
        private readonly bool isSeeded;
        private readonly int seed;
        protected string directory;
        protected string fgdirectory;
        protected string bgdirectory;
        private string subdirectory;

        public static CustomSpinner Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public CustomSpinner(EntityData data, Vector2 positionOffset, ISourceEngineContext engine)
            : base(data.Position + positionOffset, data.Bool("AttachToSolid"), CrystalSpinnerColor.Blue, engine)
        {
            ID = data.ID;
            type = data.Enum("Type", SpinnerColorMode.White);
            if (data.Has("ref"))
            {
                string reference = data.Attr("ref", "VivHelper/customSpinner/white/fg_white00");
                int split = reference.LastIndexOf('/');
                directory = reference.Substring(0, split);
                subdirectory = reference.Substring(split + 3);
            }
            else
            {
                directory = data.Attr("Directory").TrimStart(' ').TrimEnd('/', ' ');
                if (directory == "") directory = "VivHelper/customSpinner/white";
                subdirectory = data.Attr("Subdirectory");
                if (data.Bool("FrostHelper") || data.Has("CurrentVersion") && subdirectory == "") subdirectory = "";
                else subdirectory = "_" + (subdirectory == "" ? "white" : subdirectory);
            }

            fgdirectory = directory + "/fg";
            bgdirectory = directory + "/bg";

            string color = data.Attr("Color");
            if (color == "") color = "ffffff";
            tint = SpawnPointRules.ParseColor(color);
            string shatter = data.Attr("ShatterColor");
            shatterColor = SpawnPointRules.ParseColor(shatter == "" ? color : shatter);
            string border = data.Attr("BorderColor");
            borderColor = SpawnPointRules.ParseColor(border == "" ? "000000" : border);
            scale = ReadScale(data, "Scale");
            imageScale = ReadScale(data, "ImageScale");
            debrisToScale = data.Bool("DebrisToScale", true);
            customDebris = data.Bool("CustomDebris");
            shatterDash = data.Bool("shatterOnDash");
            shatterFlag = data.Attr("ShatterFlag");
            string hitbox = data.Attr("HitboxType", data.Bool("removeRectHitbox") ? "C:6" : "C:6|R:16,4;-8,*1@-4");
            if (hitbox == "") hitbox = "C:6|R:16,4;-8,*1@-4";
            Collider = SpinnerHitboxParser.Parse(hitbox, scale, VectorArithmetic);
            if (Collider == null) Collidable = false;
            Depth = data.Int("Depth", -8500);
            seed = data.Int("seed", -1);
            isSeeded = data.Bool("isSeeded");
            flagToggle = data.Attr("flagToggle");
            if (flagToggle.StartsWith("!"))
            {
                flagToggleInvert = true;
                flagToggle = flagToggle.Substring(1);
            }

            createConnectors = !data.Bool("ignoreConnection");
        }

        private static float ReadScale(EntityData data, string key)
        {
            float value = data.Float(key, 1f);
            return value == -1f ? 1f : Math.Max(1f / 3f, value);
        }

        protected override bool EnableCoreModeListener => false;
        protected override bool IsRainbow => type is SpinnerColorMode.RainbowClassic or SpinnerColorMode.CustomRainbow;

        protected override bool ShouldShow => string.IsNullOrWhiteSpace(flagToggle) ||
                                              Level.Session.GetFlag(flagToggle) != flagToggleInvert;

        protected override bool ShouldUpdateHue => type == SpinnerColorMode.RainbowClassic ||
                                                   type == SpinnerColorMode.CustomRainbow &&
                                                   ((ISourceScene)Scene).SourceScene.Clock.OnInterval(.08f, offset,
                                                       engine);

        public override PackedColor GetHue(Vector2 position)
        {
            return Modules.VivHelperSpinnerCapabilities.GetHue(engine, Scene, position);
        }

        protected override bool InView()
        {
            var camera = Level.Camera;
            return (Collider?.AbsoluteRight ?? Position.X) > camera.X - 16f &&
                   (Collider?.AbsoluteBottom ?? Position.Y) > camera.Y - 16f &&
                   (Collider?.AbsoluteLeft ?? Position.X) < camera.X + 320f * camera.Zoom + 16f &&
                   (Collider?.AbsoluteTop ?? Position.Y) < camera.Y + 180f * camera.Zoom + 16f;
        }

        protected override void AfterSpinnerUpdate()
        {
            if (Visible && shatterFlag != "" && Level.Session.GetFlag(shatterFlag)) Destroy();
        }

        protected override void OnPlayer(RoomPlayerSession player)
        {
            if (shatterDash && Modules.VivHelperSpinnerCapabilities.IsDashState(engine, player.Machine.State))
            {
                Destroy();
                OriginalCelesteFreeze.Freeze(engine, .02f);
            }
            else base.OnPlayer(player);
        }

        public override void Destroy(bool boss = false)
        {
            if (InView())
            {
                ((Celeste.Core.Audio.ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                    "event:/game/06_reflection/fall_spike_smash", Position);
                CustomCrystalDebris.Burst(engine, Position, shatterColor, boss, (int)(8f * scale),
                    customDebris ? directory + "/debris" : "particles/shard", debrisToScale ? scale : 1f);
            }

            ClearSprites();
            RemoveSelf();
        }
    }
}
