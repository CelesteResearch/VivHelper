using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Mods.VivHelper.Entities.Curves;
using Celeste.Mods.VivHelper.Runtime.Curves;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.VivHelper.Entities.Platforms.ZipMovers
{
    [CustomEntity("VivHelper/CurvedZipMover2=Curve", "VivHelper/CustomZipMover=Custom")]
    [RuntimeTypeAlias("VivHelper.Entities.CurvedStuff.CustomCurvedZipMover")]
    public partial class CustomCurvedZipMover : SourceSolidEntityState
    {
        internal readonly ISourceEngineContext Engine;
        private readonly VivCurveContext curves;
        private readonly ISourceExtremaArithmetic extrema;
        private readonly Func<float, float> easing;
        private readonly bool moveType, uniform, reverse;
        internal readonly bool drawBlackBorder;
        internal readonly string spritePath;
        internal readonly PackedColor baseColor, ropeColor, ropeLightColor;
        private readonly float speedModF, speedModB;
        private readonly float PostLaunchPause;
        private readonly string audio;
        public string identifier;
        internal Vector2 start, target;
        internal float percent;
        private readonly AtlasEntry[,] edges = new AtlasEntry[3, 3];
        private readonly AtlasEntry temporary = new();
        private readonly List<AtlasEntry> innerCogs;
        private readonly Sprite streetlight;
        private readonly BloomPointState bloom;
        private readonly SoundSourceComponentState sound;
        private CurveEntity curve;
        private CurvedPath curvedPath;
        private CustomCurvedZipMoverPathRenderer pathRenderer;

        public static CustomCurvedZipMover Curve(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => Create(level, data, offset, true);

        public static CustomCurvedZipMover Custom(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => Create(level, data, offset, false);

        private static CustomCurvedZipMover Create(OriginalLevelSceneRoot level, EntityData data, Vector2 offset,
            bool move)
            => new(data, offset, move,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, false));

        public CustomCurvedZipMover(EntityData data, Vector2 offset, bool move, ISourceSolidWorld world)
            : base(data.Position + offset, data.Width, data.Height, false, world)
        {
            Engine = world.EngineContext;
            curves = VivCurveContext.For(Engine);
            extrema = ((ISourceExtremaEnvironment)Engine).ExtremaArithmetic;
            moveType = move;
            identifier = data.Attr("CurveIdentifier", "");
            if (identifier == "") target = data.Nodes[0] + offset;
            spritePath = data.Attr("CustomSpritePath", "").TrimEnd('/');
            ropeColor = SpawnPointRules.ParseColor(data.Attr("RopeColor", "663931"));
            ropeLightColor = SpawnPointRules.ParseColor(data.Attr("RopeNotchColor", "9b6157"));
            var arithmetic = ((ISourceEasingEnvironment)Engine).EasingArithmetic;
            if (!((ISourceNamedEasingArithmetic)arithmetic).NamedEasers.TryGetValue(data.Attr("EaseType", "SineIn"),
                    out easing)) easing = arithmetic.SineIn;
            baseColor = SpawnPointRules.ParseColor(data.Attr("BaseColor", "FFFFFF"));
            audio = data.Attr("AudioOnLaunch", "");
            string graphicsPath = spritePath == "" ? "objects/zipmover" : spritePath;
            drawBlackBorder = data.Bool("DrawBlackBorder", true);
            speedModF = data.Float("SpeedModF", 1f);
            speedModB = data.Float("SpeedModB", 1f);
            PostLaunchPause = data.Float("PostLaunchPause", .5f);
            Depth = -9999;
            start = Position;
            if (moveType) uniform = data.Bool("Uniform", false);
            else target = data.NodesOffset(offset)[0];
            reverse = data.Bool("Reverse", false);
            Add(new SimulationCoroutine(Engine, data.Bool("Slingshot", false) ? SequenceLaunch() : SequenceDefault()));
            Add(new LightOccludeState(data.Float("LightOcclusion", 1f)));
            var atlas = ((ISourceGameGraphicsEnvironment)Engine).GameGraphics.GameAtlas;
            innerCogs = atlas.GetAtlasSubtextures(graphicsPath + "/innercog");
            Add(streetlight = new Sprite(Engine, atlas, graphicsPath + "/light"));
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames", false, false);
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position = new(Width / 2f - streetlight.Width / 2f, 0f);
            streetlight.Color = baseColor;
            Add(bloom = new BloomPointState(data.Bool("RemoveBloomPoint", false) ? 0f : 1f, 6f));
            bloom.Position = new(Width / 2f, 4f);
            var block = atlas[graphicsPath + "/block"];
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                edges[i, j] = block.GetSubtexture(i * 8, j * 8, 8, 8);
            SurfaceSoundIndex = 7;
            Add(sound = new SoundSourceComponentState(Engine));
            sound.Position = new Vector2(Width, Height) / 2f;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (!moveType)
                ((ISourceScene)scene).SourceScene.AddEntity(pathRenderer = new CustomCurvedZipMoverPathRenderer(this));
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (moveType && curves.Curves.Count > 0)
            {
                if (!curves.Curves.TryGetValue(identifier, out curve))
                    throw new Exception("No curve with the Identifier " + identifier + " was found.");
                Position = curve.bezier.GetPoint(reverse ? 1f : 0f) - new Vector2(Width / 2f, Height / 2f);
                var lines = new[]
                {
                    new VivPathLine
                    {
                        distance = 5f, startT = curve.bezier.tStart, endT = curve.bezier.tEnd, color = ropeColor,
                        offset = Vector2.Zero, thickness = 1.7f, type = "line", addEnds = false
                    },
                    new VivPathLine
                    {
                        distance = 6f, startT = curve.bezier.tStart, endT = curve.bezier.tEnd, color = ropeLightColor,
                        offset = Vector2.Zero, thickness = 1.7f, type = "dashed", addEnds = false
                    }
                };
                ((ISourceScene)scene).SourceScene.AddEntity(curvedPath = new CurvedPath(Engine, curve.bezier, lines));
                start = curve.bezier.GetPoint(0.0);
            }
        }

        public override void Removed(object scene)
        {
            ((ISourceScene)scene).SourceScene.RemoveEntity(moveType ? curvedPath : pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        public override void Update()
        {
            base.Update();
            bloom.Y = streetlight.CurrentAnimationFrame * 3;
        }

        public override void Render(ISourceRenderContext context)
        {
            var saved = Position;
            Position += Shake;
            try
            {
                ZipMoverBodyDrawing.Draw(context, this, percent, innerCogs, edges, temporary, 1f, baseColor);
                base.Render(context);
            }
            finally
            {
                Position = saved;
            }
        }

        private Vector2 CurvePosition(float at)
            => (uniform
                ? curve.bezier.GetPointFromLength(at * curve.bezier.GetBezierLength(25))
                : curve.bezier.GetPoint(at * curve.bezier.curves.Length)) - new Vector2(Width / 2f, Height / 2f);

        private IEnumerator SequenceDefault()
        {
            var initial = Position;
            var input = ((IOriginalGameInputProcessEnvironment)Engine).InputSource;
            var scalar = ((ISourceScalarEnvironment)Engine).ScalarArithmetic;
            while (true)
            {
                if (!HasPlayerRider())
                {
                    yield return null;
                    continue;
                }

                sound.Play(audio == "" ? "event:/game/01_forsaken_city/zip_mover" : audio, null, 0f);
                input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(.1f);
                yield return .1f;
                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;
                float at = 0;
                while (at < 1f)
                {
                    yield return null;
                    at = ScalarMath.Approach(at, 1f, 2f * Engine.DeltaTime * speedModF, extrema);
                    percent = reverse ? 1f - easing(at) : easing(at);
                    Vector2 next = moveType
                        ? CurvePosition(percent)
                        : Vector2Interpolation.Lerp(initial, target, percent, scalar);
                    ZipMoverDrawing.ScrapeParticles(this, Engine, next);
                    if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(.1f, Engine) && !moveType)
                        pathRenderer.CreateSparks();
                    MoveTo(next);
                }

                StartShaking(.2f);
                input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                ((OriginalLevelSceneRoot)Scene).Shake(.3f);
                StopPlayerRunIntoAnimation = true;
                yield return PostLaunchPause;
                StopPlayerRunIntoAnimation = false;
                streetlight.SetAnimationFrame(2);
                at = 0;
                while (at < 1f)
                {
                    yield return null;
                    at = ScalarMath.Approach(at, 1f, speedModB * Engine.DeltaTime / 2f, extrema);
                    percent = 1f - easing(at);
                    MoveTo(moveType
                        ? CurvePosition(percent)
                        : Vector2Interpolation.Lerp(initial, target, percent, scalar));
                }

                StopPlayerRunIntoAnimation = true;
                StartShaking(.2f);
                streetlight.SetAnimationFrame(1);
                yield return .5f;
            }
        }
    }
}
