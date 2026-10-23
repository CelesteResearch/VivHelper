using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Environment.Ridge;
using Celeste.Graphics.Content;
using Celeste.Mods.VivHelper.Spawn;

namespace Celeste.Mods.VivHelper.Entities
{
    [CustomEntity("VivHelper/CustomLightbeam")]
    public sealed class CustomLightBeam : OriginalLightBeamState
    {
        private readonly ISourceEngineContext engine;
        private readonly string disableFlag;
        private readonly string disableParticlesFlag;
        private readonly bool fadeWhenNear;
        private readonly bool noParticles;

        public CustomLightBeam(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(data, offset, engine,
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[data.Attr("Texture", "util/lightbeam")],
                SpawnPointRules.ParseColor(data.Attr("Color", "ccffff")) *
                ScalarMath.Clamp(data.Float("Alpha", 1f), 0f, 1f),
                SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, 1000f))
        {
            this.engine = engine;
            Depth = data.Int("Depth", -9998);
            disableFlag = data.Attr("DisableFlag", "");
            Flag = data.Attr("ChangeFlag", "");
            fadeWhenNear = data.Bool("FadeWhenNear", true);
            noParticles = data.Bool("NoParticles", false);
            disableParticlesFlag = data.Attr("DisableParticlesFlag", "");
        }

        protected override bool FadeWhenNear => fadeWhenNear;

        protected override bool ShouldEmitGlow(OriginalLevelSceneRoot level)
            => !noParticles && (disableParticlesFlag == ""
                                || (disableParticlesFlag != "*" && !level.Session.GetFlag(disableParticlesFlag)));

        protected override void EmitGlow(OriginalLevelSceneRoot level, Vector2 position, float rotation)
        {
            // Keep the amount/range overload: even Vector2.Zero consumes the source's two random samples.
            level.Particles.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.LightBeam.P_Glow,
                1, position, Vector2.Zero, PackedColor.Lerp(PackedColor.White, BeamColor, 0.5f), rotation);
        }

        public override void Render(ISourceRenderContext context)
        {
            if (BeamAlpha > 0f
                && (string.IsNullOrEmpty(disableFlag) || !((OriginalLevelSceneRoot)Scene).Session.GetFlag(disableFlag)))
                base.Render(context);
        }
    }
}
