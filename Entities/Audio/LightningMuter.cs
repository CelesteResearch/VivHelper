// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Reflection;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Components.Audio;
using Celeste.Mods.VivHelper.Runtime.Audio.Muting;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Hazards.Lightning;

namespace Celeste.Mods.VivHelper.Entities.Audio;

[CustomEntity("VivHelper/LightningMuter")]
[SceneTracked(false)]
[RuntimeTypeAlias("VivHelper.Entities.LightningMuter")]
public class LightningMuter : SceneEntityState
{
    private readonly ISourceEngineContext engine;
    public string flag;
    public string ParentClassRef;
    public Type type;

    public LightningMuter(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
        ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
    {
        this.engine = engine;
        flag = NoEmptyString(data, "flag");
        ParentClassRef = NoEmptyString(data, "AudioPlayingClass");
        Depth = 1000000000;
    }

    private static string NoEmptyString(EntityData data, string key)
    {
        string text = data.Attr(key, null);
        return string.IsNullOrEmpty(text) ? null : text;
    }

    public override void Awake(object scene)
    {
        base.Awake(scene);
        var tracker = ((ISourceScene)scene).SourceScene.Entities;
        if (ParentClassRef == null)
        {
            if (tracker.TryGetTrackedEntities(typeof(LightningRendererState), out var renderers) &&
                renderers.Count != 0)
                ((LightningRendererState)renderers[0]).PreUpdate += RendererPreUpdate;
        }
        else
        {
            type = ((ISourceEverestReflectionEnvironment)engine).Reflection.GetFakeEntryAssembly()
                .GetType(ParentClassRef, false);
            if (type != null && tracker.TryGetTrackedEntities(type, out var entities) && entities.Count != 0)
                ((SceneEntityState)entities[0]).Add(new EntityMuterComponent(EntityMuterContext.For(engine), flag));
        }
    }

    private void RendererPreUpdate(SceneEntityState entity)
    {
        if (entity is not LightningRendererState renderer) return;
        if (!string.IsNullOrWhiteSpace(flag) &&
            (renderer.Scene as OriginalLevelSceneRoot)?.Session?.GetFlag(flag) != true)
            renderer.StartAmbience();
        else renderer.StopAmbience();
    }

    public override void Update() => RemoveSelf();
}
