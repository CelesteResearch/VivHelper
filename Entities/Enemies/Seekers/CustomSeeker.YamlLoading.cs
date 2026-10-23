// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.IO;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Registry;
using Celeste.Everest.Content.Serialization;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Framework.Diagnostics.Commands;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Mods.VivHelper.Configuration.Enemies.Seekers;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    public partial class CustomSeeker
    {
        public static CustomSeeker Yaml(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
        {
            var engine = (IOriginalSeekerProcess)level.EngineContext;
            return new CustomSeeker(LoadYaml(data.Attr("YamlPath", ""), engine).SeekerDataFromYaml(data.Position),
                offset, engine);
        }

        public static CustomSeekerYaml LoadYaml(string path, ISourceEngineContext engine)
        {
            string key = "Seekers/" + path;
            var logger = ((IEverestLoggerProcessEnvironment)engine).Logger;
            if (!((IEverestContentRegistryEnvironment)engine).SourceContentRegistry.TryGet(key, out var asset, false))
            {
                logger.Log("VivHelper",
                    "Failed loading Seeker data file \"" + path + "\": The file could not be found.");
                ((ISourceCommandsProcessEnvironment)engine).Commands.Log(
                    "VivHelper: The game tried to load the file at \"" + path +
                    "\" but couldn't find it. You may have input the name of the file wrong.");
                return null;
            }

            try
            {
                using var reader = new StreamReader(asset.Stream);
                return ((IEverestYamlEnvironment)engine).Yaml.Deserializer.Deserialize<CustomSeekerYaml>(reader);
            }
            catch (Exception e)
            {
                logger.Log("VivHelper", "Failed loading Seeker file \"" + path + "\": " + e.Message);
                return null;
            }
        }
    }
}
