// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Simulation;

namespace Celeste.Mods.VivHelper.Entities.Spikes
{
    public partial class RainbowTriggerSpikes
    {
        protected struct SpikeInfo
        {
            public RainbowTriggerSpikes Parent;
            public int Index;
            public int TextureIndex;
            public Vector2 Position;
            public bool Triggered;
            public float RetractTimer;
            public float Lerp;
            public PackedColor color;

            public float DelayTimer
            {
                get => Parent.Grouped ? Parent.delayTimer : RetractTimer;
                set
                {
                    if (Parent.Grouped) Parent.delayTimer = value;
                    else RetractTimer = value;
                }
            }

            public void SetColor()
                => color = Parent.oneColor != PackedColor.Transparent
                    ? Parent.oneColor
                    : Parent.GetRainbowHue(Parent.Position + Position);

            public bool PlayerCheck() => Parent.PlayerCheck(Index);

            public void Update()
            {
                if (Parent.Grouped ? Parent.Triggered : Triggered)
                {
                    if (DelayTimer > 0f)
                    {
                        DelayTimer -= Parent.Engine.DeltaTime;
                        if (DelayTimer <= Parent.Engine.DeltaTime)
                        {
                            if (Parent.Grouped ? Parent.PlayerCheck() : PlayerCheck()) DelayTimer = 0.05f;
                            else
                                Parent.PlaySound("event:/game/03_resort/fluff_tendril_emerge",
                                    Parent.Position + Position);
                        }
                    }
                    else Lerp = ScalarMath.Approach(Lerp, 1f, 8f * Parent.Engine.DeltaTime);
                }
                else
                {
                    Lerp = ScalarMath.Approach(Lerp, 0f, 4f * Parent.Engine.DeltaTime);
                    if (Lerp <= 0f) Triggered = false;
                }

                SetColor();
            }

            public bool OnPlayer(RoomPlayerSession player, Vector2 outwards)
            {
                if (Parent.Grouped ? !Parent.Triggered : !Triggered)
                {
                    Parent.PlaySound("event:/game/03_resort/fluff_tendril_touch", Parent.Position + Position);
                    if (Parent.Grouped) Parent.Triggered = true;
                    else Triggered = true;
                    DelayTimer = 0.05f;
                    return false;
                }

                if (Lerp >= 1f)
                {
                    player.Die(outwards, false, true);
                    return true;
                }

                return false;
            }
        }
    }
}
