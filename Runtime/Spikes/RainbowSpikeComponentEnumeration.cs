// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Components;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.VivHelper.Runtime.Spikes
{
    internal static class RainbowSpikeComponentEnumeration
    {
        private static readonly MethodDetourInfo boxedEnumerator;
        private static readonly MethodDetourInfo valueEnumerator;

        static RainbowSpikeComponentEnumeration()
        {
            try
            {
                boxedEnumerator = DetourManager.GetDetourInfo(
                    typeof(SourceOwnedComponentList).GetMethod(nameof(SourceOwnedComponentList.GetEnumerator),
                        Type.EmptyTypes));
                valueEnumerator = DetourManager.GetDetourInfo(
                    typeof(SourceOwnedComponentList).GetMethod(nameof(SourceOwnedComponentList.GetValueEnumerator)));
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException
                                              or NotSupportedException)
            {
                boxedEnumerator = null;
                valueEnumerator = null;
            }
        }

        internal static bool CanUseValueEnumerator => boxedEnumerator != null && valueEnumerator != null &&
                                                      !boxedEnumerator.IsDetoured && !valueEnumerator.IsDetoured;
    }
}
