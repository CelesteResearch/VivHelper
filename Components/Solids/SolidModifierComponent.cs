// SPDX-License-Identifier: AGPL-3.0-only

using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.VivHelper.Components.Solids
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.SolidModifierComponent")]
    public class SolidModifierComponent : SourceComponentState
    {
        public int CornerBoostBlock;
        public bool CornerBoostRetention;
        public bool bufferClimbJump;
        public bool triggerClimbOnTouch;
        public bool OnTouchFromBelow;
        public bool Legacy;
        public bool HasBeenBufferJumpedOn;

        public SolidModifierComponent(int cornerBoostBlock, bool bufferClimbJump, bool triggerClimbOnTouch,
            bool onTouchFromBelow = false) : base(true, false)
        {
            CornerBoostBlock = -cornerBoostBlock;
            this.bufferClimbJump = bufferClimbJump;
            this.triggerClimbOnTouch = triggerClimbOnTouch;
            OnTouchFromBelow = onTouchFromBelow;
        }

        public SolidModifierComponent(SolidModifierComponent c) : base(true, false)
        {
            CornerBoostBlock = c.CornerBoostBlock;
            bufferClimbJump = c.bufferClimbJump;
            triggerClimbOnTouch = c.triggerClimbOnTouch;
            OnTouchFromBelow = c.OnTouchFromBelow;
        }

        public override string ToString()
            =>
                $"SMC: {CornerBoostBlock}\nBuffer: {bufferClimbJump}\tTouch: {triggerClimbOnTouch}\nBelow: {OnTouchFromBelow}\n";
    }
}
