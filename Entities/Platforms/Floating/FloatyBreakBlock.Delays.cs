using System.Collections;

namespace Celeste.Mods.VivHelper.Entities.Platforms.Floating
{
    public partial class FloatyBreakBlock
    {
        public IEnumerator DelayTimer()
        {
            while (!beginBreak && !HasPlayerRider()) yield return null;
            while (delay > 0f)
            {
                delay -= engine.DeltaTime;
                yield return null;
            }

            Break();
        }

        public IEnumerator DelaySinking()
        {
            while (!beginBreak && !HasPlayerRider()) yield return null;
            while (delay > 0f)
            {
                delay -= HasPlayerRider() ? 1.5f * engine.DeltaTime : engine.DeltaTime;
                yield return null;
            }

            Break();
        }

        public IEnumerator DelayPressure()
        {
            while (!beginBreak && !HasPlayerRider()) yield return null;
            while (delay > 0f)
            {
                if (delay > origDelay / 5f)
                {
                    if (HasPlayerRider()) delay -= engine.DeltaTime;
                    else
                    {
                        delay += .5f * engine.DeltaTime;
                        if (delay > origDelay) delay = origDelay;
                    }

                    yield return null;
                }
                else
                {
                    delay -= engine.DeltaTime;
                    yield return null;
                }
            }

            Break();
        }
    }
}
