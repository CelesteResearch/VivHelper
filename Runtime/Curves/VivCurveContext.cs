using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Mods.VivHelper.Entities.Curves;

namespace Celeste.Mods.VivHelper.Runtime.Curves
{
    public sealed class VivCurveContext
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, VivCurveContext> owners = new();
        public Dictionary<string, CurveEntity> Curves { get; } = new();

        public static VivCurveContext For(ISourceEngineContext engine) =>
            owners.GetValue(engine, _ => new VivCurveContext());

        public static void Release(ISourceEngineContext engine)
        {
            if (owners.TryGetValue(engine, out var context)) context.Curves.Clear();
            owners.Remove(engine);
        }
    }
}
