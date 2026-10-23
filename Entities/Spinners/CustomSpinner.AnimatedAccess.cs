// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Core.Display;

namespace Celeste.Mods.VivHelper.Entities.Spinners
{
    public partial class CustomSpinner
    {
        protected string AnimationDirectory => directory;
        protected PackedColor AnimationTint => tint;
        protected PackedColor AnimationBorderColor => borderColor;
        protected PackedColor AnimationShatterColor => shatterColor;
        protected float AnimationScale => scale;
        protected float AnimationImageScale => imageScale;
        protected bool AnimationDebrisToScale => debrisToScale;
        protected bool AnimationCustomDebris => customDebris;
        protected bool AnimationIsSeeded => isSeeded;
        protected int AnimationSeed => seed;

        protected static int ChooseAnimationIndex(int x, int y, int seed, int count)
            => ChooseIndex(x, y, seed, count);
    }
}
