// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Processes;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Scene.Entities;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.VivHelper.Entities.Solids.FallingBlocks
{
    public partial class CornerBoostFallingBlock
    {
        private IOriginalGameplayProcess Process => (IOriginalGameplayProcess)engine;
        public ParticleTypePresentation P_FallDustA => Process.ParticleTypes.FallingBlock.P_FallDustA;
        public ParticleTypePresentation P_FallDustB => Process.ParticleTypes.FallingBlock.P_FallDustB;
        public ParticleTypePresentation P_LandDust => Process.ParticleTypes.FallingBlock.P_LandDust;

        private bool CheckPoint(Vector2 at) => SourceSceneCollision.FirstPoint<ISourceEntityCollider>(
            ((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid", at) != null;

        private void FallParticles()
        {
            for (var i = 2; i < Width; i += 4)
            {
                if (CheckPoint(TopLeft + new Vector2(i, -2f)))
                    Level.Particles.Emit(P_FallDustA, 2, new Vector2(X + i, Y), Vector2.One * 4f, (float)Math.PI / 2f);
                Level.Particles.Emit(P_FallDustB, 2, new Vector2(X + i, Y), Vector2.One * 4f);
            }
        }

        private void LandParticles()
        {
            for (var i = 2; i <= Width; i += 4)
                if (CheckPoint(BottomLeft + new Vector2(i, 3f)))
                {
                    Level.ParticlesFG.Emit(P_FallDustA, 1, new Vector2(X + i, Bottom), Vector2.One * 4f,
                        -(float)Math.PI / 2f);
                    Level.ParticlesFG.Emit(P_LandDust, 1, new Vector2(X + i, Bottom), Vector2.One * 4f,
                        i < Width / 2f ? (float)Math.PI : 0f);
                }
        }

        private void ShakeSfx() => Process.AudioSession.Play(TileType switch
        {
            '3' => "event:/game/01_forsaken_city/fallblock_ice_shake",
            '9' => "event:/game/03_resort/fallblock_wood_shake",
            'g' => "event:/game/06_reflection/fallblock_boss_shake",
            _ => "event:/game/general/fallblock_shake"
        }, Center, Process.SaveContext);

        private void ImpactSfx() => Process.AudioSession.Play(TileType switch
        {
            '3' => "event:/game/01_forsaken_city/fallblock_ice_impact",
            '9' => "event:/game/03_resort/fallblock_wood_impact",
            'g' => "event:/game/06_reflection/fallblock_boss_impact",
            _ => "event:/game/general/fallblock_impact"
        }, BottomCenter, Process.SaveContext);
    }
}
