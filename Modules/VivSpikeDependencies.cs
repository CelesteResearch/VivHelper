// SPDX-License-Identifier: AGPL-3.0-only

using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Modules.Updating;

namespace Celeste.Mods.VivHelper.Modules
{
    public static class VivSpikeDependencies
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, VivSpikeDependencyState> Owners = new();

        public static VivSpikeDependencyState For(ISourceEngineContext engine)
            => Owners.GetValue(engine, static _ => new VivSpikeDependencyState());

        public static void Initialize(ISourceEngineContext engine)
        {
            For(engine).MaxHelpingHandLoaded = ((IEverestLoaderProcessEnvironment)engine).Loader.TryGetDependency(
                new EverestModuleMetadata { Name = "MaxHelpingHand", VersionString = "1.16.5" }, out _);
        }
    }
}
